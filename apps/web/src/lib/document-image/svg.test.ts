import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { firstFontFamily, fontFaceCss, fontFaceUrl, imageFileName, wrapInSvg } from "./svg.ts";

describe("firstFontFamily", () => {
  it("takes the first family and drops its quotes", () => {
    assert.equal(firstFontFamily('"IBM Plex Sans Arabic", "Fallback", sans-serif'), "IBM Plex Sans Arabic");
    assert.equal(firstFontFamily("'Alexandria', sans-serif"), "Alexandria");
    assert.equal(firstFontFamily("monospace"), "monospace");
    assert.equal(firstFontFamily(""), "");
  });
});

describe("fontFaceUrl", () => {
  it("finds the address with or without quotes and spaces", () => {
    assert.equal(fontFaceUrl('url("/_next/static/media/a.woff2") format("woff2")'), "/_next/static/media/a.woff2");
    assert.equal(fontFaceUrl("url( /x/b.woff2 )"), "/x/b.woff2");
    assert.equal(fontFaceUrl("url('https://cdn.test/c.woff2')"), "https://cdn.test/c.woff2");
    assert.equal(fontFaceUrl("local(Arial)"), null);
  });
});

describe("fontFaceCss", () => {
  it("writes the rule with the file inside, and the unicode range only when there is one", () => {
    const face = { family: "Plex", weight: "700", style: "normal", unicodeRange: "U+0600-06FF", data: "data:font/woff2;base64,AAAA" };
    assert.equal(fontFaceCss(face), '@font-face{font-family:"Plex";font-weight:700;font-style:normal;unicode-range:U+0600-06FF;src:url(data:font/woff2;base64,AAAA);}');
    assert.ok(!fontFaceCss({ ...face, unicodeRange: "" }).includes("unicode-range"));
  });
});

describe("wrapInSvg", () => {
  it("puts the markup in a foreignObject of the same size", () => {
    const svg = wrapInSvg("<div>hi</div>", "a{}", 1080, 1350);
    assert.ok(svg.startsWith('<svg xmlns="http://www.w3.org/2000/svg" width="1080" height="1350" viewBox="0 0 1080 1350">'));
    assert.ok(svg.includes('<foreignObject x="0" y="0" width="1080" height="1350"><div>hi</div></foreignObject>'));
    assert.ok(svg.includes("<style>a{}</style>"));
  });
});

describe("imageFileName", () => {
  it("is safe to save and falls back to a plain name", () => {
    assert.equal(imageFileName("quote", "Gaming PC / 2026"), "rigx-quote-Gaming-PC-2026.png");
    assert.equal(imageFileName("quote", "جهاز"), "rigx-quote.png");
    assert.equal(imageFileName("invoice", "INV-2026-0012"), "rigx-invoice-INV-2026-0012.png");
    assert.equal(imageFileName("invoice", ""), "rigx-invoice.png");
  });
});
