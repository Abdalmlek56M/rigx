import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { clearDraft, draftIsFresh, draftKeyFor, DRAFT_MAX_AGE_MS, readDraft, writeDraft, type DraftStorage } from "./use-form-draft.ts";

function storage(seed: Record<string, string> = {}) {
  const items = new Map(Object.entries(seed));
  return {
    items,
    getItem: (key: string) => items.get(key) ?? null,
    setItem: (key: string, value: string) => { items.set(key, value); },
    removeItem: (key: string) => { items.delete(key); }
  } satisfies DraftStorage & { items: Map<string, string> };
}

describe("draft storage", () => {
  it("builds one key per user and form", () => {
    assert.equal(draftKeyFor("preview-owner", "purchase.new"), "rigx.draft.preview-owner.purchase.new");
  });

  it("writes and reads a value with its save time", () => {
    const fake = storage();
    writeDraft(fake, "k", { price: "10" });
    const draft = readDraft<{ price: string }>(fake, "k");
    assert.equal(draft?.value.price, "10");
    assert.equal(typeof draft?.savedAt, "string");
    assert.ok(Date.parse(draft!.savedAt) > 0);
  });

  it("removes the draft", () => {
    const fake = storage({ k: JSON.stringify({ savedAt: new Date().toISOString(), value: 1 }) });
    clearDraft(fake, "k");
    assert.equal(fake.items.has("k"), false);
  });

  it("returns null for missing, malformed or incomplete JSON", () => {
    assert.equal(readDraft(storage(), "k"), null);
    assert.equal(readDraft(storage({ k: "not json" }), "k"), null);
    assert.equal(readDraft(storage({ k: JSON.stringify({ value: 1 }) }), "k"), null);
    assert.equal(readDraft(storage({ k: JSON.stringify({ savedAt: new Date().toISOString() }) }), "k"), null);
    assert.equal(readDraft(storage({ k: JSON.stringify({ savedAt: new Date().toISOString(), value: null }) }), "k"), null);
  });

  it("keeps working when storage is blocked", () => {
    const broken: DraftStorage = {
      getItem: () => { throw new Error("blocked"); },
      setItem: () => { throw new Error("blocked"); },
      removeItem: () => { throw new Error("blocked"); }
    };
    assert.equal(readDraft(broken, "k"), null);
    assert.doesNotThrow(() => writeDraft(broken, "k", 1));
    assert.doesNotThrow(() => clearDraft(broken, "k"));
  });
});

describe("draft age", () => {
  const now = Date.parse("2026-11-20T10:00:00Z");
  const draft = (savedAt: string) => ({ savedAt, value: 1 });

  it("offers a draft saved within seven days", () => {
    assert.equal(draftIsFresh(draft(new Date(now - 60_000).toISOString()), now), true);
    assert.equal(draftIsFresh(draft(new Date(now - DRAFT_MAX_AGE_MS + 1).toISOString()), now), true);
  });

  it("rejects an older draft and a future timestamp", () => {
    assert.equal(draftIsFresh(draft(new Date(now - DRAFT_MAX_AGE_MS - 1).toISOString()), now), false);
    assert.equal(draftIsFresh(draft(new Date(now + 60_000).toISOString()), now), false);
    assert.equal(draftIsFresh(draft("not a date"), now), false);
  });
});
