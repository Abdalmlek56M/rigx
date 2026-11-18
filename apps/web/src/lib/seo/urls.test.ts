import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { absoluteUrl, languageAlternates, localizedPath, parseLegacyProductPath, productPath, siteOrigin } from "./urls.ts";

const ORIGIN = "https://rigxhardware.com";

describe("site urls", () => {
  it("reads the origin without a trailing slash and falls back to localhost", () => {
    assert.equal(siteOrigin("https://rigxhardware.com/"), ORIGIN);
    assert.equal(siteOrigin(""), "http://localhost:3000");
    assert.equal(siteOrigin(undefined), "http://localhost:3000");
  });

  it("puts English under /en and keeps Arabic unprefixed", () => {
    assert.equal(localizedPath("ar", "/faq"), "/faq");
    assert.equal(localizedPath("en", "/faq"), "/en/faq");
    assert.equal(localizedPath("en", "/"), "/en");
    assert.equal(absoluteUrl(localizedPath("en", productPath("rtx-3060-12gb-pny")), ORIGIN), `${ORIGIN}/en/p/rtx-3060-12gb-pny`);
  });

  it("lists every locale and x-default (Arabic) as absolute alternates", () => {
    assert.deepEqual(languageAlternates("/products?category=gpu", ["ar", "en"], ORIGIN), {
      ar: `${ORIGIN}/products?category=gpu`,
      en: `${ORIGIN}/en/products?category=gpu`,
      "x-default": `${ORIGIN}/products?category=gpu`
    });
  });

  it("recognises old RX product links in any locale and nothing else", () => {
    assert.deepEqual(parseLegacyProductPath("/products/RX-0142"), { locale: "ar", code: "RX-0142" });
    assert.deepEqual(parseLegacyProductPath("/en/products/rx0142/"), { locale: "en", code: "rx0142" });
    assert.deepEqual(parseLegacyProductPath("/ar/products/RX-7"), { locale: "ar", code: "RX-7" });
    assert.equal(parseLegacyProductPath("/products"), null);
    assert.equal(parseLegacyProductPath("/products/rtx-3060"), null);
    assert.equal(parseLegacyProductPath("/p/rx-0142"), null);
  });
});
