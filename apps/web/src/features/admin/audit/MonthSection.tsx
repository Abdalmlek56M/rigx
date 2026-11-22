"use client";

import { useCallback } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatMonth, formatWeekday } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { ChevronDownIcon, RefreshIcon, UserIcon } from "@/components/ui/icons";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { Button } from "../kit/Button";
import { cairoDate } from "../offers/offer-rules";
import { fetchDays, type AuditDay } from "./audit-api";
import { actorsLine, dayWhen, type AuditValues } from "./audit-rules";
import { useAuditLabels } from "./use-audit-labels";

type Kind = Pick<AuditValues, "module" | "entity" | "action">;

/** One month of the log: a header that opens the month's days, each day a card (date, count of changes, who). */
export function MonthSection({ month, count, title, open, onToggle, kind, onPickDay }: {
  month: string; count: number; title: string; open: boolean; onToggle: () => void; kind: Kind; onPickDay: (date: string) => void;
}) {
  const t = useTranslations("admin.audit");
  const panel = `audit-month-${month}`;
  return (
    <section className="flex flex-col gap-3" aria-label={title}>
      <h2 className="m-0">
        <button
          type="button" aria-expanded={open} aria-controls={panel} onClick={onToggle}
          className="press flex w-full items-center gap-3 rounded-card border border-line bg-surface px-4 py-3 text-start transition-colors hover:border-orange/50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-orange"
        >
          <span className={`inline-flex shrink-0 text-ink-muted transition-transform ${open ? "" : "-rotate-90 rtl:rotate-90"}`}><ChevronDownIcon size={18} /></span>
          <span className="type-section-title flex-1">{title}</span>
          <StatusBadge tone="neutral">{t("changes.count", { count })}</StatusBadge>
        </button>
      </h2>
      <div id={panel} hidden={!open}>{open ? <MonthDays month={month} kind={kind} onPickDay={onPickDay} /> : null}</div>
    </section>
  );
}

function MonthDays({ month, kind, onPickDay }: { month: string; kind: Kind; onPickDay: (date: string) => void }) {
  const t = useTranslations("admin.audit");
  const days = useLoad(useCallback(
    () => fetchDays({ month, module: kind.module || undefined, entity: kind.entity || undefined, action: kind.action || undefined }),
    [month, kind.module, kind.entity, kind.action]
  ));
  if (days.state.status === "loading") return <div aria-hidden="true" className="h-28 animate-pulse rounded-card border border-line bg-surface" />;
  if (days.state.status === "error") {
    return (
      <div role="alert" className="flex flex-col items-start gap-3 rounded-card border border-line bg-surface p-5">
        <p className="m-0 font-bold">{t("failed")}</p>
        <Button variant="primary" icon={RefreshIcon} onClick={days.reload}>{t("retry")}</Button>
      </div>
    );
  }
  const today = cairoDate(new Date().toISOString());
  return (
    <ul className="m-0 grid list-none gap-3 p-0 sm:grid-cols-2 xl:grid-cols-3">
      {days.state.data.map((day) => <li key={day.date}><DayCard day={day} today={today} onPick={onPickDay} /></li>)}
    </ul>
  );
}

/** A day as a card: the date, the weekday, how many changes, and the first people behind them. */
function DayCard({ day, today, onPick }: { day: AuditDay; today: string; onPick: (date: string) => void }) {
  const t = useTranslations("admin.audit");
  const locale = useLocale();
  const labels = useAuditLabels();
  const when = dayWhen(day.date, today);
  const noon = `${day.date}T12:00:00Z`;
  const who = actorsLine([...day.actorNames, ...day.otherActors.map(labels.actor)]);
  return (
    <button
      type="button" onClick={() => onPick(day.date)}
      className="press flex h-full w-full items-start gap-3 rounded-card border border-line bg-surface p-4 text-start transition-colors hover:border-orange/50 focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-orange"
    >
      <span className="flex w-12 shrink-0 flex-col items-center rounded-control bg-paper py-1.5 leading-tight">
        <span className="font-mono text-lg font-bold">{Number(day.date.slice(8))}</span>
        <span className="text-[11px] text-ink-muted">{formatMonth(noon, locale)}</span>
      </span>
      <span className="flex min-w-0 flex-1 flex-col gap-1">
        <span className="flex flex-wrap items-center gap-2">
          <span className="text-sm font-bold">{formatWeekday(noon, locale)}</span>
          {when === "earlier" ? null : <StatusBadge tone="info">{t(`days.${when}`)}</StatusBadge>}
        </span>
        <span className="text-sm">{t("changes.count", { count: day.count })}</span>
        <span className="flex min-w-0 items-center gap-1.5 text-xs text-ink-muted">
          <UserIcon size={14} />
          <span className="min-w-0 [overflow-wrap:anywhere]">
            {who.shown.join(t("changes.separator"))}{who.more > 0 ? ` ${t("changes.more", { count: who.more })}` : ""}
          </span>
        </span>
      </span>
    </button>
  );
}
