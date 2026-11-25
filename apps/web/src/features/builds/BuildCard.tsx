"use client";

import { useId, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { StockDot } from "@/components/ui/StockDot";
import { Button, ButtonLink } from "@/components/ui/Button";
import { cardClass } from "@/components/ui/Card";
import { ChevronDownIcon, ImagePlaceholderIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import { parseCondition } from "@/lib/condition";
import { formatEgp } from "@/lib/format";
import { mediaUrl } from "@/lib/media";
import { parseUsage } from "@/lib/usage";
import { AddToCartButton } from "@/features/cart/AddToCartButton";
import { useCatalogPending } from "@/features/catalog/use-catalog-pending";
import { FavouriteButton } from "@/features/favourites/FavouriteButton";
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
  const soldOut = build.copies <= 0;

  return (
    <article className={`${cardClass({ padding: "none", interactive: true })} relative h-full overflow-hidden rounded-panel transition-opacity duration-200 starting:opacity-0 motion-reduce:transition-none ${pending ? "opacity-60" : "opacity-100"}`}>
      <Link href={href} aria-label={title} className="relative flex aspect-square items-center justify-center overflow-hidden bg-surface text-muted">
        {photo ? (
          // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
          <img src={mediaUrl(photo.thumbnailStorageKey)} alt="" width={420} height={420} loading="lazy" className="product-photo h-full w-full object-cover" />
        ) : (
          <ImagePlaceholderIcon size={96} />
        )}
        <span className="absolute start-3.5 top-3.5 flex flex-wrap justify-start gap-1.5">
          {usage ? <StatusBadge tone="info">{tu(usage)}</StatusBadge> : null}
        </span>
        <span className="absolute bottom-3.5 start-3.5"><StockDot available={!soldOut} label={soldOut ? t("soldOutBadge") : t("inStockBadge")} /></span>
      </Link>
      <FavouriteButton code={build.code} modelName={title} className="absolute end-3 top-3" />
      <div className="flex flex-1 flex-col gap-3 p-5 md:p-[22px]">
        <h2 className="m-0 line-clamp-2 min-h-12 type-section-title">
          <Link href={href}>{title}</Link>
        </h2>
        <Button
          type="button"
          variant="secondary"
          size="sm"
          aria-expanded={open}
          aria-controls={detailsId}
          onClick={() => setOpen((value) => !value)}
          className="w-full justify-between"
        >
          <span className="flex items-center gap-1.5">
            {t("specs")}
            <span className="font-mono text-ink-muted">{build.units.length}</span>
          </span>
          <span aria-hidden="true" className={`flex transition-transform duration-200 motion-reduce:transition-none ${open ? "rotate-180" : ""}`}>
            <ChevronDownIcon size={16} />
          </span>
        </Button>
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
          <span className="whitespace-nowrap text-2xl font-bold">{formatEgp(build.salePrice, locale)}</span>
        </div>
        <div className="flex gap-2">
          <AddToCartButton line={buildCartLine(build, locale)} thenOpenCart variant="accent" className="flex-1" />
          <ButtonLink href={`/builder?from=${build.code}`} variant="secondary" className="flex-1">{t("edit")}</ButtonLink>
        </div>
      </div>
    </article>
  );
}
