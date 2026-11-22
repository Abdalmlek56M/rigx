"use client";

import { useCallback, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { StatusBadge } from "@/components/ui/StatusBadge";
import type { StatusTone } from "@/components/ui/status-badge-rules";
import { CheckIcon, CloseIcon, ExternalIcon } from "@/components/ui/icons";
import { formatDateTime, formatEgp } from "@/lib/format";
import { parseUsage } from "@/lib/usage";
import { useLoad, type Load } from "@/lib/hooks/use-load";
import { usePermissions } from "../use-permissions";
import { Button, buttonClass, buttonIconSize } from "../kit/Button";
import { DataTable, type Column } from "../kit/DataTable";
import { FilterBar, FilterSelect } from "../kit/FilterBar";
import { NameCell } from "../kit/NameCell";
import { PageHeader } from "../kit/PageHeader";
import { Pager } from "../kit/Pager";
import { useToast } from "../kit/Toast";
import { pageNumber } from "../kit/url-filters";
import { useUrlFilters } from "../kit/use-url-filters";
import { MODERATION_STATUSES, decisionsFor, parseStatus, type Decision } from "./customer-build-rules";
import { decideCustomerBuild, fetchCustomerBuilds, type CustomerBuild } from "./customer-builds-api";

const FILTERS = { status: "Pending", page: "1" };
const PERMISSION = "Catalog.ManageProducts";

const BADGE = { Pending: "warning", Approved: "success", Rejected: "danger" } as const satisfies Record<string, StatusTone>;
const DECISION_ICON = { Approved: CheckIcon, Rejected: CloseIcon } as const;

/** ADM-CustomerBuilds: the builds customers made public. A waiting one is approved (it then shows in "تجميعات العملاء") or rejected; every decision is audited. */
export function CustomerBuilds() {
  const t = useTranslations("admin.customerBuilds");
  const tu = useTranslations("usage");
  const locale = useLocale();
  const toast = useToast();
  const { has } = usePermissions();
  const { values, set } = useUrlFilters(FILTERS);
  const status = parseStatus(values.status);
  const page = pageNumber(values.page);
  const [busyToken, setBusyToken] = useState<string | null>(null);
  const load = useCallback(() => fetchCustomerBuilds(status, page), [status, page]);
  const { state, reload } = useLoad(load);
  const tableState: Load<readonly CustomerBuild[]> = state.status === "ready" ? { status: "ready", data: state.data.items } : state;
  const canDecide = has(PERMISSION);

  async function decide(token: string, decision: Decision) {
    setBusyToken(token);
    try {
      await decideCustomerBuild(token, decision);
      toast(t(decision === "Approved" ? "approved" : "rejected"));
      reload();
    } catch {
      toast(t("failed"), "error");
    } finally {
      setBusyToken(null);
    }
  }

  const columns: Column<CustomerBuild>[] = [
    {
      key: "build", header: t("columns.build"), phoneFull: true,
      cell: ({ build }) => {
        const usage = parseUsage(build.usage);
        return (
          <span className="flex min-w-0 flex-col items-start gap-0.5">
            <NameCell name={build.name?.trim() || t("untitled")} detail={build.authorName ?? t("anonymous")} />
            {usage ? <span className="text-xs text-ink-muted">{tu(usage)}</span> : null}
          </span>
        );
      }
    },
    {
      key: "parts", header: t("columns.parts"), phoneFull: true,
      cell: ({ build }) => (
        <ul className="m-0 flex max-w-80 list-none flex-col gap-0.5 p-0 text-xs">
          {build.parts.map((part) => (
            <li key={`${part.slot}-${part.modelName}`} dir="ltr" className="truncate text-start [unicode-bidi:isolate]">
              <span className="font-mono text-ink-muted">{part.slot}</span> {part.modelName}
            </li>
          ))}
        </ul>
      )
    },
    { key: "total", header: t("columns.total"), align: "end", cell: ({ build }) => <span className="whitespace-nowrap font-bold">{formatEgp(build.total, locale)}</span> },
    {
      key: "status", header: t("columns.status"),
      cell: (row) => {
        const rowStatus = parseStatus(row.status);
        return <StatusBadge tone={BADGE[rowStatus]}>{t(`statuses.${rowStatus}`)}</StatusBadge>;
      }
    },
    {
      key: "date", header: t("columns.date"),
      cell: (row) => <span dir="ltr" className="whitespace-nowrap font-mono text-xs">{formatDateTime(row.decidedAtUtc ?? row.submittedAtUtc, locale)}</span>
    },
    {
      key: "actions", header: t("columns.actions"),
      cell: (row) => (
        <span className="flex flex-wrap items-center gap-1.5">
          <a href={`/${locale}/b/${encodeURIComponent(row.build.token)}`} target="_blank" rel="noopener noreferrer" className={buttonClass({ size: "sm" })}>
            <ExternalIcon size={buttonIconSize("sm")} />
            {t("open")}
          </a>
          {canDecide
            ? decisionsFor(parseStatus(row.status)).map((decision) => (
                <Button
                  key={decision} size="sm" variant={decision === "Rejected" ? "danger" : "primary"} icon={DECISION_ICON[decision]}
                  loading={busyToken === row.build.token} onClick={() => void decide(row.build.token, decision)}
                >
                  {t(`decision.${decision}`)}
                </Button>
              ))
            : null}
        </span>
      )
    }
  ];

  return (
    <>
      <PageHeader title={t("title")} />
      <p className="m-0 max-w-3xl text-sm text-ink-muted">{t("intro")}</p>
      <FilterBar>
        <FilterSelect
          label={t("statusLabel")} value={status} onChange={(next) => set({ status: next })}
          options={MODERATION_STATUSES.map((entry) => ({ value: entry, label: t(`statuses.${entry}`) }))}
        />
      </FilterBar>
      <DataTable label={t("title")} columns={columns} state={tableState} onRetry={reload} rowKey={(row) => row.build.token} empty={t(`empty.${status}`)} />
      {state.status === "ready" ? <Pager info={{ page: state.data.page, pageSize: state.data.pageSize, total: state.data.total }} onPage={(next) => set({ page: String(next) })} /> : null}
    </>
  );
}
