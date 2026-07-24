mod automation;
mod commands;
mod config;
mod error;
mod runtime;
mod shortcuts;

use commands::{apply_system_settings, dispatch_action, get_app_info, save_config};
use config::{config_path, Config};
use error::{AppError, AppResult};
use runtime::{handle_shortcut, RuntimeState};
use tauri::{
    menu::{Menu, MenuItem},
    tray::{MouseButton, MouseButtonState, TrayIconBuilder, TrayIconEvent},
    AppHandle, Manager, WindowEvent,
};
use tauri_plugin_autostart::MacosLauncher;

const TRAY_ID: &str = "main-tray";

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    let single_instance = tauri_plugin_single_instance::Builder::new()
        .callback(|app, _arguments, _working_directory| show_main_window(app))
        .build();
    let global_shortcuts = tauri_plugin_global_shortcut::Builder::new()
        .with_handler(handle_shortcut)
        .build();

    tauri::Builder::default()
        .plugin(single_instance)
        .plugin(tauri_plugin_log::Builder::new().build())
        .plugin(tauri_plugin_notification::init())
        .plugin(tauri_plugin_autostart::init(
            MacosLauncher::LaunchAgent,
            None,
        ))
        .plugin(global_shortcuts)
        .setup(|app| {
            let directory = app.path().app_config_dir()?;
            let path = config_path(directory);
            let config = Config::load(&path).map_err(|error| error.to_string())?;
            let runtime = RuntimeState::new(path, config.clone());
            app.manage(runtime.clone());
            create_tray(app.handle())?;
            runtime
                .apply_shortcuts(app.handle(), &config)
                .map_err(|error| error.to_string())?;
            if let Err(error) = apply_system_settings(app.handle(), &config) {
                log::warn!("System settings could not be fully applied: {error}");
            }
            Ok(())
        })
        .on_window_event(|window, event| {
            if let WindowEvent::CloseRequested { api, .. } = event {
                let runtime = window.app_handle().state::<std::sync::Arc<RuntimeState>>();
                if !runtime.config().settings.show_tray_icon {
                    window.app_handle().exit(0);
                    return;
                }
                api.prevent_close();
                if let Err(error) = window.hide() {
                    log::error!("Failed to hide the main window: {error}");
                }
            }
        })
        .invoke_handler(tauri::generate_handler![
            get_app_info,
            save_config,
            dispatch_action
        ])
        .run(tauri::generate_context!())
        .expect("AI Commander failed to start");
}

fn create_tray(app: &AppHandle) -> tauri::Result<()> {
    let show = MenuItem::with_id(app, "show", "Open AI Commander", true, None::<&str>)?;
    let quit = MenuItem::with_id(app, "quit", "Quit", true, None::<&str>)?;
    let menu = Menu::with_items(app, &[&show, &quit])?;
    let mut builder = TrayIconBuilder::with_id(TRAY_ID)
        .tooltip("AI Commander")
        .menu(&menu)
        .show_menu_on_left_click(false)
        .on_menu_event(|app, event| match event.id().as_ref() {
            "show" => show_main_window(app),
            "quit" => app.exit(0),
            _ => {}
        })
        .on_tray_icon_event(|tray, event| {
            if matches!(
                event,
                TrayIconEvent::Click {
                    button: MouseButton::Left,
                    button_state: MouseButtonState::Up,
                    ..
                }
            ) {
                show_main_window(tray.app_handle());
            }
        });
    if let Some(icon) = app.default_window_icon() {
        builder = builder.icon(icon.clone());
    }
    builder.build(app)?;
    Ok(())
}

pub fn set_tray_visibility(app: &AppHandle, visible: bool) -> AppResult<()> {
    let tray = app
        .tray_by_id(TRAY_ID)
        .ok_or_else(|| AppError::Message("Tray icon is not initialized.".into()))?;
    tray.set_visible(visible)
        .map_err(|error| AppError::Message(format!("Tray visibility update failed: {error}")))
}

fn show_main_window(app: &AppHandle) {
    let Some(window) = app.get_webview_window("main") else {
        return;
    };
    if let Err(error) = window.show() {
        log::error!("Failed to show the main window: {error}");
    }
    if let Err(error) = window.unminimize() {
        log::error!("Failed to restore the main window: {error}");
    }
    if let Err(error) = window.set_focus() {
        log::error!("Failed to focus the main window: {error}");
    }
}
