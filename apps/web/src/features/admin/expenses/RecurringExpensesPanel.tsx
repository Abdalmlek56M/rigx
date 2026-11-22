"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatEgp } from "@/lib/format";
import { DataTable, type Column } from "../kit/DataTable";
import { RecurringExpenseDialog } from "./RecurringExpenseDialog";
import type { ExpenseType, MoneyAccount, RecurringExpense } from "./expenses-api";
import { PencilIcon, PlusIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";
import { StatusBadge } from "@/components/ui/StatusBadge";

/** The monthly fixed expenses the job posts on their day. */
export function RecurringExpensesPanel({ templates, types, accounts, canSeeVaults, canManage, onChanged }: {
  templates: readonly RecurringExpense[];
  types: readonly ExpenseType[];
  accounts: readonly MoneyAccount[];
  canSeeVaults: boolean;
  canManage: boolean;
  onChanged: () => void;
}) {
  const t = useTranslations("admin.expenses.recurring");
  const activeLabel = useTranslations("admin.expenses.active");
  const locale = useLocale();
  const [editing, setEditing] = useState<RecurringExpense | "new" | null>(null);
  const accountsById = new Map(accounts.map((account) => [account.id, account]));

  const columns: Column<RecurringExpense>[] = [
    { key: "category", header: t("category"), cell: (template) => <span className="font-bold">{template.category}</span> },
    { key: "amount", header: t("amount"), align: "end", cell: (template) => <span className="whitespace-nowrap font-bold">{formatEgp(template.amount, locale)}</span> },
    { key: "day", header: t("day"), align: "end", cell: (template) => <span className="text-sm">{template.dayOfMonth}</span> },
    ...(canSeeVaults ? [{ key: "account", header: t("account"), cell: (template: RecurringExpense) => <span className="text-xs text-ink-muted">{template.moneyAccountId ? accountsById.get(template.moneyAccountId)?.name ?? t("defaultAccount") : t("defaultAccount")}</span> }] : []),
    { key: "active", header: t("active"), cell: (template) => <StatusBadge tone={template.isActive ? "success" : "neutral"}>{template.isActive ? activeLabel("yes") : activeLabel("no")}</StatusBadge> },
    ...(canManage ? [{ key: "edit", header: "", cell: (template: RecurringExpense) => <Button size="sm" variant="ghost" icon={PencilIcon} onClick={() => setEditing(template)}>{t("edit")}</Button> }] : [])
  ];

  return (
    <section className="flex flex-col gap-3 rounded-card border border-line bg-surface p-5">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div className="flex flex-col gap-0.5">
          <h2 className="m-0 text-[17px] font-bold">{t("title")}</h2>
          <p className="m-0 text-xs text-ink-muted">{t("hint")}</p>
        </div>
        {canManage ? <Button icon={PlusIcon} onClick={() => setEditing("new")}>{t("add")}</Button> : null}
      </div>
      <DataTable label={t("title")} columns={columns} state={{ status: "ready", data: templates }} onRetry={onChanged} rowKey={(template) => template.id} empty={t("empty")} />
      {editing ? (
        <RecurringExpenseDialog
          template={editing === "new" ? null : editing} types={types} accounts={accounts} canSeeVaults={canSeeVaults}
          onSaved={onChanged} onStale={onChanged} onClose={() => setEditing(null)}
        />
      ) : null}
    </section>
  );
}
