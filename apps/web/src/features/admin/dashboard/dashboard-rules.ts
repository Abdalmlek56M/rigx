/** Dashboard period, chart and breakdown math. Dates are Cairo business days as `YYYY-MM-DD`. Pure, so it is tested. */
export const PERIODS = ["today", "week", "month", "year", "custom"] as const;
export type Period = (typeof PERIODS)[number] | "lastMonth";

export function parsePeriod(value: string): Period {
  if (value === "lastMonth") return "lastMonth";
  return PERIODS.find((period) => period === value) ?? "month";
}

const DATE = /^\d{4}-\d{2}-\d{2}$/;

/** Today's date in Cairo. */
export function cairoToday(now: Date): string {
  return new Intl.DateTimeFormat("en-CA", { timeZone: "Africa/Cairo", year: "numeric", month: "2-digit", day: "2-digit" }).format(now);
}

export function addDays(date: string, days: number): string {
  const value = new Date(`${date}T00:00:00Z`);
  value.setUTCDate(value.getUTCDate() + days);
  return value.toISOString().slice(0, 10);
}

export interface DateRange {
  from: string;
  to: string;
}

/**
 * The range a period covers up to today: today, the last 7 days, this month so far, this year so far, or the custom range
 * (`null` while it is incomplete or reversed).
 */
export function periodRange(period: Period, today: string, custom: { from: string; to: string }): DateRange | null {
  switch (period) {
    case "today":
      return { from: today, to: today };
    case "week":
      return { from: addDays(today, -6), to: today };
    case "month":
      return { from: `${today.slice(0, 8)}01`, to: today };
    case "lastMonth": {
      const previousMonthEnd = addDays(`${today.slice(0, 8)}01`, -1);
      return { from: `${previousMonthEnd.slice(0, 8)}01`, to: previousMonthEnd };
    }
    case "year":
      return { from: `${today.slice(0, 5)}01-01`, to: today };
    case "custom":
      return DATE.test(custom.from) && DATE.test(custom.to) && custom.from <= custom.to ? { from: custom.from, to: custom.to } : null;
  }
}

export interface ChartPoint {
  date: string;
  grossSales: number;
  netItemProfit: number;
}

/** Days summed into weeks of 7, counted back from the last day (the first week may be shorter); each labelled by its first day. */
export function weeklyPoints(days: readonly ChartPoint[]): ChartPoint[] {
  const weeks: ChartPoint[] = [];
  for (let end = days.length; end > 0; end -= 7) {
    const slice = days.slice(Math.max(0, end - 7), end);
    const first = slice[0];
    if (!first) break;
    weeks.unshift({
      date: first.date,
      grossSales: slice.reduce((sum, day) => sum + day.grossSales, 0),
      netItemProfit: slice.reduce((sum, day) => sum + day.netItemProfit, 0)
    });
  }
  return weeks;
}

/** A round axis top at or above `max` (1, 2 or 5 × a power of ten), so grid labels read cleanly. 0 stays 0. */
export function niceMax(max: number): number {
  if (max <= 0) return 0;
  const power = 10 ** Math.floor(Math.log10(max));
  const step = [1, 2, 5, 10].find((factor) => factor * power >= max) ?? 10;
  return step * power;
}

export interface ShareRow {
  /** Share of the total, 0–100 (one decimal). */
  percent: number;
  /** Bar length relative to the biggest row, 0–100. */
  width: number;
}

/** Each row with its share of the total and its bar length against the largest row. */
export function withShares<T>(rows: readonly T[], value: (row: T) => number): (T & ShareRow)[] {
  const values = rows.map(value);
  const total = values.reduce((sum, item) => sum + item, 0);
  const max = Math.max(0, ...values);
  return rows.map((row, index) => {
    const item = values[index] ?? 0;
    return { ...row, percent: total > 0 ? Math.round((item / total) * 1000) / 10 : 0, width: max > 0 ? (item / max) * 100 : 0 };
  });
}

/** Direction of a KPI change in percent: `null` when there is nothing to compare with. */
export function changeDirection(change: number | null | undefined): "up" | "down" | "flat" | null {
  if (change === null || change === undefined) return null;
  return change > 0 ? "up" : change < 0 ? "down" : "flat";
}
