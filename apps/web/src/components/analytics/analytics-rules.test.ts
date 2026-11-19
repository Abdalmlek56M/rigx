import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { cleanId, GA_ID_PATTERN, isMeasuredPath, isWhatsAppLink, META_PIXEL_ID_PATTERN } from "./analytics-rules.ts";

describe("analytics rules", () => {
  it("never measures admin or POS pages, in either locale", () => {
    for (const path of ["/admin", "/admin/pos", "/en/admin/orders"]) assert.equal(isMeasuredPath(path), false, path);
    for (const path of ["/", "/en", "/p/rtx-3060", "/products", "/administration-guide"]) assert.equal(isMeasuredPath(path), true, path);
  });

  it("counts WhatsApp chat links as leads", () => {
    assert.equal(isWhatsAppLink("https://wa.me/201000000000?text=hi"), true);
    assert.equal(isWhatsAppLink("https://api.whatsapp.com/send?phone=20100"), true);
    assert.equal(isWhatsAppLink("https://rigxhardware.com/p/x"), false);
    assert.equal(isWhatsAppLink("/relative"), false);
    assert.equal(isWhatsAppLink(undefined), false);
  });

  it("accepts only well-formed ids", () => {
    assert.equal(cleanId(" G-AB12CD ", GA_ID_PATTERN), "G-AB12CD");
    assert.equal(cleanId("G-x');alert(1)//", GA_ID_PATTERN), null);
    assert.equal(cleanId("123456789012345", META_PIXEL_ID_PATTERN), "123456789012345");
    assert.equal(cleanId("", META_PIXEL_ID_PATTERN), null);
  });
});
