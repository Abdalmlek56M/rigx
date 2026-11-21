export const POLICY_KINDS = [
  { slug: "shipping", api: "Shipping" },
  { slug: "returns", api: "Returns" },
  { slug: "exchanges", api: "Exchanges" },
  { slug: "warranty", api: "Warranty" },
  { slug: "terms", api: "Terms" },
  { slug: "privacy", api: "Privacy" }
] as const;

export type PolicySlug = (typeof POLICY_KINDS)[number]["slug"];
export type PolicyApiKind = (typeof POLICY_KINDS)[number]["api"];

/** The API kind behind a URL slug; `null` for anything that is not a policy page. */
export function policyApiKind(slug: string): PolicyApiKind | null {
  return POLICY_KINDS.find((kind) => kind.slug === slug)?.api ?? null;
}

export type PolicyBlock =
  | { type: "heading"; text: string }
  | { type: "paragraph"; lines: string[] }
  | { type: "list"; items: string[] };

export interface PolicyHeading {
  /** The anchor id the body gives this heading. */
  id: string;
  text: string;
}

const HEADING = /^#{1,6}\s+(.*)$/;
const BULLET = /^[-*]\s+(.*)$/;

/** Plain text with light markdown (`#` headings, `-` lists, blank-line paragraphs) as blocks; rendered as text, never as HTML. */
export function parsePolicyBody(body: string): PolicyBlock[] {
  const blocks: PolicyBlock[] = [];
  for (const chunk of body.replace(/\r\n?/g, "\n").split(/\n{2,}/)) {
    const lines = chunk.split("\n").map((line) => line.trim()).filter(Boolean);
    if (lines.length === 0) continue;
    const heading = HEADING.exec(lines[0] ?? "");
    if (heading) {
      blocks.push({ type: "heading", text: heading[1] ?? "" });
      lines.shift();
      if (lines.length === 0) continue;
    }
    const bullets = lines.map((line) => BULLET.exec(line)?.[1]);
    if (bullets.every((item): item is string => item !== undefined)) blocks.push({ type: "list", items: bullets });
    else blocks.push({ type: "paragraph", lines });
  }
  return blocks;
}

/** The id a heading gets in the body, by its order among the headings (stable, and safe for Arabic text). */
export function headingId(index: number): string {
  return `section-${index + 1}`;
}

/** The headings of a policy in reading order, for the desktop table of contents. */
export function policyHeadings(body: string): PolicyHeading[] {
  return parsePolicyBody(body)
    .filter((block): block is Extract<PolicyBlock, { type: "heading" }> => block.type === "heading")
    .map((block, index) => ({ id: headingId(index), text: block.text }));
}
