use std::{collections::HashMap, path::PathBuf, sync::Arc};

use parking_lot::{Mutex, RwLock};
use sysinfo::System;
use tauri::{AppHandle, Emitter, Manager};
use tauri_plugin_global_shortcut::{GlobalShortcutExt, Shortcut, ShortcutEvent, ShortcutState};
use tauri_plugin_notification::NotificationExt;

use crate::{
    automation::DesktopAutomation,
    config::{Config, ProviderConfig},
    error::{AppError, AppResult},
    shortcuts::parse_shortcut,
};

pub struct RuntimeState {
    pub config_path: PathBuf,
    config: RwLock<Config>,
    shortcut_actions: RwLock<HashMap<u32, String>>,
    dispatch_lock: Mutex<()>,
    automation: DesktopAutomation,
}

impl RuntimeState {
    pub fn new(config_path: PathBuf, config: Config) -> Arc<Self> {
        Arc::new(Self {
            config_path,
            config: RwLock::new(config),
            shortcut_actions: RwLock::new(HashMap::new()),
            dispatch_lock: Mutex::new(()),
            automation: DesktopAutomation,
        })
    }

    pub fn config(&self) -> Config {
        self.config.read().clone()
    }

    pub fn replace_config(&self, config: Config) {
        *self.config.write() = config;
    }

    pub fn action_for_shortcut(&self, shortcut_id: u32) -> Option<String> {
        self.shortcut_actions.read().get(&shortcut_id).cloned()
    }

    pub fn apply_shortcuts(&self, app: &AppHandle, config: &Config) -> AppResult<()> {
        let registrations = parse_registrations(config)?;
        let previous_config = self.config();
        let manager = app.global_shortcut();

        manager
            .unregister_all()
            .map_err(|error| AppError::Shortcut(error.to_string()))?;

        match register_all(app, &registrations) {
            Ok(actions) => {
                *self.shortcut_actions.write() = actions;
                Ok(())
            }
            Err(error) => {
                let _ = manager.unregister_all();
                if let Ok(previous) = parse_registrations(&previous_config) {
                    if let Ok(actions) = register_all(app, &previous) {
                        *self.shortcut_actions.write() = actions;
                    }
                }
                Err(error)
            }
        }
    }

    pub fn dispatch(&self, app: &AppHandle, action_id: &str) -> AppResult<String> {
        let _dispatch_guard = self.dispatch_lock.lock();
        let config = self.config();
        let processes = System::new_all();
        let (provider_name, provider, action_name, process_id) =
            self.resolve_action(&config, action_id, &processes)?;
        let action = provider
            .actions
            .iter()
            .find(|(name, _)| name.eq_ignore_ascii_case(&action_name))
            .map(|(_, action)| action)
            .ok_or_else(|| {
                AppError::Message(format!(
                    "Action '{provider_name}.{action_name}' is not configured."
                ))
            })?;

        self.automation
            .send_key_sequence(process_id, &action.key_sequence)?;

        let message = format!("Executed {action_name} in {provider_name}");
        log::info!("{message}");
        let _ = app.emit("action-triggered", &message);
        if config.settings.show_action_notifications {
            if let Err(error) = app
                .notification()
                .builder()
                .title("AI Commander")
                .body(&message)
                .show()
            {
                log::warn!("Failed to show action notification: {error}");
            }
        }
        Ok(message)
    }

    fn resolve_action<'a>(
        &self,
        config: &'a Config,
        action_id: &str,
        processes: &System,
    ) -> AppResult<(String, &'a ProviderConfig, String, u32)> {
        if let Some((provider_name, action_name)) = action_id.split_once('.') {
            let (resolved_name, provider) = config
                .providers
                .iter()
                .find(|(name, _)| name.eq_ignore_ascii_case(provider_name))
                .ok_or_else(|| {
                    AppError::Message(format!("Provider '{provider_name}' not found."))
                })?;
            let process_id = self.ensure_provider_available(processes, resolved_name, provider)?;
            return Ok((
                resolved_name.clone(),
                provider,
                action_name.to_string(),
                process_id,
            ));
        }

        for priority_name in &config.provider_priority {
            let Some((resolved_name, provider)) = config
                .providers
                .iter()
                .find(|(name, _)| name.eq_ignore_ascii_case(priority_name))
            else {
                continue;
            };
            if !provider.enabled
                || !provider
                    .actions
                    .keys()
                    .any(|name| name.eq_ignore_ascii_case(action_id))
            {
                continue;
            }
            let process_name = provider.effective_process_name();
            if let Some(process_id) = self.automation.visible_process_id(processes, process_name) {
                return Ok((
                    resolved_name.clone(),
                    provider,
                    action_id.to_string(),
                    process_id,
                ));
            }
        }

        Err(AppError::Message(format!(
            "No enabled, running, visible application can handle '{action_id}'."
        )))
    }

    fn ensure_provider_available(
        &self,
        processes: &System,
        provider_name: &str,
        provider: &ProviderConfig,
    ) -> AppResult<u32> {
        if !provider.enabled {
            return Err(AppError::Message(format!(
                "Provider '{provider_name}' is disabled."
            )));
        }
        let process_name = provider.effective_process_name();
        if !self.automation.is_process_running(processes, process_name) {
            return Err(AppError::Message(format!(
                "Process '{process_name}' is not running."
            )));
        }
        self.automation
            .visible_process_id(processes, process_name)
            .ok_or_else(|| {
                AppError::Message(format!("Process '{process_name}' has no visible window."))
            })
    }
}

fn parse_registrations(config: &Config) -> AppResult<Vec<(Shortcut, String)>> {
    config
        .hotkeys
        .iter()
        .map(|(gesture, action)| Ok((parse_shortcut(gesture)?, action.clone())))
        .collect()
}

fn register_all(
    app: &AppHandle,
    registrations: &[(Shortcut, String)],
) -> AppResult<HashMap<u32, String>> {
    let manager = app.global_shortcut();
    let mut actions = HashMap::new();
    for (shortcut, action) in registrations {
        manager
            .register(*shortcut)
            .map_err(|error| AppError::Shortcut(error.to_string()))?;
        actions.insert(shortcut.id(), action.clone());
    }
    Ok(actions)
}

pub fn handle_shortcut(app: &AppHandle, shortcut: &Shortcut, event: ShortcutEvent) {
    if event.state != ShortcutState::Pressed {
        return;
    }
    let runtime = app.state::<Arc<RuntimeState>>().inner().clone();
    let app_handle = app.clone();
    let shortcut_id = shortcut.id();
    std::thread::spawn(move || {
        let Some(action_id) = runtime.action_for_shortcut(shortcut_id) else {
            log::warn!("No action mapping exists for shortcut ID {shortcut_id}");
            return;
        };
        if let Err(error) = runtime.dispatch(&app_handle, &action_id) {
            log::error!("Failed to dispatch '{action_id}': {error}");
            let _ = app_handle.emit("action-error", error.to_string());
        }
    });
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::config::{ActionConfig, AppSettings, PlatformProcessNames, ProviderConfig};
    use indexmap::IndexMap;

    #[test]
    fn scoped_registration_is_parsed() {
        let config = Config {
            version: 2,
            provider_priority: vec!["vscode".into()],
            providers: IndexMap::from([(
                "vscode".into(),
                ProviderConfig {
                    enabled: true,
                    process_name: "code".into(),
                    process_names: PlatformProcessNames::default(),
                    icon: String::new(),
                    actions: IndexMap::from([(
                        "accept".into(),
                        ActionConfig {
                            key_sequence: vec!["Enter".into()],
                        },
                    )]),
                },
            )]),
            hotkeys: IndexMap::from([("Ctrl+Enter".into(), "vscode.accept".into())]),
            settings: AppSettings::default(),
        };
        let registrations = parse_registrations(&config).unwrap();
        assert_eq!(registrations.len(), 1);
        assert_eq!(registrations[0].1, "vscode.accept");
    }
}
