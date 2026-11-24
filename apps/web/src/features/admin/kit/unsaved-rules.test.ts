import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { leavingTarget, type AnchorInfo, type ClickInfo } from "./unsaved-rules.ts";

const click: ClickInfo = { button: 0, metaKey: false, ctrlKey: false, shiftKey: false, altKey: false, defaultPrevented: false };
const here = { origin: "https://store.test", pathname: "/admin/settings", search: "?section=shipping" };
const link = (href: string, extra: Partial<AnchorInfo> = {}): AnchorInfo => ({ href, target: null, download: false, ...extra });

describe("which clicks leave a screen with unsaved edits", () => {
  it("catches a plain click on a link to another page of the site", () => {
    assert.equal(leavingTarget(click, link("/admin/orders?status=New#top"), here), "/admin/orders?status=New#top");
    assert.equal(leavingTarget(click, link("https://store.test/admin/orders"), here), "/admin/orders");
    assert.equal(leavingTarget(click, link("/admin/settings?section=payment"), here), "/admin/settings?section=payment");
  });

  it("lets the click through when it stays on this page, goes to another site or opens elsewhere", () => {
    assert.equal(leavingTarget(click, link("/admin/settings?section=shipping#fees"), here), null);
    assert.equal(leavingTarget(click, link("https://example.com/x"), here), null);
    assert.equal(leavingTarget(click, link("/admin/orders", { target: "_blank" }), here), null);
    assert.equal(leavingTarget(click, link("/admin/orders", { download: true }), here), null);
    assert.equal(leavingTarget(click, link("/admin/orders", { target: "_self" }), here), "/admin/orders");
  });

  it("ignores clicks that are not a plain left click", () => {
    assert.equal(leavingTarget({ ...click, button: 1 }, link("/admin/orders"), here), null);
    assert.equal(leavingTarget({ ...click, ctrlKey: true }, link("/admin/orders"), here), null);
    assert.equal(leavingTarget({ ...click, metaKey: true }, link("/admin/orders"), here), null);
    assert.equal(leavingTarget({ ...click, shiftKey: true }, link("/admin/orders"), here), null);
    assert.equal(leavingTarget({ ...click, defaultPrevented: true }, link("/admin/orders"), here), null);
  });

  it("does not trip on a link that is not a valid address", () => {
    assert.equal(leavingTarget(click, link("http://"), here), null);
  });
});
