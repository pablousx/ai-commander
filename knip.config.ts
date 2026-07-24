import type { KnipConfig } from "knip";

export default {
  entry: ["scripts/*.ts"],
  project: ["src/**/*.ts!", "scripts/**/*.ts", "*.config.ts"],
  // The typed Tauri wrapper invokes `pnpm exec tauri` through spawnSync, which
  // Knip cannot resolve from the dynamically constructed argument array.
  ignoreDependencies: ["@tauri-apps/cli"],
} satisfies KnipConfig;
