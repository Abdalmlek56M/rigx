import { defineConfig, devices } from "@playwright/test";
import { OWNER_STATE } from "./e2e/support/env";

export default defineConfig({
  testDir: "./e2e",
  // The flows share one database, so they run one after the other.
  workers: 1,
  fullyParallel: false,
  retries: 0,
  forbidOnly: true,
  reporter: [["list"], ["html", { outputFolder: "playwright-report", open: "never" }]],
  use: {
    baseURL: process.env.E2E_BASE_URL ?? "http://localhost:3000",
    locale: "ar-EG",
    trace: "on-first-retry"
  },
  projects: [
    { name: "seed", testMatch: "seed.setup.ts", use: { ...devices["Desktop Chrome"] } },
    // Signs the owner in through the login page and saves the state the other specs start from.
    { name: "login", testMatch: "login.spec.ts", dependencies: ["seed"], use: { ...devices["Desktop Chrome"] } },
    {
      name: "flows",
      testIgnore: ["login.spec.ts", "seed.setup.ts"],
      dependencies: ["login"],
      use: { ...devices["Desktop Chrome"], storageState: OWNER_STATE }
    }
  ]
});
