"use client";

import { useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
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

/** The builder's slots with what is chosen in each; the active one is dark, an empty one dashed. */
export function SlotList({ slots, parts, active, onSelect, onClear }: SlotListProps) {
  const t = useTranslations("builder.slots");

  return (
    <ul className="m-0 flex list-none flex-col gap-2 p-0">
      {slots.map((entry) => {
        const part = parts.find((candidate) => candidate.slot === entry.slot);
        const isActive = entry.slot === active;
        const tone = isActive
          ? "border-carbon bg-carbon text-on-carbon"
          : part
            ? "border-line bg-surface"
            : "border-dashed border-input-line text-ink-muted";
        return (
          <li key={entry.slot} className={`flex items-stretch rounded-card border ${tone}`}>
            <button
              type="button"
              aria-pressed={isActive}
              onClick={() => onSelect(entry.slot)}
              className="flex min-h-14 min-w-0 flex-1 items-center gap-3 px-3.5 py-3 text-start"
            >
              <span dir="ltr" className="w-11 shrink-0 font-mono text-xs font-semibold">{entry.slot}</span>
              <span className="flex min-w-0 flex-col gap-0.5">
                <span className="text-[13px] opacity-75">{entry.name}</span>
                <span dir="ltr" className="truncate text-start text-sm font-bold [unicode-bidi:isolate]">
                  {part ? `${part.item.modelName}${part.quantity > 1 ? ` ×${part.quantity}` : ""}` : isActive ? t("choosing") : t("empty")}
                </span>
              </span>
            </button>
            {part ? (
              <button
                type="button"
                aria-label={t("remove", { slot: entry.name })}
                onClick={() => onClear(entry.slot)}
                className="flex w-11 shrink-0 items-center justify-center"
              >
                <CloseIcon size={16} />
              </button>
            ) : null}
          </li>
        );
      })}
    </ul>
  );
}
