import { describe, expect, it } from "vitest";

import { normalizeHotkey, splitSequence } from "./hotkeys";

describe("normalizeHotkey", () => {
  it.each([
    ["control+win+o", "Ctrl+Win+O"],
    ["Ctrl+OemPlus", "Ctrl+Equal"],
    ["Ctrl+Shift+Oem6", "Ctrl+Shift+BracketRight"],
    ["esc", "Escape"],
  ])("normalizes %s", (input, expected) => {
    expect(normalizeHotkey(input)).toBe(expected);
  });

  it("rejects modifier-only shortcuts", () => {
    expect(() => normalizeHotkey("Ctrl+Shift")).toThrow("requires a primary key");
  });
});

describe("splitSequence", () => {
  it("returns canonical key tokens", () => {
    expect(splitSequence("ctrl+i")).toEqual(["Ctrl", "I"]);
  });
});
