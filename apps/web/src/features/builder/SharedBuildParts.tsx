import { useLocale, useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { Card } from "@/components/ui/Card";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { BanIcon, CaseIcon, ImagePlaceholderIcon } from "@/components/ui/icons";
import { parseCondition } from "@/lib/condition";
import { formatEgp } from "@/lib/format";
import { mediaUrl } from "@/lib/media";

/** The parts of a shared build, one row each: photo, slot, model, condition, warranty and the price at which the build sells it. */
export function SharedBuildParts({ parts, slotNames }: { parts: Schemas["BuilderPartView"][]; slotNames: Readonly<Record<string, string>> }) {
  const t = useTranslations("sharedBuild");
  const locale = useLocale();

  return (
    <Card title={t("parts")} icon={CaseIcon} padding="none" className="overflow-hidden">
      <ul className="m-0 flex list-none flex-col p-0">
        {parts.map((part) => {
          const condition = parseCondition(part.item.condition);
          return (
            <li key={part.item.code} className="flex items-center gap-3 border-b border-line px-4 py-3.5 last:border-b-0 sm:gap-4 sm:px-5">
              <span className="flex size-16 shrink-0 items-center justify-center overflow-hidden rounded-control bg-paper text-muted">
                {part.item.thumbnailStorageKey ? (
                  // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
                  <img src={mediaUrl(part.item.thumbnailStorageKey)} alt="" width={64} height={64} loading="lazy" className="h-full w-full object-cover" />
                ) : (
                  <ImagePlaceholderIcon />
                )}
              </span>
              <div className="flex min-w-0 flex-1 flex-col gap-1">
                <span className="type-label">{slotNames[part.slot] ?? part.slot}</span>
                <span dir="ltr" className="text-start font-bold leading-snug [unicode-bidi:isolate]">{part.item.modelName}</span>
                <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-[13px] text-ink-muted">
                  {condition ? <ConditionBadge condition={condition} /> : null}
                  {part.item.warrantyLabel ? <span>{part.item.warrantyLabel}</span> : null}
                  {part.quantity > 1 ? <span>{t("quantity", { count: part.quantity })}</span> : null}
                  {!part.isAvailable ? <StatusBadge tone="neutral" icon={BanIcon}>{t("unavailable")}</StatusBadge> : null}
                </div>
              </div>
              <span className="shrink-0 text-end font-bold tabular-nums">{formatEgp(part.lineTotal, locale)}</span>
            </li>
          );
        })}
      </ul>
    </Card>
  );
}
