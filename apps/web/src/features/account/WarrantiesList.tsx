"use client";

import { useLocale, useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { ShieldIcon, WhatsAppIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import { parseCondition } from "@/lib/condition";
import { fillTemplate, whatsAppUrl, NEW_TAB } from "@/lib/contact";
import { formatDate } from "@/lib/format";
import { fetchInvoices, fetchWarranties } from "./account-api";
import { invoiceTokenFor, warrantyStatusKey, WARRANTY_BADGE } from "./account-labels";
import type { AccountContact } from "./account-page";
import { EmptyState, LoadBoundary } from "./LoadBoundary";
import { useLoad } from "@/lib/hooks/use-load";

type Warranty = Schemas["WarrantyAccountView"];
type WhatsApp = Pick<AccountContact, "number" | "warrantyTemplate">;

/** The warranties, plus the invoices they can link to (a failed invoice list only drops the links). */
async function loadWarranties() {
  const [warranties, invoices] = await Promise.all([fetchWarranties(), fetchInvoices().catch(() => [])]);
  return { warranties, invoices };
}

function WarrantyCard({ warranty, invoiceToken, whatsApp }: { warranty: Warranty; invoiceToken: string | null; whatsApp: WhatsApp }) {
  const t = useTranslations("account.warranties");
  const locale = useLocale();
  const status = warrantyStatusKey(warranty.status);
  const condition = parseCondition(warranty.condition);
  const text = fillTemplate(whatsApp.warrantyTemplate, { item: warranty.name, code: warranty.code, order: warranty.orderReference })
    || t("whatsAppMessage", { name: warranty.name, code: warranty.code });
  const action = "inline-flex min-h-11 items-center gap-1.5 text-sm font-bold";

  return (
    <li className="flex flex-col gap-3 rounded-card border border-line bg-surface p-5">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div className="flex min-w-0 flex-col gap-1">
          <span dir="auto" className="font-bold [unicode-bidi:isolate]">{warranty.name}</span>
          <span className="flex flex-wrap items-center gap-2">
            <span dir="ltr" className="font-mono text-xs text-ink-muted">{warranty.code}</span>
            {condition ? <ConditionBadge condition={condition} /> : null}
            {warranty.buildCode ? <span dir="ltr" className="font-mono text-xs text-ink-muted">{warranty.buildCode}</span> : null}
          </span>
        </div>
        {status ? <span className={`rounded-full px-3 py-1 text-xs font-bold ${WARRANTY_BADGE[status]}`}>{t(`status.${status}`)}</span> : null}
      </div>
      <dl className="m-0 flex flex-col gap-1.5 text-sm">
        <div className="flex justify-between gap-4"><dt className="text-ink-muted">{t("plan")}</dt><dd className="m-0">{warranty.label}</dd></div>
        <div className="flex justify-between gap-4"><dt className="text-ink-muted">{t("from")}</dt><dd className="m-0">{formatDate(warranty.startsAtUtc, locale)}</dd></div>
        {warranty.endsAtUtc ? <div className="flex justify-between gap-4"><dt className="text-ink-muted">{t("until")}</dt><dd className="m-0">{formatDate(warranty.endsAtUtc, locale)}</dd></div> : null}
      </dl>
      {status === "Active" && warranty.daysLeft !== null ? <p className="m-0 text-sm font-bold">{t("daysLeft", { days: warranty.daysLeft })}</p> : null}
      <div className="flex flex-wrap gap-x-5">
        {invoiceToken ? <Link href={`/i/${invoiceToken}`} className={`${action} text-orange-text underline hover:text-orange-text-hover`}>{t("invoice")}</Link> : null}
        {whatsApp.number ? (
          <a {...NEW_TAB} href={whatsAppUrl(whatsApp.number, text)} className={`${action} text-orange-text underline hover:text-orange-text-hover`}>
            <WhatsAppIcon size={16} />
            {t("problem")}
          </a>
        ) : null}
      </div>
    </li>
  );
}

/** "ضمان منتجاتي": every warranty on the account with its status, days left, invoice and a WhatsApp link for problems. */
export function WarrantiesList({ whatsApp }: { whatsApp: WhatsApp }) {
  const t = useTranslations("account.warranties");
  const { state, reload } = useLoad(loadWarranties);

  return (
    <div className="flex flex-col gap-4">
      <h2 className="m-0 text-xl font-bold">{t("title")}</h2>
      <LoadBoundary state={state} reload={reload}>
        {({ warranties, invoices }) =>
          warranties.length === 0 ? (
            <EmptyState icon={ShieldIcon} message={t("empty")} />
          ) : (
            <ul className="m-0 flex list-none flex-col gap-3 p-0">
              {warranties.map((warranty) => (
                <WarrantyCard key={warranty.code} warranty={warranty} invoiceToken={invoiceTokenFor(warranty, invoices)} whatsApp={whatsApp} />
              ))}
            </ul>
          )
        }
      </LoadBoundary>
    </div>
  );
}
