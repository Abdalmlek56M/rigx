"use client";

import type { ReactNode } from "react";
import { useTranslations } from "next-intl";
import { CloseIcon } from "@/components/ui/icons";
import { ModalSheet } from "@/components/ui/ModalSheet";
import { IconButton } from "./Button";

/**
 * A record's form in a side sheet over its list (the list keeps its place and filters), with a title and a close button.
 * `open` is true while a record is picked or a new one is being made; closing reports back through `onClose`.
 */
export function EditSheet({ open, title, onClose, children }: { open: boolean; title: string; onClose: () => void; children: ReactNode }) {
  const t = useTranslations("admin.kit");
  return (
    <ModalSheet open={open} onClose={onClose} label={title} edge="end" fullHeight wide>
      <div className="flex h-full flex-col gap-4 overflow-y-auto overscroll-contain bg-paper p-4 md:p-5">
        <div className="flex items-center justify-between gap-3">
          <h2 className="m-0 type-section-title">{title}</h2>
          <IconButton icon={CloseIcon} label={t("close")} onClick={onClose} />
        </div>
        <div className="flex flex-col gap-3 rounded-card border border-line bg-surface p-4 md:p-5">{children}</div>
      </div>
    </ModalSheet>
  );
}
