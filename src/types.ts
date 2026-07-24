type PlatformProcessNames = {
  windows: string;
  macos: string;
  linux: string;
};

type ActionConfig = {
  key_sequence: string[];
};

export type ProviderConfig = {
  enabled: boolean;
  process_name: string;
  process_names: PlatformProcessNames;
  icon: string;
  actions: Record<string, ActionConfig>;
};

type AppSettings = {
  auto_start_on_boot: boolean;
  show_tray_icon: boolean;
  show_action_notifications: boolean;
};

export type AICommanderConfig = {
  version: number;
  provider_priority: string[];
  providers: Record<string, ProviderConfig>;
  hotkeys: Record<string, string>;
  settings: AppSettings;
};

export type AppInfo = {
  config: AICommanderConfig;
  config_path: string;
  platform: "windows" | "macos" | "linux";
  version: string;
};

export type ProviderTemplate = {
  id: string;
  label: string;
  icon: string;
  processNames: PlatformProcessNames;
  actions: Record<string, string>;
};
