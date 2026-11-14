import type { Schemas } from "@rigx/api-client";

/** One chosen part as the builder's API takes it. */
export interface PartInput {
  code: string;
  quantity: number;
}

export const MAX_PARTS = 8;
export const MAX_QUANTITY = 4;

const ITEM_CODE = /^RX-\d{1,9}$/;
const BUILD_CODE = /^BLD-\d{1,9}$/;
const SAVED_TOKEN = /^[A-Za-z0-9_-]{8,64}$/;

type RawParams = Record<string, string | string[] | undefined>;

const first = (value: string | string[] | undefined): string | undefined => (Array.isArray(value) ? value[0] : value);

/** `RX-1,RX-2:2` (a quantity after a colon) → parts; bad, repeated or surplus entries are dropped. */
export function parsePartsParam(value: string | undefined): PartInput[] {
  const parts: PartInput[] = [];
  for (const entry of (value ?? "").split(",")) {
    const [rawCode = "", rawQuantity] = entry.trim().split(":");
    const code = rawCode.toUpperCase();
    const quantity = rawQuantity === undefined ? 1 : Math.floor(Number(rawQuantity));
    if (!ITEM_CODE.test(code) || parts.some((part) => part.code === code)) continue;
    if (parts.length < MAX_PARTS) parts.push({ code, quantity: quantity >= 1 && quantity <= MAX_QUANTITY ? quantity : 1 });
  }
  return parts;
}

/** The `parts` URL value: `RX-1,RX-2:2`; empty when nothing is chosen. */
export function partsParam(parts: readonly PartInput[]): string {
  return parts.map((part) => (part.quantity > 1 ? `${part.code}:${part.quantity}` : part.code)).join(",");
}

export interface BuilderParams {
  parts: PartInput[];
  /** Token of a saved build to open. */
  saved: string | null;
  /** BLD code of a published build to start from ("عدّل فيها"). */
  from: string | null;
}

/** What the builder URL asks for: explicit parts, a saved build or a published build to edit. */
export function parseBuilderParams(params: RawParams): BuilderParams {
  const saved = first(params.saved)?.trim() ?? "";
  const from = first(params.from)?.trim().toUpperCase() ?? "";
  return {
    parts: parsePartsParam(first(params.parts)),
    saved: SAVED_TOKEN.test(saved) ? saved : null,
    from: BUILD_CODE.test(from) ? from : null
  };
}

function withParts(path: string, parts: readonly PartInput[]): string {
  const value = partsParam(parts);
  return value ? `${path}?parts=${encodeURIComponent(value)}` : path;
}

export const builderPath = (parts: readonly PartInput[]) => withParts("/builder", parts);

export const quotePath = (parts: readonly PartInput[]) => withParts("/builder/quote", parts);

/** The parts of an evaluation as the next request's input. */
export function partsOf(evaluation: Pick<Schemas["BuilderEvaluation"], "parts">): PartInput[] {
  return evaluation.parts.map((part) => ({ code: part.item.code, quantity: part.quantity }));
}

/** Puts `code` in `slot` (`null` empties it): any part already in that slot goes. `slotOf` says which slot each code is in. */
export function replaceInSlot(parts: readonly PartInput[], slotOf: ReadonlyMap<string, string>, slot: string, code: string | null): PartInput[] {
  const kept = parts.filter((part) => slotOf.get(part.code) !== slot && part.code !== code);
  return code ? [...kept, { code, quantity: 1 }] : kept;
}

export function removeCode(parts: readonly PartInput[], code: string): PartInput[] {
  return parts.filter((part) => part.code !== code);
}
