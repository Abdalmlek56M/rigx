import { getLocale, getTranslations } from "next-intl/server";
import QRCode from "qrcode";
import type { Schemas } from "@rigx/api-client";
import { invoiceKindKey, paymentMethodKey } from "@/features/account/account-labels";
import { displayPhone, Platform } from "@/lib/contact";
import { formatDateTimeFull, joinValues } from "@/lib/format";
import type { StorefrontData } from "@/lib/storefront";
import { InvoiceBlock } from "./InvoiceBlock";
import { InvoiceBackgroundMark, InvoiceHeader } from "./InvoiceHeader";
import { isVoid, pieceCount, shapeLines } from "./invoice-lines";
import { InvoiceLines } from "./InvoiceLines";
import { InvoiceTotals } from "./InvoiceTotals";

/**
 * The invoice as an official store document: a graphite header with the logo and title; the store, customer and invoice blocks; the item
 * table; the totals beside the warranty QR; the terms. Always a light paper (`doc-paper`), whatever theme the visitor uses, and on screen
 * always the same layout (`DocumentFrame` scales it). On A4 it flows over as many pages as it needs (`data-print-invoice` in the CSS).
 */
export async function InvoiceSheet({ document, storefront }: { document: Schemas["InvoiceDocument"]; storefront: StorefrontData }) {
  const { invoice, branding } = document;
  const t = await getTranslations("invoice");
  const methods = await getTranslations("checkout.payment.methods");
  const locale = await getLocale();
  const kind = invoiceKindKey(invoice.kind);
  const method = paymentMethodKey(invoice.paymentMethod);
  const qr = branding.showWarrantyQr && document.warrantyLookupUrl ? await QRCode.toString(document.warrantyLookupUrl, { type: "svg", margin: 0 }) : null;
  const phones = branding.contacts.flatMap((link) => ((link.platform === Platform.Phone || link.platform === Platform.WhatsApp) && link.value ? [displayPhone(link.value)] : []));
  const address = invoice.address
    ? joinValues([invoice.address.governorate, invoice.address.area, invoice.address.street, invoice.address.building, invoice.address.landmark], locale)
    : "";
  const terms = [t("warrantyNote"), branding.termsLine, t("electronic")].filter((term): term is string => Boolean(term));

  return (
    <article data-print-invoice className="doc-paper relative isolate overflow-hidden rounded-card border border-line bg-surface text-ink print:overflow-visible print:rounded-none print:border-0">
      {branding.showBackgroundLogo ? <InvoiceBackgroundMark storeName={branding.storeName} /> : null}
      {isVoid(invoice.status) ? (
        <span className="pointer-events-none absolute end-10 top-40 -rotate-12 select-none rounded-lg border-[5px] border-error px-6 py-1 text-5xl font-bold text-error-text opacity-80 [print-color-adjust:exact]">{t("void")}</span>
      ) : null}

      <InvoiceHeader storeName={branding.storeName} title={t("title")} kindLabel={kind ? t(`kind.${kind}`) : null} />

      <div className="flex flex-col gap-5 p-8 print:px-0 print:pb-0 print:pt-4">
        <div className="grid grid-cols-[1.15fr_0.9fr_1.1fr] gap-3">
          <InvoiceBlock title={t("store")} name={branding.storeName} facts={[
            { label: t("address"), value: branding.pickupAddress },
            { label: t("phone"), value: [...new Set(phones)].join(" - "), ltr: true },
            { label: t("email"), value: storefront.email, ltr: true }
          ]} />
          <InvoiceBlock title={t("customer")} name={invoice.customerName} facts={[
            { label: t("phone"), value: invoice.phoneNumber, ltr: true },
            { label: t("address"), value: address }
          ]} />
          <InvoiceBlock title={t("details")} facts={[
            { label: t("number"), value: invoice.number, ltr: true },
            { label: t("date"), value: formatDateTimeFull(invoice.issuedAtUtc, locale) },
            { label: t("order"), value: invoice.orderReference, ltr: true },
            { label: t("fulfillment.label"), value: invoice.fulfillmentMethod === "Pickup" ? t("fulfillment.Pickup") : t("fulfillment.Delivery") },
            { label: t("payment"), value: method ? methods(method) : null },
            { label: t("seller"), value: invoice.sellerName }
          ]} />
        </div>

        <InvoiceLines lines={invoice.lines} />

        {/* The totals, the warranty code and the terms close the invoice together: in print they move to the next page as one block. */}
        <div className="flex break-inside-avoid flex-col gap-5">
          <div className="flex items-start justify-between gap-6">
            <div className="flex flex-col gap-3">
              <span className="text-[13px] text-ink-muted">{t("pieces", { count: pieceCount(shapeLines(invoice.lines)) })}</span>
              {qr ? (
                <div className="flex items-start gap-3">
                  <div aria-hidden="true" className="size-24 shrink-0 [&>svg]:size-full [print-color-adjust:exact]" dangerouslySetInnerHTML={{ __html: qr }} />
                  <div className="flex max-w-[250px] flex-col gap-1 text-xs text-ink-muted">
                    <b className="text-sm text-ink">{t("warranty")}</b>
                    <span>{t("qrNote")}</span>
                  </div>
                </div>
              ) : null}
            </div>
            <InvoiceTotals invoice={invoice} />
          </div>

          <footer className="flex flex-col gap-2 border-t-2 border-ink pt-3 text-xs">
            <b className="text-[13px]">{t("terms")}</b>
            <ul className="m-0 flex list-disc flex-col gap-0.5 ps-4 text-ink-muted">
              {terms.map((term) => <li key={term}>{term}</li>)}
            </ul>
            <span className="mt-1 text-center font-bold">{t("thanks")}</span>
          </footer>
        </div>
      </div>
    </article>
  );
}
