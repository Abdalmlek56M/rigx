"use client";

import type { ReactNode } from "react";
import { useLocale, useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { WalletIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import { formatEgp } from "@/lib/format";
import { mediaUrl } from "@/lib/media";
import type { CartLine } from "@/features/cart/cart-rules";
import { FieldAlert, useFieldError, type SectionProps } from "./form-parts";
import { CouponBox, type AppliedCoupon } from "./CouponBox";
import type { Quote } from "./use-quote";

interface OrderSummaryProps extends Pick<SectionProps, "form" | "set" | "errors"> {
  lines: CartLine[];
  subtotal: number;
  quote: Quote | null;
  /** Set when the order is below the store's minimum. */
  minimum: number | null;
  busy: boolean;
  coupon: {
    applied: AppliedCoupon | null;
    problem: string | null;
    phone: string | null;
    onApplied: (coupon: AppliedCoupon) => void;
    onRemoved: () => void;
    onProblem: (problem: string | null) => void;
  };
  /** Why the last send failed (already worded), shown above the button. */
  problem: ReactNode;
}

function Row({ label, value, strong }: { label: string; value: string; strong?: boolean }) {
  return (
    <div className={`flex items-baseline justify-between gap-3 text-sm ${strong ? "font-bold" : ""}`}>
      <dt className={strong ? "" : "text-ink-muted"}>{label}</dt>
      <dd className="m-0">{value}</dd>
    </div>
  );
}

/** The cart lines, the amounts (shipping from the quote) and the confirm button with the terms tick. */
export function OrderSummary({ form, set, errors, lines, subtotal, quote, minimum, busy, coupon, problem }: OrderSummaryProps) {
  const t = useTranslations("checkout.summary");
  const locale = useLocale();
  const error = useFieldError(errors);
  const shipping = quote ? (quote.shippingFee > 0 ? formatEgp(quote.shippingFee, locale) : t("freeShipping")) : t("shippingPending");

  return (
    <Card as="aside" title={t("title")} icon={WalletIcon} padding="md" className="gap-3.5 lg:sticky lg:top-24 lg:self-start">
      <ul className="m-0 flex list-none flex-col gap-3 p-0">
        {lines.map((line) => (
          <li key={`${line.code}:${line.priceOption}`} className="flex items-center gap-3">
            <span className="flex aspect-square h-12 w-12 shrink-0 items-center justify-center overflow-hidden rounded-control bg-paper">
              {line.thumbnailStorageKey ? (
                // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
                <img src={mediaUrl(line.thumbnailStorageKey)} alt="" className="h-full w-full object-contain p-1" />
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
            <b className="text-sm tabular-nums">{formatEgp(line.unitPrice * line.quantity, locale)}</b>
          </li>
        ))}
      </ul>
      <div className="border-t border-line pt-3.5">
        <CouponBox
          orderTotal={subtotal} phone={coupon.phone} applied={coupon.applied} problem={coupon.problem}
          onApplied={coupon.onApplied} onRemoved={coupon.onRemoved} onProblem={coupon.onProblem}
        />
      </div>
      <dl className="m-0 flex flex-col gap-2 border-t border-line pt-3.5">
        <Row label={t("subtotal")} value={formatEgp(subtotal, locale)} />
        {coupon.applied ? <Row label={t("couponDiscount")} value={`− ${formatEgp(coupon.applied.discount, locale)}`} /> : null}
        <Row label={t("shipping")} value={shipping} />
        {quote && quote.upfrontTotal > 0 ? <Row label={t("upfront")} value={formatEgp(quote.upfrontTotal, locale)} /> : null}
        <div className="flex items-baseline justify-between border-t border-line pt-2.5">
          <dt className="font-bold">{t("total")}</dt>
          <dd className="m-0 text-[22px] font-bold tabular-nums">{formatEgp(subtotal - (coupon.applied?.discount ?? 0) + (quote?.shippingFee ?? 0), locale)}</dd>
        </div>
      </dl>
      <label className="flex items-start gap-2.5 text-xs leading-6">
        <input
          type="checkbox" name="termsAccepted" checked={form.termsAccepted} onChange={(event) => set("termsAccepted", event.target.checked)}
          aria-invalid={errors.includes("terms") ? true : undefined} className="mt-1 h-[18px] w-[18px] shrink-0 accent-ink"
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
      {minimum !== null ? <p role="alert" className="m-0 text-sm text-error-text">{t("minimum", { amount: formatEgp(minimum, locale) })}</p> : null}
      {problem}
      <Button type="submit" variant="accent" loading={busy} disabled={minimum !== null} className="w-full">
        {busy ? t("sending") : t("confirm")}
      </Button>
      <p className="m-0 text-center text-xs text-ink-muted">{t("callNote")}</p>
    </Card>
  );
}
