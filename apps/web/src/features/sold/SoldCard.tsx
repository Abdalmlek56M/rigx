import { useLocale, useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { ImagePlaceholderIcon } from "@/components/ui/icons";
import { parseCondition } from "@/lib/condition";
import { formatMonth } from "@/lib/format";
import { mediaUrl } from "@/lib/media";
import { NotifyMeButton } from "@/features/requests/NotifyMeButton";

interface SoldCardProps {
  item: Schemas["SoldFeedItem"];
  /** The governorate as the visitor reads it; `null` when the store hides it (or it is unknown). */
  governorate: string | null;
}

/** One sold item or build: photo, "اتباعت ✓", how it left (pickup or shipped), the month, and "عايز زيها · بلغني". No prices, no customer names. */
export function SoldCard({ item, governorate }: SoldCardProps) {
  const t = useTranslations("sold");
  const locale = useLocale();
  const condition = parseCondition(item.condition);
  const isBuild = item.kind === "Build";
  const how = item.fulfillmentMethod === "Pickup" ? t("pickup") : governorate ? t("shippedTo", { governorate }) : null;
  const meta = [how, formatMonth(item.soldAtUtc, locale)].filter(Boolean).join(" · ");

  return (
    <article className="flex w-full flex-col overflow-hidden rounded-[10px] border border-line bg-surface">
      <div className="relative flex aspect-[4/3] items-center justify-center bg-sand text-muted">
        {item.thumbnailStorageKey ? (
          // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
          <img src={mediaUrl(item.thumbnailStorageKey)} alt="" width={320} height={240} loading="lazy" className="h-full w-full object-cover" />
        ) : (
          <ImagePlaceholderIcon size={72} />
        )}
        {condition ? (
          <span className="absolute end-3 top-3">
            <ConditionBadge condition={condition} />
          </span>
        ) : null}
        <span className="absolute bottom-3 start-3 rounded bg-carbon px-2.5 py-1 text-xs font-bold text-on-carbon">{t("soldBadge")}</span>
      </div>
      <div className="flex flex-1 flex-col gap-1.5 p-3.5">
        <b dir={isBuild ? undefined : "ltr"} className="text-start [unicode-bidi:isolate]">{item.title}</b>
        <span className="text-xs text-ink-muted">{meta}</span>
        <NotifyMeButton
          target={{ modelName: item.title }}
          label={t("wantOne")}
          className="mt-auto flex min-h-11 items-center gap-2 self-start pt-1 text-[13px] font-bold text-orange-text hover:text-orange-text-hover"
        />
      </div>
    </article>
  );
}
