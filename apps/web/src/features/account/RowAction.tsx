"use client";

import type { ComponentType } from "react";
import { Link } from "@/i18n/navigation";

interface RowActionProps {
  /** Accessible name and hover tip. */
  label: string;
  icon: ComponentType<{ size?: number }>;
  danger?: boolean;
}

const BASE = "tip press flex h-10 w-10 items-center justify-center rounded-full border border-line transition-colors";
const NEUTRAL = "text-ink-muted hover:border-ink hover:text-ink";
const DANGER = "text-ink-muted hover:border-error hover:bg-error/5 hover:text-error";

/** An icon-only action on a row (view, invoice, edit, set default); the label is its name and tip. */
export function RowLink({ href, label, icon: Icon, danger = false, external = false }: RowActionProps & { href: string; external?: boolean }) {
  const className = `${BASE} ${danger ? DANGER : NEUTRAL}`;
  if (external) {
    return (
      <a href={href} target="_blank" rel="noreferrer" aria-label={label} data-tip={label} className={className}>
        <Icon size={16} />
      </a>
    );
  }
  return (
    <Link href={href} aria-label={label} data-tip={label} className={className}>
      <Icon size={16} />
    </Link>
  );
}

export function RowButton({ label, icon: Icon, onClick, danger = false }: RowActionProps & { onClick: () => void }) {
  return (
    <button type="button" aria-label={label} data-tip={label} onClick={onClick} className={`${BASE} ${danger ? DANGER : NEUTRAL}`}>
      <Icon size={16} />
    </button>
  );
}
