import assert from "node:assert/strict";
import { afterEach, describe, it } from "node:test";
import { fetchInPageLanguage } from "./browser.ts";

const realFetch = globalThis.fetch;
const scope = globalThis as unknown as { document?: unknown };

afterEach(() => {
  globalThis.fetch = realFetch;
  delete scope.document;
});

describe("API calls in the page language", () => {
  it("sends the page language and keeps the method and body", async () => {
    scope.document = { documentElement: { lang: "ar" } };
    let seen: Request | undefined;
    globalThis.fetch = (async (input: Request) => { seen = input; return new Response("{}"); }) as typeof fetch;

    await fetchInPageLanguage("https://rigx.test/api/v1/x", { method: "POST", body: "{\"a\":1}", headers: { "Content-Type": "application/json" } });

    assert.equal(seen?.headers.get("Accept-Language"), "ar");
    assert.equal(seen?.method, "POST");
    assert.equal(await seen?.text(), "{\"a\":1}");
  });

  it("sends nothing when the page has no language", async () => {
    let seen: Request | undefined;
    globalThis.fetch = (async (input: Request) => { seen = input; return new Response("{}"); }) as typeof fetch;

    await fetchInPageLanguage("https://rigx.test/api/v1/x");

    assert.equal(seen?.headers.get("Accept-Language"), null);
  });
});
