import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { articleFilterStatus, articleStatusKey, articleStatusTone, ARTICLE_FILTERS, type ArticleStatusKey } from "./article-labels.ts";

describe("article status", () => {
  it("reads a published future article as scheduled", () => {
    const now = new Date("2026-11-18T12:00:00Z");
    assert.equal(articleStatusKey("Published", "2026-11-20T00:00:00Z", now), "Scheduled");
    assert.equal(articleStatusKey("Published", "2026-11-10T00:00:00Z", now), "Published");
    assert.equal(articleStatusKey("Published", null, now), "Published");
    assert.equal(articleStatusKey("Draft", null, now), "Draft");
    assert.equal(articleStatusKey("Archived", null, now), "Archived");
    assert.equal(articleStatusKey("Deleted", null, now), "Other");
  });

  it("maps each display status to its API filter", () => {
    assert.equal(articleFilterStatus("Scheduled"), "Scheduled");
    assert.equal(articleFilterStatus("Published"), "Published");
    assert.equal(articleFilterStatus("Other"), "");
    assert.deepEqual([...ARTICLE_FILTERS], ["", "Draft", "Published", "Scheduled"]);
  });

  it("gives each status a tone", () => {
    const keys: ArticleStatusKey[] = ["Published", "Scheduled", "Draft", "Archived", "Other"];
    assert.deepEqual(keys.map(articleStatusTone), ["live", "scheduled", "draft", "archived", "other"]);
  });
});
