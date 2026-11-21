"use client";

import { DocumentIcon, EyeIcon } from "@/components/ui/icons";
import { useLocale, useTranslations } from "next-intl";
import { Card } from "@/components/ui/Card";
import { InfoField, InfoGrid } from "@/components/ui/InfoGrid";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { invoiceTone } from "@/components/ui/status-badge-rules";
import { formatDate, formatEgp } from "@/lib/format";
import { fetchInvoices } from "./account-api";
import { invoiceKindKey } from "./account-labels";
import { EmptyState, LoadBoundary } from "./LoadBoundary";
import { RowLink } from "./RowAction";
import { useLoad } from "@/lib/hooks/use-load";

/** "فواتيري": each invoice opens its own page; a cancelled order's invoice is marked void. */
export function InvoicesList() {
  const t = useTranslations("account.invoices");
  const locale = useLocale();
  const { state, reload } = useLoad(fetchInvoices);

  return (
    <div className="flex flex-col gap-4">
      <h2 className="sr-only">{t("title")}</h2>
      <LoadBoundary state={state} reload={reload}>
        {(invoices) =>
          invoices.length === 0 ? (
            <EmptyState icon={DocumentIcon} message={t("empty")} />
          ) : (
            <ul className="m-0 flex list-none flex-col gap-3 p-0">
              {invoices.map((invoice) => {
                const kind = invoiceKindKey(invoice.kind);
                const voided = invoice.status === "Void";
                return (
                  <Card
                    key={invoice.accessToken}
                    as="li"
                    title={<span dir="ltr" className="font-mono text-orange-text">{invoice.number}</span>}
                    icon={DocumentIcon}
                    padding="md"
                    className="gap-3"
                    actions={
                      <div className="flex items-center gap-1.5">
                        {voided ? <StatusBadge tone={invoiceTone(invoice.status)}>{t("void")}</StatusBadge> : null}
                        <RowLink href={`/i/${invoice.accessToken}`} label={t("open")} icon={EyeIcon} />
                      </div>
                    }
                  >
                    <InfoGrid columns={2} className="gap-y-3">
                      {kind ? <InfoField label={t("kindLabel")} value={t(`kind.${kind}`)} /> : null}
                      <InfoField label={t("date")} value={formatDate(invoice.issuedAtUtc, locale)} />
                      <InfoField label={t("total")} value={formatEgp(invoice.total, locale)} />
                    </InfoGrid>
                  </Card>
                );
              })}
            </ul>
          )
        }
      </LoadBoundary>
    </div>
  );
}
