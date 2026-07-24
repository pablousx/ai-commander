import { runNativeProcess } from "./native-process.ts";

runNativeProcess("cargo", process.argv.slice(2));
