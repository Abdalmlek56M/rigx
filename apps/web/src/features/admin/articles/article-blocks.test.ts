import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { ArticleBlock } from "./articles-api.ts";
import {
  addBlock,
  blockInput,
  blockRows,
  blockRowsInput,
  blockTypeKey,
  changeBlockType,
  emptyBlock,
  moveBlock,
  removeBlock
} from "./article-blocks.ts";

describe("article blocks", () => {
  it("maps each stored block into a row and back", () => {
    const blocks: ArticleBlock[] = [
      { type: "heading", level: 3, text: "Title" },
      { type: "paragraph", spans: [{ text: "Hello world", bold: false }] },
      { type: "list", ordered: true, items: [[{ text: "one", bold: false }], [{ text: "two", bold: false }]] },
      { type: "table", rows: [["A", "B"], ["1", "2"]] },
      { type: "image", imageKey: "content/a.webp", alt: "A photo", caption: "Cap" },
      { type: "callout", variant: "warning", spans: [{ text: "Careful", bold: false }] },
      { type: "product", itemCode: "RX-0154" },
      { type: "youtube", youTubeId: "abc123" }
    ];
    const rows = blockRows(blocks);
    assert.deepEqual(rows.map((row) => row.type), ["heading", "paragraph", "list", "table", "image", "callout", "product", "youtube"]);
    assert.deepEqual([rows[0]?.text, rows[0]?.level], ["Title", "3"]);
    assert.equal(rows[1]?.text, "Hello world");
    assert.deepEqual([rows[2]?.text, rows[2]?.ordered], ["one\ntwo", true]);
    assert.equal(rows[3]?.text, "A | B\n1 | 2");
    assert.deepEqual(blockRowsInput(rows), blocks);
  });

  it("reads an unknown block as a paragraph but keeps the type list closed", () => {
    assert.equal(blockTypeKey("video"), "paragraph");
    const row = blockRows([{ type: "video", text: "x" }])[0]!;
    assert.equal(row.type, "paragraph");
  });

  it("builds the fields each type needs", () => {
    assert.deepEqual(blockInput({ ...emptyBlock("heading"), text: "H", level: "3" }), { type: "heading", level: 3, text: "H" });
    assert.deepEqual(blockInput({ ...emptyBlock("paragraph"), text: "P" }), { type: "paragraph", spans: [{ text: "P", bold: false }] });
    assert.deepEqual(blockInput({ ...emptyBlock("list"), text: "a\nb", ordered: true }), { type: "list", ordered: true, items: [[{ text: "a", bold: false }], [{ text: "b", bold: false }]] });
    assert.deepEqual(blockInput({ ...emptyBlock("table"), text: "A | B\n1 | 2" }), { type: "table", rows: [["A", "B"], ["1", "2"]] });
    assert.deepEqual(blockInput({ ...emptyBlock("product"), itemCode: " rx-1 " }), { type: "product", itemCode: "RX-1" });
    assert.deepEqual(blockInput({ ...emptyBlock("youtube"), youTube: " https://youtu.be/x " }), { type: "youtube", youTubeId: "https://youtu.be/x" });
  });

  it("adds, removes and moves rows", () => {
    const rows = [emptyBlock("paragraph"), emptyBlock("heading"), emptyBlock("list")];
    assert.equal(addBlock(rows, "callout").length, 4);
    assert.equal(removeBlock(rows, 1).length, 2);
    assert.deepEqual(moveBlock(rows, 1, -1).map((row) => row.type), ["heading", "paragraph", "list"]);
    assert.deepEqual(moveBlock(rows, 1, 1).map((row) => row.type), ["paragraph", "list", "heading"]);
    assert.deepEqual(moveBlock(rows, 0, -1).map((row) => row.type), ["paragraph", "heading", "list"]);
    assert.deepEqual(moveBlock(rows, 2, 1).map((row) => row.type), ["paragraph", "heading", "list"]);
    assert.deepEqual(changeBlockType(rows[0]!, "image").type, "image");
    assert.equal(changeBlockType(rows[0]!, "image").id, rows[0]!.id);
  });
});
