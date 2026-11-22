import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { blankToNull, feedbackProblems } from "./feedback-rules.ts";

const valid = { name: "أحمد علي", phone: "01012345678", email: "", message: "الرسالة طويلة بما يكفي للإرسال" };

describe("feedback form checks", () => {
  it("accepts a complete form, with or without an email", () => {
    assert.deepEqual(feedbackProblems(valid), []);
    assert.deepEqual(feedbackProblems({ ...valid, email: "a@example.com" }), []);
  });

  it("names every field that is wrong", () => {
    assert.deepEqual(feedbackProblems({ name: "أ", phone: "123", email: "not-an-email", message: "قصيرة" }), ["name", "phone", "email", "message"]);
  });

  it("takes Arabic digits and a +20 prefix in the phone", () => {
    assert.deepEqual(feedbackProblems({ ...valid, phone: "٠١٠١٢٣٤٥٦٧٨" }), []);
    assert.deepEqual(feedbackProblems({ ...valid, phone: "+201012345678" }), []);
  });

  it("sends an empty optional field as null", () => {
    assert.equal(blankToNull("  "), null);
    assert.equal(blankToNull(" طلب 12 "), "طلب 12");
  });
});
