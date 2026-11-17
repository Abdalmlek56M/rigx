import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { orderStatusKey, trackingSteps } from "./tracking-steps.ts";

const timeline = [
  { status: "New", at: "2026-11-13T12:22:00Z" },
  { status: "Confirmed", at: "2026-11-13T14:05:00Z" }
];

const states = (steps: ReturnType<typeof trackingSteps>) => steps.map((step) => `${step.status}:${step.state}`);

describe("orderStatusKey", () => {
  it("maps the internal Completed status to Delivered and unknown ones to New", () => {
    assert.equal(orderStatusKey("Completed"), "Delivered");
    assert.equal(orderStatusKey("Shipped"), "Shipped");
    assert.equal(orderStatusKey("whatever"), "New");
  });
});

describe("trackingSteps", () => {
  it("draws five steps for delivery with the current one marked", () => {
    const steps = trackingSteps("Confirmed", "Delivery", timeline);
    assert.deepEqual(states(steps), ["New:done", "Confirmed:current", "Preparing:upcoming", "Shipped:upcoming", "Delivered:upcoming"]);
    assert.equal(steps[1]?.at, "2026-11-13T14:05:00Z");
    assert.equal(steps[2]?.at, null);
  });
  it("skips Shipped for pickup", () => {
    assert.deepEqual(states(trackingSteps("Preparing", "Pickup", timeline)), ["New:done", "Confirmed:done", "Preparing:current", "Delivered:upcoming"]);
  });
  it("shows every step done once delivered", () => {
    assert.ok(trackingSteps("Delivered", "Delivery", timeline).every((step) => step.state === "done"));
    assert.ok(trackingSteps("Completed", "Pickup", timeline).every((step) => step.state === "done"));
  });
  it("shows a single cancelled state", () => {
    const steps = trackingSteps("Cancelled", "Delivery", [...timeline, { status: "Cancelled", at: "2026-11-14T09:00:00Z" }]);
    assert.deepEqual(states(steps), ["Cancelled:current"]);
    assert.equal(steps[0]?.at, "2026-11-14T09:00:00Z");
  });
  it("falls back to the last dated step when the status is not in a pickup sequence", () => {
    assert.deepEqual(states(trackingSteps("Shipped", "Pickup", timeline)), ["New:done", "Confirmed:current", "Preparing:upcoming", "Delivered:upcoming"]);
  });
});
