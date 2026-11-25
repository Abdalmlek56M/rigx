"use client";

import { useCallback } from "react";
import { useTranslations } from "next-intl";
import { ModalSheet } from "@/components/ui/ModalSheet";
import { Link } from "@/i18n/navigation";
import { useLoad } from "@/lib/hooks/use-load";
import { fetchBrands } from "../brands/brands-api";
import type { Category } from "../categories/categories-api";
import { buttonClass, buttonIconSize, IconButton } from "../kit/Button";
import { ErrorPanel, LoadingCard } from "../kit/LoadState";
import { usePermissions } from "../use-permissions";
import { CloseIcon, EyeIcon } from "@/components/ui/icons";
import { fetchWarrantyLabels, type StockItem } from "./inventory-api";
import { StockItemForm } from "./StockItemForm";

interface QuickEditProps {
  item: StockItem | null;
  tree: readonly Category[];
  onClose: () => void;
  /** The item was saved, or a newer copy exists: the list reads again. */
  onChanged: () => void;
}

function Editor({ item, tree, onClose, onChanged }: QuickEditProps & { item: StockItem }) {
  const t = useTranslations("admin.inventory.quick");
  const { has } = usePermissions();
  const canReadWarranties = has("System.StoreSettings");
  const { state, reload } = useLoad(useCallback(async () => {
    const [brands, warrantyLabels] = await Promise.all([fetchBrands(), canReadWarranties ? fetchWarrantyLabels().catch(() => null) : Promise.resolve(null)]);
    return { brands, warrantyLabels };
  }, [canReadWarranties]));

  return (
    <div className="flex h-full flex-col gap-4 overflow-y-auto overscroll-contain bg-paper p-4 md:p-5">
      <div className="flex items-start justify-between gap-3">
        <div className="flex min-w-0 flex-col gap-0.5">
          <h2 className="m-0 type-section-title">{t("title")}</h2>
          <p dir="auto" className="m-0 truncate text-sm text-ink-muted [unicode-bidi:isolate]"><span dir="ltr" className="font-mono font-bold">{item.code}</span> {item.modelName}</p>
        </div>
        <IconButton icon={CloseIcon} label={t("close")} onClick={onClose} />
      </div>
      {state.status === "loading" ? <LoadingCard className="h-48" label={t("loading")} /> : null}
      {state.status === "error" ? <ErrorPanel message={t("failed")} retryLabel={t("retry")} onRetry={reload} /> : null}
      {state.status === "ready" ? (
        <section className="rounded-card border border-line bg-surface p-4">
          <StockItemForm
            item={item} tree={tree} brands={state.data.brands} warrantyLabels={state.data.warrantyLabels} canEdit showCosts={has("Inventory.ViewCosts")}
            onSaved={() => {
              onChanged();
              onClose();
            }}
            onStale={onChanged}
          />
        </section>
      ) : null}
      <Link href={`/admin/inventory/${item.id}`} className={`${buttonClass()} self-start`}>
        <EyeIcon size={buttonIconSize()} />{t("fullPage")}
      </Link>
    </div>
  );
}

/** The quick edit of a stock item from the list: the item's own form in a side sheet, so the list keeps its place and filters. */
export function QuickEditSheet(props: QuickEditProps) {
  const t = useTranslations("admin.inventory.quick");
  const { item, onClose } = props;
  return (
    <ModalSheet open={item !== null} onClose={onClose} label={t("title")} edge="end" fullHeight wide>
      {item ? <Editor {...props} item={item} /> : null}
    </ModalSheet>
  );
}
