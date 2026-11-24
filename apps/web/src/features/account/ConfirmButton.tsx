"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Button, IconButton, type ButtonIcon } from "@/components/ui/Button";
import { ModalSheet } from "@/components/ui/ModalSheet";

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

/** An action that asks "are you sure?" in a window in the middle of the screen before it runs (delete, unlink, sign out of a device). */
export function ConfirmButton({ label, icon, onConfirm, busy = false, iconOnly = false, className = "" }: ConfirmButtonProps) {
  const t = useTranslations("account.state");
  const [asking, setAsking] = useState(false);
  const close = () => setAsking(false);

  return (
    <>
      {iconOnly ? (
        <IconButton
          icon={icon}
          label={label}
          variant="secondary"
          size="sm"
          aria-haspopup="dialog"
          onClick={() => setAsking(true)}
          className={`border border-line text-ink-muted hover:border-error hover:bg-error/10 hover:text-error-text ${className}`}
        />
      ) : (
        <Button type="button" variant="secondary" size="sm" icon={icon} aria-haspopup="dialog" onClick={() => setAsking(true)} className={`text-error-text ${className}`}>
          {label}
        </Button>
      )}
      <ModalSheet open={asking} onClose={close} label={label} edge="center">
        <div className="flex flex-col gap-4 p-5">
          <h2 className="type-section-title m-0">{label}</h2>
          <p className="m-0 text-sm text-ink-muted">{t("sure")}</p>
          <div className="flex flex-wrap justify-end gap-2">
            <Button type="button" variant="ghost" onClick={close}>{t("cancel")}</Button>
            <Button
              type="button"
              variant="danger"
              icon={icon}
              loading={busy}
              onClick={() => {
                close();
                onConfirm();
              }}
            >
              {t("confirm")}
            </Button>
          </div>
        </div>
      </ModalSheet>
    </>
  );
}
