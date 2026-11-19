import { useLocale, useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { ImagePlaceholderIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import { parseCondition } from "@/lib/condition";
import { formatEgp } from "@/lib/format";
import { mediaUrl } from "@/lib/media";
import { parseUsage } from "@/lib/usage";
import { AddToCartButton } from "@/features/cart/AddToCartButton";
import { buildCartLine, buildSuitableFor, buildTitle } from "./build-view";

/** One ready-made build: usage and code on the photo, parts with جديد / مستعمل tags, the all-in price, order and edit. */
export function BuildCard({ build }: { build: Schemas["AssemblyPublicView"] }) {
  const t = useTranslations("builds");
  const tu = useTranslations("usage");
  const locale = useLocale();
  const usage = parseUsage(build.usage);
  const title = buildTitle(build, locale);
  const suitableFor = buildSuitableFor(build, locale);
  const photo = build.photos[0];
  const href = `/builds/${build.code}`;

  return (
    <article className="lift flex w-full flex-col overflow-hidden rounded-panel border border-line bg-surface">
      <Link href={href} aria-label={title} className="relative flex h-[180px] items-center justify-center bg-sand text-muted">
        {photo ? (
          // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
          <img src={mediaUrl(photo.thumbnailStorageKey)} alt="" width={420} height={180} loading="lazy" className="h-full w-full object-cover" />
        ) : (
          <ImagePlaceholderIcon size={96} />
        )}
        {usage ? (
          <span className="absolute end-3.5 top-3.5 rounded-full bg-carbon px-2.5 py-1 text-xs font-bold text-on-carbon">{tu(usage)}</span>
        ) : null}
      </Link>
      <div className="flex flex-1 flex-col gap-3.5 p-5 md:p-[22px]">
        <h2 className="m-0 text-[22px] font-bold">
          <Link href={href}>{title}</Link>
        </h2>
        <ul className="m-0 flex list-none flex-col gap-1.5 p-0 text-sm">
          {build.units.map((unit) => {
            const condition = parseCondition(unit.condition);
            return (
              <li key={unit.stockItemCode} className="flex items-center justify-between gap-2.5 border-b border-line pb-1.5">
                <span dir="ltr" className="text-start font-medium [unicode-bidi:isolate]">{unit.name}</span>
                {condition ? <ConditionBadge condition={condition} /> : null}
              </li>
            );
          })}
        </ul>
        {suitableFor ? (
          <p className="m-0 rounded-card bg-paper px-3.5 py-3 text-sm leading-7">
            <b>{t("suitableFor")}</b> {suitableFor}
          </p>
        ) : null}
        <div className="mt-auto flex items-baseline justify-between">
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
