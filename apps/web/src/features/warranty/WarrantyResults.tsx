import { getLocale, getTranslations } from "next-intl/server";
import type { Schemas } from "@rigx/api-client";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { WhatsAppIcon } from "@/components/ui/icons";
import { warrantyStatusKey, WARRANTY_BADGE } from "@/features/account/account-labels";
import { parseCondition } from "@/lib/condition";
import { fillTemplate, whatsAppUrl, NEW_TAB } from "@/lib/contact";
import { formatDate } from "@/lib/format";
import { daysLeftToShow } from "./warranty-query";

export interface WarrantyWhatsApp {
  number: string | null;
  template: string;
}

async function WarrantyCard({ item, whatsApp }: { item: Schemas["WarrantyPublicView"]; whatsApp: WarrantyWhatsApp }) {
  const t = await getTranslations("account.warranties");
  const locale = await getLocale();
  const status = warrantyStatusKey(item.status);
  const condition = parseCondition(item.condition);
  const days = daysLeftToShow(item.status, item.daysLeft);
  const text = fillTemplate(whatsApp.template, { item: item.name, code: item.code })
    || t("whatsAppMessage", { name: item.name, code: item.code });

  return (
    <li className="flex flex-col gap-3 rounded-card border border-line bg-surface p-5">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div className="flex min-w-0 flex-col gap-1">
          <span dir="auto" className="font-bold [unicode-bidi:isolate]">{item.name}</span>
          <span className="flex flex-wrap items-center gap-2">
            <span dir="ltr" className="font-mono text-xs text-ink-muted">{item.code}</span>
            {condition ? <ConditionBadge condition={condition} /> : null}
            {item.buildCode ? <span dir="ltr" className="font-mono text-xs text-ink-muted">{item.buildCode}</span> : null}
          </span>
        </div>
        {status ? <span className={`rounded-full px-3 py-1 text-xs font-bold ${WARRANTY_BADGE[status]}`}>{t(`status.${status}`)}</span> : null}
      </div>
      <dl className="m-0 grid grid-cols-2 gap-x-4 gap-y-2 text-sm">
        <div className="flex flex-col gap-0.5">
          <dt className="text-xs text-ink-muted">{t("plan")}</dt>
          <dd className="m-0 font-bold">{item.label}</dd>
        </div>
        <div className="flex flex-col gap-0.5">
          <dt className="text-xs text-ink-muted">{t("from")}</dt>
          <dd className="m-0">{formatDate(item.startsAtUtc, locale)}</dd>
        </div>
        <div className="flex flex-col gap-0.5">
          <dt className="text-xs text-ink-muted">{t("until")}</dt>
          <dd className="m-0">{item.endsAtUtc ? formatDate(item.endsAtUtc, locale) : "—"}</dd>
        </div>
        {days !== null ? (
          <div className="flex flex-col gap-0.5">
            <dt className="text-xs text-ink-muted">{t("daysLeftLabel")}</dt>
            <dd className="m-0 font-bold text-success">{t("daysCount", { days })}</dd>
          </div>
        ) : null}
      </dl>
      {whatsApp.number ? (
        <a {...NEW_TAB} href={whatsAppUrl(whatsApp.number, text)} className="inline-flex min-h-11 items-center gap-1.5 self-start text-sm font-bold text-orange-text underline hover:text-orange-text-hover">
          <WhatsAppIcon size={16} />
          {t("problem")}
        </a>
      ) : null}
    </li>
  );
}

/** The warranty of every item the search matched, one card each. */
export async function WarrantyResults({ items, whatsApp }: { items: Schemas["WarrantyPublicView"][]; whatsApp: WarrantyWhatsApp }) {
  return (
    <ul className={`m-0 grid list-none gap-3 p-0 ${items.length > 1 ? "md:grid-cols-2" : ""}`}>
      {items.map((item) => (
        <WarrantyCard key={item.code} item={item} whatsApp={whatsApp} />
      ))}
    </ul>
  );
}
