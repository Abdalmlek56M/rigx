import { defineConfig, devices } from "@playwright/test";

const webPort = process.env.E2E_WEB_PORT ?? "3000";
const baseUrl = `http://127.0.0.1:${webPort}`;

export default defineConfig({
  testDir: "./tests/e2e",
  timeout: 60_000,
  fullyParallel: true,
  workers: 1,
  reporter: "list",
  use: {
    baseURL: baseUrl,
    trace: "retain-on-failure",
    storageState: {
      cookies: [
        { name: "hardware-store-locale", value: "ar", domain: "127.0.0.1", path: "/", expires: -1, httpOnly: false, secure: false, sameSite: "Lax" as const },
      ],
      origins: [
        { origin: baseUrl, localStorage: [{ name: "hardware-store-locale", value: "ar" }] },
      ],
    },
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
  webServer: {
    command: `npm run dev -- --hostname 127.0.0.1 --port ${webPort}`,
    url: `${baseUrl}/api/health`,
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
});

