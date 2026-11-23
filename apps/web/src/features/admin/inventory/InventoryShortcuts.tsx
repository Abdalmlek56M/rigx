"use client";

import { useTranslations } from "next-intl";
import { BanIcon, CartIcon, ExternalIcon, EyeIcon, PencilIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import { buttonClass, buttonIconSize, IconButton } from "../kit/Button";
import { usePermissions } from "../use-permissions";
import type { StockItem } from "./inventory-api";

/** On a phone every shortcut keeps a 44 px touch area. */
const TOUCH = "max-md:size-11";
const linkClass = `tip ${buttonClass({ variant: "ghost", size: "sm", iconOnly: true })} ${TOUCH}`;

interface ShortcutsProps {
  item: StockItem;
  onQuickEdit: (item: StockItem) => void;
  onToggleHidden: (item: StockItem) => void;
  /** True while this item's visibility is being changed. */
  busy: boolean;
}

/** The quick actions of an item row: edit in a side sheet, hide or show in the store, add to the till, preview in the store. Each one follows the person's permissions. */
export function InventoryShortcuts({ item, onQuickEdit, onToggleHidden, busy }: ShortcutsProps) {
  const t = useTranslations("admin.inventory.shortcuts");
  const { has } = usePermissions();
  const canAdjust = has("Inventory.StockAdjust");
  const size = buttonIconSize("sm");
  const sellable = item.availableCount > 0;
  const previewable = item.isPublished && !item.isHidden;
  return (
    <span className="flex items-center gap-1">
      {canAdjust ? <IconButton icon={PencilIcon} size="sm" label={t("edit")} className={TOUCH} onClick={() => onQuickEdit(item)} /> : null}
      {canAdjust ? (
        <IconButton
          icon={item.isHidden ? EyeIcon : BanIcon} size="sm" label={t(item.isHidden ? "show" : "hide")} className={TOUCH}
          loading={busy} onClick={() => onToggleHidden(item)}
        />
      ) : null}
      {has("Sales.CreatePOS") && sellable ? (
        <Link href={`/admin/pos?add=${encodeURIComponent(item.code)}`} aria-label={t("sell")} data-tip={t("sell")} className={linkClass}>
          <CartIcon size={size} />
        </Link>
      ) : null}
      {previewable ? (
        <Link href={`/p/${item.code}`} target="_blank" rel="noreferrer" aria-label={t("preview")} data-tip={t("preview")} className={linkClass}>
          <ExternalIcon size={size} />
        </Link>
      ) : null}
    </span>
  );
}
