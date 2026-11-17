import { useLocale, useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { parseCondition } from "@/lib/condition";
import { formatEgp } from "@/lib/format";
import { parseUsage } from "@/lib/usage";
import type { DashboardData } from "./dashboard-api";
import { withShares } from "./dashboard-rules";
import { Empty, Panel } from "./Panel";

function Bar({ width, tone = "bg-carbon" }: { width: number; tone?: string }) {
  return (
    <span aria-hidden="true" className="block h-2.5 overflow-hidden rounded-full bg-line">
      <span className={`block h-full rounded-full ${tone}`} style={{ width: `${width}%` }} />
    </span>
  );
}

/** What needs the owner today, each a link to its list with the count. */
export function TodoPanel({ todo, newRequests }: { todo: DashboardData["todo"]; newRequests: number }) {
  const t = useTranslations("admin.dashboard.todo");
  const rows = [
    { key: "newOrders", count: todo.newOrders, href: "/admin/orders?status=New" },
    { key: "awaitingPayment", count: todo.awaitingPayment, href: "/admin/orders" },
    { key: "expiredReservations", count: todo.expiredReservations, href: "/admin/orders" },
    { key: "pendingReturns", count: todo.pendingReturns, href: "/admin/orders" },
    { key: "newRequests", count: newRequests, href: "/admin/requests?status=New" },
    { key: "recurringExpensesDue", count: todo.recurringExpensesDue, href: "/admin/expenses" }
  ] as const;

  return (
    <Panel title={t("title")}>
      <ul className="m-0 list-none p-0">
        {rows.map((row) => (
          <li key={row.key} className="border-b border-line last:border-b-0">
            <Link href={row.href} className="flex min-h-11 items-center gap-2.5 py-2 text-sm hover:underline">
              <span className="flex-1">{t(row.key)}</span>
              <span className={`rounded-[4px] px-2 py-0.5 text-xs font-bold ${row.count > 0 ? "bg-orange/15 text-orange-text" : "bg-line text-ink-muted"}`}>{row.count}</span>
            </Link>
          </li>
        ))}
      </ul>
    </Panel>
  );
}

/** Orders per answer (lead source or usage): a bar, the share and the count. */
function ShareList({ rows }: { rows: readonly { label: string; orders: number }[] }) {
  return (
    <ul className="m-0 flex list-none flex-col gap-2.5 p-0">
      {withShares(rows, (row) => row.orders).map((row) => (
        <li key={row.label} className="grid grid-cols-[minmax(0,130px)_1fr_auto] items-center gap-2.5 text-[13px]">
          <span className="truncate">{row.label}</span>
          <Bar width={row.width} />
          <span dir="ltr" className="font-mono text-xs text-ink-muted">{row.percent}% · {row.orders}</span>
        </li>
      ))}
    </ul>
  );
}

/** The API's label for orders where the question was not answered. */
const UNANSWERED = "Unknown";

export function LeadSourcesPanel({ rows }: { rows: DashboardData["leadSources"] }) {
  const t = useTranslations("admin.dashboard");
  const labelled = rows.map((row) => ({ ...row, label: row.label === UNANSWERED ? t("unanswered") : row.label }));
  return (
    <Panel title={t("leadSources.title")} note={t("leadSources.note")}>
      {rows.length ? <ShareList rows={labelled} /> : <Empty message={t("empty")} />}
    </Panel>
  );
}

export function UsagesPanel({ rows }: { rows: DashboardData["usages"] }) {
  const t = useTranslations("admin.dashboard");
  const tUsage = useTranslations("usage");
  const labelled = rows.map((row) => {
    const usage = parseUsage(row.label);
    return { ...row, label: usage ? tUsage(usage) : row.label === UNANSWERED ? t("unanswered") : row.label };
  });
  return (
    <Panel title={t("usages.title")} note={t("usages.note")}>
      {rows.length ? <ShareList rows={labelled} /> : <Empty message={t("empty")} />}
    </Panel>
  );
}

export function CategoriesPanel({ rows }: { rows: DashboardData["topCategories"] }) {
  const t = useTranslations("admin.dashboard");
  const locale = useLocale();
  return (
    <Panel title={t("categories.title")}>
      {rows.length ? (
        <ul className="m-0 flex list-none flex-col gap-3 p-0">
          {withShares(rows, (row) => row.sales).map((row, index) => (
            <li key={row.categoryId ?? `none-${index}`} className="flex flex-col gap-1.5 text-[13px]">
              <span className="flex justify-between gap-2">
                <span className="truncate font-bold">{row.categoryName ?? t("categories.none")}</span>
                <span className="whitespace-nowrap text-ink-muted">{t("categories.salesMargin", { sales: formatEgp(row.sales, locale), margin: row.marginPercent })}</span>
              </span>
              <Bar width={row.width} tone="bg-orange" />
            </li>
          ))}
        </ul>
      ) : <Empty message={t("empty")} />}
    </Panel>
  );
}

/** New against used (sales and margin), whole builds against parts sold from builds, and how long used items take to sell. */
export function ConditionPanel({ data }: { data: DashboardData }) {
  const t = useTranslations("admin.dashboard.condition");
  const tCondition = useTranslations("common.condition");
  const locale = useLocale();
  const { buildSales } = data;
  const facts = [
    { label: t("fullBuilds"), value: t("builds", { count: buildSales.fullBuildCount, sales: formatEgp(buildSales.fullBuildSales, locale) }) },
    { label: t("splitParts"), value: t("parts", { count: buildSales.splitPartsCount, sales: formatEgp(buildSales.splitPartsSales, locale) }) },
    { label: t("singleItems"), value: t("parts", { count: buildSales.singleItemsCount, sales: formatEgp(buildSales.singleItemsSales, locale) }) },
    { label: t("daysToSellUsed"), value: data.avgDaysToSellUsed === null ? "—" : t("days", { count: Math.round(data.avgDaysToSellUsed) }) }
  ];

  return (
    <Panel title={t("title")}>
      <div className="grid grid-cols-2 gap-3">
        {data.newVsUsed.map((row) => {
          const condition = parseCondition(row.condition);
          return (
            <div key={row.condition} className="flex flex-col gap-1 rounded-card bg-paper p-3">
              <span className="text-[13px] font-bold">{condition ? tCondition(condition) : row.condition}</span>
              <span className="text-lg font-bold">{formatEgp(row.sales, locale)}</span>
              <span className="text-xs text-ink-muted">{t("margin", { percent: row.marginPercent })}</span>
            </div>
          );
        })}
      </div>
      <dl className="m-0 flex flex-col text-[13px]">
        {facts.map((fact) => (
          <div key={fact.label} className="flex justify-between gap-2 border-b border-line py-2 last:border-b-0">
            <dt className="text-ink-muted">{fact.label}</dt>
            <dd className="m-0 text-end font-bold">{fact.value}</dd>
          </div>
        ))}
      </dl>
    </Panel>
  );
}
