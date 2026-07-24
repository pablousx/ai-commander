import type { ProviderTemplate } from "./types";

export const providerTemplates: ProviderTemplate[] = [
  {
    id: "antigravity",
    label: "Antigravity",
    icon: "antigravity.png",
    processNames: {
      windows: "Antigravity IDE.exe",
      macos: "Antigravity",
      linux: "antigravity",
    },
    actions: {
      accept: "Ctrl+Enter",
      reject: "Escape",
      next: "Alt+N",
      previous: "Alt+P",
    },
  },
  {
    id: "vscode",
    label: "Visual Studio Code",
    icon: "vscode.png",
    processNames: {
      windows: "Code.exe",
      macos: "Visual Studio Code",
      linux: "code",
    },
    actions: {
      accept: "Ctrl+Enter",
      reject: "Escape",
      inline_chat: "Ctrl+I",
    },
  },
  {
    id: "claude",
    label: "Claude",
    icon: "claude.png",
    processNames: {
      windows: "Claude.exe",
      macos: "Claude",
      linux: "claude",
    },
    actions: {
      accept: "Ctrl+Enter",
      reject: "Escape",
    },
  },
];

export const emptyTemplate: ProviderTemplate = {
  id: "custom-app",
  label: "Custom application",
  icon: "logo.png",
  processNames: { windows: "", macos: "", linux: "" },
  actions: { new_action: "Ctrl+Enter" },
};
