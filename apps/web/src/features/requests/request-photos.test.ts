import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { MAX_PHOTOS, MAX_PHOTO_BYTES, addPhotos } from "./request-photos.ts";

const photo = (id: string, patch: { size?: number; type?: string } = {}) => ({ id, size: 1000, type: "image/jpeg", ...patch });

describe("addPhotos", () => {
  it("adds valid photos after the current ones", () => {
    const result = addPhotos([photo("a")], [photo("b"), photo("c", { type: "image/webp" })]);
    assert.deepEqual(result.photos.map((entry) => entry.id), ["a", "b", "c"]);
    assert.equal(result.problem, null);
  });
  it("does nothing for no incoming photos", () => {
    assert.deepEqual(addPhotos([photo("a")], []), { photos: [photo("a")], problem: null });
  });
  it("rejects other types", () => {
    const result = addPhotos([], [photo("a", { type: "image/gif" }), photo("b", { type: "" })]);
    assert.deepEqual([result.photos.length, result.problem], [0, "type"]);
  });
  it("accepts exactly the size limit and rejects one byte over", () => {
    assert.equal(addPhotos([], [photo("a", { size: MAX_PHOTO_BYTES })]).problem, null);
    const over = addPhotos([], [photo("a", { size: MAX_PHOTO_BYTES + 1 })]);
    assert.deepEqual([over.photos.length, over.problem], [0, "size"]);
  });
  it("stops at the maximum count and reports it", () => {
    const current = Array.from({ length: MAX_PHOTOS - 1 }, (_, index) => photo(`c${index}`));
    const result = addPhotos(current, [photo("x"), photo("y")]);
    assert.equal(result.photos.length, MAX_PHOTOS);
    assert.equal(result.problem, "count");
    assert.equal(addPhotos(current, [photo("x")]).problem, null);
  });
  it("reports the first problem but still adds the valid ones, without mutating the input", () => {
    const current = [photo("a")];
    const result = addPhotos(current, [photo("big", { size: MAX_PHOTO_BYTES + 1 }), photo("gif", { type: "image/gif" }), photo("ok")]);
    assert.equal(result.problem, "size");
    assert.deepEqual(result.photos.map((entry) => entry.id), ["a", "ok"]);
    assert.equal(current.length, 1);
  });
});
