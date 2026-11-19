"use client";

import type { ReactNode } from "react";
import { useLocale, useTranslations } from "next-intl";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { Link } from "@/i18n/navigation";
import { formatEgp } from "@/lib/format";
import { mediaUrl } from "@/lib/media";
import type { CartLine } from "@/features/cart/cart-rules";
import { FieldAlert, useFieldError, type SectionProps } from "./form-parts";
import type { Quote } from "./use-quote";

interface OrderSummaryProps extends Pick<SectionProps, "form" | "set" | "errors"> {
  lines: CartLine[];
  subtotal: number;
  quote: Quote | null;
  /** Set when the order is below the store's minimum. */
  minimum: number | null;
  busy: boolean;
  /** Why the last send failed (already worded), shown above the button. */
  problem: ReactNode;
}

function Row({ label, value, strong }: { label: string; value: string; strong?: boolean }) {
  return (
    <div className={`flex items-baseline justify-between gap-3 text-sm ${strong ? "font-bold" : ""}`}>
      <span className={strong ? "" : "text-ink-muted"}>{label}</span>
      <span>{value}</span>
    </div>
  );
}

/** The cart lines, the amounts (shipping from the quote) and the confirm button with the terms tick. */
export function OrderSummary({ form, set, errors, lines, subtotal, quote, minimum, busy, problem }: OrderSummaryProps) {
  const t = useTranslations("checkout.summary");
  const locale = useLocale();
  const error = useFieldError(errors);
  const shipping = quote ? (quote.shippingFee > 0 ? formatEgp(quote.shippingFee, locale) : t("freeShipping")) : t("shippingPending");

  return (
    <aside className="flex flex-col gap-3.5 rounded-card border border-line bg-surface p-5 md:sticky md:top-24 md:self-start md:p-6">
      <h2 className="m-0 text-lg font-bold">{t("title")}</h2>
      <ul className="m-0 flex list-none flex-col gap-3 p-0">
        {lines.map((line) => (
          <li key={`${line.code}:${line.priceOption}`} className="flex items-center gap-3">
            <span className="h-11 w-14 shrink-0 overflow-hidden rounded bg-sand">
              {line.thumbnailStorageKey ? (
                // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
                <img src={mediaUrl(line.thumbnailStorageKey)} alt="" className="h-full w-full object-contain" />
              ) : null}
            </span>
            <span className="flex min-w-0 flex-1 flex-col gap-0.5">
              <span dir="auto" className="truncate text-start text-sm font-bold [unicode-bidi:isolate]">
                {line.quantity > 1 ? `${line.quantity} × ` : ""}{line.modelName}
              </span>
              <span className="flex items-center gap-1.5 text-xs text-ink-muted">
                {line.condition ? <ConditionBadge condition={line.condition} /> : null}
                <span dir="ltr" className="font-mono">{line.code}</span>
              </span>
            </span>
            <b className="text-sm">{formatEgp(line.unitPrice * line.quantity, locale)}</b>
          </li>
        ))}
      </ul>
      <div className="flex flex-col gap-2 border-t border-line pt-3.5">
        <Row label={t("subtotal")} value={formatEgp(subtotal, locale)} />
        <Row label={t("shipping")} value={shipping} />
        {quote && quote.upfrontTotal > 0 ? <Row label={t("upfront")} value={formatEgp(quote.upfrontTotal, locale)} /> : null}
        <div className="flex items-baseline justify-between border-t border-line pt-2.5">
          <b>{t("total")}</b>
          <b className="text-[22px]">{formatEgp(subtotal + (quote?.shippingFee ?? 0), locale)}</b>
        </div>
      </div>
      <label className="flex items-start gap-2.5 text-[13px] leading-6">
        <input
          type="checkbox" checked={form.termsAccepted} onChange={(event) => set("termsAccepted", event.target.checked)}
          aria-invalid={errors.includes("terms") ? true : undefined} className="mt-1 h-[18px] w-[18px] shrink-0 accent-carbon"
        />
        <span>
          {t.rich("terms", {
            terms: (chunks) => <Link href="/policies/terms" className="text-orange-text underline hover:text-orange-text-hover">{chunks}</Link>,
            warranty: (chunks) => <Link href="/policies/warranty" className="text-orange-text underline hover:text-orange-text-hover">{chunks}</Link>,
            returns: (chunks) => <Link href="/policies/returns" className="text-orange-text underline hover:text-orange-text-hover">{chunks}</Link>
          })}
        </span>
      </label>
      <FieldAlert>{error("terms")}</FieldAlert>
      {minimum !== null ? <p role="alert" className="m-0 text-sm text-error">{t("minimum", { amount: formatEgp(minimum, locale) })}</p> : null}
      {problem}
      <button
        type="submit" disabled={busy || minimum !== null}
        className="flex h-[52px] items-center justify-center rounded-control bg-orange font-bold text-on-orange disabled:cursor-not-allowed disabled:bg-disabled disabled:text-ink-muted"
      >
        {busy ? t("sending") : t("confirm")}
      </button>
      <p className="m-0 text-center text-[13px] text-ink-muted">{t("callNote")}</p>
    </aside>
  );
}
