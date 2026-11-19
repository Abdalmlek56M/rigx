import { getLocale, getTranslations } from "next-intl/server";
import type { Schemas } from "@rigx/api-client";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { WhatsAppIcon } from "@/components/ui/icons";
import { warrantyStatusKey, WARRANTY_BADGE } from "@/features/account/account-labels";
import { parseCondition } from "@/lib/condition";
import { fillTemplate, whatsAppUrl } from "@/lib/contact";
import { formatDate } from "@/lib/format";
import { daysLeftToShow } from "./warranty-query";

export interface WarrantyWhatsApp {
  number: string | null;
  template: string;
}

async function WarrantyItem({ item, orderReference, whatsApp }: { item: Schemas["WarrantyPublicView"]; orderReference: string; whatsApp: WarrantyWhatsApp }) {
  const t = await getTranslations("account.warranties");
  const locale = await getLocale();
  const status = warrantyStatusKey(item.status);
  const condition = parseCondition(item.condition);
  const days = daysLeftToShow(item.status, item.daysLeft);
  const text = fillTemplate(whatsApp.template, { item: item.name, code: item.code, order: orderReference })
    || t("whatsAppMessage", { name: item.name, code: item.code });

  return (
    <li className="flex flex-col gap-2.5 py-4">
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
      <p className="m-0 text-sm text-ink-muted">
        {item.label} · {t("from")} {formatDate(item.startsAtUtc, locale)}
        {item.endsAtUtc ? ` · ${t("until")} ${formatDate(item.endsAtUtc, locale)}` : ""}
        {days !== null ? <b className="text-ink"> · {t("daysLeft", { days })}</b> : null}
      </p>
      {whatsApp.number ? (
        <a href={whatsAppUrl(whatsApp.number, text)} className="inline-flex min-h-11 items-center gap-1.5 self-start text-sm font-bold text-orange-text underline hover:text-orange-text-hover">
          <WhatsAppIcon size={16} />
          {t("problem")}
        </a>
      ) : null}
    </li>
  );
}

/** One card per order the search matched, with every part's warranty. */
export async function WarrantyResults({ groups, whatsApp }: { groups: Schemas["WarrantyLookupGroup"][]; whatsApp: WarrantyWhatsApp }) {
  const t = await getTranslations("warranty");
  const locale = await getLocale();

  return (
    <div className="flex flex-col gap-4">
      {groups.map((group) => (
        <section key={`${group.orderReference}-${group.invoiceNumber ?? ""}`} className="rounded-card border border-line bg-surface p-5 md:p-6">
          <header className="flex flex-col gap-1 border-b border-line pb-3">
            <span className="flex flex-wrap items-center gap-x-3 gap-y-1 font-bold">
              <span>{t("order")} <span dir="ltr" className="font-mono">{group.orderReference}</span></span>
              {group.invoiceNumber ? <span className="text-ink-muted">{t("invoice")} <span dir="ltr" className="font-mono">{group.invoiceNumber}</span></span> : null}
            </span>
            <span className="text-sm text-ink-muted">
              {t("purchased", { date: formatDate(group.purchasedAtUtc, locale) })} · {t("phone", { phone: group.phoneMasked })}
            </span>
          </header>
          <ul className="m-0 flex list-none flex-col divide-y divide-line p-0">
            {group.items.map((item) => (
              <WarrantyItem key={item.code} item={item} orderReference={group.orderReference} whatsApp={whatsApp} />
            ))}
          </ul>
        </section>
      ))}
    </div>
  );
}
