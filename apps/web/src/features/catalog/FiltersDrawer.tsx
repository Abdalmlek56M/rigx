"use client";

import { useState, type ReactNode } from "react";
import { useTranslations } from "next-intl";
import { ModalSheet } from "@/components/ui/ModalSheet";
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
      <button
        type="button"
        aria-haspopup="dialog"
        onClick={() => setOpenedAt(queryKey)}
        className="flex h-11 items-center gap-2 rounded-control border border-input-line bg-surface px-4 text-sm font-bold"
      >
        <FilterIcon size={18} />
        {t("open")}
        {activeCount > 0 ? <span className="rounded-full bg-carbon px-2 text-xs text-on-carbon">{activeCount}</span> : null}
      </button>
      <ModalSheet open={openedAt === queryKey} onClose={close} label={t("title")} edge="end" fullHeight>
        <div className="flex h-full flex-col">
          <div className="flex items-center justify-between border-b border-line px-4 py-3">
            <b className="text-lg">{t("title")}</b>
            <button
              type="button"
              aria-label={tc("close")}
              onClick={close}
              className="flex h-11 w-11 items-center justify-center rounded-control border border-input-line bg-surface"
            >
              <CloseIcon size={18} />
            </button>
          </div>
          <div className="min-h-0 flex-1 overflow-y-auto p-4">{children}</div>
        </div>
      </ModalSheet>
    </div>
  );
}
