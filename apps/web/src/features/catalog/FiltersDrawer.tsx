"use client";

import { useState, type ReactNode } from "react";
import { useTranslations } from "next-intl";
import { ModalSheet } from "@/components/ui/ModalSheet";
import { Button, IconButton } from "@/components/ui/Button";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { CloseIcon, FilterIcon } from "@/components/ui/icons";

interface FiltersDrawerProps {
  /** The current filter query: the drawer closes by itself when it changes (a filter was picked). */
  queryKey: string;
  activeCount: number;
  children: ReactNode;
}

/** Mobile only: the filter panel in a side sheet behind a "تصفية" button. */
export function FiltersDrawer({ queryKey, activeCount, children }: FiltersDrawerProps) {
  const t = useTranslations("catalog.filters");
  const tc = useTranslations("common");
  const [openedAt, setOpenedAt] = useState<string | null>(null);
  const close = () => setOpenedAt(null);

  return (
    <div className="md:hidden">
      <Button
        type="button"
        variant="secondary"
        icon={FilterIcon}
        aria-haspopup="dialog"
        onClick={() => setOpenedAt(queryKey)}
      >
        {t("open")}
        {activeCount > 0 ? <StatusBadge tone="warning">{activeCount}</StatusBadge> : null}
      </Button>
      <ModalSheet open={openedAt === queryKey} onClose={close} label={t("title")} edge="end" fullHeight>
        <div className="flex h-full flex-col">
          <div className="flex items-center justify-between border-b border-line px-4 py-3">
            <b className="type-section-title">{t("title")}</b>
            <IconButton icon={CloseIcon} label={tc("close")} variant="secondary" onClick={close} />
          </div>
          <div className="min-h-0 flex-1 overflow-y-auto p-4">{children}</div>
        </div>
      </ModalSheet>
    </div>
  );
}
