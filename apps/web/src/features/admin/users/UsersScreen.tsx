"use client";

import { useTranslations } from "next-intl";
import { FilterBar, FilterSelect } from "../kit/FilterBar";
import { PageHeader } from "../kit/PageHeader";
import { useUrlFilters } from "../kit/use-url-filters";
import { RolesView } from "./RolesView";
import { USER_FILTERS, USER_VIEWS, userView } from "./user-rules.ts";
import { UsersView } from "./UsersView";

/** ADM-Users: the staff list and the roles list, switched by `view` in the URL. */
export function UsersScreen() {
  const t = useTranslations("admin.users");
  const { values, set } = useUrlFilters(USER_FILTERS);
  const view = userView(values.view);
  return (
    <>
      <PageHeader title={t("title")} />
      <FilterBar>
        <FilterSelect
          label={t("viewLabel")} value={view} onChange={(next) => set({ view: userView(next), user: "", role: "" })}
          options={USER_VIEWS.map((entry) => ({ value: entry, label: t(`views.${entry}`) }))}
        />
      </FilterBar>
      {view === "users" ? <UsersView values={values} set={set} /> : <RolesView values={values} set={set} />}
    </>
  );
}
