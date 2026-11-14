"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { MailIcon } from "@/components/ui/icons";
import { NotifyMeSheet, type NotifyTarget } from "./NotifyMeSheet";

interface NotifyMeButtonProps {
  target: NotifyTarget;
  className: string;
  /** The button text; defaults to "بلغني لما يتوفر". */
  label?: string;
}

/** The "بلغني لما يتوفر" button with its sheet; `className` sets the size and colours. */
export function NotifyMeButton({ target, className, label }: NotifyMeButtonProps) {
  const t = useTranslations("notify");
  const [open, setOpen] = useState(false);

  return (
    <>
      <button type="button" aria-haspopup="dialog" onClick={() => setOpen(true)} className={className}>
        <MailIcon size={16} />
        {label ?? t("cta")}
      </button>
      <NotifyMeSheet open={open} onClose={() => setOpen(false)} target={target} />
    </>
  );
}
