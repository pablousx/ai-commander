use std::sync::Arc;

use serde::Serialize;
use tauri::{AppHandle, State};
use tauri_plugin_autostart::ManagerExt;

use crate::{
    config::Config,
    error::{AppError, AppResult},
    runtime::RuntimeState,
    set_tray_visibility,
};

#[derive(Serialize)]
pub struct AppInfo {
    config: Config,
    config_path: String,
    platform: &'static str,
    version: String,
}

#[tauri::command]
pub fn get_app_info(app: AppHandle, state: State<'_, Arc<RuntimeState>>) -> AppInfo {
    app_info(&app, &state)
}

#[tauri::command]
pub fn save_config(
    app: AppHandle,
    state: State<'_, Arc<RuntimeState>>,
    mut config: Config,
) -> AppResult<AppInfo> {
    config.version = 2;
    config.validate()?;
    let previous = state.config();
    state.apply_shortcuts(&app, &config)?;

    if let Err(error) = apply_system_settings(&app, &config) {
        let _ = state.apply_shortcuts(&app, &previous);
        let _ = apply_system_settings(&app, &previous);
        return Err(error);
    }

    if let Err(error) = config.save(&state.config_path) {
        let _ = state.apply_shortcuts(&app, &previous);
        let _ = apply_system_settings(&app, &previous);
        return Err(error);
    }

    state.replace_config(config);
    Ok(app_info(&app, &state))
}

#[tauri::command]
pub fn dispatch_action(
    app: AppHandle,
    state: State<'_, Arc<RuntimeState>>,
    action_id: String,
) -> AppResult<String> {
    state.dispatch(&app, &action_id)
}

pub fn apply_system_settings(app: &AppHandle, config: &Config) -> AppResult<()> {
    let autostart = app.autolaunch();
    let result = if config.settings.auto_start_on_boot {
        autostart.enable()
    } else {
        autostart.disable()
    };
    result.map_err(|error| AppError::Message(format!("Autostart update failed: {error}")))?;
    set_tray_visibility(app, config.settings.show_tray_icon)?;
    Ok(())
}

fn app_info(app: &AppHandle, state: &RuntimeState) -> AppInfo {
    AppInfo {
        config: state.config(),
        config_path: state.config_path.display().to_string(),
        platform: if cfg!(target_os = "windows") {
            "windows"
        } else if cfg!(target_os = "macos") {
            "macos"
        } else {
            "linux"
        },
        version: app.package_info().version.to_string(),
    }
}
