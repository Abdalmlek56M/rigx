"use client";

import { useLocale, useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { Card } from "@/components/ui/Card";
import { InfoField, InfoGrid } from "@/components/ui/InfoGrid";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { warrantyTone } from "@/components/ui/status-badge-rules";
import { DocumentIcon, ShieldIcon, WhatsAppIcon } from "@/components/ui/icons";
import { WarrantyBattery } from "@/features/warranty/WarrantyBattery";
import { warrantyBattery } from "@/features/warranty/warranty-battery";
import { parseCondition } from "@/lib/condition";
import { fillTemplate, whatsAppUrl } from "@/lib/contact";
import { formatDate } from "@/lib/format";
import { fetchInvoices, fetchWarranties } from "./account-api";
import { invoiceTokenFor, warrantyStatusKey } from "./account-labels";
import type { AccountContact } from "./account-page";
import { EmptyState, LoadBoundary } from "./LoadBoundary";
import { RowLink } from "./RowAction";
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
  const tCommon = useTranslations("common");
  const locale = useLocale();
  const status = warrantyStatusKey(warranty.status);
  const condition = parseCondition(warranty.condition);
  const battery = warrantyBattery(warranty.status, warranty.daysLeft, warranty.startsAtUtc, warranty.endsAtUtc);
  const text = fillTemplate(whatsApp.warrantyTemplate, { item: warranty.name, code: warranty.code, order: warranty.orderReference })
    || t("whatsAppMessage", { name: warranty.name, code: warranty.code });

  return (
    <Card
      as="li"
      title={<span dir="auto" className="[unicode-bidi:isolate]">{warranty.name}</span>}
      icon={ShieldIcon}
      padding="md"
      className="gap-3"
      actions={
        <div className="flex flex-wrap items-center gap-1.5">
          {status ? <StatusBadge tone={warrantyTone(status)}>{t(`status.${status}`)}</StatusBadge> : null}
          {invoiceToken ? <RowLink href={`/i/${invoiceToken}`} label={t("invoice")} icon={DocumentIcon} /> : null}
          {whatsApp.number ? <RowLink href={whatsAppUrl(whatsApp.number, text)} label={t("problem")} icon={WhatsAppIcon} external /> : null}
        </div>
      }
    >
      {battery ? <WarrantyBattery battery={battery} days={status === "Active" ? warranty.daysLeft : null} /> : null}
      <InfoGrid columns={2} className="gap-y-3">
        <InfoField label={t("code")} value={warranty.code} ltr copy={{ value: warranty.code, label: tCommon("copy"), copiedLabel: tCommon("copied") }} />
        <InfoField label={t("plan")} value={warranty.label} />
        {condition ? <InfoField label={t("condition")} value={<ConditionBadge condition={condition} />} /> : null}
        {warranty.buildCode ? <InfoField label={t("build")} value={warranty.buildCode} ltr /> : null}
        <InfoField label={t("from")} value={formatDate(warranty.startsAtUtc, locale)} />
        {warranty.endsAtUtc ? <InfoField label={t("until")} value={formatDate(warranty.endsAtUtc, locale)} /> : null}
      </InfoGrid>
    </Card>
  );
}

/** "ضمان منتجاتي": every warranty on the account with its status, days left, invoice and a WhatsApp link for problems. */
export function WarrantiesList({ whatsApp }: { whatsApp: WhatsApp }) {
  const t = useTranslations("account.warranties");
  const { state, reload } = useLoad(loadWarranties);

  return (
    <div className="flex flex-col gap-4">
      <h2 className="sr-only">{t("title")}</h2>
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
