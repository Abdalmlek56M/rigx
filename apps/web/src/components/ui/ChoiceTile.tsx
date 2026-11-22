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
  /** One short row (icon beside the word; on a phone the word alone, so three fit in a row) for a filter above a list; it keeps a 44 px touch target. */
  compact?: boolean;
  className?: string;
}

const TILE_BASE = "choice-card press relative flex min-w-0 cursor-pointer items-center justify-center rounded-card border border-line bg-surface text-center";
const TILE_FULL = "min-h-[84px] flex-col gap-1.5 px-3 py-3";
// The visible tile is 36 px; the pseudo-element stretches the touch area to 46 px without moving its neighbours.
const TILE_COMPACT = "min-h-9 gap-1.5 px-2.5 py-1 after:absolute after:inset-x-0 after:-inset-y-1.5 after:content-['']";

const tileClass = ({ compact }: Pick<TileLook, "compact">) => `${TILE_BASE} ${compact ? TILE_COMPACT : TILE_FULL}`;

function TileBody({ icon: TileIconComponent, label, note, badge, selected, compact }: TileLook) {
  return (
    <>
      <span aria-hidden="true" className={`flex ${compact ? "size-6 max-sm:hidden" : "size-9"} shrink-0 items-center justify-center rounded-full transition-colors duration-150 ${selected ? "bg-orange text-on-orange" : "bg-paper text-ink-muted"}`}>
        <TileIconComponent size={compact ? 14 : 18} />
      </span>
      <span className={`flex items-center justify-center gap-1.5 text-[13px] font-bold leading-snug ${compact ? "whitespace-nowrap" : "flex-wrap"}`}>
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
    <button type="button" aria-pressed={look.selected} disabled={disabled} onClick={onSelect} className={`${tileClass(look)} ${className}`}>
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
    <label className={`${tileClass(look)} has-focus-visible:outline-2 has-focus-visible:outline-offset-2 has-focus-visible:outline-orange ${className}`}>
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
    <Link href={href} aria-current={look.selected ? "true" : undefined} className={`${tileClass(look)} ${className}`}>
      <TileBody {...look} />
    </Link>
  );
}
