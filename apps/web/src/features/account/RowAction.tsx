"use client";

import type { ComponentType } from "react";
import { ButtonLink, IconButton } from "@/components/ui/Button";

interface RowActionProps {
  /** Accessible name and hover tip. */
  label: string;
  icon: ComponentType<{ size?: number }>;
  danger?: boolean;
}

const DANGER = "border border-line text-ink-muted hover:border-error hover:bg-error/10 hover:text-error-text";

/** An icon-only action on a row (view, invoice, edit, set default); the label is its name and tip. */
export function RowLink({ href, label, icon, danger = false, external = false }: RowActionProps & { href: string; external?: boolean }) {
  return (
    <ButtonLink
      href={href}
      icon={icon}
      iconOnly
      label={label}
      variant="secondary"
      size="sm"
      external={external}
      className={danger ? DANGER : undefined}
    />
  );
}

export function RowButton({ label, icon, onClick, danger = false }: RowActionProps & { onClick: () => void }) {
  return (
    <IconButton
      icon={icon}
      label={label}
      variant="secondary"
      size="sm"
      onClick={onClick}
      className={danger ? DANGER : undefined}
    />
  );
}
