"use client";

import { useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { IconButton } from "@/components/ui/Button";
import { cardClass } from "@/components/ui/Card";
import { CategoryIcon } from "@/components/ui/category-icon";
import { CloseIcon } from "@/components/ui/icons";

export interface SlotInfo {
  slot: string;
  name: string;
}

interface SlotListProps {
  slots: SlotInfo[];
  parts: Schemas["BuilderPartView"][];
  active: string;
  onSelect: (slot: string) => void;
  onClear: (slot: string) => void;
}

/** The builder's slots with what is chosen in each; the active one is graphite, an empty one dashed. */
export function SlotList({ slots, parts, active, onSelect, onClear }: SlotListProps) {
  const t = useTranslations("builder.slots");

  return (
    <ul className="m-0 flex list-none flex-col gap-2 p-0">
      {slots.map((entry) => {
        const part = parts.find((candidate) => candidate.slot === entry.slot);
        const isActive = entry.slot === active;
        const tone = isActive
          ? "surface-strong border-transparent"
          : part
            ? ""
            : "border-dashed text-ink-muted";
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
                <span dir="ltr" className="truncate text-start text-sm font-bold [unicode-bidi:isolate]">
                  {part ? `${part.item.modelName}${part.quantity > 1 ? ` ×${part.quantity}` : ""}` : isActive ? t("choosing") : t("empty")}
                </span>
              </span>
            </button>
            {part ? (
              <IconButton
                icon={CloseIcon}
                label={t("remove", { slot: entry.name })}
                variant="ghost"
                size="md"
                className="m-1"
                onClick={() => onClear(entry.slot)}
              />
            ) : null}
          </li>
        );
      })}
    </ul>
  );
}
