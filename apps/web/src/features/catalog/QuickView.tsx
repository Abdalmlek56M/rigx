"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { ButtonLink, IconButton } from "@/components/ui/Button";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { InfoField, InfoGrid } from "@/components/ui/InfoGrid";
import { ModalSheet } from "@/components/ui/ModalSheet";
import { Skeleton } from "@/components/ui/Skeleton";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { ArrowEndIcon, BanIcon, CloseIcon, EyeIcon, ImagePlaceholderIcon } from "@/components/ui/icons";
import { parseCondition } from "@/lib/condition";
import { formatEgp } from "@/lib/format";
import { mediaUrl } from "@/lib/media";
import { productPath } from "@/lib/seo/urls";
import { defaultOption, offeredOptions, priceOf } from "@/features/product/price-options";
import { ItemAction } from "./ItemAction";
import { loadQuickView, type QuickViewData } from "./quick-view-data";

type Load = { status: "idle" } | { status: "loading" } | { status: "failed" } | { status: "ready"; data: QuickViewData };

/**
 * The eye on a catalog card: a side sheet (a bottom sheet on a phone) with the photos, the main specs, the price and the one action,
 * so a buyer can look closer without leaving the list. The details load on the first open; the full page is one link away.
 */
export function QuickView({ item }: { item: Schemas["StoreItemCard"] }) {
  const t = useTranslations("quickView");
  const tc = useTranslations("common");
  const ti = useTranslations("item");
  const locale = useLocale();
  const [open, setOpen] = useState(false);
  const [load, setLoad] = useState<Load>({ status: "idle" });
  const [selected, setSelected] = useState(0);
  const condition = parseCondition(item.condition);
  const soldOut = item.available <= 0;
  const price = priceOf(item.prices, defaultOption(offeredOptions(item.priceOptions)));

  function show() {
    setOpen(true);
    if (load.status !== "idle") return;
    setLoad({ status: "loading" });
    loadQuickView(item.slug)
      .then((data) => setLoad(data ? { status: "ready", data } : { status: "failed" }))
      .catch(() => setLoad({ status: "failed" }));
  }

  const data = load.status === "ready" ? load.data : null;
  const photos = data?.photos ?? [];
  const current = photos[Math.min(selected, photos.length - 1)];
  const mainKey = current?.storageKey ?? item.thumbnailStorageKey;

  return (
    <>
      <IconButton
        icon={EyeIcon}
        label={t("open", { item: item.modelName })}
        variant="secondary"
        aria-haspopup="dialog"
        onClick={show}
        className="text-ink-muted shadow-soft"
      />
      <ModalSheet open={open} onClose={() => setOpen(false)} label={t("title")} edge="end">
        <div className="flex max-h-[88dvh] flex-col md:h-full md:max-h-none">
          <div className="flex items-center justify-between gap-3 border-b border-line px-4 py-3">
            <b className="type-section-title">{t("title")}</b>
            <IconButton icon={CloseIcon} label={tc("close")} variant="secondary" onClick={() => setOpen(false)} />
          </div>
          <div className="flex min-h-0 flex-1 flex-col gap-4 overflow-y-auto p-4">
            <div className="flex aspect-square items-center justify-center overflow-hidden rounded-card bg-paper text-muted">
              {mainKey ? (
                // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
                <img src={mediaUrl(mainKey)} alt={item.modelName} width={480} height={480} className="h-full w-full object-contain" />
              ) : (
                <ImagePlaceholderIcon size={48} />
              )}
            </div>
            {photos.length > 1 ? (
              <ul className="m-0 flex list-none gap-2 overflow-x-auto p-0 [scrollbar-width:none]">
                {photos.map((photo, index) => (
                  <li key={photo.storageKey}>
                    <button
                      type="button"
                      aria-label={t("photo", { number: index + 1 })}
                      aria-current={index === selected ? "true" : undefined}
                      onClick={() => setSelected(index)}
                      className={`size-14 shrink-0 overflow-hidden rounded-control border bg-paper ${index === selected ? "border-orange" : "border-line"}`}
                    >
                      {/* eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite */}
                      <img src={mediaUrl(photo.thumbnailStorageKey)} alt="" width={56} height={56} loading="lazy" className="h-full w-full object-cover" />
                    </button>
                  </li>
                ))}
              </ul>
            ) : null}

            <div className="flex flex-col gap-2">
              <span className="text-start text-lg font-bold leading-snug [unicode-bidi:isolate]">{item.modelName}</span>
              <div className="flex flex-wrap items-center gap-2">
                {condition ? <ConditionBadge condition={condition} /> : null}
                {soldOut ? <StatusBadge tone="neutral" icon={BanIcon}>{ti("soldOut")}</StatusBadge> : null}
              </div>
            </div>

            {load.status === "loading" ? (
              <div className="flex flex-col gap-2" aria-busy="true">
                <Skeleton className="h-10 w-full" />
                <Skeleton className="h-10 w-full" />
              </div>
            ) : null}
            {load.status === "failed" ? <p role="alert" className="m-0 text-sm text-error-text">{t("failed")}</p> : null}
            {data ? (
              <InfoGrid columns={2} className="gap-y-3">
                {data.brand ? <InfoField label={t("brand")} value={data.brand} /> : null}
                {data.warrantyLabel ? <InfoField label={t("warranty")} value={data.warrantyLabel} /> : null}
                {data.specs.map((row) => (
                  <InfoField key={row.key} label={row.name} value={row.value} />
                ))}
              </InfoGrid>
            ) : null}
          </div>
          <div className="flex flex-col gap-3 border-t border-line p-4">
            <span className="text-[24px] font-bold tabular-nums">{formatEgp(price, locale)}</span>
            <div className="flex flex-col gap-2 sm:flex-row">
              <ItemAction item={item} size="md" className="sm:flex-1" />
              <ButtonLink href={productPath(item.slug)} variant="primary" icon={ArrowEndIcon} className="sm:flex-1">{t("openPage")}</ButtonLink>
            </div>
          </div>
        </div>
      </ModalSheet>
    </>
  );
}
