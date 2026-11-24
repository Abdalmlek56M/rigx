import { getLocale, getTranslations } from "next-intl/server";
import QRCode from "qrcode";
import type { Schemas } from "@rigx/api-client";
import { InfoField, InfoGrid } from "@/components/ui/InfoGrid";
import { invoiceKindKey, paymentMethodKey } from "@/features/account/account-labels";
import { displayPhone, Platform } from "@/lib/contact";
import { formatDateTimeFull, formatEgp } from "@/lib/format";
import type { StorefrontData } from "@/lib/storefront";
import { InvoiceBackgroundMark, InvoiceHeader } from "./InvoiceHeader";
import { InvoiceLines } from "./InvoiceLines";
import { isVoid, totalsRows } from "./invoice-lines";

type Invoice = Schemas["InvoiceView"];

const BOX = "flex flex-col gap-3 rounded-card border border-line p-4";
const BOX_TITLE = "m-0 border-b border-line pb-2 text-xs font-bold text-ink-muted";

async function TotalRow({ label, amount, strong = false, zeroText }: { label: string; amount: number; strong?: boolean; zeroText?: string }) {
  const locale = await getLocale();
  return (
    <div className={strong ? "flex justify-between gap-6 rounded-control bg-carbon px-3 py-2.5 text-base font-bold text-on-carbon" : "flex justify-between gap-6 px-3"}>
      <dt>{label}</dt>
      <dd className="m-0 whitespace-nowrap tabular-nums">{amount === 0 && zeroText ? zeroText : formatEgp(amount, locale)}</dd>
    </div>
  );
}

async function Totals({ invoice }: { invoice: Invoice }) {
  const t = await getTranslations("invoice");
  const show = totalsRows(invoice);
  return (
    <dl className="m-0 flex w-[320px] shrink-0 flex-col gap-2 text-sm">
      <TotalRow label={t("subtotal")} amount={invoice.linesTotal} />
      {show.discount ? <TotalRow label={t("discount")} amount={invoice.discount} /> : null}
      {invoice.fulfillmentMethod === "Delivery" ? <TotalRow label={t("delivery")} amount={invoice.deliveryFee} zeroText={t("freeShipping")} /> : null}
      <TotalRow label={t("total")} amount={invoice.total} strong />
      {show.payments ? <TotalRow label={t("paid")} amount={invoice.paid} /> : null}
      {show.payments ? <TotalRow label={t("remaining")} amount={invoice.remaining} /> : null}
      {show.refunded ? <TotalRow label={t("refunded")} amount={invoice.refunded} /> : null}
    </dl>
  );
}

/**
 * The invoice as an official store document: a graphite header with the logo, the invoice title, number and date; boxes for the store, the
 * customer and the order; the lines with each part's condition (new or used) and warranty; the totals; the warranty QR; the terms. Always a
 * light paper (`doc-paper`), whatever theme the visitor uses, and always the same layout (`DocumentFrame` scales it).
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
    ? [invoice.address.governorate, invoice.address.area, invoice.address.street, invoice.address.building, invoice.address.landmark].filter(Boolean).join(" › ")
    : "";
  const phoneLine = [...new Set(phones)].join(" ، ");

  return (
    <article data-print-invoice className="doc-paper relative isolate overflow-hidden rounded-card border border-line bg-surface text-ink print:rounded-none print:border-0">
      {branding.showBackgroundLogo ? <InvoiceBackgroundMark storeName={branding.storeName} /> : null}
      {isVoid(invoice.status) ? (
        <span className="pointer-events-none absolute end-10 top-40 -rotate-12 select-none rounded-lg border-[5px] border-error px-6 py-1 text-5xl font-bold text-error-text opacity-80 [print-color-adjust:exact]">{t("void")}</span>
      ) : null}

      <InvoiceHeader storeName={branding.storeName} title={t("title")} kindLabel={kind ? t(`kind.${kind}`) : null} />

      <div className="flex flex-col gap-5 p-8">
        <div className="grid grid-cols-3 gap-4">
          <section className={BOX}>
            <h2 className={BOX_TITLE}>{t("store")}</h2>
            <InfoGrid columns={1} className="gap-y-2">
              <InfoField label={t("address")} value={branding.pickupAddress} />
              <InfoField label={t("phone")} ltr value={phoneLine} />
              <InfoField label={t("email")} ltr value={storefront.email} />
            </InfoGrid>
          </section>
          <section className={BOX}>
            <h2 className={BOX_TITLE}>{t("customer")}</h2>
            <InfoGrid columns={1} className="gap-y-2">
              <InfoField label={t("name")} value={invoice.customerName} />
              <InfoField label={t("phone")} ltr value={invoice.phoneNumber} />
              {address ? <InfoField label={t("address")} value={address} /> : null}
            </InfoGrid>
          </section>
          <section className={BOX}>
            <h2 className={BOX_TITLE}>{t("details")}</h2>
            <InfoGrid columns={1} className="gap-y-2">
              <InfoField label={t("number")} ltr value={invoice.number} />
              <InfoField label={t("date")} value={formatDateTimeFull(invoice.issuedAtUtc, locale)} />
              <InfoField label={t("order")} ltr value={invoice.orderReference} />
              <InfoField label={t("fulfillment.label")} value={invoice.fulfillmentMethod === "Pickup" ? t("fulfillment.Pickup") : t("fulfillment.Delivery")} />
              {method ? <InfoField label={t("payment")} value={methods(method)} /> : null}
              {invoice.sellerName ? <InfoField label={t("seller")} value={invoice.sellerName} /> : null}
            </InfoGrid>
          </section>
        </div>

        <InvoiceLines lines={invoice.lines} />

        <div className="flex items-start justify-between gap-6 break-inside-avoid">
          {qr ? (
            <div className="flex items-start gap-3 break-inside-avoid">
              <div aria-hidden="true" className="size-24 shrink-0 [&>svg]:size-full [print-color-adjust:exact]" dangerouslySetInnerHTML={{ __html: qr }} />
              <div className="flex max-w-[260px] flex-col gap-1 text-xs text-ink-muted">
                <b className="text-sm text-ink">{t("warranty")}</b>
                <span>{t("qrNote")}</span>
                <span>{t("warrantyNote")}</span>
              </div>
            </div>
          ) : <span />}
          <Totals invoice={invoice} />
        </div>

        <footer className="flex flex-col gap-1 border-t border-line pt-3 text-xs text-ink-muted">
          {branding.termsLine ? <span>{branding.termsLine}</span> : null}
          <span>{t("thanks")}</span>
          <span>{t("electronic")}</span>
        </footer>
      </div>
    </article>
  );
}
