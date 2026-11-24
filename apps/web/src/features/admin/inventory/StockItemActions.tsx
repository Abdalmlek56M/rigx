"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { toFormErrors } from "../kit/form-errors";
import { useToast } from "../kit/Toast";
import { usePermissions } from "../use-permissions";
import { setItemsHidden, type StockItem } from "./inventory-api";
import { photosTooFew } from "./inventory-rules";
import { BanIcon, CartIcon, CopyIcon, ExternalIcon, EyeIcon } from "@/components/ui/icons";
import { Button, buttonClass, buttonIconSize } from "../kit/Button";

/** What staff can do with the item from its page: repeat it as a new purchase line, hide or show it, sell it, or see it on the store. */
export function StockItemActions({ item, onChanged }: { item: StockItem; onChanged: () => Promise<void> }) {
  const t = useTranslations("admin.inventory.detail.actions");
  const toast = useToast();
  const { has } = usePermissions();
  const [busy, setBusy] = useState(false);

  async function toggleHidden() {
    setBusy(true);
    try {
      await setItemsHidden([item.id], !item.isHidden);
      await onChanged();
      toast(t(item.isHidden ? (photosTooFew(item) ? "shownNeedsPhotos" : "shownDone") : "hiddenDone"));
    } catch (error) {
      toast(toFormErrors(error, t("failed")).general ?? t("failed"), "error");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="flex flex-wrap items-center gap-2">
      {has("Sales.CreatePOS") ? (
        <Link href={`/admin/pos?add=${encodeURIComponent(item.code)}`} className={buttonClass({ variant: "primary" })}>
          <CartIcon size={buttonIconSize()} />{t("sell")}
        </Link>
      ) : null}
      {has("Purchasing.CreateOrder") ? (
        <Link href={`/admin/purchases/new?copyFrom=${item.id}`} className={buttonClass()}>
          <CopyIcon size={buttonIconSize()} />{t("repeat")}
        </Link>
      ) : null}
      {has("Inventory.StockAdjust") ? (
        <Button icon={item.isHidden ? EyeIcon : BanIcon} loading={busy} onClick={() => void toggleHidden()}>{t(item.isHidden ? "show" : "hide")}</Button>
      ) : null}
      {item.isPublished ? (
        <Link href={`/p/${item.code}`} target="_blank" rel="noreferrer" className={buttonClass()}>
          <ExternalIcon size={buttonIconSize()} />{t("viewOnStore")}
        </Link>
      ) : null}
    </div>
  );
}
