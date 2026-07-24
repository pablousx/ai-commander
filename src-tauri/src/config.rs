use std::{
    collections::HashSet,
    fs,
    path::{Path, PathBuf},
};

use indexmap::IndexMap;
use serde::{Deserialize, Serialize};

use crate::{
    error::{AppError, AppResult},
    shortcuts::canonical_shortcut,
};

const DEFAULT_CONFIG: &str = include_str!("../../config/ai-commander.yaml");

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct Config {
    #[serde(default = "default_config_version")]
    pub version: u32,
    #[serde(default)]
    pub provider_priority: Vec<String>,
    #[serde(default)]
    pub providers: IndexMap<String, ProviderConfig>,
    #[serde(default)]
    pub hotkeys: IndexMap<String, String>,
    #[serde(default)]
    pub settings: AppSettings,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ProviderConfig {
    #[serde(default = "default_true")]
    pub enabled: bool,
    #[serde(default)]
    pub process_name: String,
    #[serde(default)]
    pub process_names: PlatformProcessNames,
    #[serde(default)]
    pub icon: String,
    #[serde(default)]
    pub actions: IndexMap<String, ActionConfig>,
}

#[derive(Debug, Clone, Default, Serialize, Deserialize)]
pub struct PlatformProcessNames {
    #[serde(default)]
    pub windows: String,
    #[serde(default)]
    pub macos: String,
    #[serde(default)]
    pub linux: String,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct ActionConfig {
    #[serde(default)]
    pub key_sequence: Vec<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct AppSettings {
    #[serde(default)]
    pub auto_start_on_boot: bool,
    #[serde(default = "default_true")]
    pub show_tray_icon: bool,
    #[serde(default = "default_true")]
    pub show_action_notifications: bool,
}

impl Default for AppSettings {
    fn default() -> Self {
        Self {
            auto_start_on_boot: false,
            show_tray_icon: true,
            show_action_notifications: true,
        }
    }
}

impl Config {
    pub fn load(path: &Path) -> AppResult<Self> {
        if !path.exists() {
            if let Some(parent) = path.parent() {
                fs::create_dir_all(parent)?;
            }
            fs::write(path, DEFAULT_CONFIG)?;
        }
        let yaml = fs::read_to_string(path)?;
        let mut config: Self = serde_yaml::from_str(&yaml)?;
        config.migrate();
        config.validate()?;
        Ok(config)
    }

    pub fn save(&self, path: &Path) -> AppResult<()> {
        self.validate()?;
        if let Some(parent) = path.parent() {
            fs::create_dir_all(parent)?;
        }
        let yaml = serde_yaml::to_string(self)?;
        let temporary = path.with_extension("yaml.tmp");
        let backup = path.with_extension("yaml.bak");
        fs::write(&temporary, yaml)?;
        if path.exists() {
            fs::copy(path, &backup)?;
            #[cfg(target_os = "windows")]
            fs::remove_file(path)?;
        }
        if let Err(error) = fs::rename(&temporary, path) {
            let _ = fs::remove_file(&temporary);
            #[cfg(target_os = "windows")]
            if backup.exists() {
                let _ = fs::copy(&backup, path);
            }
            return Err(error.into());
        }
        Ok(())
    }

    pub fn validate(&self) -> AppResult<()> {
        let mut errors = Vec::new();
        if self.version != 2 {
            errors.push(format!(
                "Unsupported configuration version '{}'; expected 2.",
                self.version
            ));
        }

        let mut priority_names = HashSet::new();
        for name in &self.provider_priority {
            let normalized = name.trim().to_lowercase();
            if normalized.is_empty() {
                errors.push("Provider priority contains an empty name.".into());
            } else if !priority_names.insert(normalized) {
                errors.push(format!("Provider priority contains duplicate '{name}'."));
            }
            if !self
                .providers
                .keys()
                .any(|candidate| candidate.eq_ignore_ascii_case(name))
            {
                errors.push(format!("Provider priority references missing '{name}'."));
            }
        }

        for (name, provider) in &self.providers {
            if name.trim().is_empty() {
                errors.push("Provider name cannot be empty.".into());
            }
            if provider.enabled && provider.effective_process_name().trim().is_empty() {
                errors.push(format!(
                    "Enabled provider '{name}' needs a process name for this platform."
                ));
            }
            if provider.actions.is_empty() {
                errors.push(format!(
                    "Provider '{name}' must define at least one action."
                ));
            }
            for (action_name, action) in &provider.actions {
                if action_name.trim().is_empty() || action_name.contains('.') {
                    errors.push(format!(
                        "Provider '{name}' has invalid action '{action_name}'."
                    ));
                }
                if action.key_sequence.is_empty()
                    || action.key_sequence.iter().any(|key| {
                        key.trim().is_empty()
                            || key.eq_ignore_ascii_case("unset")
                            || key.eq_ignore_ascii_case("none")
                    })
                {
                    errors.push(format!(
                        "Action '{name}.{action_name}' has an invalid key sequence."
                    ));
                }
            }
        }

        let mut gestures = HashSet::new();
        for (gesture, action_id) in &self.hotkeys {
            match canonical_shortcut(gesture) {
                Ok(canonical) => {
                    if !gestures.insert(canonical.to_lowercase()) {
                        errors.push(format!("Global shortcut '{gesture}' is duplicated."));
                    }
                }
                Err(error) => errors.push(error.to_string()),
            }
            if !self.action_exists(action_id) {
                errors.push(format!(
                    "Global shortcut '{gesture}' references missing action '{action_id}'."
                ));
            }
        }

        if errors.is_empty() {
            Ok(())
        } else {
            Err(AppError::Message(format!(
                "Configuration validation failed:\n- {}",
                errors.join("\n- ")
            )))
        }
    }

    pub fn action_exists(&self, action_id: &str) -> bool {
        if let Some((provider_name, action_name)) = action_id.split_once('.') {
            return self.providers.iter().any(|(name, provider)| {
                name.eq_ignore_ascii_case(provider_name)
                    && provider
                        .actions
                        .keys()
                        .any(|action| action.eq_ignore_ascii_case(action_name))
            });
        }
        self.providers.values().any(|provider| {
            provider
                .actions
                .keys()
                .any(|action| action.eq_ignore_ascii_case(action_id))
        })
    }

    fn migrate(&mut self) {
        if self.version == 1 {
            self.version = 2;
        }
        for provider in self.providers.values_mut() {
            if provider.process_names.windows.is_empty() {
                provider.process_names.windows = provider.process_name.clone();
            }
            if provider.process_names.macos.is_empty() {
                provider.process_names.macos = provider.process_name.clone();
            }
            if provider.process_names.linux.is_empty() {
                provider.process_names.linux = provider.process_name.clone();
            }
        }
    }
}

impl ProviderConfig {
    pub fn effective_process_name(&self) -> &str {
        let platform_name = if cfg!(target_os = "windows") {
            &self.process_names.windows
        } else if cfg!(target_os = "macos") {
            &self.process_names.macos
        } else {
            &self.process_names.linux
        };
        if platform_name.trim().is_empty() {
            &self.process_name
        } else {
            platform_name
        }
    }
}

pub fn config_path(app_config_dir: PathBuf) -> PathBuf {
    #[cfg(debug_assertions)]
    {
        let repository_config =
            Path::new(env!("CARGO_MANIFEST_DIR")).join("../config/ai-commander.yaml");
        if repository_config.exists() {
            return repository_config;
        }
    }
    app_config_dir.join("ai-commander.yaml")
}

const fn default_config_version() -> u32 {
    1
}

const fn default_true() -> bool {
    true
}

#[cfg(test)]
mod tests {
    use super::*;

    fn valid_config() -> Config {
        let mut providers = IndexMap::new();
        providers.insert(
            "vscode".into(),
            ProviderConfig {
                enabled: true,
                process_name: "code".into(),
                process_names: PlatformProcessNames {
                    windows: "Code.exe".into(),
                    macos: "Visual Studio Code".into(),
                    linux: "code".into(),
                },
                icon: "vscode.png".into(),
                actions: IndexMap::from([(
                    "accept".into(),
                    ActionConfig {
                        key_sequence: vec!["Ctrl".into(), "Enter".into()],
                    },
                )]),
            },
        );
        Config {
            version: 2,
            provider_priority: vec!["vscode".into()],
            providers,
            hotkeys: IndexMap::from([("Ctrl+Shift+Enter".into(), "accept".into())]),
            settings: AppSettings::default(),
        }
    }

    #[test]
    fn valid_configuration_has_no_errors() {
        assert!(valid_config().validate().is_ok());
    }

    #[test]
    fn duplicate_priority_is_rejected_case_insensitively() {
        let mut config = valid_config();
        config.provider_priority.push("VSCODE".into());
        assert!(config
            .validate()
            .unwrap_err()
            .to_string()
            .contains("duplicate"));
    }

    #[test]
    fn save_creates_backup() {
        let directory = tempfile::tempdir().unwrap();
        let path = directory.path().join("config.yaml");
        let config = valid_config();
        config.save(&path).unwrap();
        config.save(&path).unwrap();
        assert!(path.with_extension("yaml.bak").exists());
        assert_eq!(Config::load(&path).unwrap().version, 2);
    }

    #[test]
    fn version_one_is_migrated() {
        let yaml = r#"
version: 1
provider_priority: [vscode]
providers:
  vscode:
    process_name: Code.exe
    actions:
      accept:
        key_sequence: [Ctrl, Enter]
hotkeys:
  Ctrl+Enter: accept
"#;
        let mut config: Config = serde_yaml::from_str(yaml).unwrap();
        config.migrate();
        assert_eq!(config.version, 2);
        assert_eq!(config.providers["vscode"].process_names.windows, "Code.exe");
    }
}
