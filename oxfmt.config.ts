import { defineConfig } from "oxfmt";

export default defineConfig({
  printWidth: 88,
  singleQuote: false,
  ignorePatterns: [
    ".agents/skills/**",
    "dist/**",
    "node_modules/**",
    "src-tauri/target/**",
    "src-tauri/gen/**",
    "CHANGELOG.md",
    "pnpm-lock.yaml",
  ],
});
