export type ServiceId = "consult" | "build" | "test" | "maintenance" | "sourcing" | "upgrade";

export interface ServiceDef {
  id: ServiceId;
  /** `dark` is the highlighted card. */
  tone: "dark" | "light";
}

/** The services page cards in display order (copy, including the list of steps, in the `services.items` messages). */
export const SERVICES: readonly ServiceDef[] = [
  { id: "consult", tone: "dark" },
  { id: "build", tone: "light" },
  { id: "test", tone: "light" },
  { id: "maintenance", tone: "light" },
  { id: "sourcing", tone: "light" },
  { id: "upgrade", tone: "light" }
];

/** What the visitor wants to do with the part. */
export const SELL_WANTS = ["Sell", "TradeIn"] as const;

export type SellWant = (typeof SELL_WANTS)[number];

/** Part kinds a customer can offer, in the order of the store's main categories. */
export const PART_TYPES = ["gpu", "cpu", "board", "ram", "storage", "psu", "case", "cooling", "monitor", "accessory", "pc"] as const;

export type PartType = (typeof PART_TYPES)[number];

/** Customer-facing conditions are only "جديد" and "مستعمل" (01-business §2). */
export const PART_CONDITIONS = ["New", "Used"] as const;

export type PartCondition = (typeof PART_CONDITIONS)[number];

/** At most this many parts in one sell / trade-in request. */
export const MAX_PARTS = 6;

export interface OfferedPart {
  type: PartType;
  model: string;
  condition: PartCondition;
  ownedFor: string;
}

/** Labels for writing the parts into the request (from the messages, so staff read them in the shop's language). */
export interface PartLabels {
  type: (type: PartType) => string;
  condition: (condition: PartCondition) => string;
  ownedFor: (value: string) => string;
}

const MODEL_SUMMARY_MAX = 200;

/**
 * The request fields for the offered parts: the part types and models joined for the list columns, and one line per part
 * (type, model, condition, how long it was owned) in the message, so a request with several parts keeps every detail.
 */
export function describeParts(parts: readonly OfferedPart[], labels: PartLabels): { partType: string; modelName: string; message: string } {
  const filled = parts.map((part) => ({ ...part, model: part.model.trim(), ownedFor: part.ownedFor.trim() })).filter((part) => part.model);
  const models = filled.map((part) => part.model).join(" + ");
  return {
    partType: [...new Set(filled.map((part) => labels.type(part.type)))].join("، "),
    modelName: models.length > MODEL_SUMMARY_MAX ? `${models.slice(0, MODEL_SUMMARY_MAX - 1)}…` : models,
    message: filled
      .map((part, index) =>
        [`${index + 1}. ${labels.type(part.type)}: ${part.model}`, labels.condition(part.condition), part.ownedFor ? labels.ownedFor(part.ownedFor) : null]
          .filter(Boolean)
          .join(" · ")
      )
      .join("\n")
  };
}

/** A message that holds a list of texts (`t.raw`), as strings; anything else is an empty list. */
export function textList(raw: unknown): string[] {
  return Array.isArray(raw) ? raw.filter((entry): entry is string => typeof entry === "string") : [];
}
