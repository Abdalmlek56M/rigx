"use client";

import { useTranslations } from "next-intl";
import { useRouter } from "@/i18n/navigation";
import { FIELD_BASE } from "@/components/ui/field-class";
import { BUDGETS, buildsHref, isBudget, type BuildsState } from "./build-query";

/** The budget dropdown of the builds list: changing it reloads the list with the budget in the URL. */
export function BudgetSelect({ state }: { state: BuildsState }) {
  const t = useTranslations("builds");
  const tb = useTranslations("home.recommend.budgets");
  const router = useRouter();

  return (
    <label className="flex items-center gap-2 text-sm">
      {t("budget")}
      <select
        value={state.budget ?? ""}
        onChange={(event) => router.push(buildsHref(state, { budget: isBudget(event.target.value) ? event.target.value : null }))}
        className={`${FIELD_BASE} h-11`}
      >
        <option value="">{t("anyBudget")}</option>
        {BUDGETS.map((entry) => (
          <option key={entry} value={entry}>{tb(entry)}</option>
        ))}
      </select>
    </label>
  );
}
