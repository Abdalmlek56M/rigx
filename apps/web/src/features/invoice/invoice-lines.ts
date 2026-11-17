import type { Schemas } from "@rigx/api-client";

type Line = Schemas["InvoiceLineView"];
type Part = Schemas["InvoicePartView"];

export interface LineRow {
  /** The public code printed in the first column (a build's BLD code or the part's RX code). */
  code: string | null;
  name: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  /** A line for one part carries its serial and warranty under the name; a build lists its parts. */
  parts: Part[];
  /** Whether each part gets its own code/name row (a build) rather than only serial and warranty details. */
  listParts: boolean;
}

/** One printed row per order line. A single part sold alone is not listed twice; a build shows each part it includes. */
export function shapeLines(lines: readonly Line[]): LineRow[] {
  return lines.map((line) => ({
    code: line.code,
    name: line.name,
    quantity: line.quantity,
    unitPrice: line.unitPrice,
    lineTotal: line.lineTotal,
    parts: line.parts,
    listParts: !(line.parts.length === 1 && line.parts[0]?.code === line.code)
  }));
}

export function isVoid(status: string): boolean {
  return status === "Void";
}

/** The money rows under the lines: discount and refunds only when there are some. */
export function totalsRows(invoice: Pick<Schemas["InvoiceView"], "discount" | "refunded" | "paid" | "remaining">): { discount: boolean; refunded: boolean; payments: boolean } {
  return { discount: invoice.discount > 0, refunded: invoice.refunded > 0, payments: invoice.paid > 0 || invoice.remaining > 0 };
}
