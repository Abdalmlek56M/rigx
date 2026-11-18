import { readFileSync } from "node:fs";

/** The Arabic copy the customer and the owner read, so locators follow `messages/ar.json` instead of repeating it. */
export const ar = JSON.parse(readFileSync(new URL("../../messages/ar.json", import.meta.url), "utf8")) as typeof import("../../messages/ar.json");
