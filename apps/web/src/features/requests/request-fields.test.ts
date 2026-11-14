import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { isEgyptMobile, isEmail, normalizePhone } from "./request-fields.ts";

describe("request fields", () => {
  it("normalizes Arabic digits and the +20 prefix", () => {
    assert.equal(normalizePhone("٠١٢٨ ٩١٨-٦٨٤٠"), "01289186840");
    assert.equal(normalizePhone("+20 128 918 6840"), "01289186840");
  });
  it("accepts Egyptian mobiles only", () => {
    assert.equal(isEgyptMobile("01289186840"), true);
    assert.equal(isEgyptMobile("+201289186840"), true);
    assert.equal(isEgyptMobile("0212345678"), false);
    assert.equal(isEgyptMobile("0128918684"), false);
  });
  it("checks the email shape", () => {
    assert.equal(isEmail("a@b.co"), true);
    assert.equal(isEmail("a@b"), false);
  });
});
