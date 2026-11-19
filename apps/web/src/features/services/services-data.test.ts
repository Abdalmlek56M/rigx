import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { describeParts, SERVICES, textList, type OfferedPart } from "./services-data.ts";

describe("textList", () => {
  it("keeps only the strings of an array", () => {
    assert.deepEqual(textList(["a", 1, null, "b", { x: 1 }]), ["a", "b"]);
    assert.deepEqual(textList([]), []);
  });
  it("is empty for anything that is not an array", () => {
    for (const value of [undefined, null, "text", 5, { 0: "a" }]) assert.deepEqual(textList(value), []);
  });
});

describe("SERVICES", () => {
  it("has unique ids and exactly one highlighted card", () => {
    assert.equal(new Set(SERVICES.map((service) => service.id)).size, SERVICES.length);
    assert.equal(SERVICES.filter((service) => service.tone === "accent").length, 1);
  });
});

describe("describeParts", () => {
  const labels = {
    type: (type: string) => ({ gpu: "كارت شاشة", cpu: "معالج" })[type] ?? type,
    condition: (condition: string) => (condition === "New" ? "جديد" : "مستعمل"),
    ownedFor: (value: string) => `معاه من ${value}`
  };
  const part = (fields: Partial<OfferedPart>): OfferedPart => ({ type: "gpu", model: "", condition: "Used", ownedFor: "", ...fields });

  it("writes one numbered line per part and joins the models and types", () => {
    const result = describeParts([part({ model: " RTX 3060 ", ownedFor: "سنة" }), part({ type: "cpu", model: "i5 12400", condition: "New" })], labels);
    assert.equal(result.modelName, "RTX 3060 + i5 12400");
    assert.equal(result.partType, "كارت شاشة، معالج");
    assert.equal(result.message, "1. كارت شاشة: RTX 3060 · مستعمل · معاه من سنة\n2. معالج: i5 12400 · جديد");
  });

  it("skips parts without a model and lists a repeated type once", () => {
    const result = describeParts([part({ model: "A" }), part({ model: "  " }), part({ model: "B" })], labels);
    assert.equal(result.partType, "كارت شاشة");
    assert.equal(result.message.split("\n").length, 2);
  });

  it("keeps the model summary within 200 characters", () => {
    const result = describeParts(Array.from({ length: 6 }, () => part({ model: "x".repeat(60) })), labels);
    assert.equal(result.modelName.length, 200);
    assert.ok(result.modelName.endsWith("…"));
  });
});
