"use client";

import { useId, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { ChevronDownIcon, ImagePlaceholderIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import { parseCondition } from "@/lib/condition";
import { formatEgp } from "@/lib/format";
import { mediaUrl } from "@/lib/media";
import { parseUsage } from "@/lib/usage";
import { AddToCartButton } from "@/features/cart/AddToCartButton";
import { useCatalogPending } from "@/features/catalog/use-catalog-pending";
import { buildCartLine, buildSuitableFor, buildTitle } from "./build-view";

/** One ready-made build: usage code on a square photo, the all-in price, order and edit, with the parts behind "المواصفات". */
export function BuildCard({ build }: { build: Schemas["AssemblyPublicView"] }) {
  const t = useTranslations("builds");
  const tu = useTranslations("usage");
  const locale = useLocale();
  const pending = useCatalogPending();
  const [open, setOpen] = useState(false);
  const detailsId = useId();
  const usage = parseUsage(build.usage);
  const title = buildTitle(build, locale);
  const suitableFor = buildSuitableFor(build, locale);
  const photo = build.photos[0];
  const href = `/builds/${build.code}`;

  return (
    <article className={`lift flex h-full w-full flex-col overflow-hidden rounded-panel border border-line bg-surface transition-opacity duration-200 starting:opacity-0 motion-reduce:transition-none ${pending ? "opacity-60" : "opacity-100"}`}>
      <Link href={href} aria-label={title} className="relative flex aspect-square items-center justify-center overflow-hidden bg-surface text-muted">
        {photo ? (
          // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
          <img src={mediaUrl(photo.thumbnailStorageKey)} alt="" width={420} height={420} loading="lazy" className="h-full w-full object-contain p-5" />
        ) : (
          <ImagePlaceholderIcon size={96} />
        )}
        {usage ? (
          <span className="absolute end-3.5 top-3.5 rounded-full bg-carbon px-2.5 py-1 text-xs font-bold text-on-carbon">{tu(usage)}</span>
        ) : null}
      </Link>
      <div className="flex flex-1 flex-col gap-3 p-5 md:p-[22px]">
        <h2 className="m-0 line-clamp-2 min-h-10 text-lg leading-6 font-bold">
          <Link href={href}>{title}</Link>
        </h2>
        <button
          type="button"
          aria-expanded={open}
          aria-controls={detailsId}
          onClick={() => setOpen((value) => !value)}
          className="flex min-h-10 items-center justify-between gap-2 rounded-control border border-input-line px-3 text-sm font-bold transition-colors duration-150 hover:bg-paper motion-reduce:transition-none"
        >
          <span className="text-sm">{t("specs")} <span className="font-mono text-ink-muted">{build.units.length}</span></span>
          <span className={`flex transition-transform duration-200 motion-reduce:transition-none ${open ? "rotate-180" : ""}`}>
            <ChevronDownIcon size={16} />
          </span>
        </button>
        <div id={detailsId} className={`grid transition-[grid-template-rows] duration-200 ease-out motion-reduce:transition-none ${open ? "grid-rows-[1fr]" : "grid-rows-[0fr]"}`}>
          <div className="overflow-hidden">
            <ul className="m-0 flex list-none flex-col gap-1.5 p-0 pb-2 text-sm">
              {build.units.map((unit) => {
                const condition = parseCondition(unit.condition);
                return (
                  <li key={unit.stockItemCode} className="flex items-center justify-between gap-2.5 border-b border-line pb-1.5 last:border-b-0">
                    <span dir="ltr" className="text-start font-medium [unicode-bidi:isolate]">{unit.name}</span>
                    {condition ? <ConditionBadge condition={condition} /> : null}
                  </li>
                );
              })}
            </ul>
            {suitableFor ? (
              <p className="m-0 rounded-card bg-paper px-3.5 py-3 pb-3 text-sm leading-7">
                <b>{t("suitableFor")}</b> {suitableFor}
              </p>
            ) : null}
          </div>
        </div>
        <div className="mt-auto flex items-baseline justify-between gap-3">
          <span className="text-[13px] text-ink-muted">{t("priceNote")}</span>
          <span className="text-2xl font-bold">{formatEgp(build.salePrice, locale)}</span>
        </div>
        <div className="flex gap-2">
          <AddToCartButton
            line={buildCartLine(build, locale)}
            thenOpenCart
            className="flex h-12 flex-1 items-center justify-center rounded-control bg-orange font-bold text-on-orange"
          >
            {t("order")}
          </AddToCartButton>
          <Link
            href={`/builder?from=${build.code}`}
            className="flex h-12 flex-1 items-center justify-center rounded-control border-[1.5px] border-ink font-bold"
          >
            {t("edit")}
          </Link>
        </div>
      </div>
    </article>
  );
}
