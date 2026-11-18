import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { formMessage } from "./form-message.ts";

describe("form message", () => {
  it("prefers the general error", () => {
    assert.equal(formMessage({ fields: { name: "x" }, general: "down" }), "down");
  });

  it("shows a field error that has no input and skips the ones that do", () => {
    assert.equal(formMessage({ fields: { name: "Name is required", rowVersion: "Stale" }, general: null }, ["name"]), "Stale");
    assert.equal(formMessage({ fields: { name: "Name is required" }, general: null }, ["name"]), null);
  });

  it("is empty without errors", () => {
    assert.equal(formMessage({ fields: {}, general: null }), null);
  });
});
