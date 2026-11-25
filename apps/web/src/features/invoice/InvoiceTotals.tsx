import { getLocale, getTranslations } from "next-intl/server";
import type { Schemas } from "@rigx/api-client";
import { formatEgp } from "@/lib/format";
import { totalsRows } from "./invoice-lines";

async function TotalRow({ label, amount, strong = false, zeroText }: { label: string; amount: number; strong?: boolean; zeroText?: string }) {
  const locale = await getLocale();
  return (
    <div className={`flex justify-between gap-6 border-b border-line px-3 py-2 last:border-b-0 ${strong ? "bg-carbon text-base font-bold text-on-carbon [print-color-adjust:exact]" : ""}`}>
      <dt>{label}</dt>
      <dd className="m-0 whitespace-nowrap font-bold tabular-nums">{amount === 0 && zeroText ? zeroText : formatEgp(amount, locale)}</dd>
    </div>
  );
}

/** The totals box: the lines, the discount, shipping, the total to pay, and what has been paid, is left or was returned. */
export async function InvoiceTotals({ invoice }: { invoice: Schemas["InvoiceView"] }) {
  const t = await getTranslations("invoice");
  const show = totalsRows(invoice);
  return (
    <dl className="m-0 flex w-[300px] shrink-0 flex-col overflow-hidden rounded-control border border-line text-[13px]">
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
