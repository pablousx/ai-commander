const modifierOrder = ["Ctrl", "Alt", "Shift", "Win"] as const;

const keyAliases: Record<string, string> = {
  control: "Ctrl",
  ctrl: "Ctrl",
  alt: "Alt",
  option: "Alt",
  shift: "Shift",
  win: "Win",
  windows: "Win",
  super: "Win",
  meta: "Win",
  command: "Win",
  cmd: "Win",
  esc: "Escape",
  return: "Enter",
  arrowleft: "Left",
  arrowright: "Right",
  arrowup: "Up",
  arrowdown: "Down",
  oemopenbrackets: "BracketLeft",
  leftbracket: "BracketLeft",
  oemclosebrackets: "BracketRight",
  oem6: "BracketRight",
  rightbracket: "BracketRight",
  oemplus: "Equal",
  plus: "Equal",
};

export function normalizeHotkey(value: string): string {
  const rawParts = value
    .split("+")
    .map((part) => part.trim())
    .filter(Boolean);
  const modifiers = new Set<string>();
  let primary = "";

  for (const rawPart of rawParts) {
    const alias = keyAliases[rawPart.toLowerCase()] ?? rawPart;
    if (modifierOrder.includes(alias as (typeof modifierOrder)[number])) {
      modifiers.add(alias);
      continue;
    }
    if (primary) {
      throw new Error(`Hotkey "${value}" contains more than one primary key.`);
    }
    primary = alias.length === 1 ? alias.toUpperCase() : alias;
  }

  if (!primary) {
    throw new Error(`Hotkey "${value}" requires a primary key.`);
  }

  return [...modifierOrder.filter((modifier) => modifiers.has(modifier)), primary].join(
    "+",
  );
}

export function eventToHotkey(event: KeyboardEvent): string | null {
  const ignored = new Set(["Control", "Alt", "Shift", "Meta"]);
  if (ignored.has(event.key)) return null;

  const keys: string[] = [];
  if (event.ctrlKey) keys.push("Ctrl");
  if (event.altKey) keys.push("Alt");
  if (event.shiftKey) keys.push("Shift");
  if (event.metaKey) keys.push("Win");

  const codeAliases: Record<string, string> = {
    BracketLeft: "BracketLeft",
    BracketRight: "BracketRight",
    Equal: "Equal",
    NumpadAdd: "NumpadAdd",
    Space: "Space",
  };
  const key =
    codeAliases[event.code] ??
    (event.key.length === 1 ? event.key.toUpperCase() : event.key);
  keys.push(key);
  return keys.join("+");
}

export function splitSequence(value: string): string[] {
  return normalizeHotkey(value).split("+");
}
