"use client";

import { useCallback, useMemo } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatDate, formatTime } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { TextField } from "@/components/ui/TextField";
import { RefreshIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";
import { FilterBar, FilterSelect } from "../kit/FilterBar";
import { Pager } from "../kit/Pager";
import { pageNumber } from "../kit/url-filters";
import { useReveal } from "../kit/use-reveal";
import { cairoDate } from "../offers/offer-rules";
import { AuditActionBadge } from "./AuditActionBadge";
import { AuditEntryDetail } from "./AuditEntryDetail";
import type { AuditEntry } from "./audit-api";
import { fetchAuditFilters, fetchEntries } from "./audit-api";
import { entryTitle, groupByDay, type AuditDay, type AuditSet, type AuditValues } from "./audit-rules";
import { useAuditLabels } from "./use-audit-labels";

/** The data-change log: filters, the page grouped by day and kind of work, and the picked entry's before/after beside it. */
export function ChangesView({ values, set }: { values: AuditValues; set: AuditSet }) {
  const t = useTranslations("admin.audit");
  const labels = useAuditLabels();
  const page = pageNumber(values.page);
  const filters = useLoad(useCallback(() => fetchAuditFilters(), []));
  const entries = useLoad(useCallback(
    () => fetchEntries({
      module: values.module || undefined, entity: values.entity || undefined, action: values.action || undefined,
      from: values.from || undefined, to: values.to || undefined, page
    }),
    [values.module, values.entity, values.action, values.from, values.to, page]
  ));
  const options = filters.state.status === "ready" ? filters.state.data : { modules: [], entities: [], actions: [] };
  const items = entries.state.status === "ready" ? entries.state.data.items : null;
  const days = useMemo(() => (items ? groupByDay(items, cairoDate(new Date().toISOString())) : []), [items]);
  const detailRef = useReveal<HTMLDivElement>(values.entry || null);
  const pick = (id: string) => set({ entry: id, page: values.page });

  return (
    <>
      <FilterBar>
        <FilterSelect
          label={t("filters.module")} value={values.module} onChange={(module) => set({ module, entry: "" })}
          options={[{ value: "", label: t("filters.all") }, ...options.modules.map((module) => ({ value: module, label: labels.module(module) }))]}
        />
        <FilterSelect
          label={t("filters.entity")} value={values.entity} onChange={(entity) => set({ entity, entry: "" })}
          options={[{ value: "", label: t("filters.all") }, ...options.entities.map((entity) => ({ value: entity, label: labels.entity(entity) }))]}
        />
        <FilterSelect
          label={t("filters.action")} value={values.action} onChange={(action) => set({ action, entry: "" })}
          options={[{ value: "", label: t("filters.all") }, ...options.actions.map((action) => ({ value: action, label: labels.action(action) }))]}
        />
        <div className="w-36"><TextField label={t("filters.from")} name="from" type="date" ltr value={values.from} onChange={(from) => set({ from, entry: "" })} /></div>
        <div className="w-36"><TextField label={t("filters.to")} name="to" type="date" ltr value={values.to} onChange={(to) => set({ to, entry: "" })} /></div>
      </FilterBar>
      <div className="grid items-start gap-4 lg:grid-cols-[minmax(0,1fr)_minmax(0,1.1fr)]">
        <div className="flex min-w-0 flex-col gap-4">
          {entries.state.status === "loading" ? <div aria-hidden="true" className="h-64 animate-pulse rounded-card border border-line bg-surface" /> : null}
          {entries.state.status === "error" ? (
            <div role="alert" className="flex flex-col items-start gap-3 rounded-card border border-line bg-surface p-5">
              <p className="m-0 font-bold">{t("failed")}</p>
              <Button variant="primary" icon={RefreshIcon} onClick={entries.reload}>{t("retry")}</Button>
            </div>
          ) : null}
          {items && items.length === 0 ? <p className="m-0 rounded-card border border-line bg-surface p-5 text-sm text-ink-muted">{t("empty")}</p> : null}
          {days.map((day) => <DaySection key={day.date} day={day} picked={values.entry} onPick={pick} />)}
          {entries.state.status === "ready" ? (
            <Pager info={{ page: entries.state.data.page, pageSize: entries.state.data.pageSize, total: entries.state.data.total }} onPage={(next) => set({ page: String(next), entry: "" })} />
          ) : null}
        </div>
        <div ref={detailRef} className="min-w-0 scroll-mt-20 lg:sticky lg:top-20">
          {values.entry ? (
            <AuditEntryDetail id={values.entry} onClose={() => set({ entry: "", page: values.page })} onRestored={entries.reload} />
          ) : items && items.length > 0 ? (
            <p className="m-0 hidden rounded-card border border-dashed border-line p-5 text-sm text-ink-muted lg:block">{t("changes.pick")}</p>
          ) : null}
        </div>
      </div>
    </>
  );
}

/** One day: its heading (اليوم / امبارح / the date), then a block per kind of work. */
function DaySection({ day, picked, onPick }: { day: AuditDay<AuditEntry>; picked: string; onPick: (id: string) => void }) {
  const t = useTranslations("admin.audit");
  const locale = useLocale();
  const count = day.groups.reduce((total, group) => total + group.entries.length, 0);
  const heading = day.when === "earlier" ? formatDate(`${day.date}T12:00:00Z`, locale) : t(`days.${day.when}`);
  return (
    <section className="flex flex-col gap-3" aria-label={heading}>
      <h2 className="m-0 flex items-baseline gap-2 text-[17px] font-bold">
        {heading}
        <span className="text-xs font-normal text-ink-muted">{t("changes.count", { count })}</span>
      </h2>
      {day.groups.map(({ group, entries }) => (
        <div key={group} className="overflow-hidden rounded-card border border-line bg-surface">
          <h3 className="m-0 border-b border-line bg-paper px-4 py-2 text-[13px] font-bold text-ink-muted">{t(`groups.${group}`)}</h3>
          <ul className="m-0 list-none divide-y divide-line p-0">
            {entries.map((entry) => <EntryRow key={entry.id} entry={entry} picked={picked === entry.id} onPick={onPick} />)}
          </ul>
        </div>
      ))}
    </section>
  );
}

/** One change as a sentence: what happened to which record, by whom and when. */
function EntryRow({ entry, picked, onPick }: { entry: AuditEntry; picked: boolean; onPick: (id: string) => void }) {
  const labels = useAuditLabels();
  const locale = useLocale();
  const title = entryTitle(entry.changes);
  return (
    <li>
      <button
        type="button" aria-current={picked ? "true" : undefined} onClick={() => onPick(entry.id)}
        className={`flex w-full min-w-0 items-start gap-3 px-4 py-3 text-start transition-colors hover:bg-paper ${picked ? "bg-orange/10 hover:bg-orange/10" : ""}`}
      >
        <AuditActionBadge action={entry.action} />
        <span className="flex min-w-0 flex-1 flex-col gap-0.5">
          <span className="text-sm">
            <span className="font-bold">{labels.entity(entry.entity)}</span>
            {title ? <> «<span dir="auto" className="[overflow-wrap:anywhere]">{title}</span>»</> : null}
          </span>
          <span className="text-xs text-ink-muted">{entry.actorName ?? labels.actor(entry.actorKind)}</span>
        </span>
        <span className="shrink-0 text-xs text-ink-muted">{formatTime(entry.occurredAtUtc, locale)}</span>
      </button>
    </li>
  );
}
