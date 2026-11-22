import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { imageFromItems, pastedFileName } from "./clipboard-image.ts";

const file = (type: string) => new File(["x"], "image.png", { type });
const item = (kind: string, type: string, content: File | null = file(type)) => ({ kind, type, getAsFile: () => content });

describe("pasted file names", () => {
  it("carry a stamp and the extension of the type", () => {
    const at = new Date("2026-11-22T21:05:09Z");
    assert.equal(pastedFileName("image/png", at), "pasted-20261122210509.png");
    assert.equal(pastedFileName("image/jpeg", at), "pasted-20261122210509.jpg");
    assert.equal(pastedFileName("image/webp", at), "pasted-20261122210509.webp");
    assert.equal(pastedFileName("image/gif", at), "pasted-20261122210509.png");
  });
});

describe("a picture in the pasted items", () => {
  it("takes the first accepted picture and keeps its type", () => {
    const result = imageFromItems([item("string", "text/plain"), item("file", "image/webp"), item("file", "image/png")]);
    assert.equal(result?.type, "image/webp");
    assert.match(result?.name ?? "", /^pasted-\d{14}\.webp$/);
  });

  it("ignores text, other files and types the upload does not take", () => {
    assert.equal(imageFromItems([item("string", "text/plain")]), null);
    assert.equal(imageFromItems([item("file", "application/pdf")]), null);
    assert.equal(imageFromItems([item("file", "image/gif")]), null);
    assert.equal(imageFromItems([item("file", "image/png", null)]), null);
    assert.equal(imageFromItems([]), null);
  });
});
