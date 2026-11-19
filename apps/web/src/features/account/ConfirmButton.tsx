"use client";

import { useState, type ReactNode } from "react";
import { useTranslations } from "next-intl";

interface ConfirmButtonProps {
  label: string;
  /** Drawn before the label (unlink, delete, sign out), from the shared icon set. */
  icon: ReactNode;
  onConfirm: () => void;
  busy?: boolean;
  /** Icon-only round button (the label becomes its accessible name and hover tip); for actions on a row. */
  iconOnly?: boolean;
  className?: string;
}

const base = "inline-flex h-10 items-center gap-2 rounded-full px-4 text-sm font-bold transition-colors";

/** An action that asks "are you sure?" in place before it runs (delete, unlink, sign out of a device). */
export function ConfirmButton({ label, icon, onConfirm, busy = false, iconOnly = false, className = "" }: ConfirmButtonProps) {
  const t = useTranslations("account.state");
  const [asking, setAsking] = useState(false);

  if (!asking && iconOnly) {
    return (
      <button
        type="button"
        aria-label={label}
        data-tip={label}
        onClick={() => setAsking(true)}
        className={`tip press flex h-10 w-10 items-center justify-center rounded-full border border-line text-ink-muted transition-colors hover:border-error hover:bg-error/5 hover:text-error ${className}`}
      >
        {icon}
      </button>
    );
  }
  if (!asking) {
    return (
      <button type="button" onClick={() => setAsking(true)} className={`${base} border border-line text-error hover:border-error hover:bg-error/5 ${className}`}>
        {icon}
        {label}
      </button>
    );
  }
  return (
    <span role="group" aria-label={label} className={`inline-flex flex-wrap items-center gap-1 ${className}`}>
      <button
        type="button" disabled={busy}
        onClick={() => {
          setAsking(false);
          onConfirm();
        }}
        className={`${base} pop-down bg-error text-white disabled:opacity-60`}
      >
        {icon}
        {t("confirm")}
      </button>
      <button type="button" onClick={() => setAsking(false)} className={`${base} text-ink-muted hover:bg-paper`}>{t("cancel")}</button>
    </span>
  );
}
