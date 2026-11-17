"use client";

import { useLocale, useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { formatDate, formatEgp } from "@/lib/format";
import { fetchInvoices } from "./account-api";
import { invoiceKindKey } from "./account-labels";
import { EmptyState, LoadBoundary } from "./LoadBoundary";
import { useLoad } from "./use-load";

/** "فواتيري": each invoice opens its own page; a cancelled order's invoice is marked void. */
export function InvoicesList() {
  const t = useTranslations("account.invoices");
  const locale = useLocale();
  const { state, reload } = useLoad(fetchInvoices);

  return (
    <div className="flex flex-col gap-4">
      <h2 className="m-0 text-xl font-bold">{t("title")}</h2>
      <LoadBoundary state={state} reload={reload}>
        {(invoices) =>
          invoices.length === 0 ? (
            <EmptyState message={t("empty")} />
          ) : (
            <ul className="m-0 flex list-none flex-col gap-3 p-0">
              {invoices.map((invoice) => {
                const kind = invoiceKindKey(invoice.kind);
                return (
                  <li key={invoice.accessToken} className="flex flex-wrap items-center justify-between gap-3 rounded-[10px] border border-line bg-surface p-5">
                    <div className="flex flex-col gap-1">
                      <span dir="ltr" className="font-mono text-sm font-semibold text-orange-text">{invoice.number}</span>
                      <span className="text-sm text-ink-muted">
                        {kind ? `${t(`kind.${kind}`)} · ` : ""}{formatDate(invoice.issuedAtUtc, locale)}
                      </span>
                    </div>
                    <div className="flex items-center gap-4">
                      {invoice.status === "Void" ? <span className="rounded-full bg-error px-3 py-1 text-xs font-bold text-surface">{t("void")}</span> : null}
                      <b>{formatEgp(invoice.total, locale)}</b>
                      <Link href={`/i/${invoice.accessToken}`} className="inline-flex min-h-11 items-center text-sm font-bold text-orange-text underline hover:text-orange-text-hover">{t("open")}</Link>
                    </div>
                  </li>
                );
              })}
            </ul>
          )
        }
      </LoadBoundary>
    </div>
  );
}
