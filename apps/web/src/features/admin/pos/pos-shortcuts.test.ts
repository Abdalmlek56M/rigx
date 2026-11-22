import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { POS_SHORTCUT_KEYS, posShortcut, type PosKeyEvent } from "./pos-shortcuts.ts";

const key = (patch: Partial<PosKeyEvent>): PosKeyEvent => ({ key: "", code: "", ctrlKey: false, metaKey: false, altKey: false, shiftKey: false, ...patch });

describe("posShortcut", () => {
  it("F2 and / jump to the item search, F4 to the customer search", () => {
    assert.equal(posShortcut(key({ key: "F2" }), false), "focusItems");
    assert.equal(posShortcut(key({ key: "/", code: "Slash" }), false), "focusItems");
    assert.equal(posShortcut(key({ key: "F4" }), false), "focusCustomer");
  });
  it("/ works on any keyboard layout, because it follows the physical key", () => {
    assert.equal(posShortcut(key({ key: "ظ", code: "Slash" }), false), "focusItems");
    assert.equal(posShortcut(key({ key: "؟", code: "Slash", shiftKey: true }), false), "help");
  });
  it("Ctrl+Enter confirms and Ctrl+Shift+Enter saves as pending, also while typing", () => {
    assert.equal(posShortcut(key({ key: "Enter", ctrlKey: true }), true), "confirm");
    assert.equal(posShortcut(key({ key: "Enter", metaKey: true }), true), "confirm");
    assert.equal(posShortcut(key({ key: "Enter", ctrlKey: true, shiftKey: true }), true), "savePending");
  });
  it("leaves / and ? as text while a field has the focus, but F-keys still act", () => {
    assert.equal(posShortcut(key({ key: "/", code: "Slash" }), true), null);
    assert.equal(posShortcut(key({ key: "?", code: "Slash", shiftKey: true }), true), null);
    assert.equal(posShortcut(key({ key: "F2" }), true), "focusItems");
  });
  it("ignores Alt combinations, other Ctrl keys and plain Enter", () => {
    assert.equal(posShortcut(key({ key: "F2", altKey: true }), false), null);
    assert.equal(posShortcut(key({ key: "s", ctrlKey: true }), false), null);
    assert.equal(posShortcut(key({ key: "Enter" }), false), null);
  });
  it("lists every shortcut once for the legend", () => {
    assert.deepEqual(POS_SHORTCUT_KEYS.map((entry) => entry.shortcut), ["focusItems", "focusCustomer", "confirm", "savePending", "help"]);
  });
});
