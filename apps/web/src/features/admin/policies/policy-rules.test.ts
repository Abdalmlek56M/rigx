import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { Policy } from "./policies-api.ts";
import {
  emptyPolicyForm, isEditable, kindSummary, policyForm, policyPublishable, policySavable, policyStatusKey, sameForm, toPolicyInput
} from "./policy-rules.ts";

const policy = (patch: Partial<Policy> = {}): Policy => ({
  id: "1", kind: "Returns", titleAr: "الاسترجاع", titleEn: "Returns", bodyAr: "نص", bodyEn: "Text", status: "Draft",
  versionNumber: 1, publishedAtUtc: null, createdAtUtc: "2026-11-01T00:00:00Z", updatedAtUtc: "2026-11-01T00:00:00Z",
  rowVersion: 1, ...patch
});

describe("policy status", () => {
  it("maps known statuses, falls back, and only drafts and reviews are editable", () => {
    assert.equal(policyStatusKey("Published"), "Published");
    assert.equal(policyStatusKey("Gone"), "Other");
    assert.deepEqual(["Draft", "Review", "Published", "Archived"].map((status) => isEditable(policy({ status }))), [true, true, false, false]);
  });
});

describe("kindSummary", () => {
  const all = [
    policy({ id: "a", versionNumber: 1, status: "Archived" }),
    policy({ id: "b", versionNumber: 2, status: "Published" }),
    policy({ id: "c", versionNumber: 3, status: "Draft" }),
    policy({ id: "d", kind: "Terms", versionNumber: 1, status: "Published" })
  ];
  it("finds the live version, the waiting draft and the history newest first", () => {
    const summary = kindSummary("Returns", all);
    assert.deepEqual([summary.published?.id, summary.draft?.id, summary.versions.map((v) => v.id)], ["b", "c", ["c", "b", "a"]]);
  });
  it("is empty for a kind with no versions and has no draft when none waits", () => {
    assert.deepEqual(kindSummary("Privacy", all), { published: null, draft: null, versions: [] });
    assert.equal(kindSummary("Terms", all).draft, null);
  });
});

describe("policy form", () => {
  it("maps a view to the form and back with trimmed text", () => {
    assert.deepEqual(toPolicyInput({ titleAr: " ع ", titleEn: " T ", bodyAr: " ب\n", bodyEn: " B " }, 7),
      { titleAr: "ع", titleEn: "T", bodyAr: "ب", bodyEn: "B", rowVersion: 7 });
    assert.deepEqual(policyForm(policy()), { titleAr: "الاسترجاع", titleEn: "Returns", bodyAr: "نص", bodyEn: "Text" });
  });
  it("saves with both titles and publishes only with both bodies too", () => {
    assert.equal(policySavable(emptyPolicyForm()), false);
    const draft = { ...emptyPolicyForm(), titleAr: "ع", titleEn: "T" };
    assert.deepEqual([policySavable(draft), policyPublishable(draft)], [true, false]);
    assert.equal(policyPublishable({ ...draft, bodyAr: "ب", bodyEn: " " }), false);
    assert.equal(policyPublishable({ ...draft, bodyAr: "ب", bodyEn: "B" }), true);
  });
  it("compares forms field by field", () => {
    assert.equal(sameForm(emptyPolicyForm(), emptyPolicyForm()), true);
    assert.equal(sameForm(emptyPolicyForm(), { ...emptyPolicyForm(), bodyEn: "x" }), false);
  });
});
