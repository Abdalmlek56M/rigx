/** Article body blocks ⇄ the simplest editable rows. Pure, so it is tested. */
import type { ArticleBlock } from "./articles-api";

export const ARTICLE_BLOCK_TYPES = ["heading", "paragraph", "list", "table", "image", "callout", "product", "youtube"] as const;
export type ArticleBlockTypeKey = (typeof ARTICLE_BLOCK_TYPES)[number];

export function blockTypeKey(type: string | null | undefined): ArticleBlockTypeKey {
  return ARTICLE_BLOCK_TYPES.find((known) => known === type) ?? "paragraph";
}

export interface BlockRow {
  /** A stable local identity (never sent to the API). */
  id: string;
  type: ArticleBlockTypeKey;
  /** Paragraph/callout text, a heading, or one list item / table row per line. */
  text: string;
  level: string;
  ordered: boolean;
  variant: string;
  imageKey: string;
  alt: string;
  caption: string;
  itemCode: string;
  youTube: string;
  /** The stored block this row was read from; saved back as it is while the row is unchanged (keeps bold and links). */
  original: ArticleBlock | null;
}

function spansText(spans: readonly { text?: string | null }[] | null | undefined): string {
  return (spans ?? []).map((span) => span.text ?? "").join("");
}

export function blockRows(blocks: readonly ArticleBlock[]): BlockRow[] {
  return blocks.map((block) => ({ ...fieldsOf(block), id: crypto.randomUUID(), original: block }));
}

type BlockFields = Omit<BlockRow, "id" | "original">;

function fieldsOf(block: ArticleBlock): BlockFields {
  const type = blockTypeKey(block.type);
  return {
    type,
    text: type === "list"
      ? (block.items ?? []).map((item) => spansText(item)).join("\n")
      : type === "table"
        ? (block.rows ?? []).map((row) => row.join(" | ")).join("\n")
        : type === "heading"
          ? block.text ?? ""
          : type === "paragraph" || type === "callout"
            ? spansText(block.spans)
            : "",
    level: String(block.level ?? 2),
    ordered: block.ordered ?? false,
    variant: block.variant ?? "info",
    imageKey: block.imageKey ?? "",
    alt: block.alt ?? "",
    caption: block.caption ?? "",
    itemCode: block.itemCode ?? "",
    youTube: block.youTubeId ?? ""
  };
}

function unchanged(row: BlockRow, original: ArticleBlock): boolean {
  const before = fieldsOf(original);
  return (Object.keys(before) as (keyof BlockFields)[]).every((key) => before[key] === row[key]);
}

export function emptyBlock(type: ArticleBlockTypeKey): BlockRow {
  return {
    id: crypto.randomUUID(), type, text: "", level: "2", ordered: false, variant: "info",
    imageKey: "", alt: "", caption: "", itemCode: "", youTube: "", original: null
  };
}

function lines(text: string): string[] {
  return text.split("\n").map((line) => line.trim()).filter(Boolean);
}

export function blockInput(row: BlockRow): ArticleBlock {
  if (row.original && unchanged(row, row.original)) return row.original;
  switch (row.type) {
    case "heading":
      return { type: "heading", level: Number(row.level) === 3 ? 3 : 2, text: row.text.trim() };
    case "paragraph":
      return { type: "paragraph", spans: [{ text: row.text, bold: false }] };
    case "list":
      return { type: "list", ordered: row.ordered, items: lines(row.text).map((line) => [{ text: line, bold: false }]) };
    case "table":
      return { type: "table", rows: lines(row.text).map((line) => line.split("|").map((cell) => cell.trim())) };
    case "image":
      return { type: "image", imageKey: row.imageKey.trim(), alt: row.alt.trim(), caption: row.caption.trim() === "" ? null : row.caption.trim() };
    case "callout":
      return { type: "callout", variant: row.variant, spans: [{ text: row.text, bold: false }] };
    case "product":
      return { type: "product", itemCode: row.itemCode.trim().toUpperCase() };
    case "youtube":
      return { type: "youtube", youTubeId: row.youTube.trim() };
  }
}

export function blockRowsInput(rows: readonly BlockRow[]): ArticleBlock[] {
  return rows.map(blockInput);
}

export function addBlock(rows: readonly BlockRow[], type: ArticleBlockTypeKey): BlockRow[] {
  return [...rows, emptyBlock(type)];
}

export function removeBlock(rows: readonly BlockRow[], index: number): BlockRow[] {
  return rows.filter((_, position) => position !== index);
}

/** Moves one row up (-1) or down (1); the ends keep the list unchanged. */
export function moveBlock(rows: readonly BlockRow[], index: number, delta: -1 | 1): BlockRow[] {
  const target = index + delta;
  if (index < 0 || index >= rows.length || target < 0 || target >= rows.length) return [...rows];
  const next = [...rows];
  const [row] = next.splice(index, 1);
  if (!row) return next;
  next.splice(target, 0, row);
  return next;
}

export function changeBlockType(row: BlockRow, type: ArticleBlockTypeKey): BlockRow {
  return { ...emptyBlock(type), id: row.id };
}
