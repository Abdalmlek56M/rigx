"use client";

import { useCallback, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatMonthYear } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { FilterBar, FilterSelect } from "../kit/FilterBar";
import { EmptyPanel, ErrorPanel, LoadingCard } from "../kit/LoadState";
import { DaySheet } from "./DaySheet";
import { MonthSection } from "./MonthSection";
import { fetchAuditFilters, fetchMonths } from "./audit-api";
import { defaultOpenMonth, type AuditSet, type AuditValues } from "./audit-rules";
import { useAuditLabels } from "./use-audit-labels";

/** The data-change log read month → day → entry: filters, a header per month that opens its day cards, and the picked day in a side sheet. */
export function ChangesView({ values, set }: { values: AuditValues; set: AuditSet }) {
  const t = useTranslations("admin.audit");
  const locale = useLocale();
  const labels = useAuditLabels();
  const filters = useLoad(useCallback(() => fetchAuditFilters(), []));
  const months = useLoad(useCallback(
    () => fetchMonths({ module: values.module || undefined, entity: values.entity || undefined, action: values.action || undefined }),
    [values.module, values.entity, values.action]
  ));
  const [toggled, setToggled] = useState<Readonly<Record<string, boolean>>>({});
  const options = filters.state.status === "ready" ? filters.state.data : { modules: [], entities: [], actions: [] };
  const list = months.state.status === "ready" ? months.state.data : null;
  const startOpen = list ? defaultOpenMonth(list, values.day) : "";
  const pickDay = (day: string) => set({ day, entry: "", page: "1" });

  return (
    <>
      <FilterBar>
        <FilterSelect
          label={t("filters.module")} value={values.module} onChange={(module) => set({ module })}
          options={[{ value: "", label: t("filters.all") }, ...options.modules.map((module) => ({ value: module, label: labels.module(module) }))]}
        />
        <FilterSelect
          label={t("filters.entity")} value={values.entity} onChange={(entity) => set({ entity })}
          options={[{ value: "", label: t("filters.all") }, ...options.entities.map((entity) => ({ value: entity, label: labels.entity(entity) }))]}
        />
        <FilterSelect
          label={t("filters.action")} value={values.action} onChange={(action) => set({ action })}
          options={[{ value: "", label: t("filters.all") }, ...options.actions.map((action) => ({ value: action, label: labels.action(action) }))]}
        />
      </FilterBar>
      <div className="flex min-w-0 flex-col gap-4">
        {months.state.status === "loading" ? <LoadingCard className="h-40" /> : null}
        {months.state.status === "error" ? (
          <ErrorPanel message={t("failed")} retryLabel={t("retry")} onRetry={months.reload} />
        ) : null}
        {list && list.length === 0 ? <EmptyPanel message={t("empty")} /> : null}
        {list?.map(({ month, count }) => (
          <MonthSection
            key={month} month={month} count={count} title={formatMonthYear(`${month}-15T12:00:00Z`, locale)}
            open={toggled[month] ?? month === startOpen} onToggle={() => setToggled((current) => ({ ...current, [month]: !(current[month] ?? month === startOpen) }))}
            kind={values} onPickDay={pickDay}
          />
        ))}
      </div>
      <DaySheet values={values} set={set} />
    </>
  );
}
