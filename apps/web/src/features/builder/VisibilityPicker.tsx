"use client";

import { useId } from "react";
import { useTranslations } from "next-intl";
import { GlobeIcon, LinkIcon, LockIcon } from "@/components/ui/icons";
import { VISIBILITIES, type Visibility } from "./build-visibility";
import { ChoiceTile } from "@/components/ui/ChoiceTile";

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
        {VISIBILITIES.map((entry) => (
          <ChoiceTile key={entry} icon={ICONS[entry]} label={t(`options.${entry}.label`)} selected={value === entry} onSelect={() => !busy && onChange(entry)} />
        ))}
      </div>
      <p id={noteId} className="type-label m-0">{t(`options.${value}.note`)}</p>
    </div>
  );
}
