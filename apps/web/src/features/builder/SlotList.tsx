"use client";

import { useLocale, useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { IconButton } from "@/components/ui/Button";
import { cardClass } from "@/components/ui/Card";
import { CategoryIcon } from "@/components/ui/category-icon";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { WarrantyTag } from "@/components/ui/WarrantyTag";
import { CloseIcon } from "@/components/ui/icons";
import { QuantityStepper } from "@/components/ui/QuantityStepper";
import { parseCondition } from "@/lib/condition";
import { formatEgp } from "@/lib/format";
import { mediaUrl } from "@/lib/media";
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
 * The builder's slots with what is chosen in each. A slot with one model shows its photo, the model, the condition pill, the warranty and the
 * price; the slot being chosen has the same orange frame the drawing gives it; an empty one is dashed. Memory and fans carry a quantity
 * stepper; storage lists each drive with its own remove button.
 */
export function SlotList({ slots, parts, active, onSelect, onClear, onRemove, onQuantity }: SlotListProps) {
  const t = useTranslations("builder.slots");
  const locale = useLocale();

  return (
    <ul className="m-0 grid list-none gap-2 p-0 sm:grid-cols-2 lg:grid-cols-1">
      {slots.map((entry) => {
        const held = parts.filter((candidate) => candidate.slot === entry.slot);
        const several = takesSeveralParts(entry.slot);
        const single = held.length === 1 && !several ? held[0]! : null;
        const condition = single ? parseCondition(single.item.condition) : null;
        const isActive = entry.slot === active;
        const tone = isActive
          ? "border-orange! bg-orange-soft! shadow-[0_0_0_3px_var(--orange-soft)]"
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
              {single?.item.thumbnailStorageKey ? (
                <span className="flex size-11 shrink-0 overflow-hidden rounded-control border border-line bg-paper">
                  {/* eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite */}
                  <img src={mediaUrl(single.item.thumbnailStorageKey)} alt="" width={44} height={44} loading="lazy" className="product-photo h-full w-full object-cover" />
                </span>
              ) : (
                <span
                  aria-hidden="true"
                  className={`flex size-11 shrink-0 items-center justify-center rounded-full ${held.length > 0 || isActive ? "bg-orange-soft text-orange-text" : "bg-paper text-ink-muted"} ${isActive ? "bg-surface!" : ""}`}
                >
                  <CategoryIcon slug={entry.slot.toLowerCase()} size={20} />
                </span>
              )}
              <span className="flex min-w-0 flex-1 flex-col gap-1">
                <span className={`text-xs font-semibold ${isActive ? "text-orange-text" : "text-ink-muted"}`}>{entry.name}</span>
                <span dir={single ? "ltr" : undefined} className={`truncate text-start text-sm font-bold [unicode-bidi:isolate] ${held.length === 0 ? "font-medium text-ink-muted" : ""}`}>{summary}</span>
                {single ? (
                  <span className="flex flex-wrap items-center gap-x-2.5 gap-y-1">
                    {condition ? <ConditionBadge condition={condition} /> : null}
                    <WarrantyTag label={single.item.warrantyLabel} />
                    <b className="ms-auto whitespace-nowrap text-[13px] tabular-nums">{formatEgp(single.lineTotal, locale)}</b>
                  </span>
                ) : null}
              </span>
            </button>
            {held.length > 0 && !several ? (
              <IconButton
                icon={CloseIcon}
                label={t("remove", { slot: entry.name })}
                variant="ghost"
                size="sm"
                className="m-1.5 self-start text-ink-muted hover:bg-error/10 hover:text-error-text"
                onClick={() => onClear(entry.slot)}
              />
            ) : null}
            {single && takesQuantity(entry.slot) ? (
              <div className="col-span-2 flex items-center justify-between gap-3 border-t border-line px-3.5 py-2">
                <span className="text-xs text-ink-muted">{t("quantity")}</span>
                <QuantityStepper
                  size="sm"
                  value={single.quantity}
                  max={ceiling(single, held)}
                  onChange={(value) => onQuantity(entry.slot, single.item.code, value)}
                  label={t("quantityOf", { slot: entry.name })}
                  decreaseLabel={t("decrease", { slot: entry.name })}
                  increaseLabel={t("increase", { slot: entry.name })}
                />
              </div>
            ) : null}
            {several && held.length > 0 ? (
              <ul className="col-span-2 m-0 flex list-none flex-col border-t border-line p-0">
                {held.map((part) => {
                  const partCondition = parseCondition(part.item.condition);
                  return (
                    <li key={part.item.code} className="flex items-center gap-2 border-b border-line py-1.5 ps-3.5 pe-1.5 last:border-b-0">
                      <span className="flex min-w-0 flex-1 flex-col gap-1">
                        <span dir="ltr" className="truncate text-start text-[13px] font-bold [unicode-bidi:isolate]">{partText(part)}</span>
                        <span className="flex flex-wrap items-center gap-x-2.5 gap-y-1">
                          {partCondition ? <ConditionBadge condition={partCondition} /> : null}
                          <WarrantyTag label={part.item.warrantyLabel} />
                          <b className="ms-auto whitespace-nowrap text-[13px] tabular-nums">{formatEgp(part.lineTotal, locale)}</b>
                        </span>
                      </span>
                      <IconButton icon={CloseIcon} label={t("removeModel", { model: part.item.modelName })} variant="ghost" size="sm" className="text-ink-muted hover:bg-error/10 hover:text-error-text" onClick={() => onRemove(part.item.code)} />
                    </li>
                  );
                })}
              </ul>
            ) : null}
          </li>
        );
      })}
    </ul>
  );
}
