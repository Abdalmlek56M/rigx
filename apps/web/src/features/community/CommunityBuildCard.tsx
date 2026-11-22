import { useLocale, useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { ButtonLink } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { CategoryIcon } from "@/components/ui/category-icon";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { CaseIcon } from "@/components/ui/icons";
import { formatEgp } from "@/lib/format";
import { parseUsage } from "@/lib/usage";
import { BuildReactions } from "./BuildReactions";

/** Parts listed on a card before "+n more". */
const SHOWN_PARTS = 5;

/** A customer's build in the gallery, on the favourites page or in the admin queue (with `actions`): name, who made it, the parts, the live total and the reactions. */
export function CommunityBuildCard({ build, actions }: { build: Schemas["CommunityBuildCard"]; actions?: React.ReactNode }) {
  const t = useTranslations("communityBuilds");
  const tu = useTranslations("usage");
  const locale = useLocale();
  const usage = parseUsage(build.usage);
  const shown = build.parts.slice(0, SHOWN_PARTS);

  return (
    <Card
      as="article"
      title={<span dir="auto" className="[unicode-bidi:isolate]">{build.name?.trim() || t("untitled")}</span>}
      icon={CaseIcon}
      className="h-full w-full gap-4"
      actions={usage ? <StatusBadge tone="info">{tu(usage)}</StatusBadge> : undefined}
    >
      {build.authorName ? <p className="type-label m-0">{t("by", { name: build.authorName })}</p> : null}
      <ul className="m-0 flex list-none flex-col gap-2 p-0 text-sm">
        {shown.map((part) => (
          <li key={`${part.slot}-${part.modelName}`} className="flex items-center gap-2.5">
            <span aria-hidden="true" className="flex size-7 shrink-0 items-center justify-center rounded-full bg-paper text-ink-muted"><CategoryIcon slug={part.slot.toLowerCase()} size={15} /></span>
            <span dir="ltr" className="min-w-0 truncate text-start font-medium [unicode-bidi:isolate]">{part.modelName}</span>
          </li>
        ))}
        {build.parts.length > shown.length ? <li className="type-label ps-9">{t("moreParts", { count: build.parts.length - shown.length })}</li> : null}
      </ul>
      <div className="mt-auto flex items-baseline justify-between gap-3 border-t border-dashed border-line pt-3.5">
        <span className="type-label">{t("total")}</span>
        <span className="text-xl font-bold">{formatEgp(build.total, locale)}</span>
      </div>
      {!build.canOrder ? <p className="m-0 text-xs text-orange-text">{t("unavailable")}</p> : null}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <BuildReactions token={build.token} initial={build.reactions} />
        {actions ?? <ButtonLink href={`/b/${encodeURIComponent(build.token)}`} variant="secondary" size="sm">{t("open")}</ButtonLink>}
      </div>
    </Card>
  );
}
