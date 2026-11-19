import { getLocale, getTranslations } from "next-intl/server";
import QRCode from "qrcode";
import type { ReactNode } from "react";
import type { Schemas } from "@rigx/api-client";
import { invoiceKindKey, paymentMethodKey } from "@/features/account/account-labels";
import { displayPhone, Platform } from "@/lib/contact";
import { formatDateTimeFull, formatEgp } from "@/lib/format";
import type { StorefrontData } from "@/lib/storefront";
import { InvoiceLines } from "./InvoiceLines";
import { isVoid, totalsRows } from "./invoice-lines";

type Invoice = Schemas["InvoiceView"];

function Field({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex flex-col gap-0.5">
      <dt className="text-xs text-ink-muted">{label}</dt>
      <dd dir="auto" className="m-0 font-semibold [unicode-bidi:isolate]">{children}</dd>
    </div>
  );
}

async function TotalRow({ label, amount, strong = false }: { label: string; amount: number; strong?: boolean }) {
  const locale = await getLocale();
  return (
    <div className={`flex justify-between gap-6 ${strong ? "border-t border-ink pt-2 text-base font-bold" : ""}`}>
      <dt>{label}</dt>
      <dd className="m-0 whitespace-nowrap">{formatEgp(amount, locale)}</dd>
    </div>
  );
}

async function Totals({ invoice }: { invoice: Invoice }) {
  const t = await getTranslations("invoice");
  const show = totalsRows(invoice);
  return (
    <dl className="m-0 ms-auto flex w-full max-w-[320px] flex-col gap-1.5 text-sm">
      <TotalRow label={t("subtotal")} amount={invoice.linesTotal} />
      {show.discount ? <TotalRow label={t("discount")} amount={invoice.discount} /> : null}
      {invoice.fulfillmentMethod === "Delivery" ? <TotalRow label={t("delivery")} amount={invoice.deliveryFee} /> : null}
      <TotalRow label={t("total")} amount={invoice.total} strong />
      {show.payments ? <TotalRow label={t("paid")} amount={invoice.paid} /> : null}
      {show.payments ? <TotalRow label={t("remaining")} amount={invoice.remaining} /> : null}
      {show.refunded ? <TotalRow label={t("refunded")} amount={invoice.refunded} /> : null}
    </dl>
  );
}

/** The printable invoice: branding, customer, lines, totals, warranty QR, a watermark and a "void" stamp for a cancelled order. */
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
    ? [invoice.address.governorate, invoice.address.area, invoice.address.street, invoice.address.building, invoice.address.landmark].filter(Boolean).join("، ")
    : "";
  const contactLine = [storefront.domain, ...new Set(phones)].filter(Boolean).join(" · ");
  const placeLine = [storefront.email, branding.pickupAddress].filter(Boolean).join(" · ");

  return (
    <article data-print-invoice className="relative isolate overflow-hidden rounded-card border border-line bg-surface p-6 md:p-10 print:rounded-none print:border-0 print:p-0">
      {branding.showBackgroundLogo ? (
        <span aria-hidden="true" className="pointer-events-none absolute inset-0 -z-10 flex select-none items-center justify-center overflow-hidden">
          <span dir="ltr" className="-rotate-[28deg] whitespace-nowrap text-[96px] font-bold uppercase tracking-widest text-ink opacity-[0.045] [print-color-adjust:exact]">{branding.storeName}</span>
        </span>
      ) : null}
      {isVoid(invoice.status) ? (
        <span className="pointer-events-none absolute end-10 top-24 -rotate-12 select-none rounded-lg border-[5px] border-error px-6 py-1 text-5xl font-bold text-error opacity-80 [print-color-adjust:exact]">{t("void")}</span>
      ) : null}

      <header className="flex flex-wrap items-start justify-between gap-4 border-b-2 border-ink pb-5">
        <div className="flex flex-col gap-1">
          <span dir="ltr" className="font-mono text-2xl font-semibold tracking-[3px]">{branding.storeName}</span>
          {contactLine ? <span dir="ltr" className="text-xs text-ink-muted">{contactLine}</span> : null}
          <span className="text-xs text-ink-muted">{placeLine}</span>
        </div>
        <div className="flex flex-col items-end gap-1 text-end">
          <span className="text-2xl font-bold">{t("title")}</span>
          {kind ? <span className="text-sm text-ink-muted">{t(`kind.${kind}`)}</span> : null}
          <span dir="ltr" className="font-mono text-lg font-semibold">{invoice.number}</span>
        </div>
      </header>

      <dl className="m-0 grid grid-cols-2 gap-x-6 gap-y-4 py-5 md:grid-cols-3">
        <Field label={t("customer")}>{invoice.customerName}</Field>
        <Field label={t("phone")}><span dir="ltr">{invoice.phoneNumber}</span></Field>
        {address ? <Field label={t("address")}>{address}</Field> : null}
        <Field label={t("date")}><span dir="ltr">{formatDateTimeFull(invoice.issuedAtUtc, locale)}</span></Field>
        <Field label={t("order")}><span dir="ltr">{invoice.orderReference}</span></Field>
        <Field label={t("fulfillment.label")}>{invoice.fulfillmentMethod === "Pickup" ? t("fulfillment.Pickup") : t("fulfillment.Delivery")}</Field>
        {method ? <Field label={t("payment")}>{methods(method)}</Field> : null}
        {invoice.sellerName ? <Field label={t("seller")}>{invoice.sellerName}</Field> : null}
      </dl>

      <InvoiceLines lines={invoice.lines} />
      <div className="mt-5 flex flex-col-reverse gap-6 md:flex-row md:items-start md:justify-between">
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

      <footer className="mt-8 flex flex-col gap-1 border-t border-line pt-3 text-xs text-ink-muted">
        {branding.termsLine ? <span>{branding.termsLine}</span> : null}
        <span>{t("electronic")}</span>
      </footer>
    </article>
  );
}
