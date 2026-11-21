"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Button, IconButton, type ButtonIcon } from "@/components/ui/Button";

interface ConfirmButtonProps {
  label: string;
  /** The icon before the label (unlink, delete, sign out), from the shared icon set. */
  icon: ButtonIcon;
  onConfirm: () => void;
  busy?: boolean;
  /** Icon-only button (the label becomes its accessible name and hover tip); for actions on a row. */
  iconOnly?: boolean;
  className?: string;
}

/** An action that asks "are you sure?" in place before it runs (delete, unlink, sign out of a device). */
export function ConfirmButton({ label, icon, onConfirm, busy = false, iconOnly = false, className = "" }: ConfirmButtonProps) {
  const t = useTranslations("account.state");
  const [asking, setAsking] = useState(false);

  if (!asking && iconOnly) {
    return (
      <IconButton
        icon={icon}
        label={label}
        variant="secondary"
        size="sm"
        onClick={() => setAsking(true)}
        className={`border border-line text-ink-muted hover:border-error hover:bg-error/10 hover:text-error ${className}`}
      />
    );
  }
  if (!asking) {
    return (
      <Button type="button" variant="secondary" size="sm" icon={icon} onClick={() => setAsking(true)} className={`text-error ${className}`}>
        {label}
      </Button>
    );
  }
  return (
    <span role="group" aria-label={label} className={`inline-flex flex-wrap items-center gap-1 ${className}`}>
      <Button
        type="button"
        variant="danger"
        size="sm"
        icon={icon}
        loading={busy}
        onClick={() => {
          setAsking(false);
          onConfirm();
        }}
      >
        {t("confirm")}
      </Button>
      <Button type="button" variant="ghost" size="sm" onClick={() => setAsking(false)}>{t("cancel")}</Button>
    </span>
  );
}
