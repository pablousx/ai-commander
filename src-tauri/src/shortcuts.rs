use std::str::FromStr;

use tauri_plugin_global_shortcut::Shortcut;

use crate::error::{AppError, AppResult};

pub fn canonical_shortcut(value: &str) -> AppResult<String> {
    let parts: Vec<&str> = value
        .split('+')
        .map(str::trim)
        .filter(|part| !part.is_empty())
        .collect();
    if parts.is_empty() {
        return Err(AppError::Shortcut("Shortcut cannot be empty.".into()));
    }

    let normalized = parts
        .iter()
        .map(|part| normalize_token(part))
        .collect::<Vec<_>>()
        .join("+");
    let shortcut = Shortcut::from_str(&normalized).map_err(|error| {
        AppError::Shortcut(format!("Shortcut '{value}' is not supported: {error}"))
    })?;
    Ok(shortcut.into_string())
}

pub fn parse_shortcut(value: &str) -> AppResult<Shortcut> {
    Shortcut::from_str(&canonical_shortcut(value)?)
        .map_err(|error| AppError::Shortcut(error.to_string()))
}

fn normalize_token(token: &str) -> String {
    match token.to_ascii_lowercase().as_str() {
        "ctrl" | "control" => "Control".into(),
        "alt" | "option" => "Alt".into(),
        "shift" => "Shift".into(),
        "win" | "windows" | "super" | "meta" | "cmd" | "command" => "Super".into(),
        "esc" => "Escape".into(),
        "return" => "Enter".into(),
        "left" | "arrowleft" => "ArrowLeft".into(),
        "right" | "arrowright" => "ArrowRight".into(),
        "up" | "arrowup" => "ArrowUp".into(),
        "down" | "arrowdown" => "ArrowDown".into(),
        "oemopenbrackets" | "leftbracket" | "bracketleft" => "BracketLeft".into(),
        "oemclosebrackets" | "oem6" | "rightbracket" | "bracketright" => "BracketRight".into(),
        "oemplus" | "plus" | "equal" => "Equal".into(),
        _ if token.len() == 1 => token.to_ascii_uppercase(),
        _ => token.into(),
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn legacy_windows_names_are_supported() {
        assert!(canonical_shortcut("Ctrl+Shift+Oem6").is_ok());
        assert!(canonical_shortcut("Ctrl+OemOpenBrackets").is_ok());
    }

    #[test]
    fn modifier_only_shortcut_is_rejected() {
        assert!(canonical_shortcut("Ctrl+Shift").is_err());
    }
}
