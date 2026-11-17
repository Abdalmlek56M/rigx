import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { avatarProblem, initials, profileErrors, toProfileRequest } from "./profile-rules.ts";

describe("profile", () => {
  it("requires a name and validates a given mobile", () => {
    assert.deepEqual(profileErrors({ fullName: " ", phone: "" }), ["fullName"]);
    assert.deepEqual(profileErrors({ fullName: "منى", phone: "123" }), ["phone"]);
    assert.deepEqual(profileErrors({ fullName: "منى", phone: "" }), []);
  });
  it("builds the request with a normalized or empty phone", () => {
    assert.deepEqual(toProfileRequest({ fullName: " منى ", phone: "+201012345678" }), { fullName: "منى", phoneNumber: "01012345678" });
    assert.deepEqual(toProfileRequest({ fullName: "منى", phone: " " }), { fullName: "منى", phoneNumber: null });
  });
});

describe("avatarProblem", () => {
  it("accepts images and refuses other types or huge files", () => {
    assert.equal(avatarProblem({ size: 1000, type: "image/png" }), null);
    assert.equal(avatarProblem({ size: 1000, type: "application/pdf" }), "type");
    assert.equal(avatarProblem({ size: 13 * 1024 * 1024, type: "image/jpeg" }), "size");
  });
});

describe("initials", () => {
  it("takes two letters and falls back for an empty name", () => {
    assert.equal(initials("Mona Ali Hassan"), "MA");
    assert.equal(initials("منى"), "م");
    assert.equal(initials("  "), "؟");
  });
});
