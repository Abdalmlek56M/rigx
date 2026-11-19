"use client";

import { useCallback, useState } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { useLoad } from "@/lib/hooks/use-load";
import { fetchCategories } from "../categories/categories-api";
import { cairoToday } from "../dashboard/dashboard-rules";
import { fetchPartners } from "../expenses/expenses-api";
import { fetchShippingRates } from "../customers/customers-api";
import { fetchWarrantyLabels } from "../inventory/inventory-api";
import { PageHeader } from "../kit/PageHeader";
import { financingPartners } from "../stock/stock-fields";
import { usePermissions } from "../use-permissions";
import { PurchaseForm } from "./PurchaseForm";
import { PurchaseReceivedPanel } from "./PurchaseReceivedPanel";
import { PurchaseStatusBadge } from "./PurchaseStatusBadge";
import { fetchPurchase, type Purchase, type PurchaseReceived } from "./purchases-api";

const backLink = "inline-flex h-11 items-center rounded-control border border-input-line bg-surface px-4 text-sm font-bold";

/** `/admin/purchases/new` (no id) and `/admin/purchases/[id]`: loads what the form needs and keeps the purchase as the last action returned it. */
export function PurchaseEditor({ id }: { id: string | null }) {
  const t = useTranslations("admin.purchases");
  const { has } = usePermissions();
  const canSeePartners = has("Finance.ViewReports");
  const canReadWarranties = has("System.StoreSettings");
  const load = useCallback(async () => {
    const [purchase, tree, partners, warrantyLabels, rates] = await Promise.all([
      id ? fetchPurchase(id) : Promise.resolve(null),
      fetchCategories().catch(() => []),
      canSeePartners ? fetchPartners().catch(() => null) : Promise.resolve(null),
      canReadWarranties ? fetchWarrantyLabels().catch(() => null) : Promise.resolve(null),
      fetchShippingRates().catch(() => null)
    ]);
    return {
      purchase, tree, warrantyLabels,
      partners: partners ? financingPartners(partners) : null,
      governorates: (rates?.governorates ?? []).map(({ code, name }) => ({ code, name }))
    };
  }, [id, canSeePartners, canReadWarranties]);
  const { state, reload } = useLoad(load);
  const [today] = useState(() => cairoToday(new Date()));
  // The purchase as the last save or cancel returned it, and what receiving created; a 409 drops them and reads the purchase again.
  const [changed, setChanged] = useState<Purchase | null>(null);
  const [received, setReceived] = useState<PurchaseReceived | null>(null);

  const title = id ? t("detailTitle") : t("newTitle");
  const header = <PageHeader title={title} actions={<Link href="/admin/purchases" className={backLink}>{t("back")}</Link>} />;

  if (state.status === "loading") {
    return (
      <>
        {header}
        <div role="status" aria-label={t("loading")} className="h-64 animate-pulse rounded-card border border-line bg-surface" />
      </>
    );
  }
  if (state.status === "error") {
    return (
      <>
        {header}
        <div role="alert" className="flex flex-col items-start gap-3 rounded-card border border-line bg-surface p-6">
          <p className="m-0 font-bold">{t("failed")}</p>
          <button type="button" onClick={reload} className="h-11 rounded-control bg-carbon px-5 font-bold text-on-carbon">{t("retry")}</button>
        </div>
      </>
    );
  }

  const purchase = received?.purchase ?? changed ?? state.data.purchase;
  return (
    <>
      <PageHeader
        title={purchase ? purchase.code : title}
        actions={
          <>
            {purchase ? <PurchaseStatusBadge status={purchase.status} /> : null}
            <Link href="/admin/purchases" className={backLink}>{t("back")}</Link>
          </>
        }
      />
      {received ? <PurchaseReceivedPanel result={received} /> : null}
      {purchase?.status === "Cancelled" ? <p role="status" className="m-0 rounded-control bg-line px-3 py-2 text-sm font-bold text-ink-muted">{t("cancelledNote")}</p> : null}
      <PurchaseForm
        key={purchase ? `${purchase.id}-${purchase.rowVersion}` : "new"}
        purchase={purchase} today={today} lookups={state.data}
        onUpdated={setChanged}
        onReceived={setReceived}
        onCancelled={setChanged}
        onStale={() => {
          setChanged(null);
          reload();
        }}
      />
    </>
  );
}
