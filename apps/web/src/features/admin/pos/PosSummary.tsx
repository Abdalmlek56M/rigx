"use client";

import { useLocale, useTranslations } from "next-intl";
import { useCouponProblemText } from "@/lib/coupon-problem-text";
import { formatEgp } from "@/lib/format";
import { FormMessage } from "../kit/PageHeader";
import type { PosPreview } from "./pos-api";
import { BanIcon, CheckIcon, ClockIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

interface PosSummaryProps {
  lineCount: number;
  /** The server's pricing of the cart; null until the first answer or while the cart has nothing to price. */
  preview: PosPreview | null;
  /** The server's message when the last preview failed (the previous numbers stay on screen). */
  previewError: string | null;
  busy: boolean;
  /** The server's message for a sale that failed. */
  saleError: string | null;
  onSell: (confirm: boolean) => void;
  couponCode: string;
  onCouponCode: (code: string) => void;
}

function Row({ label, value, strong = false, tone }: { label: string; value: string; strong?: boolean; tone?: "error" }) {
  return (
    <div className={`flex items-baseline justify-between gap-3 ${strong ? "border-t border-line pt-2.5 text-lg font-bold" : "text-sm"}`}>
      <dt className={strong ? undefined : "text-ink-muted"}>{label}</dt>
      <dd dir="ltr" className={`m-0 text-end font-bold ${tone === "error" ? "text-error-text" : ""}`}>{value}</dd>
    </div>
  );
}

/** The sticky side: what the server priced (cost and profit only when the staff may see them), warnings and the two sell buttons. */
export function PosSummary({ lineCount, preview, previewError, busy, saleError, onSell, couponCode, onCouponCode }: PosSummaryProps) {
  const t = useTranslations("admin.pos.summary");
  const locale = useLocale();
  const egp = (amount: number) => formatEgp(amount, locale);
  const problemText = useCouponProblemText();
  const coupon = preview?.coupon ?? null;

  return (
    <aside aria-label={t("title")} className="flex flex-col gap-3.5 rounded-card border border-line bg-surface p-5 xl:sticky xl:top-4 xl:self-start">
      <h2 className="m-0 text-lg font-bold">{t("title")}</h2>
      {preview === null ? <p className="m-0 text-sm text-ink-muted">{lineCount === 0 ? t("empty") : t("calculating")}</p> : (
        <dl className="m-0 flex flex-col gap-2">
          <Row label={t("lines")} value={String(lineCount)} />
          <Row label={t("sale")} value={egp(preview.linesTotal)} />
          {preview.discount > 0 ? <Row label={t("discount")} value={`− ${egp(preview.discount)}`} /> : null}
          {coupon && !coupon.problem ? <Row label={t("couponDiscount", { code: coupon.code ?? "" })} value={`− ${egp(coupon.discount)}`} /> : null}
          {preview.deliveryFee > 0 ? <Row label={t("delivery")} value={egp(preview.deliveryFee)} /> : null}
          {preview.costTotal !== null ? <Row label={t("cost")} value={egp(preview.costTotal)} /> : null}
          {preview.profit !== null ? <Row label={t("profit")} value={egp(preview.profit)} tone={preview.profit < 0 ? "error" : undefined} /> : null}
          {(preview.partnerShares ?? []).map((share) => <Row key={share.partnerId} label={`${t("partners")} · ${share.partnerName}`} value={egp(share.amount)} />)}
          <Row label={t("total")} value={egp(preview.total)} strong />
        </dl>
      )}
      <label className="flex flex-col gap-1.5 text-[13px] font-bold">
        {t("couponLabel")}
        <input
          dir="ltr" value={couponCode} maxLength={30} autoComplete="off"
          onChange={(event) => onCouponCode(event.target.value.toUpperCase())}
          className="h-11 w-full rounded-control border border-input-line bg-surface px-3 text-start font-mono text-sm font-normal text-ink"
        />
        {coupon?.problem ? <span role="alert" className="text-xs font-normal text-error-text">{problemText(coupon.problem)}</span> : null}
      </label>
      {preview?.customerBlacklisted ? (
        <p role="alert" className="m-0 flex items-start gap-2 rounded-control border border-error/50 bg-error/10 px-3 py-2 text-sm font-bold text-error-text">
          <span className="mt-0.5"><BanIcon size={16} /></span>
          {t("blacklisted")}
        </p>
      ) : null}
      {preview && preview.warnings.length > 0 ? (
        <div role="status" className="flex flex-col gap-1 rounded-control bg-orange/15 px-3 py-2 text-sm">
          <b>{t("warnings")}</b>
          <ul className="m-0 list-disc ps-5">{preview.warnings.map((warning) => <li key={warning}>{warning}</li>)}</ul>
        </div>
      ) : null}
      {preview && preview.returningToStock.length > 0 ? (
        <p className="m-0 text-[13px] text-ink-muted">
          {t("returning", { count: preview.returningToStock.length, codes: preview.returningToStock.map((part) => part.code).join(", ") })}
        </p>
      ) : null}
      {previewError ? <p role="alert" className="m-0 text-xs text-error-text">{t("previewFailed", { message: previewError })}</p> : null}
      <FormMessage message={saleError} />
      <div className="flex flex-col gap-2">
        <Button variant="primary" icon={CheckIcon} loading={busy} disabled={lineCount === 0} onClick={() => onSell(true)}>
          {t("confirm")}
        </Button>
        <Button icon={ClockIcon} loading={busy} disabled={lineCount === 0} onClick={() => onSell(false)}>
          {t("pending")}
        </Button>
      </div>
    </aside>
  );
}
