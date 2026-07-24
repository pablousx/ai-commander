import { runNativeProcess } from "./native-process.ts";

runNativeProcess("pnpm", ["exec", "tauri", ...process.argv.slice(2)]);
