import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { classifyCodeFailure, formatCountdown, loginHref, maskEmail, normalizeCode, safeNext } from "./auth-rules.ts";

describe("loginHref", () => {
  it("encodes the return path", () => {
    assert.equal(loginHref("/account/orders?x=1"), "/login?next=%2Faccount%2Forders%3Fx%3D1");
  });
});

describe("safeNext", () => {
  it("keeps same-site paths", () => {
    assert.equal(safeNext("/account/orders"), "/account/orders");
    assert.equal(safeNext("/checkout?step=2"), "/checkout?step=2");
  });
  it("falls back for anything else", () => {
    for (const value of [null, undefined, "", "account", "//evil.com", "/\\evil.com", "https://evil.com", "javascript:alert(1)", "/a\nb"]) {
      assert.equal(safeNext(value), "/account");
    }
  });
});

describe("maskEmail", () => {
  it("shows the first character and the domain", () => {
    assert.equal(maskEmail("mohamed@gmail.com"), "m••••@gmail.com");
  });
  it("leaves a malformed value alone", () => {
    assert.equal(maskEmail("nope"), "nope");
    assert.equal(maskEmail("@x.com"), "@x.com");
  });
});

describe("formatCountdown", () => {
  it("formats m:ss and never goes negative", () => {
    assert.equal(formatCountdown(42), "0:42");
    assert.equal(formatCountdown(65), "1:05");
    assert.equal(formatCountdown(-3), "0:00");
  });
});

describe("normalizeCode", () => {
  it("keeps six digits", () => {
    assert.equal(normalizeCode("47 19-02 99"), "471902");
    assert.equal(normalizeCode("ab12"), "12");
  });
});

describe("classifyCodeFailure", () => {
  it("counts the remaining attempts", () => {
    assert.deepEqual(classifyCodeFailure(401, 2), { kind: "wrongCode", attemptsLeft: 2 });
  });
  it("asks for a new code when attempts ran out or the code expired", () => {
    assert.deepEqual(classifyCodeFailure(401, 0), { kind: "tooManyAttempts" });
    assert.deepEqual(classifyCodeFailure(400, undefined), { kind: "expired" });
  });
  it("separates rate limits and server errors", () => {
    assert.deepEqual(classifyCodeFailure(429, undefined), { kind: "rateLimited" });
    assert.deepEqual(classifyCodeFailure(503, undefined), { kind: "generic" });
  });
});
