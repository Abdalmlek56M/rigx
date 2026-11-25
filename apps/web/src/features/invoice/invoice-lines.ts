import type { Schemas } from "@rigx/api-client";

type Line = Schemas["InvoiceLineView"];
type Part = Schemas["InvoicePartView"];

export interface Warranty {
  label: string | null;
  endsAtUtc: string | null;
}

/** What the table says about one or more identical units: their kind, public codes, serials, condition(s) and warranty(ies). */
export interface UnitFacts {
  name: string;
  category: string | null;
  codes: string[];
  serials: string[];
  conditions: string[];
  warranties: Warranty[];
}

interface Money {
  no: number;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export type PartRow = UnitFacts & { quantity: number };
export type InvoiceRow =
  | (Money & UnitFacts & { kind: "item" })
  | (Money & { kind: "build"; name: string; parts: PartRow[] });

/** The store's build codes (`BLD-01`) are internal: a build is printed by its name alone, without the code or the punctuation around it. */
export function customerName(name: string): string {
  return name
    .replace(/[([]?\s*\bBLD-\d+\b\s*[)\]]?/gi, " ")
    .replace(/\s{2,}/g, " ")
    .replace(/^[\s\-–—:·|،,]+|[\s\-–—:·|،,]+$/g, "");
}

const unique = <T,>(values: readonly T[]): T[] => [...new Set(values)];

function warrantiesOf(parts: readonly Part[]): Warranty[] {
  const seen = new Map<string, Warranty>();
  for (const part of parts) {
    if (!part.warrantyLabel && !part.warrantyEndsAtUtc) continue;
    seen.set(`${part.warrantyLabel}|${part.warrantyEndsAtUtc}`, { label: part.warrantyLabel ?? null, endsAtUtc: part.warrantyEndsAtUtc ?? null });
  }
  return [...seen.values()];
}

function facts(name: string, parts: readonly Part[]): UnitFacts {
  return {
    name,
    category: parts[0]?.category ?? null,
    codes: unique(parts.map((part) => part.code)),
    serials: parts.flatMap((part) => (part.serialNumber ? [part.serialNumber] : [])),
    conditions: unique(parts.map((part) => part.condition)),
    warranties: warrantiesOf(parts)
  };
}

/** Identical parts of a build (two sticks of one memory kit) share a row with a quantity; their serials stay listed. */
function partRows(parts: readonly Part[]): PartRow[] {
  const groups = new Map<string, Part[]>();
  for (const part of parts) {
    const key = [part.code, part.name, part.condition, part.warrantyLabel, part.warrantyEndsAtUtc].join("|");
    groups.set(key, [...(groups.get(key) ?? []), part]);
  }
  return [...groups.values()].map((group) => ({ ...facts(group[0]?.name ?? "", group), quantity: group.length }));
}

/**
 * One printed entry per order line, numbered from 1. A line whose parts carry codes other than its own is a whole build: it is printed under
 * the build's name with its parts and a subtotal. Any other line (one item, however many units) is a single row.
 */
export function shapeLines(lines: readonly Line[]): InvoiceRow[] {
  return lines.map((line, index) => {
    const money = { no: index + 1, quantity: line.quantity, unitPrice: line.unitPrice, lineTotal: line.lineTotal };
    if (line.parts.some((part) => part.code !== line.code)) {
      return { ...money, kind: "build", name: customerName(line.name), parts: partRows(line.parts) };
    }
    return { ...money, ...facts(line.name, line.parts), codes: line.code ? [line.code] : [], kind: "item" };
  });
}

/** How many physical pieces the invoice covers (a build counts each of its parts). */
export function pieceCount(rows: readonly InvoiceRow[]): number {
  return rows.reduce((sum, row) => sum + (row.kind === "build" ? row.parts.reduce((parts, part) => parts + part.quantity, 0) : row.quantity), 0);
}

export function isVoid(status: string): boolean {
  return status === "Void";
}

/** The money rows under the lines: discount and refunds only when there are some. */
export function totalsRows(invoice: Pick<Schemas["InvoiceView"], "discount" | "refunded" | "paid" | "remaining">): { discount: boolean; refunded: boolean; payments: boolean } {
  return { discount: invoice.discount > 0, refunded: invoice.refunded > 0, payments: invoice.paid > 0 || invoice.remaining > 0 };
}
