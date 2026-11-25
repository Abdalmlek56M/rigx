"use client";

import { useCallback, useMemo, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { PlusIcon } from "@/components/ui/icons";
import { formatDateTime, formatEgp } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { usePermissions } from "../use-permissions";
import { Button } from "../kit/Button";
import { actionsColumn, DataTable, type Column } from "../kit/DataTable";
import { RowAction } from "../kit/RowAction";
import { StatusBadge } from "@/components/ui/StatusBadge";
import type { StatusTone } from "@/components/ui/status-badge-rules";
import { ErrorPanel, LoadingCard } from "../kit/LoadState";
import { PageHeader } from "../kit/PageHeader";
import { EditSheet } from "../kit/EditSheet";
import { useUrlFilters } from "../kit/use-url-filters";
import { CouponForm } from "./CouponForm";
import { couponStatus, couponTotals, sortCoupons, type CouponStatus } from "./coupon-rules";
import { fetchCoupons, type Coupon } from "./coupons-api";

const FILTERS = { id: "" };

const TONES: Record<CouponStatus, StatusTone> = { running: "success", off: "neutral", notStarted: "neutral", ended: "danger" };

function CouponStatusBadge({ coupon }: { coupon: Coupon }) {
  const t = useTranslations("admin.coupons.statuses");
  const status = couponStatus(coupon, new Date());
  return <StatusBadge tone={TONES[status]}>{t(status)}</StatusBadge>;
}

/** ADM-Coupons: the report per code (source, discount, status, uses, revenue), then the form for the selected or a new code. */
export function Coupons() {
  const t = useTranslations("admin.coupons");
  const locale = useLocale();
  const { has } = usePermissions();
  const { values, set } = useUrlFilters(FILTERS);
  const [creating, setCreating] = useState(false);
  const { state, reload } = useLoad(fetchCoupons);
  const coupons = useMemo(() => (state.status === "ready" ? sortCoupons(state.data) : []), [state]);
  const money = has("Finance.ViewReports");
  const activeId = creating ? null : values.id || null;
  const selected = coupons.find((coupon) => coupon.id === activeId) ?? null;
  const totals = couponTotals(coupons);
  const takenCodes = useMemo(() => coupons.map((coupon) => coupon.code), [coupons]);
  const closeSheet = () => { setCreating(false); set({ id: "" }); };

  const discount = useCallback((coupon: Coupon) => (
    coupon.discountKind === "Amount" ? (
      <span className="font-bold">{formatEgp(coupon.discountValue, locale)}</span>
    ) : (
      <span className="flex flex-col">
        <span className="font-bold">{coupon.discountValue}%</span>
        {coupon.maxDiscount !== null ? <span className="text-xs text-ink-muted">{t("maxDiscountNote", { amount: formatEgp(coupon.maxDiscount, locale) })}</span> : null}
      </span>
    )
  ), [locale, t]);

  const columns: Column<Coupon>[] = [
    {
      key: "source", header: t("columns.source"),
      cell: (coupon) => (
        <button type="button" aria-current={coupon.id === activeId ? "true" : undefined} onClick={() => { setCreating(false); set({ id: coupon.id }); }} className={`flex min-w-0 flex-col items-start gap-0.5 text-start ${coupon.id === activeId ? "text-orange-text" : ""}`}>
          <span dir="auto" className="max-w-64 truncate font-bold [unicode-bidi:isolate]">{coupon.sourceName}</span>
          <span dir="ltr" className="font-mono text-xs text-ink-muted">{coupon.code}</span>
        </button>
      )
    },
    { key: "discount", header: t("columns.discount"), cell: discount },
    { key: "status", header: t("columns.status"), cell: (coupon) => <CouponStatusBadge coupon={coupon} /> },
    {
      key: "uses", header: t("columns.uses"), align: "end",
      cell: (coupon) => (
        <span className="flex flex-col items-end whitespace-nowrap">
          <span className="font-bold">{coupon.maxUses === null ? coupon.uses : t("usesOf", { uses: coupon.uses, max: coupon.maxUses })}</span>
          <span className="text-xs text-ink-muted">{t("completed", { count: coupon.completedUses })}</span>
        </span>
      )
    },
    ...(money ? [
      { key: "revenue", header: t("columns.revenue"), align: "end" as const, cell: (coupon: Coupon) => <span className="whitespace-nowrap font-bold">{formatEgp(coupon.revenue ?? 0, locale)}</span> },
      { key: "discountGiven", header: t("columns.discountGiven"), align: "end" as const, cell: (coupon: Coupon) => <span className="whitespace-nowrap">{formatEgp(coupon.discountGiven ?? 0, locale)}</span> }
    ] : []),
    {
      key: "lastUsed", header: t("columns.lastUsed"),
      cell: (coupon) => <span className="whitespace-nowrap text-xs">{coupon.lastUsedAtUtc ? formatDateTime(coupon.lastUsedAtUtc, locale) : t("none")}</span>
    },
    actionsColumn((coupon) => (
      <RowAction kind="edit" subject={coupon.code} current={coupon.id === activeId} onClick={() => { setCreating(false); set({ id: coupon.id }); }} />
    ))
  ];

  return (
    <>
      <PageHeader
        title={t("title")}
        actions={<Button variant="primary" icon={PlusIcon} onClick={() => { setCreating(true); set({ id: "" }); }}>{t("new")}</Button>}
      />
      {state.status === "error" ? (
        <ErrorPanel message={t("failed")} retryLabel={t("retry")} onRetry={reload} />
      ) : (
        <>
          {state.status === "loading" ? <LoadingCard className="h-48" /> : (
            <>
              <DataTable label={t("title")} columns={columns} state={{ status: "ready", data: coupons }} onRetry={reload} rowKey={(coupon) => coupon.id} empty={t("empty")} />
              {coupons.length > 0 ? (
                <p className="m-0 text-sm font-bold">
                  {money
                    ? t("totalsMoney", { uses: totals.uses, completed: totals.completedUses, revenue: formatEgp(totals.revenue, locale), discount: formatEgp(totals.discountGiven, locale) })
                    : t("totals", { uses: totals.uses, completed: totals.completedUses })}
                </p>
              ) : null}
            </>
          )}
          <EditSheet open={creating || selected !== null} title={creating ? t("newTitle") : t("editTitle")} onClose={closeSheet}>
            {creating ? (
              <CouponForm key="new" coupon={null} takenCodes={takenCodes} onSaved={(saved) => { setCreating(false); set({ id: saved.id }); reload(); }} onStale={reload} onCancel={closeSheet} />
            ) : selected ? (
              <CouponForm key={`${selected.id}-${selected.rowVersion}`} coupon={selected} takenCodes={takenCodes} onSaved={(saved) => { set({ id: saved.id }); reload(); }} onStale={reload} onCancel={closeSheet} onDeleted={() => { set({ id: "" }); reload(); }} />
            ) : null}
          </EditSheet>
        </>
      )}
    </>
  );
}
