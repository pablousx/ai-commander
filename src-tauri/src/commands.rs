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

#[tauri::command]
pub fn open_config_file(state: State<'_, Arc<RuntimeState>>) -> AppResult<()> {
    if !state.config_path.is_file() {
        return Err(AppError::OpenConfig(format!(
            "'{}' does not exist.",
            state.config_path.display()
        )));
    }
    platform::open_path(&state.config_path)
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

#[cfg(target_os = "windows")]
mod platform {
    use std::{os::windows::ffi::OsStrExt, path::Path};

    use windows::{
        core::PCWSTR,
        Win32::UI::{Shell::ShellExecuteW, WindowsAndMessaging::SW_SHOWNORMAL},
    };

    use crate::error::{AppError, AppResult};

    pub fn open_path(path: &Path) -> AppResult<()> {
        let path_wide = path
            .as_os_str()
            .encode_wide()
            .chain(std::iter::once(0))
            .collect::<Vec<_>>();
        let result = unsafe {
            ShellExecuteW(
                None,
                None,
                PCWSTR(path_wide.as_ptr()),
                None,
                None,
                SW_SHOWNORMAL,
            )
        };
        let result_code = result.0 as isize;
        if result_code > 32 {
            Ok(())
        } else {
            Err(AppError::OpenConfig(format!(
                "Windows ShellExecute failed with code {result_code}."
            )))
        }
    }
}

#[cfg(target_os = "macos")]
mod platform {
    use std::{path::Path, process::Command};

    use crate::error::{AppError, AppResult};

    pub fn open_path(path: &Path) -> AppResult<()> {
        open_with("open", path)
    }

    fn open_with(program: &str, path: &Path) -> AppResult<()> {
        let status = Command::new(program)
            .arg(path)
            .status()
            .map_err(|error| AppError::OpenConfig(error.to_string()))?;
        status
            .success()
            .then_some(())
            .ok_or_else(|| AppError::OpenConfig(format!("{program} exited with status {status}.")))
    }
}

#[cfg(target_os = "linux")]
mod platform {
    use std::{path::Path, process::Command};

    use crate::error::{AppError, AppResult};

    pub fn open_path(path: &Path) -> AppResult<()> {
        let program = "xdg-open";
        let status = Command::new(program)
            .arg(path)
            .status()
            .map_err(|error| AppError::OpenConfig(error.to_string()))?;
        status
            .success()
            .then_some(())
            .ok_or_else(|| AppError::OpenConfig(format!("{program} exited with status {status}.")))
    }
}

#[cfg(not(any(target_os = "windows", target_os = "macos", target_os = "linux")))]
mod platform {
    use std::path::Path;

    use crate::error::{AppError, AppResult};

    pub fn open_path(_path: &Path) -> AppResult<()> {
        Err(AppError::OpenConfig(
            "Opening files is unsupported on this platform.".into(),
        ))
    }
}
