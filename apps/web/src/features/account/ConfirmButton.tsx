"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";

interface ConfirmButtonProps {
  label: string;
  onConfirm: () => void;
  busy?: boolean;
  className?: string;
}

const base = "inline-flex min-h-11 items-center rounded-control px-3 text-sm font-bold";

/** An action that asks "are you sure?" in place before it runs (delete, unlink, sign out of a device). */
export function ConfirmButton({ label, onConfirm, busy = false, className = "" }: ConfirmButtonProps) {
  const t = useTranslations("account.state");
  const [asking, setAsking] = useState(false);

  if (!asking) {
    return (
      <button type="button" onClick={() => setAsking(true)} className={`${base} text-error hover:underline ${className}`}>
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
        className={`${base} bg-error text-surface disabled:opacity-60`}
      >
        {t("confirm")}
      </button>
      <button type="button" onClick={() => setAsking(false)} className={`${base} text-ink-muted hover:underline`}>{t("cancel")}</button>
    </span>
  );
}
