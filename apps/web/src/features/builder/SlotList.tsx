"use client";

import { useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { IconButton } from "@/components/ui/Button";
import { cardClass } from "@/components/ui/Card";
import { CategoryIcon } from "@/components/ui/category-icon";
import { CloseIcon } from "@/components/ui/icons";
import { QuantityStepper } from "@/components/ui/QuantityStepper";
import { slotLimit, takesQuantity, takesSeveralParts } from "./slot-limits";

export interface SlotInfo {
  slot: string;
  name: string;
}

type Part = Schemas["BuilderPartView"];

interface SlotListProps {
  slots: SlotInfo[];
  parts: Part[];
  active: string;
  onSelect: (slot: string) => void;
  /** Empties a slot that holds one model. */
  onClear: (slot: string) => void;
  /** Takes one model out of a slot that holds several (storage). */
  onRemove: (code: string) => void;
  onQuantity: (slot: string, code: string, quantity: number) => void;
}

/** What a part is called on its row: the model, then "×3" when there are several of it. */
const partText = (part: Part) => `${part.item.modelName}${part.quantity > 1 ? ` ×${part.quantity}` : ""}`;

/** The most of `part` the slot has room for and the store has in stock. */
function ceiling(part: Part, held: readonly Part[]): number {
  const others = held.filter((entry) => entry.item.code !== part.item.code).reduce((sum, entry) => sum + entry.quantity, 0);
  return Math.max(1, Math.min(slotLimit(part.slot).units - others, part.item.available));
}

/**
 * The builder's slots with what is chosen in each; the active one is graphite, an empty one dashed. Memory and fans carry a quantity
 * stepper; storage lists each drive with its own remove button.
 */
export function SlotList({ slots, parts, active, onSelect, onClear, onRemove, onQuantity }: SlotListProps) {
  const t = useTranslations("builder.slots");

  return (
    <ul className="m-0 grid list-none gap-2 p-0 sm:grid-cols-2 lg:grid-cols-1">
      {slots.map((entry) => {
        const held = parts.filter((candidate) => candidate.slot === entry.slot);
        const several = takesSeveralParts(entry.slot);
        const isActive = entry.slot === active;
        const tone = isActive
          ? "bg-carbon! text-on-carbon! border-transparent"
          : held.length > 0
            ? ""
            : "border-dashed text-ink-muted";
        const summary =
          held.length === 0
            ? isActive
              ? t("choosing")
              : t("empty")
            : several
              ? t("units", { count: held.reduce((sum, part) => sum + part.quantity, 0), max: slotLimit(entry.slot).units })
              : partText(held[0]!);
        return (
          <li key={entry.slot} className={`${cardClass({ padding: "none", layout: "grid", className: "grid-cols-[minmax(0,1fr)_auto] items-stretch" })} ${tone}`}>
            <button
              type="button"
              aria-pressed={isActive}
              onClick={() => onSelect(entry.slot)}
              className="flex min-h-14 min-w-0 flex-1 items-center gap-3 px-3.5 py-3 text-start"
            >
              <span aria-hidden="true" className="flex size-9 shrink-0 items-center justify-center rounded-full bg-paper/60 text-current">
                <CategoryIcon slug={entry.slot.toLowerCase()} size={18} />
              </span>
              <span className="flex min-w-0 flex-col gap-0.5">
                <span className="text-xs opacity-75">{entry.name}</span>
                <span dir={held.length === 1 ? "ltr" : undefined} className="truncate text-start text-sm font-bold [unicode-bidi:isolate]">{summary}</span>
              </span>
            </button>
            {held.length > 0 && !several ? (
              <IconButton icon={CloseIcon} label={t("remove", { slot: entry.name })} variant="ghost" size="md" className="m-1" onClick={() => onClear(entry.slot)} />
            ) : null}
            {held.length === 1 && takesQuantity(entry.slot) ? (
              <div className="col-span-2 flex items-center justify-between gap-3 border-t border-line px-3.5 py-2">
                <span className="text-xs opacity-75">{t("quantity")}</span>
                <QuantityStepper
                  size="sm"
                  value={held[0]!.quantity}
                  max={ceiling(held[0]!, held)}
                  onChange={(value) => onQuantity(entry.slot, held[0]!.item.code, value)}
                  label={t("quantityOf", { slot: entry.name })}
                  decreaseLabel={t("decrease", { slot: entry.name })}
                  increaseLabel={t("increase", { slot: entry.name })}
                />
              </div>
            ) : null}
            {several && held.length > 0 ? (
              <ul className="col-span-2 m-0 flex list-none flex-col border-t border-line p-0">
                {held.map((part) => (
                  <li key={part.item.code} className="flex items-center justify-between gap-2 ps-3.5 pe-1.5">
                    <span dir="ltr" className="min-w-0 truncate text-start text-[13px] [unicode-bidi:isolate]">{partText(part)}</span>
                    <IconButton icon={CloseIcon} label={t("removeModel", { model: part.item.modelName })} variant="ghost" size="sm" onClick={() => onRemove(part.item.code)} />
                  </li>
                ))}
              </ul>
            ) : null}
          </li>
        );
      })}
    </ul>
  );
}
