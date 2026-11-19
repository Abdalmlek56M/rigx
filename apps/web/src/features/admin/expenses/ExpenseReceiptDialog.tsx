"use client";

import { useTranslations } from "next-intl";
import { Dialog } from "../kit/Dialog";
import { expenseReceiptUrl } from "./expenses-api";
import { CloseIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

/** The private receipt photo of an expense. */
export function ExpenseReceiptDialog({ id, onClose }: { id: string; onClose: () => void }) {
  const t = useTranslations("admin.expenses.receipt");
  return (
    <Dialog open onClose={onClose} title={t("title")}>
      {/* eslint-disable-next-line @next/next/no-img-element -- private admin media needs the session cookie */}
      <img src={expenseReceiptUrl(id)} alt={t("alt")} className="max-h-[60vh] w-full rounded-control border border-line object-contain" />
      <div className="flex justify-end">
        <Button icon={CloseIcon} onClick={onClose}>{t("close")}</Button>
      </div>
    </Dialog>
  );
}
