"use client";

import type { ReactNode } from "react";
import { useTranslations } from "next-intl";
import { IconButton } from "@/components/ui/Button";
import { ModalSheet } from "@/components/ui/ModalSheet";
import { CloseIcon } from "@/components/ui/icons";

interface RequestSheetProps {
  open: boolean;
  onClose: () => void;
  title: string;
  children: ReactNode;
}

/** The frame every small request form shares (notify-me, interest sign-ups): side panel on desktop, bottom sheet on mobile. */
export function RequestSheet({ open, onClose, title, children }: RequestSheetProps) {
  const tc = useTranslations("common");
  return (
    <ModalSheet open={open} onClose={onClose} label={title} edge="end">
      <div className="flex max-h-[88dvh] flex-col md:h-full md:max-h-none">
        <div className="flex items-center justify-between border-b border-line px-5 py-4">
          <b className="type-section-title">{title}</b>
          <IconButton icon={CloseIcon} label={tc("close")} variant="secondary" onClick={onClose} />
        </div>
        <div className="flex flex-col gap-3 overflow-y-auto p-5">{children}</div>
      </div>
    </ModalSheet>
  );
}
