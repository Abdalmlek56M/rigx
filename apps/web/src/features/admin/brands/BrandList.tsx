"use client";

import { useTranslations } from "next-intl";
import { mediaUrl } from "@/lib/media";
import { ArrowDownIcon, ArrowUpIcon } from "@/components/ui/icons";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { IconButton } from "../kit/Button";
import { DragItem, DragSort } from "../kit/DragSort";
import { dropOn, moveKey } from "../kit/reorder-rules";
import type { Brand } from "./brands-api";

/**
 * The brand list: the logo (when there is one), the name, "off" when hidden. With `onReorder` each row can be dragged by its grip, and has
 * the two arrow buttons for the keyboard; the list is only orderable while it shows every brand (not while it is filtered).
 */
export function BrandList({ brands, selectedId, onSelect, onReorder }: {
  brands: readonly Brand[];
  selectedId: string | null;
  onSelect: (id: string) => void;
  onReorder?: (ids: string[]) => void;
}) {
  const t = useTranslations("admin.brands");
  const ids = brands.map((brand) => brand.id);
  return (
    <DragSort onMove={(active, over) => onReorder?.(dropOn(ids, active, over))}>
      <ul aria-label={t("listLabel")} className="m-0 flex max-h-[60dvh] list-none flex-col overflow-y-auto overflow-x-hidden overscroll-contain p-0">
        {brands.map((brand, index) => {
          const active = brand.id === selectedId;
          return (
            <DragItem key={brand.id} id={brand.id} handle disabled={!onReorder} label={t("drag")} className={`flex min-h-11 items-center gap-1 rounded-control bg-surface ${active ? "bg-orange/10" : ""}`}>
              <button
                type="button" aria-current={active ? "true" : undefined} onClick={() => onSelect(brand.id)}
                className={`flex min-h-11 min-w-0 flex-1 items-center gap-2 rounded-control px-2 text-start text-sm hover:bg-paper ${active ? "font-bold" : ""}`}
              >
                <span className="flex h-7 w-10 shrink-0 items-center justify-center overflow-hidden rounded-control bg-paper">
                  {brand.logoStorageKey ? (
                    // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
                    <img src={mediaUrl(brand.logoStorageKey)} alt="" className="h-full w-full object-contain" />
                  ) : null}
                </span>
                <span dir="ltr" className="min-w-0 flex-1 truncate text-start [unicode-bidi:isolate]">{brand.name}</span>
                {brand.isActive ? null : <StatusBadge tone="neutral">{t("off")}</StatusBadge>}
              </button>
              {onReorder ? (
                <span className="flex shrink-0">
                  <IconButton icon={ArrowUpIcon} label={t("moveUp")} size="sm" disabled={index === 0} onClick={() => onReorder(moveKey(ids, index, index - 1))} />
                  <IconButton icon={ArrowDownIcon} label={t("moveDown")} size="sm" disabled={index === brands.length - 1} onClick={() => onReorder(moveKey(ids, index, index + 1))} />
                </span>
              ) : null}
            </DragItem>
          );
        })}
      </ul>
    </DragSort>
  );
}
