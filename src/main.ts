import { invoke } from "@tauri-apps/api/core";
import { listen } from "@tauri-apps/api/event";

import { eventToHotkey, normalizeHotkey, splitSequence } from "./hotkeys";
import { emptyTemplate, providerTemplates } from "./templates";
import type {
  AICommanderConfig,
  AppInfo,
  ProviderConfig,
  ProviderTemplate,
} from "./types";
import "./styles.css";

type EditableAction = {
  name: string;
  globalHotkey: string;
  appHotkey: string;
};

type EditableProvider = {
  name: string;
  enabled: boolean;
  icon: string;
  processNames: {
    windows: string;
    macos: string;
    linux: string;
  };
  actions: EditableAction[];
};

type UiState = {
  info: AppInfo | null;
  providers: EditableProvider[];
  isSaving: boolean;
  isDirty: boolean;
  message: string;
  messageKind: "success" | "error" | "info";
};

const state: UiState = {
  info: null,
  providers: [],
  isSaving: false,
  isDirty: false,
  message: "Loading configuration…",
  messageKind: "info",
};

const appRoot = document.querySelector<HTMLDivElement>("#app");
if (!appRoot) throw new Error("Application root was not found.");
const app: HTMLDivElement = appRoot;

function escapeHtml(value: string): string {
  return value
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#039;");
}

function iconUrl(): string {
  return "/logo.png";
}

function toEditable(info: AppInfo): EditableProvider[] {
  const claimed = new Set<string>();
  const orderedNames = [
    ...info.config.provider_priority,
    ...Object.keys(info.config.providers).filter(
      (name) =>
        !info.config.provider_priority.some(
          (priorityName) => priorityName.toLowerCase() === name.toLowerCase(),
        ),
    ),
  ];

  return orderedNames.flatMap((name) => {
    const provider = info.config.providers[name];
    if (!provider) return [];
    const actions = Object.entries(provider.actions).map(([actionName, action]) => {
      const scoped = `${name}.${actionName}`;
      const mapping = Object.entries(info.config.hotkeys).find(
        ([hotkey, actionId]) =>
          !claimed.has(hotkey.toLowerCase()) &&
          (actionId.toLowerCase() === scoped.toLowerCase() ||
            actionId.toLowerCase() === actionName.toLowerCase()),
      );
      if (mapping) claimed.add(mapping[0].toLowerCase());
      return {
        name: actionName,
        globalHotkey: mapping?.[0] ?? "",
        appHotkey: action.key_sequence.join("+"),
      };
    });

    return [
      {
        name,
        enabled: provider.enabled,
        icon: provider.icon,
        processNames: {
          windows: provider.process_names.windows || provider.process_name || "",
          macos: provider.process_names.macos || provider.process_name || "",
          linux: provider.process_names.linux || provider.process_name || "",
        },
        actions,
      },
    ];
  });
}

function buildConfig(): AICommanderConfig {
  if (!state.info) throw new Error("Configuration has not loaded.");

  const providers: Record<string, ProviderConfig> = {};
  const hotkeys: Record<string, string> = {};
  const enabledActionCounts = new Map<string, number>();

  for (const provider of state.providers.filter((item) => item.enabled)) {
    for (const action of provider.actions) {
      if (!action.globalHotkey.trim()) continue;
      const key = action.name.trim().toLowerCase();
      enabledActionCounts.set(key, (enabledActionCounts.get(key) ?? 0) + 1);
    }
  }

  for (const provider of state.providers) {
    const name = provider.name.trim();
    if (!name) throw new Error("Every application needs a name.");
    if (
      Object.keys(providers).some(
        (existing) => existing.toLowerCase() === name.toLowerCase(),
      )
    ) {
      throw new Error(`Application name "${name}" is duplicated.`);
    }

    const actions: ProviderConfig["actions"] = {};
    for (const action of provider.actions) {
      const actionName = action.name.trim();
      if (!actionName) throw new Error(`${name} has an unnamed action.`);
      if (
        Object.keys(actions).some(
          (existing) => existing.toLowerCase() === actionName.toLowerCase(),
        )
      ) {
        throw new Error(`Action "${name}.${actionName}" is duplicated.`);
      }
      actions[actionName] = { key_sequence: splitSequence(action.appHotkey) };

      if (provider.enabled && action.globalHotkey.trim()) {
        const gesture = normalizeHotkey(action.globalHotkey);
        if (
          Object.keys(hotkeys).some(
            (existing) => existing.toLowerCase() === gesture.toLowerCase(),
          )
        ) {
          throw new Error(`Global hotkey "${gesture}" is assigned twice.`);
        }
        hotkeys[gesture] =
          (enabledActionCounts.get(actionName.toLowerCase()) ?? 0) > 1
            ? `${name}.${actionName}`
            : actionName;
      }
    }

    providers[name] = {
      enabled: provider.enabled,
      process_name:
        provider.processNames[state.info.platform] ||
        provider.processNames.windows ||
        provider.processNames.macos ||
        provider.processNames.linux,
      process_names: { ...provider.processNames },
      icon: provider.icon,
      actions,
    };
  }

  return {
    version: 2,
    provider_priority: state.providers.map((provider) => provider.name.trim()),
    providers,
    hotkeys,
    settings: { ...state.info.config.settings },
  };
}

function render(): void {
  const info = state.info;
  if (!info) {
    app.innerHTML = `<main class="loading"><div class="spinner"></div><p>${escapeHtml(state.message)}</p></main>`;
    return;
  }

  const templates = [...providerTemplates, emptyTemplate]
    .map(
      (template) =>
        `<option value="${escapeHtml(template.id)}">${escapeHtml(template.label)}</option>`,
    )
    .join("");

  app.innerHTML = `
    <div class="app-shell">
      <header class="topbar">
        <div class="brand">
          <img src="/logo.png" alt="" />
          <div>
            <h1>AI Commander</h1>
            <p>Global actions for every coding agent</p>
          </div>
        </div>
        <div class="topbar-actions">
          <span class="platform-pill">${escapeHtml(info.platform)}</span>
          <button class="button button-primary" id="save-button" ${state.isSaving || !state.isDirty ? "disabled" : ""}>
            ${state.isSaving ? "Saving…" : state.isDirty ? "Save changes" : "Saved"}
          </button>
        </div>
      </header>

      <main>
        <section class="hero">
          <div>
            <span class="eyebrow">Configuration</span>
            <h2>Route one shortcut to the right application.</h2>
            <p>Applications are checked in priority order. The first running, visible application that supports an action receives its configured key sequence.</p>
          </div>
          <div class="hero-stat">
            <strong>${state.providers.filter((provider) => provider.enabled).length}</strong>
            <span>enabled apps</span>
          </div>
        </section>

        <section class="settings-strip" aria-label="Application settings">
          <label class="toggle-row">
            <input id="setting-autostart" type="checkbox" ${info.config.settings.auto_start_on_boot ? "checked" : ""} />
            <span class="switch"></span>
            <span><strong>Launch at login</strong><small>Keep shortcuts ready after sign-in</small></span>
          </label>
          <label class="toggle-row">
            <input id="setting-tray" type="checkbox" ${info.config.settings.show_tray_icon ? "checked" : ""} />
            <span class="switch"></span>
            <span><strong>Show tray icon</strong><small>Quick access while the window is closed</small></span>
          </label>
          <label class="toggle-row">
            <input id="setting-notifications" type="checkbox" ${info.config.settings.show_action_notifications ? "checked" : ""} />
            <span class="switch"></span>
            <span><strong>Action notifications</strong><small>Confirm successful dispatches</small></span>
          </label>
        </section>

        <section class="section-heading">
          <div>
            <span class="eyebrow">Priority order</span>
            <h2>Applications</h2>
          </div>
          <div class="add-control">
            <select id="template-select" aria-label="Application template">${templates}</select>
            <button class="button button-secondary" id="add-provider">Add application</button>
          </div>
        </section>

        <div class="provider-list">
          ${state.providers.map(renderProvider).join("")}
        </div>

        <footer>
          <div class="status ${state.messageKind}" role="status">
            <span class="status-dot"></span>
            ${escapeHtml(state.message)}
          </div>
          <div class="path">
            <span>Config</span>
            <code title="${escapeHtml(info.config_path)}">${escapeHtml(info.config_path)}</code>
          </div>
          <span>v${escapeHtml(info.version)}</span>
        </footer>
      </main>
    </div>
  `;

  bindEvents();
}

function renderProvider(provider: EditableProvider, index: number): string {
  const processFields = (["windows", "macos", "linux"] as const)
    .map(
      (platform) => `
        <label>
          <span>${platform}</span>
          <input data-field="process-${platform}" value="${escapeHtml(provider.processNames[platform])}" placeholder="${platform === "windows" ? "Code.exe" : platform === "macos" ? "Visual Studio Code" : "code"}" />
        </label>`,
    )
    .join("");

  return `
    <article class="provider-card ${provider.enabled ? "" : "is-disabled"}" data-provider-index="${index}">
      <div class="provider-header">
        <div class="provider-identity">
          <img src="${iconUrl()}" alt="" />
          <div>
            <input class="provider-name" data-field="name" value="${escapeHtml(provider.name)}" aria-label="Application name" />
            <span>Priority ${index + 1}</span>
          </div>
        </div>
        <div class="provider-controls">
          <button class="icon-button" data-action="move-up" title="Move up" ${index === 0 ? "disabled" : ""}>↑</button>
          <button class="icon-button" data-action="move-down" title="Move down" ${index === state.providers.length - 1 ? "disabled" : ""}>↓</button>
          <label class="compact-toggle" title="${provider.enabled ? "Disable" : "Enable"}">
            <input data-field="enabled" type="checkbox" ${provider.enabled ? "checked" : ""} />
            <span class="switch"></span>
          </label>
          <button class="icon-button danger" data-action="remove-provider" title="Remove application">×</button>
        </div>
      </div>

      <details>
        <summary>Process matching <span>Use platform-specific executable or application names</span></summary>
        <div class="process-grid">${processFields}</div>
      </details>

      <div class="actions-table">
        <div class="action-row action-head">
          <span>Logical action</span>
          <span>Global shortcut</span>
          <span>Keys sent to app</span>
          <span></span>
        </div>
        ${provider.actions.map((action, actionIndex) => renderAction(action, actionIndex)).join("")}
      </div>

      <button class="text-button" data-action="add-action">+ Add action</button>
    </article>
  `;
}

function renderAction(action: EditableAction, index: number): string {
  return `
    <div class="action-row" data-action-index="${index}">
      <input data-action-field="name" value="${escapeHtml(action.name)}" placeholder="accept" aria-label="Logical action name" />
      <input class="hotkey-input" data-action-field="globalHotkey" value="${escapeHtml(action.globalHotkey)}" placeholder="Click and press shortcut" readonly aria-label="Global shortcut" />
      <input class="hotkey-input" data-action-field="appHotkey" value="${escapeHtml(action.appHotkey)}" placeholder="Click and press shortcut" readonly aria-label="Application shortcut" />
      <div class="row-actions">
        <button class="icon-button" data-action="test-action" title="Run this action">▶</button>
        <button class="icon-button danger" data-action="remove-action" title="Remove action">×</button>
      </div>
    </div>
  `;
}

function markDirty(message = "Unsaved changes"): void {
  state.isDirty = true;
  state.message = message;
  state.messageKind = "info";
  render();
}

function bindEvents(): void {
  document.querySelector("#save-button")?.addEventListener("click", () => {
    void save();
  });
  document.querySelector("#add-provider")?.addEventListener("click", () => {
    const select = document.querySelector<HTMLSelectElement>("#template-select");
    const template =
      [...providerTemplates, emptyTemplate].find((item) => item.id === select?.value) ??
      emptyTemplate;
    addProvider(template);
  });

  bindSetting("setting-autostart", "auto_start_on_boot");
  bindSetting("setting-tray", "show_tray_icon");
  bindSetting("setting-notifications", "show_action_notifications");

  for (const card of document.querySelectorAll<HTMLElement>("[data-provider-index]")) {
    const providerIndex = Number(card.dataset.providerIndex);
    const provider = state.providers[providerIndex];
    if (!provider) continue;

    card
      .querySelector<HTMLInputElement>('[data-field="name"]')
      ?.addEventListener("change", (event) => {
        provider.name = (event.target as HTMLInputElement).value;
        markDirty();
      });
    card
      .querySelector<HTMLInputElement>('[data-field="enabled"]')
      ?.addEventListener("change", (event) => {
        provider.enabled = (event.target as HTMLInputElement).checked;
        markDirty();
      });

    for (const platform of ["windows", "macos", "linux"] as const) {
      card
        .querySelector<HTMLInputElement>(`[data-field="process-${platform}"]`)
        ?.addEventListener("change", (event) => {
          provider.processNames[platform] = (event.target as HTMLInputElement).value;
          markDirty();
        });
    }

    card.addEventListener("click", (event) => {
      const button = (event.target as HTMLElement).closest<HTMLButtonElement>(
        "button[data-action]",
      );
      if (!button) return;
      handleCardAction(button.dataset.action ?? "", providerIndex, button);
    });

    for (const row of card.querySelectorAll<HTMLElement>("[data-action-index]")) {
      const actionIndex = Number(row.dataset.actionIndex);
      const editableAction = provider.actions[actionIndex];
      if (!editableAction) continue;

      row
        .querySelector<HTMLInputElement>('[data-action-field="name"]')
        ?.addEventListener("change", (event) => {
          editableAction.name = (event.target as HTMLInputElement).value;
          markDirty();
        });

      for (const field of ["globalHotkey", "appHotkey"] as const) {
        const input = row.querySelector<HTMLInputElement>(
          `[data-action-field="${field}"]`,
        );
        input?.addEventListener("keydown", (event) => {
          event.preventDefault();
          if (event.key === "Backspace" || event.key === "Delete") {
            editableAction[field] = "";
            markDirty();
            return;
          }
          const hotkey = eventToHotkey(event);
          if (hotkey) {
            editableAction[field] = hotkey;
            markDirty();
          }
        });
        input?.addEventListener("focus", () => input.select());
      }
    }
  }
}

function bindSetting(id: string, key: keyof AppInfo["config"]["settings"]): void {
  document
    .querySelector<HTMLInputElement>(`#${id}`)
    ?.addEventListener("change", (event) => {
      if (!state.info) return;
      state.info.config.settings[key] = (event.target as HTMLInputElement).checked;
      markDirty();
    });
}

function handleCardAction(
  action: string,
  providerIndex: number,
  button: HTMLButtonElement,
): void {
  const provider = state.providers[providerIndex];
  if (!provider) return;
  if (action === "move-up" && providerIndex > 0) {
    [state.providers[providerIndex - 1], state.providers[providerIndex]] = [
      provider,
      state.providers[providerIndex - 1],
    ];
    markDirty();
  } else if (action === "move-down" && providerIndex < state.providers.length - 1) {
    [state.providers[providerIndex + 1], state.providers[providerIndex]] = [
      provider,
      state.providers[providerIndex + 1],
    ];
    markDirty();
  } else if (action === "remove-provider") {
    state.providers.splice(providerIndex, 1);
    markDirty(`${provider.name} removed; save to apply.`);
  } else if (action === "add-action") {
    provider.actions.push({
      name: "",
      globalHotkey: "",
      appHotkey: "Ctrl+Enter",
    });
    markDirty();
  } else if (action === "remove-action") {
    const row = button.closest<HTMLElement>("[data-action-index]");
    const actionIndex = Number(row?.dataset.actionIndex);
    provider.actions.splice(actionIndex, 1);
    markDirty();
  } else if (action === "test-action") {
    const row = button.closest<HTMLElement>("[data-action-index]");
    const actionIndex = Number(row?.dataset.actionIndex);
    const editableAction = provider.actions[actionIndex];
    if (editableAction) {
      void testAction(provider.name, editableAction.name);
    }
  }
}

function addProvider(template: ProviderTemplate): void {
  let name = template.id;
  let suffix = 2;
  while (
    state.providers.some(
      (provider) => provider.name.toLowerCase() === name.toLowerCase(),
    )
  ) {
    name = `${template.id}-${suffix++}`;
  }
  state.providers.push({
    name,
    enabled: true,
    icon: template.icon,
    processNames: { ...template.processNames },
    actions: Object.entries(template.actions).map(([actionName, hotkey]) => ({
      name: actionName,
      globalHotkey: "",
      appHotkey: hotkey,
    })),
  });
  markDirty(`${template.label} added; save to apply.`);
}

async function save(): Promise<void> {
  if (!state.info || state.isSaving) return;
  state.isSaving = true;
  state.message = "Validating and applying configuration…";
  state.messageKind = "info";
  render();
  try {
    const config = buildConfig();
    const saved = await invoke<AppInfo>("save_config", { config });
    state.info = saved;
    state.providers = toEditable(saved);
    state.isDirty = false;
    state.message = "Configuration saved and shortcuts reloaded.";
    state.messageKind = "success";
  } catch (error) {
    state.message = String(error);
    state.messageKind = "error";
  } finally {
    state.isSaving = false;
    render();
  }
}

async function testAction(providerName: string, actionName: string): Promise<void> {
  try {
    state.message = `Sending ${actionName} to ${providerName}…`;
    state.messageKind = "info";
    render();
    await invoke("dispatch_action", {
      actionId: `${providerName}.${actionName}`,
    });
    state.message = `Action ${actionName} dispatched to ${providerName}.`;
    state.messageKind = "success";
  } catch (error) {
    state.message = String(error);
    state.messageKind = "error";
  }
  render();
}

async function load(): Promise<void> {
  try {
    const info = await invoke<AppInfo>("get_app_info");
    state.info = info;
    state.providers = toEditable(info);
    state.message = "Shortcuts are active.";
    state.messageKind = "success";
    await listen<string>("action-triggered", (event) => {
      state.message = event.payload;
      state.messageKind = "success";
      render();
    });
  } catch (error) {
    state.message = `Failed to load configuration: ${String(error)}`;
    state.messageKind = "error";
  }
  render();
}

void load();
