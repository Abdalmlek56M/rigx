"use client";

import { useTranslations } from "next-intl";
import { FilterBar, FilterSelect } from "../kit/FilterBar";
import { PageHeader } from "../kit/PageHeader";
import { useUrlFilters } from "../kit/use-url-filters";
import { ChangesView } from "./ChangesView";
import { AUDIT_FILTERS, AUDIT_VIEWS, auditView } from "./audit-rules";
import { SecurityView } from "./SecurityView";

/** ADM-Audit: the data-change log and the sign-in/security log, switched by `view` in the URL. Read only. */
export function AuditScreen() {
  const t = useTranslations("admin.audit");
  const { values, set } = useUrlFilters(AUDIT_FILTERS);
  const view = auditView(values.view);
  return (
    <>
      <PageHeader title={t("title")} />
      <FilterBar>
        <FilterSelect
          titled label={t("viewLabel")} value={view} onChange={(next) => set({ view: auditView(next), entry: "" })}
          options={AUDIT_VIEWS.map((entry) => ({ value: entry, label: t(`views.${entry}`) }))}
        />
      </FilterBar>
      {view === "changes" ? <ChangesView values={values} set={set} /> : <SecurityView values={values} set={set} />}
    </>
  );
}
