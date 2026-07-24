use std::{ffi::OsStr, thread, time::Duration};

use enigo::{
    Direction::{Click, Press, Release},
    Enigo, Key, Keyboard, Settings,
};
use sysinfo::System;

use crate::error::{AppError, AppResult};

#[derive(Debug, Default)]
pub struct DesktopAutomation;

impl DesktopAutomation {
    pub fn is_process_running(&self, system: &System, process_name: &str) -> bool {
        !self.find_process_ids(system, process_name).is_empty()
    }

    pub fn visible_process_id(&self, system: &System, process_name: &str) -> Option<u32> {
        self.find_process_ids(system, process_name)
            .into_iter()
            .find(|process_id| platform::is_process_visible(*process_id))
    }

    pub fn send_key_sequence(&self, process_id: u32, key_sequence: &[String]) -> AppResult<()> {
        let previous = platform::activate_process(process_id)?;
        thread::sleep(Duration::from_millis(75));
        let send_result = send_keys(key_sequence);
        let restore_result = platform::restore_process(previous);
        send_result?;
        restore_result
    }

    fn find_process_ids(&self, system: &System, process_name: &str) -> Vec<u32> {
        let expected = normalize_process_name(process_name);
        if expected.is_empty() {
            return Vec::new();
        }
        system
            .processes()
            .iter()
            .filter_map(|(process_id, process)| {
                let name = normalize_process_name(&process.name().to_string_lossy());
                let executable = process
                    .exe()
                    .and_then(|path| path.file_name())
                    .map(OsStr::to_string_lossy)
                    .map(|value| normalize_process_name(&value))
                    .unwrap_or_default();
                (name == expected || executable == expected).then(|| process_id.as_u32())
            })
            .collect()
    }
}

fn normalize_process_name(value: &str) -> String {
    let normalized = value.trim().to_lowercase();
    normalized
        .strip_suffix(".exe")
        .unwrap_or(&normalized)
        .to_string()
}

fn send_keys(key_sequence: &[String]) -> AppResult<()> {
    let keys = key_sequence
        .iter()
        .map(|key| parse_key(key))
        .collect::<AppResult<Vec<_>>>()?;
    let mut enigo = Enigo::new(&Settings::default())
        .map_err(|error| AppError::Automation(error.to_string()))?;
    for modifier in [Key::Control, Key::Alt, Key::Shift, Key::Meta] {
        enigo
            .key(modifier, Release)
            .map_err(|error| AppError::Automation(error.to_string()))?;
    }

    let mut pressed_modifiers = Vec::new();
    let mut send_result = Ok(());
    for key in keys {
        if is_modifier(key) {
            if let Err(error) = enigo.key(key, Press) {
                send_result = Err(AppError::Automation(error.to_string()));
                break;
            }
            pressed_modifiers.push(key);
        } else if let Err(error) = enigo.key(key, Click) {
            send_result = Err(AppError::Automation(error.to_string()));
            break;
        }
        thread::sleep(Duration::from_millis(10));
    }

    let mut release_result = Ok(());
    for modifier in pressed_modifiers.into_iter().rev() {
        if let Err(error) = enigo.key(modifier, Release) {
            release_result = Err(AppError::Automation(error.to_string()));
        }
    }
    send_result.and(release_result)
}

fn is_modifier(key: Key) -> bool {
    matches!(key, Key::Control | Key::Alt | Key::Shift | Key::Meta)
}

fn parse_key(value: &str) -> AppResult<Key> {
    let normalized = value.trim().to_ascii_lowercase();
    let key = match normalized.as_str() {
        "ctrl" | "control" => Key::Control,
        "alt" | "option" => Key::Alt,
        "shift" => Key::Shift,
        "win" | "windows" | "super" | "meta" | "cmd" | "command" => Key::Meta,
        "enter" | "return" => Key::Return,
        "escape" | "esc" => Key::Escape,
        "space" => Key::Space,
        "tab" => Key::Tab,
        "backspace" => Key::Backspace,
        "delete" => Key::Delete,
        "home" => Key::Home,
        "end" => Key::End,
        "pageup" => Key::PageUp,
        "pagedown" => Key::PageDown,
        "left" | "arrowleft" => Key::LeftArrow,
        "right" | "arrowright" => Key::RightArrow,
        "up" | "arrowup" => Key::UpArrow,
        "down" | "arrowdown" => Key::DownArrow,
        "f1" => Key::F1,
        "f2" => Key::F2,
        "f3" => Key::F3,
        "f4" => Key::F4,
        "f5" => Key::F5,
        "f6" => Key::F6,
        "f7" => Key::F7,
        "f8" => Key::F8,
        "f9" => Key::F9,
        "f10" => Key::F10,
        "f11" => Key::F11,
        "f12" => Key::F12,
        "bracketleft" | "leftbracket" | "oemopenbrackets" => Key::Unicode('['),
        "bracketright" | "rightbracket" | "oemclosebrackets" | "oem6" => Key::Unicode(']'),
        "equal" | "oemplus" | "plus" => Key::Unicode('='),
        _ => {
            let mut characters = value.trim().chars();
            match (characters.next(), characters.next()) {
                (Some(character), None) => Key::Unicode(character.to_ascii_lowercase()),
                _ => {
                    return Err(AppError::Automation(format!(
                        "Key '{value}' is not supported."
                    )))
                }
            }
        }
    };
    Ok(key)
}

#[cfg(target_os = "windows")]
mod platform {
    use std::sync::Mutex;

    use windows::Win32::{
        Foundation::{BOOL, HWND, LPARAM},
        UI::WindowsAndMessaging::{
            EnumWindows, GetForegroundWindow, GetWindowThreadProcessId, IsIconic, IsWindowVisible,
            SetForegroundWindow, ShowWindow, SW_RESTORE,
        },
    };

    use crate::error::{AppError, AppResult};

    #[derive(Debug, Clone, Copy)]
    pub struct RestoreTarget(HWND);

    struct WindowSearch {
        process_id: u32,
        result: Mutex<Option<HWND>>,
    }

    pub fn is_process_visible(process_id: u32) -> bool {
        find_window(process_id).is_some()
    }

    pub fn activate_process(process_id: u32) -> AppResult<RestoreTarget> {
        let previous = unsafe { GetForegroundWindow() };
        let target = find_window(process_id).ok_or_else(|| {
            AppError::Automation(format!(
                "No visible window belongs to process {process_id}."
            ))
        })?;
        unsafe {
            if IsIconic(target).as_bool() {
                ShowWindow(target, SW_RESTORE);
            }
            if !SetForegroundWindow(target).as_bool() {
                return Err(AppError::Automation(
                    "Windows denied foreground activation.".into(),
                ));
            }
        }
        Ok(RestoreTarget(previous))
    }

    pub fn restore_process(previous: RestoreTarget) -> AppResult<()> {
        if previous.0.is_invalid() {
            return Ok(());
        }
        unsafe {
            SetForegroundWindow(previous.0);
        }
        Ok(())
    }

    fn find_window(process_id: u32) -> Option<HWND> {
        let search = WindowSearch {
            process_id,
            result: Mutex::new(None),
        };
        unsafe {
            let _ = EnumWindows(
                Some(enum_window),
                LPARAM((&search as *const WindowSearch).cast_mut() as isize),
            );
        }
        *search.result.lock().expect("window search mutex poisoned")
    }

    unsafe extern "system" fn enum_window(window: HWND, parameter: LPARAM) -> BOOL {
        let search = &*(parameter.0 as *const WindowSearch);
        let mut window_process_id = 0;
        GetWindowThreadProcessId(window, Some(&mut window_process_id));
        if window_process_id == search.process_id && IsWindowVisible(window).as_bool() {
            *search.result.lock().expect("window search mutex poisoned") = Some(window);
            return BOOL(0);
        }
        BOOL(1)
    }
}

#[cfg(target_os = "macos")]
mod platform {
    use std::process::Command;

    use crate::error::{AppError, AppResult};

    #[derive(Debug, Clone)]
    pub struct RestoreTarget(Option<u32>);

    pub fn is_process_visible(_process_id: u32) -> bool {
        true
    }

    pub fn activate_process(process_id: u32) -> AppResult<RestoreTarget> {
        let previous = frontmost_process_id();
        set_frontmost(process_id)?;
        Ok(RestoreTarget(previous))
    }

    pub fn restore_process(previous: RestoreTarget) -> AppResult<()> {
        if let Some(process_id) = previous.0 {
            set_frontmost(process_id)?;
        }
        Ok(())
    }

    fn frontmost_process_id() -> Option<u32> {
        let output = Command::new("osascript")
            .args([
                "-e",
                "tell application \"System Events\" to unix id of first process whose frontmost is true",
            ])
            .output()
            .ok()?;
        String::from_utf8_lossy(&output.stdout).trim().parse().ok()
    }

    fn set_frontmost(process_id: u32) -> AppResult<()> {
        let script = format!(
            "tell application \"System Events\" to set frontmost of first process whose unix id is {process_id} to true"
        );
        let status = Command::new("osascript")
            .args(["-e", &script])
            .status()
            .map_err(|error| AppError::Automation(error.to_string()))?;
        if status.success() {
            Ok(())
        } else {
            Err(AppError::Automation(format!(
                "macOS could not activate process {process_id}; grant Accessibility permission to AI Commander."
            )))
        }
    }
}

#[cfg(target_os = "linux")]
mod platform {
    use std::process::Command;

    use crate::error::{AppError, AppResult};

    #[derive(Debug, Clone)]
    pub struct RestoreTarget(Option<String>);

    pub fn is_process_visible(process_id: u32) -> bool {
        window_for_process(process_id).is_some()
    }

    pub fn activate_process(process_id: u32) -> AppResult<RestoreTarget> {
        ensure_x11()?;
        let previous = command_output("xdotool", &["getactivewindow"]);
        let target = window_for_process(process_id).ok_or_else(|| {
            AppError::Automation(format!(
                "No visible X11 window belongs to process {process_id}."
            ))
        })?;
        command_success("xdotool", &["windowactivate", "--sync", &target])?;
        Ok(RestoreTarget(previous))
    }

    pub fn restore_process(previous: RestoreTarget) -> AppResult<()> {
        if let Some(window) = previous.0 {
            command_success("xdotool", &["windowactivate", "--sync", &window])?;
        }
        Ok(())
    }

    fn window_for_process(process_id: u32) -> Option<String> {
        command_output(
            "xdotool",
            &["search", "--onlyvisible", "--pid", &process_id.to_string()],
        )
        .and_then(|output| output.lines().next().map(str::to_owned))
    }

    fn ensure_x11() -> AppResult<()> {
        if std::env::var_os("WAYLAND_DISPLAY").is_some() && std::env::var_os("DISPLAY").is_none() {
            return Err(AppError::Automation(
                "Keyboard routing requires an X11/XWayland session; native Wayland blocks foreground input injection."
                    .into(),
            ));
        }
        Ok(())
    }

    fn command_output(command: &str, arguments: &[&str]) -> Option<String> {
        let output = Command::new(command).args(arguments).output().ok()?;
        output
            .status
            .success()
            .then(|| String::from_utf8_lossy(&output.stdout).trim().to_owned())
    }

    fn command_success(command: &str, arguments: &[&str]) -> AppResult<()> {
        let status = Command::new(command)
            .args(arguments)
            .status()
            .map_err(|error| {
                AppError::Automation(format!(
                    "Failed to start {command}: {error}. Install xdotool."
                ))
            })?;
        status
            .success()
            .then_some(())
            .ok_or_else(|| AppError::Automation(format!("{command} exited with status {status}.")))
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn process_names_are_normalized() {
        assert_eq!(normalize_process_name("Code.exe"), "code");
        assert_eq!(normalize_process_name(" code "), "code");
    }

    #[test]
    fn common_keys_are_parsed() {
        assert_eq!(parse_key("Ctrl").unwrap(), Key::Control);
        assert_eq!(parse_key("Enter").unwrap(), Key::Return);
        assert_eq!(parse_key("]").unwrap(), Key::Unicode(']'));
    }

    #[test]
    fn unsupported_long_key_is_rejected() {
        assert!(parse_key("UnknownKey").is_err());
    }
}
