import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { cssString, invoicePageCss, pageCounterContent } from "./page-footer.ts";

describe("invoice page footer", () => {
  it("escapes what could end the string or the style element", () => {
    assert.equal(cssString('a"</style>'), '"a\\22 \\3c /style\\3e "');
  });
  it("puts the page counters into the translated pattern", () => {
    assert.equal(pageCounterContent("صفحة {page} من {pages}"), '"صفحة " counter(page) " من " counter(pages)');
  });
  it("puts the label on the reading side and the counter opposite", () => {
    const css = invoicePageCss("INV-1", "{page}/{pages}", "rtl");
    assert.match(css, /@bottom-right\{content:"INV-1"/);
    assert.match(css, /@bottom-left\{content:counter\(page\)/);
  });
});
