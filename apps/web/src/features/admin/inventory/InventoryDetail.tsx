"use client";

import { useCallback, useState } from "react";
import { useTranslations } from "next-intl";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { useLoad } from "@/lib/hooks/use-load";
import type { Brand } from "../brands/brands-api";
import type { Category } from "../categories/categories-api";
import type { Partner } from "../expenses/expenses-api";
import { usePermissions } from "../use-permissions";
import { PhotoGallery } from "../stock/PhotoGallery";
import { StockItemForm } from "./StockItemForm";
import { StockUnits } from "./StockUnits";
import { deleteStockPhoto, fetchStockItem, fetchWarrantyLabels, uploadStockPhoto, type StockItem } from "./inventory-api";
import { Link } from "@/i18n/navigation";
import { CloseIcon, CopyIcon, RefreshIcon } from "@/components/ui/icons";
import { Button, buttonClass, buttonIconSize, IconButton } from "../kit/Button";

/** The picked stock item: its edit form and photos, and its units (status, serial, cost, partner) beside or under them. */
export function InventoryDetail({ id, tree, brands, partners, onClose, onChanged }: {
  id: string;
  tree: readonly Category[];
  brands: readonly Brand[];
  partners: readonly Partner[];
  onClose: () => void;
  onChanged: () => void;
}) {
  const t = useTranslations("admin.inventory.detail");
  const { has } = usePermissions();
  const canReadWarranties = has("System.StoreSettings");
  const load = useCallback(async () => {
    const [item, warrantyLabels] = await Promise.all([fetchStockItem(id), canReadWarranties ? fetchWarrantyLabels().catch(() => null) : Promise.resolve(null)]);
    return { item, warrantyLabels };
  }, [id, canReadWarranties]);
  const { state, reload } = useLoad(load);
  // The item as the last save or photo change returned it; a 409 drops it and reads the item again.
  const [fresh, setFresh] = useState<StockItem | null>(null);

  async function refresh() {
    setFresh(await fetchStockItem(id));
    onChanged();
  }
  const stale = () => {
    setFresh(null);
    reload();
  };

  if (state.status === "loading") return <div role="status" aria-label={t("loading")} className="h-64 animate-pulse rounded-card border border-line bg-surface" />;
  if (state.status === "error") {
    return (
      <div role="alert" className="flex flex-col items-start gap-3 rounded-card border border-line bg-surface p-5">
        <p className="m-0 font-bold">{t("failed")}</p>
        <div className="flex flex-wrap gap-2">
          <Button variant="primary" icon={RefreshIcon} onClick={reload}>{t("retry")}</Button>
          <Button icon={CloseIcon} onClick={onClose}>{t("close")}</Button>
        </div>
      </div>
    );
  }

  const item = fresh ?? state.data.item;
  const canEdit = has("Inventory.StockAdjust");

  return (
    <section className="flex min-w-0 flex-col gap-4 rounded-card border border-line bg-surface p-5">
      <header className="flex flex-wrap items-start justify-between gap-2">
        <div className="flex min-w-0 flex-wrap items-center gap-2">
          <h2 className="m-0 text-[17px] font-bold">
            <span dir="ltr" className="font-mono">{item.code}</span> <span dir="auto" className="[unicode-bidi:isolate]">{item.modelName}</span>
          </h2>
          <ConditionBadge condition={item.condition === "New" ? "New" : "Used"} />
          <span className="text-xs text-ink-muted">{item.isPublished ? t("published") : t("notPublished")}</span>
        </div>
        <div className="flex items-center gap-2">
          {has("Purchasing.CreateOrder") ? (
            <Link href={`/admin/purchases/new?copyFrom=${item.id}`} className={buttonClass({ size: "sm" })}>
              <CopyIcon size={buttonIconSize("sm")} />{t("copy")}
            </Link>
          ) : null}
          <IconButton icon={CloseIcon} label={t("close")} onClick={onClose} />
        </div>
      </header>
      <div className="grid items-start gap-5 xl:grid-cols-[minmax(0,1fr)_minmax(0,1fr)]">
        <div className="flex min-w-0 flex-col gap-5">
          <StockItemForm
            key={item.rowVersion} item={item} tree={tree} brands={brands} warrantyLabels={state.data.warrantyLabels} canEdit={canEdit}
            onSaved={(saved) => {
              setFresh(saved);
              onChanged();
            }}
            onStale={stale}
          />
          <PhotoGallery
            photos={item.photos} name={item.modelName} canManage={canEdit} onChanged={refresh}
            upload={(file) => uploadStockPhoto(item.id, file)} remove={(storageKey) => deleteStockPhoto(item.id, storageKey)}
          />
        </div>
        <StockUnits item={item} partners={partners} canAssign={has("Finance.ManagePartnerAgreements")} showCosts={has("Inventory.ViewCosts")} onChanged={refresh} onStale={stale} />
      </div>
    </section>
  );
}
