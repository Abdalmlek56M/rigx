import { describe, expect, it } from "vitest";
import { countUnicodeScalars, passwordPolicyError } from "./password-policy";

const messages = {
  minimumLength: "too short",
  maximumLength: "too long",
};

describe("password policy", () => {
  it("counts Unicode scalar values while preserving spaces and long passphrases", () => {
    expect(countUnicodeScalars("😀😀")).toBe(2);
    expect(passwordPolicyError("a".repeat(14), messages)).toBe("too short");
    expect(passwordPolicyError(`${"a".repeat(14)} `, messages)).toBeNull();
    expect(passwordPolicyError("😀".repeat(128), messages)).toBeNull();
    expect(passwordPolicyError("😀".repeat(129), messages)).toBe("too long");
  });
});
