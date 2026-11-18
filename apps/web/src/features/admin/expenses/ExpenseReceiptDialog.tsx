"use client";

import { useTranslations } from "next-intl";
import { Dialog } from "../kit/Dialog";
import { expenseReceiptUrl } from "./expenses-api";

/** The private receipt photo of an expense. */
export function ExpenseReceiptDialog({ id, onClose }: { id: string; onClose: () => void }) {
  const t = useTranslations("admin.expenses.receipt");
  return (
    <Dialog open onClose={onClose} title={t("title")}>
      {/* eslint-disable-next-line @next/next/no-img-element -- private admin media needs the session cookie */}
      <img src={expenseReceiptUrl(id)} alt={t("alt")} className="max-h-[60vh] w-full rounded-control border border-line object-contain" />
      <div className="flex justify-end">
        <button type="button" onClick={onClose} className="h-11 rounded-control border border-input-line bg-surface px-5 font-bold">{t("close")}</button>
      </div>
    </Dialog>
  );
}
