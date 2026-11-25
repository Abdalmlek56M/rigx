"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Button, type ButtonSize } from "@/components/ui/Button";
import { BellIcon } from "@/components/ui/icons";
import { NotifyMeSheet, type NotifyTarget } from "./NotifyMeSheet";

interface NotifyMeButtonProps {
  target: NotifyTarget;
  size?: ButtonSize;
  className?: string;
  /** The button text; defaults to "بلغني لما يتوفر". */
  label?: string;
}

/** The "بلغني لما يتوفر" button with its sheet. */
export function NotifyMeButton({ target, size = "md", className, label }: NotifyMeButtonProps) {
  const t = useTranslations("notify");
  const [open, setOpen] = useState(false);

  return (
    <>
      <Button type="button" variant="secondary" size={size} icon={BellIcon} aria-haspopup="dialog" onClick={() => setOpen(true)} className={className}>
        {label ?? t("cta")}
      </Button>
      <NotifyMeSheet open={open} onClose={() => setOpen(false)} target={target} />
    </>
  );
}
