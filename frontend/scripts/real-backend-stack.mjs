import { spawn } from "node:child_process";
import net from "node:net";
import { fileURLToPath } from "node:url";
import path from "node:path";

const frontendDir = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  "..",
);
const repositoryDir = path.resolve(frontendDir, "..");
const composeFiles = [
  path.join(repositoryDir, "docker-compose.dev.yml"),
  path.join(frontendDir, "e2e/real/docker-compose.real-e2e.override.yml"),
];

export const realBackend = {
  project: "notrelix-fe-real-e2e",
  apiUrl: "http://127.0.0.1:58000/api/v1",
  healthUrl: "http://127.0.0.1:58000/health/live",
  appUrl: "http://127.0.0.1:5173",
  ports: [55432, 56379, 58000, 5173],
};

const composeEnv = {
  ...process.env,
  POSTGRES_PASSWORD:
    process.env.REAL_E2E_POSTGRES_PASSWORD ?? "real-e2e-postgres-password",
  REDIS_PASSWORD:
    process.env.REAL_E2E_REDIS_PASSWORD ?? "real-e2e-redis-password",
  POSTGRES_PORT: "55432",
  REDIS_PORT: "56379",
  BACKEND_PORT: "58000",
  FRONTEND_PORT: "5173",
};

function run(command, args, options = {}) {
  return new Promise((resolve, reject) => {
    const child = spawn(command, args, {
      cwd: repositoryDir,
      env: composeEnv,
      stdio: "inherit",
      ...options,
    });
    child.once("error", reject);
    child.once("exit", (code, signal) => {
      if (code === 0) resolve();
      else reject(new Error(`${command} exited with ${code ?? signal}`));
    });
  });
}

function composeArgs(args) {
  return [
    "compose",
    "--project-name",
    realBackend.project,
    ...composeFiles.flatMap((file) => ["-f", file]),
    ...args,
  ];
}

export function compose(args) {
  return run("docker", composeArgs(args));
}

export async function assertPortsAvailable(ports = realBackend.ports) {
  const occupied = [];
  for (const port of ports) {
    const available = await new Promise((resolve) => {
      const server = net.createServer();
      server.unref();
      server.once("error", () => resolve(false));
      server.listen({ host: "127.0.0.1", port }, () => {
        server.close(() => resolve(true));
      });
    });
    if (!available) occupied.push(port);
  }
  if (occupied.length > 0) {
    throw new Error(`Real E2E requires free ports: ${occupied.join(", ")}`);
  }
}

export async function waitForUrl(url, timeoutMs = 180_000) {
  const deadline = Date.now() + timeoutMs;
  let lastError;
  while (Date.now() < deadline) {
    try {
      const response = await fetch(url);
      if (response.ok) return;
      lastError = new Error(`${url} returned ${response.status}`);
    } catch (error) {
      lastError = error;
    }
    await new Promise((resolve) => setTimeout(resolve, 1_000));
  }
  throw new Error(
    `Timed out waiting for ${url}: ${lastError?.message ?? "unknown error"}`,
  );
}

export function start(command, args, options = {}) {
  return spawn(command, args, {
    cwd: frontendDir,
    env: process.env,
    stdio: "inherit",
    detached: process.platform !== "win32",
    ...options,
  });
}

export async function stopProcess(child) {
  if (!child || child.exitCode !== null) return;
  const signalTarget = process.platform === "win32" ? child.pid : -child.pid;
  process.kill(signalTarget, "SIGTERM");
  await Promise.race([
    new Promise((resolve) => child.once("exit", resolve)),
    new Promise((resolve) => setTimeout(resolve, 5_000)),
  ]);
  if (child.exitCode === null) process.kill(signalTarget, "SIGKILL");
}

export async function down() {
  await compose(["down", "-v", "--remove-orphans"]);
}

export async function up() {
  await compose(["up", "-d", "postgres", "redis", "backend"]);
}

export async function logs() {
  await compose(["logs", "--no-color", "postgres", "redis", "backend"]);
}

export function runFrontend(command, args, options = {}) {
  return run(command, args, { cwd: frontendDir, env: process.env, ...options });
}
