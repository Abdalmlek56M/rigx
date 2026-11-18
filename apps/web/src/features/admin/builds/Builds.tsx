"use client";

import { useMemo } from "react";
import { useLocale, useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { formatEgp } from "@/lib/format";
import { useLoad, type Load } from "@/lib/hooks/use-load";
import { parseUsage } from "@/lib/usage";
import { DataTable, type Column } from "../kit/DataTable";
import { FilterBar, FilterSelect, SearchBox } from "../kit/FilterBar";
import { PageHeader } from "../kit/PageHeader";
import { useUrlFilters } from "../kit/use-url-filters";
import { usePermissions } from "../use-permissions";
import { BuildStatusBadge } from "./BuildStatusBadge";
import { NewBuildDialog } from "./NewBuildDialog";
import { fetchBuilds, type Build } from "./builds-api";
import { BUILD_STATUSES, filterBuilds } from "./build-rules";

const FILTERS = { status: "", q: "", new: "" };

/** ADM-Build list: every build with its status, parts and prices (the API returns them all, so status and search filter on the page). */
export function Builds() {
  const t = useTranslations("admin.builds");
  const usage = useTranslations("usage");
  const locale = useLocale();
  const { has } = usePermissions();
  const { values, set } = useUrlFilters(FILTERS);
  const { state, reload } = useLoad(fetchBuilds);
  const shown = useMemo(() => (state.status === "ready" ? filterBuilds(state.data, values.status, values.q) : []), [state, values.status, values.q]);
  const tableState: Load<readonly Build[]> = state.status === "ready" ? { status: "ready", data: shown } : state;
  const none = <span className="text-ink-muted">{t("none")}</span>;

  const columns: Column<Build>[] = [
    {
      key: "code",
      header: t("columns.code"),
      cell: (build) => <Link href={`/admin/builds/${build.id}`} dir="ltr" className="whitespace-nowrap font-mono text-[13px] font-bold hover:underline">{build.code}</Link>
    },
    { key: "name", header: t("columns.name"), cell: (build) => <span dir="auto" className="[unicode-bidi:isolate]">{build.name}</span> },
    {
      key: "usage",
      header: t("columns.usage"),
      cell: (build) => {
        const known = parseUsage(build.usage);
        return known ? <span className="whitespace-nowrap">{usage(known)}</span> : none;
      }
    },
    { key: "status", header: t("columns.status"), cell: (build) => <BuildStatusBadge status={build.status} /> },
    { key: "parts", header: t("columns.parts"), align: "end", cell: (build) => <span>{build.units.length}</span> },
    { key: "copies", header: t("columns.copies"), align: "end", cell: (build) => <span>{build.copies}</span> },
    { key: "price", header: t("columns.price"), align: "end", cell: (build) => <span className="whitespace-nowrap font-bold">{formatEgp(build.salePrice, locale)}</span> },
    { key: "partsTotal", header: t("columns.partsTotal"), align: "end", cell: (build) => <span className="whitespace-nowrap">{formatEgp(build.partsTotal, locale)}</span> }
  ];

  return (
    <>
      <PageHeader
        title={t("title")}
        actions={has("Catalog.ManageProducts") ? <button type="button" onClick={() => set({ new: "1" })} className="h-11 rounded-control bg-carbon px-5 font-bold text-on-carbon">{t("new")}</button> : null}
      />
      <FilterBar>
        <SearchBox label={t("search")} value={values.q} onChange={(q) => set({ q })} />
        <FilterSelect
          label={t("statusLabel")} value={values.status} onChange={(status) => set({ status })}
          options={[{ value: "", label: t("statuses.all") }, ...BUILD_STATUSES.map((status) => ({ value: status, label: t(`statuses.${status}`) }))]}
        />
      </FilterBar>
      <DataTable label={t("title")} columns={columns} state={tableState} onRetry={reload} rowKey={(build) => build.id} empty={t("empty")} />
      {values.new && has("Catalog.ManageProducts") ? <NewBuildDialog onClose={() => set({ new: "" })} /> : null}
    </>
  );
}
