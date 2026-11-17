"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatEgp } from "@/lib/format";
import { niceMax, weeklyPoints, type ChartPoint } from "./dashboard-rules";
import { Panel } from "./Panel";

const compact = new Intl.NumberFormat("en", { notation: "compact", maximumFractionDigits: 1 });

/** "18/11": the short date under the bars and in the tooltip. */
function shortDate(date: string): string {
  return `${Number(date.slice(8, 10))}/${Number(date.slice(5, 7))}`;
}

/** One bar anchored to the zero line (down for a loss), as % of the chart height. */
function barStyle(value: number, zero: number, range: number) {
  const size = range > 0 ? (Math.abs(value) / range) * 100 : 0;
  return { bottom: `${value >= 0 ? zero : zero - size}%`, height: `${size}%` };
}

/**
 * Sales and item profit for the last 30 days, per day or per week: paired bars on one money axis (a loss goes below zero),
 * a tooltip per period, and the same numbers as a table for screen readers.
 */
export function SalesChart({ days }: { days: readonly ChartPoint[] }) {
  const t = useTranslations("admin.dashboard.chart");
  const locale = useLocale();
  const [weekly, setWeekly] = useState(false);
  const points = weekly ? weeklyPoints(days) : days;

  const top = niceMax(Math.max(0, ...points.map((point) => Math.max(point.grossSales, point.netItemProfit))));
  const bottom = -niceMax(-Math.min(0, ...points.map((point) => point.netItemProfit)));
  const range = top - bottom;
  const zero = range > 0 ? (-bottom / range) * 100 : 0;
  const ticks = range === 0 ? [0] : [top, ...(bottom < 0 ? [0, bottom] : [top / 2, 0])];
  const tickPosition = (value: number) => (range > 0 ? ((value - bottom) / range) * 100 : 0);
  const labelEvery = Math.max(1, Math.ceil(points.length / 6));

  const toggle = (
    <div role="group" aria-label={t("granularity")} className="inline-flex gap-1 rounded-card border border-line bg-surface p-1">
      {[false, true].map((value) => (
        <button
          key={String(value)} type="button" aria-pressed={weekly === value} onClick={() => setWeekly(value)}
          className={`h-[30px] rounded-control px-3.5 text-[13px] ${weekly === value ? "bg-carbon font-bold text-on-carbon" : "font-medium hover:bg-line"}`}
        >
          {value ? t("weekly") : t("daily")}
        </button>
      ))}
    </div>
  );

  return (
    <Panel title={t("title")} action={toggle}>
      <div aria-hidden="true" className="flex flex-wrap gap-4 text-xs">
        <span className="inline-flex items-center gap-1.5"><span className="h-2.5 w-2.5 rounded-[2px] bg-ink-muted" />{t("sales")}</span>
        <span className="inline-flex items-center gap-1.5"><span className="h-2.5 w-2.5 rounded-[2px] bg-orange" />{t("profit")}</span>
      </div>

      <div aria-hidden="true" className="flex gap-2">
        <div className="relative w-10 shrink-0 font-mono text-[11px] text-ink-muted">
          {ticks.map((value) => (
            <span key={value} dir="ltr" className="absolute end-0 translate-y-1/2" style={{ bottom: `${tickPosition(value)}%` }}>{compact.format(value)}</span>
          ))}
        </div>
        <div className="min-w-0 flex-1">
          <div className="relative h-56">
            {ticks.map((value) => (
              <span key={value} className={`absolute inset-x-0 border-t ${value === 0 ? "border-ink-muted" : "border-line"}`} style={{ bottom: `${tickPosition(value)}%` }} />
            ))}
            <div className="absolute inset-0 flex">
              {points.map((point) => (
                <div key={point.date} className="group relative h-full flex-1 px-[1px]">
                  <span className="absolute start-[1px] w-[calc(50%-2px)] rounded-t-[4px] bg-ink-muted" style={barStyle(point.grossSales, zero, range)} />
                  <span className="absolute end-[1px] w-[calc(50%-2px)] rounded-t-[4px] bg-orange" style={barStyle(point.netItemProfit, zero, range)} />
                  <div className="pointer-events-none absolute bottom-full start-1/2 z-10 mb-1 hidden -translate-x-1/2 whitespace-nowrap rounded-control bg-carbon px-2.5 py-1.5 text-xs text-on-carbon group-hover:block rtl:translate-x-1/2">
                    <b className="block">{shortDate(point.date)}</b>
                    {t("sales")}: {formatEgp(point.grossSales, locale)}<br />
                    {t("profit")}: {formatEgp(point.netItemProfit, locale)}
                  </div>
                </div>
              ))}
            </div>
          </div>
          <div className="flex pt-1 font-mono text-[11px] text-ink-muted">
            {points.map((point, index) => (
              <span key={point.date} className="flex-1 text-center">{index % labelEvery === 0 ? shortDate(point.date) : ""}</span>
            ))}
          </div>
        </div>
      </div>

      <table className="sr-only">
        <caption>{t("title")}</caption>
        <thead><tr><th scope="col">{t("date")}</th><th scope="col">{t("sales")}</th><th scope="col">{t("profit")}</th></tr></thead>
        <tbody>
          {points.map((point) => (
            <tr key={point.date}><td>{shortDate(point.date)}</td><td>{formatEgp(point.grossSales, locale)}</td><td>{formatEgp(point.netItemProfit, locale)}</td></tr>
          ))}
        </tbody>
      </table>
    </Panel>
  );
}
