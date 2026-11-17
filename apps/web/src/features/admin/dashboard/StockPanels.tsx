import { useLocale, useTranslations } from "next-intl";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { Link } from "@/i18n/navigation";
import { parseCondition } from "@/lib/condition";
import { formatEgp } from "@/lib/format";
import type { AdminDashboard, DashboardData } from "./dashboard-api";
import { Empty, Panel, PanelTable } from "./Panel";

const cell = "border-t border-line px-2 py-2";
const linkButton = "inline-flex min-h-9 items-center rounded-control border border-input-line px-3 text-[13px] font-bold hover:bg-line";

/** Parts customers asked to be told about ("notify me"), with how many are still waiting and whether it is in stock now. */
export function DemandPanel({ rows }: { rows: AdminDashboard["notifyMeDemand"] }) {
  const t = useTranslations("admin.dashboard.demand");
  return (
    <Panel title={t("title")} note={t("note")}>
      {rows.length ? (
        <PanelTable headers={[t("part"), t("waiting"), t("stock"), ""]}>
          {rows.map((row) => (
            <tr key={row.modelKey}>
              <td className={cell}><span dir="ltr">{row.modelName}</span></td>
              <td className={cell}>{t("customers", { count: row.open })}</td>
              <td className={cell}>{row.availableNow ? <b className="text-success">{t("available")}</b> : <span className="text-ink-muted">{t("missing")}</span>}</td>
              <td className={`${cell} text-end`}>
                <Link href={`/admin/requests?type=NotifyMe&q=${encodeURIComponent(row.modelName)}`} className={linkButton}>{t("open")}</Link>
              </td>
            </tr>
          ))}
        </PanelTable>
      ) : <Empty message={t("empty")} />}
    </Panel>
  );
}

/** Stock older than 30 days, oldest first, with what it cost. */
export function AgingPanel({ aging }: { aging: DashboardData["stockAging"] }) {
  const t = useTranslations("admin.dashboard.aging");
  const locale = useLocale();
  return (
    <Panel
      title={<>{t("title")} <span className="text-sm font-normal text-ink-muted">{t("summary", { count: aging.count, cost: formatEgp(aging.costTotal, locale) })}</span></>}
      action={<Link href="/admin/inventory" className={linkButton}>{t("all")}</Link>}
    >
      {aging.oldest.length ? (
        <PanelTable headers={[t("code"), t("part"), t("condition"), t("days"), t("cost")]}>
          {aging.oldest.map((unit) => {
            const condition = parseCondition(unit.condition);
            return (
              <tr key={unit.unitId}>
                <td className={`${cell} font-mono text-xs`}><span dir="ltr">{unit.stockItemCode}</span></td>
                <td className={cell}><span dir="ltr">{unit.name}</span></td>
                <td className={cell}>{condition ? <ConditionBadge condition={condition} /> : unit.condition}</td>
                <td className={cell}>{t("daysValue", { count: unit.daysInStock })}</td>
                <td className={`${cell} whitespace-nowrap`}>{formatEgp(unit.cost, locale)}</td>
              </tr>
            );
          })}
        </PanelTable>
      ) : <Empty message={t("empty")} />}
    </Panel>
  );
}

/** Each partner's share earned this month. */
export function PartnersPanel({ rows }: { rows: DashboardData["partnerProfitsThisMonth"] }) {
  const t = useTranslations("admin.dashboard.partners");
  const locale = useLocale();
  return (
    <Panel title={t("title")}>
      {rows.length ? (
        <PanelTable headers={[t("partner"), t("earned")]}>
          {rows.map((row) => (
            <tr key={row.partnerId}>
              <td className={cell}>{row.name}</td>
              <td className={`${cell} whitespace-nowrap font-bold`}>{formatEgp(row.earned, locale)}</td>
            </tr>
          ))}
        </PanelTable>
      ) : <Empty message={t("empty")} />}
    </Panel>
  );
}

/** The balance of each cash box and wallet (they add up to "cash available"). */
export function AccountsPanel({ rows }: { rows: DashboardData["snapshot"]["accounts"] }) {
  const t = useTranslations("admin.dashboard.accounts");
  const locale = useLocale();
  return (
    <Panel title={t("title")}>
      {rows.length ? (
        <PanelTable headers={[t("account"), t("balance")]}>
          {rows.map((row) => (
            <tr key={row.id}>
              <td className={cell}>{row.name}</td>
              <td className={`${cell} whitespace-nowrap font-bold`}>{formatEgp(row.balance, locale)}</td>
            </tr>
          ))}
        </PanelTable>
      ) : <Empty message={t("empty")} />}
    </Panel>
  );
}
