"use client";

import { useCallback } from "react";
import { useLocale, useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { formatDateTimeFull } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { AuditActionBadge } from "../audit/AuditActionBadge";
import { fetchEntries } from "../audit/audit-api";
import { useAuditLabels } from "../audit/use-audit-labels";
import { ArrowEndIcon, RefreshIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

/** The item's change history from the data-change log: when, who, what kind of change and which fields; each entry opens in the log. */
export function StockItemHistory({ itemId }: { itemId: string }) {
  const t = useTranslations("admin.inventory.history");
  const labels = useAuditLabels();
  const locale = useLocale();
  const { state, reload } = useLoad(useCallback(() => fetchEntries({ entity: "stock_items", entityId: itemId, page: 1 }), [itemId]));

  return (
    <section className="flex min-w-0 flex-col gap-3 rounded-card border border-line bg-surface p-5">
      <h3 className="m-0 text-[15px] font-bold">{t("title")}</h3>
      {state.status === "loading" ? <div role="status" aria-label={t("loading")} className="h-24 animate-pulse rounded-control bg-paper" /> : null}
      {state.status === "error" ? (
        <div role="alert" className="flex flex-col items-start gap-2">
          <p className="m-0 text-sm font-bold">{t("failed")}</p>
          <Button size="sm" icon={RefreshIcon} onClick={reload}>{t("retry")}</Button>
        </div>
      ) : null}
      {state.status === "ready" && state.data.items.length === 0 ? <p className="m-0 text-sm text-ink-muted">{t("empty")}</p> : null}
      {state.status === "ready" && state.data.items.length > 0 ? (
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {state.data.items.map((entry) => (
            <li key={entry.id}>
              <Link
                href={`/admin/audit?entry=${entry.id}`}
                className="flex min-h-11 items-start justify-between gap-3 rounded-control border border-line bg-paper px-3 py-2 hover:border-input-line"
              >
                <span className="flex min-w-0 flex-col gap-1">
                  <span className="flex flex-wrap items-center gap-2">
                    <AuditActionBadge action={entry.action} />
                    <span className="text-xs text-ink-muted">{formatDateTimeFull(entry.occurredAtUtc, locale)}</span>
                  </span>
                  <span className="text-sm font-bold">{entry.actorName ?? labels.actor(entry.actorKind)}</span>
                  {entry.changes.length > 0 ? (
                    <span className="text-xs text-ink-muted [overflow-wrap:anywhere]">{entry.changes.map((change) => labels.field(change.field)).join(t("separator"))}</span>
                  ) : null}
                </span>
                <ArrowEndIcon size={16} />
              </Link>
            </li>
          ))}
        </ul>
      ) : null}
    </section>
  );
}
