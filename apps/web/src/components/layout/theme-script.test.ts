import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { THEME_COOKIE, THEME_INIT_SCRIPT, parseTheme, themeCookie } from "./theme-script.ts";

/** Runs the head script against a fake page and returns what it left behind. */
function run(cookie: string, stored: string | null) {
  const root = { dataset: {} as Record<string, string> };
  const written: string[] = [];
  const document = {
    documentElement: root,
    get cookie() {
      return cookie;
    },
    set cookie(value: string) {
      written.push(value);
    }
  };
  new Function("document", "localStorage", THEME_INIT_SCRIPT)(document, { getItem: () => stored });
  return { theme: root.dataset.theme, written };
}

describe("theme", () => {
  it("reads only dark as dark", () => {
    assert.equal(parseTheme("dark"), "dark");
    assert.equal(parseTheme("light"), "light");
    assert.equal(parseTheme("blue"), "light");
    assert.equal(parseTheme(undefined), "light");
  });

  it("writes a year-long cookie the server can read", () => {
    assert.equal(themeCookie("dark"), `${THEME_COOKIE}=dark; path=/; max-age=31536000; samesite=lax`);
  });

  it("head script: the cookie wins over the stored choice", () => {
    assert.deepEqual(run("a=1; rigx.theme=dark", "light"), { theme: "dark", written: [] });
    assert.deepEqual(run("rigx.theme=light", "dark"), { theme: "light", written: [] });
  });

  it("head script: an old stored dark choice is copied into the cookie once", () => {
    assert.deepEqual(run("", "dark"), { theme: "dark", written: [themeCookie("dark")] });
  });

  it("head script: nothing saved is light and writes nothing", () => {
    assert.deepEqual(run("", null), { theme: "light", written: [] });
    assert.deepEqual(run("other=1", "weird"), { theme: "light", written: [] });
  });
});
