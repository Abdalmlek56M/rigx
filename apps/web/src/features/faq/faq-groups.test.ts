import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { groupFaqs, privacyEmbedUrl, type FaqSource } from "./faq-groups.ts";

function faq(id: string, extra: Partial<FaqSource> = {}): FaqSource {
  return { id, questionAr: `س${id}`, questionEn: `q${id}`, answerAr: `ج${id}`, answerEn: `a${id}`, topic: null, scope: "General", youTubeEmbedUrl: null, ...extra };
}

describe("groupFaqs", () => {
  it("groups by topic in first-seen order and puts topic-less ones last", () => {
    const groups = groupFaqs([faq("1"), faq("2", { topic: "الضمان" }), faq("3", { topic: "الشحن" }), faq("4", { topic: "الضمان" })], false);
    assert.deepEqual(groups.map((group) => [group.topic, group.entries.map((entry) => entry.id)]), [
      ["الضمان", ["2", "4"]],
      ["الشحن", ["3"]],
      [null, ["1"]]
    ]);
  });
  it("keeps only general questions and follows the language", () => {
    const groups = groupFaqs([faq("1", { scope: "Item" }), faq("2"), faq("3", { questionEn: "" })], true);
    assert.deepEqual(groups[0]?.entries.map((entry) => entry.question), ["q2", "س3"]);
  });
  it("carries a safe video link", () => {
    const [group] = groupFaqs([faq("1", { youTubeEmbedUrl: "https://www.youtube.com/embed/abc" })], false);
    assert.equal(group?.entries[0]?.embedUrl, "https://www.youtube-nocookie.com/embed/abc");
  });
});

describe("privacyEmbedUrl", () => {
  it("accepts only https YouTube embed links", () => {
    assert.equal(privacyEmbedUrl("https://www.youtube.com/embed/abc?start=5"), "https://www.youtube-nocookie.com/embed/abc?start=5");
    assert.equal(privacyEmbedUrl("https://www.youtube-nocookie.com/embed/abc"), "https://www.youtube-nocookie.com/embed/abc");
    assert.equal(privacyEmbedUrl("https://evil.example/embed/abc"), null);
    assert.equal(privacyEmbedUrl("http://www.youtube.com/embed/abc"), null);
    assert.equal(privacyEmbedUrl("https://www.youtube.com/watch?v=abc"), null);
    assert.equal(privacyEmbedUrl("not a url"), null);
    assert.equal(privacyEmbedUrl(null), null);
  });
});
