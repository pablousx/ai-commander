import { spawnSync } from "node:child_process";
import { existsSync } from "node:fs";

export function runNativeProcess(command: string, arguments_: string[]): never {
  const environment: NodeJS.ProcessEnv = { ...process.env };
  if (process.platform === "linux" && existsSync("/usr/bin/pkg-config")) {
    environment.PKG_CONFIG = "/usr/bin/pkg-config";
    const systemPaths = "/usr/lib/x86_64-linux-gnu/pkgconfig:/usr/share/pkgconfig";
    environment.PKG_CONFIG_PATH = environment.PKG_CONFIG_PATH
      ? `${systemPaths}:${environment.PKG_CONFIG_PATH}`
      : systemPaths;
  }

  const result = spawnSync(command, arguments_, {
    env: environment,
    stdio: "inherit",
    shell: process.platform === "win32",
  });

  if (result.error) throw result.error;
  process.exit(result.status ?? 1);
}
