"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { usePermissions } from "../use-permissions";
import { ReasonDialog } from "./ReasonDialog";
import { blacklistOrderCustomer, type Order } from "./orders-api";

/** "Add the customer to the blacklist" (the order's phone, email and address identify them); hidden when they are listed already. */
export function OrderBlacklistAction({ order, onAdded }: { order: Pick<Order, "id" | "isBlacklisted">; onAdded: () => void }) {
  const t = useTranslations("admin.orders.actions.blacklistAdd");
  const { has } = usePermissions();
  const [open, setOpen] = useState(false);
  if (order.isBlacklisted || !has("Sales.ApproveOrder")) return null;
  return (
    <>
      <button type="button" onClick={() => setOpen(true)} className="h-11 rounded-control border border-input-line bg-surface px-4 text-sm font-bold">{t("button")}</button>
      <ReasonDialog
        open={open} title={t("title")} body={t("body")} label={t("reason")} field="reason" maxLength={500}
        valid={(reason) => reason.trim().length > 0} confirmLabel={t("confirm")} danger success={t("done")}
        onClose={() => setOpen(false)}
        onConfirm={async (reason) => {
          await blacklistOrderCustomer(order.id, reason);
          onAdded();
        }}
      />
    </>
  );
}
