import { getLocale, getTranslations } from "next-intl/server";
import QRCode from "qrcode";
import type { Schemas } from "@rigx/api-client";
import { cardClass } from "@/components/ui/Card";
import { InfoField, InfoGrid } from "@/components/ui/InfoGrid";
import { Logo } from "@/components/ui/Logo";
import { invoiceKindKey, paymentMethodKey } from "@/features/account/account-labels";
import { displayPhone, Platform } from "@/lib/contact";
import { formatDateTimeFull, formatEgp } from "@/lib/format";
import type { StorefrontData } from "@/lib/storefront";
import { InvoiceLines } from "./InvoiceLines";
import { isVoid, totalsRows } from "./invoice-lines";

type Invoice = Schemas["InvoiceView"];

async function TotalRow({ label, amount, strong = false }: { label: string; amount: number; strong?: boolean }) {
  const locale = await getLocale();
  return (
    <div className={`flex justify-between gap-6 ${strong ? "border-t border-ink pt-2 text-base font-bold" : ""}`}>
      <dt>{label}</dt>
      <dd className="m-0 whitespace-nowrap tabular-nums">{formatEgp(amount, locale)}</dd>
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
    ? [invoice.address.governorate, invoice.address.area, invoice.address.street, invoice.address.building, invoice.address.landmark].filter(Boolean).join(" › ")
    : "";
  const phoneLine = [...new Set(phones)].join(" · ");

  return (
    <article data-print-invoice className={cardClass({ padding: "none", className: "relative isolate overflow-hidden p-6 md:p-10 print:rounded-none print:border-0 print:p-0" })}>
      {branding.showBackgroundLogo ? (
        <span aria-hidden="true" className="pointer-events-none absolute inset-0 -z-10 flex select-none items-center justify-center overflow-hidden">
          <span dir="ltr" className="-rotate-[28deg] whitespace-nowrap text-[96px] font-bold uppercase tracking-widest text-ink opacity-[0.045] [print-color-adjust:exact]">{branding.storeName}</span>
        </span>
      ) : null}
      {isVoid(invoice.status) ? (
        <span className="pointer-events-none absolute end-10 top-24 -rotate-12 select-none rounded-lg border-[5px] border-error px-6 py-1 text-5xl font-bold text-error-text opacity-80 [print-color-adjust:exact]">{t("void")}</span>
      ) : null}

      <header className="grid gap-6 border-b-2 border-ink pb-5 md:grid-cols-2 md:items-start">
        <div className="flex min-w-0 flex-col gap-3">
          <Logo width={140} />
          <span className="sr-only">{branding.storeName}</span>
          <InfoGrid columns={1} className="gap-y-2">
            <InfoField label={t("address")} value={branding.pickupAddress} />
            <InfoField label={t("phone")} ltr value={phoneLine} />
            <InfoField label={t("email")} ltr value={storefront.email} />
          </InfoGrid>
        </div>
        <div className="flex shrink-0 flex-col items-end gap-1 text-end">
          <span className="text-2xl font-bold">{t("title")}</span>
          {kind ? <span className="text-sm text-ink-muted">{t(`kind.${kind}`)}</span> : null}
          <span dir="ltr" className="font-mono text-lg font-semibold tabular-nums">{invoice.number}</span>
          <span className="text-sm text-ink-muted">{formatDateTimeFull(invoice.issuedAtUtc, locale)}</span>
        </div>
      </header>

      <InfoGrid columns={3} className="break-inside-avoid gap-x-6 gap-y-4 py-5">
        <InfoField label={t("customer")} value={invoice.customerName} />
        <InfoField label={t("phone")} ltr value={invoice.phoneNumber} />
        <InfoField label={t("order")} ltr value={invoice.orderReference} />
        <InfoField label={t("fulfillment.label")} value={invoice.fulfillmentMethod === "Pickup" ? t("fulfillment.Pickup") : t("fulfillment.Delivery")} />
        {method ? <InfoField label={t("payment")} value={methods(method)} /> : null}
        {invoice.sellerName ? <InfoField label={t("seller")} value={invoice.sellerName} /> : null}
        {address ? <InfoField label={t("address")} value={address} wide /> : null}
      </InfoGrid>

      <InvoiceLines lines={invoice.lines} />
      <div className="mt-5 flex flex-col-reverse gap-6 break-inside-avoid md:flex-row md:items-start md:justify-between">
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
