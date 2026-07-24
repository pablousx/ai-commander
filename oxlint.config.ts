import { defineConfig } from "oxlint";

export default defineConfig({
  categories: {
    correctness: "error",
    suspicious: "error",
    perf: "warn",
  },
  plugins: ["typescript", "promise", "import", "unicorn", "vitest"],
  ignorePatterns: ["dist/**", "src-tauri/**"],
  options: {
    maxWarnings: 0,
  },
  rules: {
    "import/no-unassigned-import": "off",
    "typescript/no-explicit-any": "error",
    "unicorn/prefer-node-protocol": "error",
  },
});
