export type ServiceId = "consult" | "build" | "test" | "maintenance" | "sourcing" | "upgrade";

export interface ServiceDef {
  id: ServiceId;
  code: string;
  /** `dark` is the highlighted card. */
  tone: "dark" | "light";
}

/** The services page cards in display order (copy, including the list of steps, in the `services.items` messages). */
export const SERVICES: readonly ServiceDef[] = [
  { id: "consult", code: "01 · CONSULT", tone: "dark" },
  { id: "build", code: "02 · BUILD", tone: "light" },
  { id: "test", code: "06 · TEST", tone: "light" },
  { id: "maintenance", code: "03 · SERVICE", tone: "light" },
  { id: "sourcing", code: "04 · SOURCE", tone: "light" },
  { id: "upgrade", code: "05 · UPGRADE", tone: "light" }
];

/** Services not open yet: each gets an "interest" sign-up (copy in `services.soon.<id>`). */
export const COMING_SOON = ["business", "wholesale", "branch"] as const;

/** What the visitor wants to do with the part. */
export const SELL_WANTS = ["Sell", "TradeIn"] as const;

export type SellWant = (typeof SELL_WANTS)[number];

export const PART_TYPES = ["gpu", "cpu", "board", "pc"] as const;

export type PartType = (typeof PART_TYPES)[number];

/** A message that holds a list of texts (`t.raw`), as strings; anything else is an empty list. */
export function textList(raw: unknown): string[] {
  return Array.isArray(raw) ? raw.filter((entry): entry is string => typeof entry === "string") : [];
}
