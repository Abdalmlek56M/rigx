"use client";

import { useCallback, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { ArrowBackIcon, RefreshIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import { formatDate } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { fetchBrands } from "../brands/brands-api";
import { fetchCategories } from "../categories/categories-api";
import { fetchPartners } from "../expenses/expenses-api";
import { Button, buttonClass, buttonIconSize } from "../kit/Button";
import { PageHeader } from "../kit/PageHeader";
import { PhotoGallery } from "../stock/PhotoGallery";
import { usePermissions } from "../use-permissions";
import { StockItemActions } from "./StockItemActions";
import { StockItemForm } from "./StockItemForm";
import { StockItemHistory } from "./StockItemHistory";
import { StockUnits } from "./StockUnits";
import { deleteStockPhoto, fetchStockItem, fetchWarrantyLabels, reorderStockPhotos, uploadStockPhoto } from "./inventory-api";
import type { StockItem } from "./inventory-api";

/** `/admin/inventory/[id]`: one stock item's control page: actions, details form, photos, units and change history. */
export function StockItemPage({ id }: { id: string }) {
  const t = useTranslations("admin.inventory.detail");
  const locale = useLocale();
  const { has } = usePermissions();
  const canSeePartners = has("Finance.ViewReports");
  const canReadWarranties = has("System.StoreSettings");
  const load = useCallback(async () => {
    const [item, tree, brands, partners, warrantyLabels] = await Promise.all([
      fetchStockItem(id),
      fetchCategories().catch(() => []),
      fetchBrands().catch(() => []),
      canSeePartners ? fetchPartners().catch(() => []) : Promise.resolve([]),
      canReadWarranties ? fetchWarrantyLabels().catch(() => null) : Promise.resolve(null)
    ]);
    return { item, tree, brands, partners, warrantyLabels };
  }, [id, canSeePartners, canReadWarranties]);
  const { state, reload } = useLoad(load);
  // The item as the last save or photo change returned it; a 409 drops it and reads the item again.
  const [fresh, setFresh] = useState<StockItem | null>(null);

  const back = <Link href="/admin/inventory" className={buttonClass()}><ArrowBackIcon size={buttonIconSize()} />{t("back")}</Link>;
  if (state.status === "loading") {
    return (
      <>
        <PageHeader title={t("pageTitle")} actions={back} />
        <div role="status" aria-label={t("loading")} className="h-64 animate-pulse rounded-card border border-line bg-surface" />
      </>
    );
  }
  if (state.status === "error") {
    return (
      <>
        <PageHeader title={t("pageTitle")} actions={back} />
        <div role="alert" className="flex flex-col items-start gap-3 rounded-card border border-line bg-surface p-6">
          <p className="m-0 font-bold">{t("failed")}</p>
          <Button variant="primary" icon={RefreshIcon} onClick={reload}>{t("retry")}</Button>
        </div>
      </>
    );
  }

  const { tree, brands, partners, warrantyLabels } = state.data;
  const item = fresh ?? state.data.item;
  const canEdit = has("Inventory.StockAdjust");
  const refresh = async () => setFresh(await fetchStockItem(id));
  const stale = () => {
    setFresh(null);
    reload();
  };
  const facts: { label: string; value: string }[] = [
    { label: t("facts.available"), value: String(item.availableCount) },
    { label: t("facts.days"), value: t("facts.daysValue", { count: item.daysInStock }) },
    { label: t("facts.added"), value: formatDate(item.createdAtUtc, locale) }
  ];

  return (
    <>
      <PageHeader title={item.code} actions={back} />
      <section className="flex min-w-0 flex-col gap-4 rounded-card border border-line bg-surface p-5">
        <div className="flex flex-wrap items-center gap-2">
          <h2 dir="auto" className="m-0 text-[17px] font-bold [unicode-bidi:isolate]">{item.modelName}</h2>
          <ConditionBadge condition={item.condition === "New" ? "New" : "Used"} />
          <span className="text-xs text-ink-muted">{item.isPublished ? t("published") : t("notPublished")}</span>
        </div>
        <dl className="m-0 grid grid-cols-2 gap-3 sm:grid-cols-3">
          {facts.map((fact) => (
            <div key={fact.label} className="flex flex-col gap-0.5">
              <dt className="text-xs text-ink-muted">{fact.label}</dt>
              <dd className="m-0 font-bold">{fact.value}</dd>
            </div>
          ))}
        </dl>
        <StockItemActions item={item} onChanged={refresh} />
      </section>
      <div className="grid items-start gap-5 xl:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
        <div className="flex min-w-0 flex-col gap-5">
          <section className="flex min-w-0 flex-col gap-4 rounded-card border border-line bg-surface p-5">
            <StockItemForm
              key={item.rowVersion} item={item} tree={tree} brands={brands} warrantyLabels={warrantyLabels} canEdit={canEdit}
              onSaved={setFresh}
              onStale={stale}
            />
          </section>
          <PhotoGallery
            photos={item.photos} name={item.modelName} canManage={canEdit} onChanged={refresh}
            upload={(file) => uploadStockPhoto(item.id, file)} remove={(storageKey) => deleteStockPhoto(item.id, storageKey)}
            reorder={(keys) => reorderStockPhotos(item.id, keys)}
          />
        </div>
        <div className="flex min-w-0 flex-col gap-5">
          <section className="flex min-w-0 flex-col gap-4 rounded-card border border-line bg-surface p-5">
            <StockUnits item={item} partners={partners} canAssign={has("Finance.ManagePartnerAgreements")} showCosts={has("Inventory.ViewCosts")} onChanged={refresh} onStale={stale} />
          </section>
          {has("System.ViewAuditLogs") ? <StockItemHistory key={item.rowVersion} itemId={item.id} /> : null}
        </div>
      </div>
    </>
  );
}
