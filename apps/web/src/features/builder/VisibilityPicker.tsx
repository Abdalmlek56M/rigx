"use client";

import { useId } from "react";
import { useTranslations } from "next-intl";
import { GlobeIcon, LinkIcon, LockIcon } from "@/components/ui/icons";
import { VISIBILITIES, type Visibility } from "./build-visibility";

const ICONS = { Private: LockIcon, Link: LinkIcon, Public: GlobeIcon } as const;

interface VisibilityPickerProps {
  value: Visibility;
  onChange: (value: Visibility) => void;
  /** A busy picker keeps its look but ignores clicks (a save is running). */
  busy?: boolean;
}

/** "من يرى هذه التجميعة؟": private / by link / public as three cards with an icon; the chosen one glows and its sentence shows underneath. */
export function VisibilityPicker({ value, onChange, busy = false }: VisibilityPickerProps) {
  const t = useTranslations("builder.visibility");
  const noteId = useId();

  return (
    <div className="flex flex-col gap-2">
      <div role="group" aria-label={t("title")} aria-describedby={noteId} className="grid grid-cols-3 gap-2">
        {VISIBILITIES.map((entry) => {
          const EntryIcon = ICONS[entry];
          return (
            <button
              key={entry}
              type="button"
              aria-pressed={value === entry}
              aria-busy={busy}
              onClick={() => !busy && onChange(entry)}
              className="choice-card press flex min-h-16 flex-col items-center justify-center gap-1 rounded-card border border-line bg-surface px-2 py-2 text-[13px] font-bold"
            >
              <span aria-hidden="true" className={value === entry ? "text-orange-text" : "text-ink-muted"}><EntryIcon size={18} /></span>
              {t(`options.${entry}.label`)}
            </button>
          );
        })}
      </div>
      <p id={noteId} className="type-label m-0">{t(`options.${value}.note`)}</p>
    </div>
  );
}
