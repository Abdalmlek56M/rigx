"use client";

import { useLocale, useTranslations } from "next-intl";
import { Card } from "@/components/ui/Card";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { TicketIcon } from "@/components/ui/icons";
import { orderTone } from "@/components/ui/status-badge-rules";
import { orderStatusKey } from "@/features/tracking/tracking-steps";
import { Link } from "@/i18n/navigation";
import { formatDate, formatEgp } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { fetchCouponUses } from "./account-api";
import { totalSaved } from "./coupon-history";

/**
 * "الكوبونات التي استخدمتها": every coupon the customer used, with what it took off and the order it was used on. Nothing is shown when
 * there is none (or while loading), so a customer who never used a coupon sees no empty card.
 */
export function CouponHistory() {
  const t = useTranslations("account.coupons");
  const tStatus = useTranslations("orderStatus");
  const locale = useLocale();
  const { state } = useLoad(fetchCouponUses);
  if (state.status !== "ready" || state.data.length === 0) return null;
  const uses = state.data;

  return (
    <Card
      title={t("title")} icon={TicketIcon} padding="md" className="gap-3"
      actions={<StatusBadge tone="success">{t("saved", { amount: formatEgp(totalSaved(uses), locale) })}</StatusBadge>}
    >
      <ul className="m-0 flex list-none flex-col gap-2.5 p-0">
        {uses.map((use) => {
          const status = orderStatusKey(use.orderStatus);
          return (
            <li key={use.orderId} className="flex flex-wrap items-center justify-between gap-x-4 gap-y-1.5 border-b border-dashed border-line pb-2.5 last:border-b-0 last:pb-0">
              <span className="flex min-w-0 flex-col gap-0.5">
                <span dir="ltr" className="font-mono text-sm font-bold">{use.code}</span>
                <span className="text-xs text-ink-muted">
                  <Link href={`/account/orders/${use.orderId}`} dir="ltr" className="font-mono text-orange-text hover:underline">{use.reference}</Link>
                  {" "}
                  {formatDate(use.usedAtUtc, locale)}
                </span>
              </span>
              <span className="flex items-center gap-2">
                {status === "Cancelled" ? <StatusBadge tone={orderTone(status)}>{tStatus(status)}</StatusBadge> : null}
                <b className="tabular-nums">{t("discount", { amount: formatEgp(use.discount, locale) })}</b>
              </span>
            </li>
          );
        })}
      </ul>
    </Card>
  );
}
