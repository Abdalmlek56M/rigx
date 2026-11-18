import { test as setup } from "@playwright/test";
import { initializePreview, newApi } from "./support/api";

// Seeds the demo store (idempotent) so the flows find their data. The seeding signs every session out, so it runs before the login.
setup("the demo store is seeded", async () => {
  const api = await newApi();
  await initializePreview(api);
  await api.dispose();
});
