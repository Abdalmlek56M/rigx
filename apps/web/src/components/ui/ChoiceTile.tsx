import type { ComponentType, ReactNode } from "react";
import { Link } from "@/i18n/navigation";

type TileIcon = ComponentType<{ size?: number }>;

interface TileLook {
  icon: TileIcon;
  label: string;
  /** A small line under the label (a price, a sentence). */
  note?: string;
  /** A pill after the label (a count). */
  badge?: ReactNode;
  selected: boolean;
  className?: string;
}

const TILE =
  "choice-card press relative flex min-h-[84px] min-w-0 cursor-pointer flex-col items-center justify-center gap-1.5 rounded-card border border-line bg-surface px-3 py-3 text-center";

function TileBody({ icon: TileIconComponent, label, note, badge, selected }: TileLook) {
  return (
    <>
      <span aria-hidden="true" className={`flex size-9 items-center justify-center rounded-full transition-colors duration-150 ${selected ? "bg-orange text-on-orange" : "bg-paper text-ink-muted"}`}>
        <TileIconComponent size={18} />
      </span>
      <span className="flex flex-wrap items-center justify-center gap-1.5 text-[13px] font-bold leading-snug">
        {label}
        {badge}
      </span>
      {note ? <span className="text-xs leading-5 text-ink-muted">{note}</span> : null}
    </>
  );
}

interface ChoiceTileProps extends TileLook {
  onSelect: () => void;
  /** A tile that cannot be chosen reads flat grey; say why in `note`. */
  disabled?: boolean;
}

/**
 * The one control for choosing between a few options in the store (new / used, delivery / pickup, payment methods, who can see a build):
 * a square tile with the icon above the word and an orange glowing frame when it is the chosen one. Rectangular buttons stay for actions and links.
 */
export function ChoiceTile({ onSelect, disabled = false, className = "", ...look }: ChoiceTileProps) {
  return (
    <button type="button" aria-pressed={look.selected} disabled={disabled} onClick={onSelect} className={`${TILE} ${className}`}>
      <TileBody {...look} />
    </button>
  );
}

interface ChoiceRadioTileProps extends TileLook {
  name: string;
  value: string;
  onSelect: () => void;
}

/** The same tile over a real radio input, for form groups (keyboard and screen readers work as for any radio group). */
export function ChoiceRadioTile({ name, value, onSelect, className = "", ...look }: ChoiceRadioTileProps) {
  return (
    <label className={`${TILE} has-focus-visible:outline-2 has-focus-visible:outline-offset-2 has-focus-visible:outline-orange ${className}`}>
      <input type="radio" name={name} value={value} checked={look.selected} onChange={onSelect} className="sr-only" />
      <TileBody {...look} />
    </label>
  );
}

interface ChoiceLinkTileProps extends TileLook {
  href: string;
}

/** The same tile as a link, for a choice that is a page address (the catalog's condition filter). */
export function ChoiceLinkTile({ href, className = "", ...look }: ChoiceLinkTileProps) {
  return (
    <Link href={href} aria-current={look.selected ? "true" : undefined} className={`${TILE} ${className}`}>
      <TileBody {...look} />
    </Link>
  );
}
