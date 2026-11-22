import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { decisionsFor, parseStatus } from "./customer-build-rules.ts";

describe("parseStatus", () => {
  it("knows the three statuses and opens the queue for anything else", () => {
    assert.equal(parseStatus("Approved"), "Approved");
    assert.equal(parseStatus("Rejected"), "Rejected");
    assert.equal(parseStatus("Pending"), "Pending");
    assert.equal(parseStatus("Hidden"), "Pending");
    assert.equal(parseStatus(undefined), "Pending");
  });
});

describe("decisionsFor", () => {
  it("offers both decisions for a waiting build and only the other one for a decided build", () => {
    assert.deepEqual(decisionsFor("Pending"), ["Approved", "Rejected"]);
    assert.deepEqual(decisionsFor("Approved"), ["Rejected"]);
    assert.deepEqual(decisionsFor("Rejected"), ["Approved"]);
  });
});
