"use client";

import { useCallback, useState } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { useLoad } from "@/lib/hooks/use-load";
import { fetchCategories } from "../categories/categories-api";
import { cairoToday } from "../dashboard/dashboard-rules";
import { fetchShippingRates } from "../customers/customers-api";
import { fetchStockItem, fetchWarrantyLabels } from "../inventory/inventory-api";
import { fetchBrands } from "../brands/brands-api";
import { ErrorPanel, LoadingCard } from "../kit/LoadState";
import { PageHeader } from "../kit/PageHeader";
import { usePartners } from "../stock/use-partners";
import { usePermissions } from "../use-permissions";
import { PurchaseForm } from "./PurchaseForm";
import { PurchaseReceivedPanel } from "./PurchaseReceivedPanel";
import { PurchaseStatusBadge } from "./PurchaseStatusBadge";
import { fetchPurchase, type Purchase, type PurchaseReceived } from "./purchases-api";
import { ArrowBackIcon } from "@/components/ui/icons";
import { buttonClass, buttonIconSize } from "../kit/Button";

/** `/admin/purchases/new` (no id) and `/admin/purchases/[id]`: loads what the form needs and keeps the purchase as the last action returned it. */
export function PurchaseEditor({ id, copyFromId = null }: { id: string | null; copyFromId?: string | null }) {
  const t = useTranslations("admin.purchases");
  const { has } = usePermissions();
  const partners = usePartners(has("Finance.ViewReports"));
  const canReadWarranties = has("System.StoreSettings");
  const load = useCallback(async () => {
    const [purchase, tree, brands, copySource, warrantyLabels, rates] = await Promise.all([
      id ? fetchPurchase(id) : Promise.resolve(null),
      fetchCategories().catch(() => []),
      fetchBrands().catch(() => []),
      !id && copyFromId ? fetchStockItem(copyFromId).catch(() => null) : Promise.resolve(null),
      canReadWarranties ? fetchWarrantyLabels().catch(() => null) : Promise.resolve(null),
      fetchShippingRates().catch(() => null)
    ]);
    return {
      purchase, tree, brands, copySource, warrantyLabels,
      governorates: (rates?.governorates ?? []).map(({ code, name }) => ({ code, name }))
    };
  }, [id, copyFromId, canReadWarranties]);
  const { state, reload } = useLoad(load);
  const [today] = useState(() => cairoToday(new Date()));
  // The purchase as the last save or cancel returned it, and what receiving created; a 409 drops them and reads the purchase again.
  const [changed, setChanged] = useState<Purchase | null>(null);
  const [received, setReceived] = useState<PurchaseReceived | null>(null);

  const title = id ? t("detailTitle") : t("newTitle");
  const header = <PageHeader title={title} actions={<Link href="/admin/purchases" className={buttonClass()}><ArrowBackIcon size={buttonIconSize()} />{t("back")}</Link>} />;

  if (state.status === "loading") {
    return (
      <>
        {header}
        <LoadingCard className="h-64" label={t("loading")} />
      </>
    );
  }
  if (state.status === "error") {
    return (
      <>
        {header}
        <ErrorPanel message={t("failed")} retryLabel={t("retry")} onRetry={reload} />
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
            <Link href="/admin/purchases" className={buttonClass()}><ArrowBackIcon size={buttonIconSize()} />{t("back")}</Link>
          </>
        }
      />
      {received ? <PurchaseReceivedPanel result={received} /> : null}
      {purchase?.status === "Cancelled" ? <p role="status" className="m-0 rounded-control bg-line px-3 py-2 text-sm font-bold text-ink-muted">{t("cancelledNote")}</p> : null}
      <PurchaseForm
        key={purchase ? `${purchase.id}-${purchase.rowVersion}` : "new"}
        purchase={purchase} today={today} lookups={{ ...state.data, partners }}
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
