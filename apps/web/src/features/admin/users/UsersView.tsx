"use client";

import { useCallback, useState } from "react";
import { useTranslations } from "next-intl";
import { useLoad, type Load } from "@/lib/hooks/use-load";
import { DataTable, type Column } from "../kit/DataTable";
import { FilterBar, FilterSelect, SearchBox } from "../kit/FilterBar";
import { Pager } from "../kit/Pager";
import { pageNumber } from "../kit/url-filters";
import { useReveal } from "../kit/use-reveal";
import { InviteUserDialog } from "./InviteUserDialog";
import { useBuiltInRoleLabel } from "./use-role-label";
import { UserDetail } from "./UserDetail";
import { ACTIVE_FILTERS, activeFilter, roleNames, type UserSet, type UserValues } from "./user-rules.ts";
import { fetchRoles, fetchUsers, type StaffUser } from "./users-api";
import { PlusIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

/** The staff list with search, the active filter and paging; the picked user's detail below. */
export function UsersView({ values, set }: { values: UserValues; set: UserSet }) {
  const t = useTranslations("admin.users");
  const builtIn = useBuiltInRoleLabel();
  const [inviting, setInviting] = useState(false);
  const page = pageNumber(values.page);
  const roles = useLoad(useCallback(() => fetchRoles(), []));
  const users = useLoad(useCallback(
    () => fetchUsers({ search: values.search || undefined, isActive: activeFilter(values.active), page }),
    [values.search, values.active, page]
  ));
  const tableState: Load<readonly StaffUser[]> = users.state.status === "ready" ? { status: "ready", data: users.state.data.items } : users.state;
  const roleList = roles.state.status === "ready" ? roles.state.data : [];
  const detailRef = useReveal<HTMLDivElement>(values.user || null);

  const columns: Column<StaffUser>[] = [
    {
      key: "name", header: t("columns.name"), cell: (user) => (
        <button
          type="button" aria-current={values.user === user.id ? "true" : undefined} onClick={() => set({ user: user.id, page: values.page })}
          className={`text-start font-bold [overflow-wrap:anywhere] ${values.user === user.id ? "text-orange-text" : ""}`}
        >
          {user.fullName}
        </button>
      )
    },
    { key: "email", header: t("columns.email"), cell: (user) => <span dir="ltr" className="block break-all font-mono text-xs">{user.email}</span> },
    {
      key: "roles", header: t("columns.roles"), cell: (user) => {
        const names = roleNames(user, roleList, builtIn);
        return names.length === 0 ? <span className="text-ink-muted">{t("noRoles")}</span> : <span className="[overflow-wrap:anywhere]">{names.join("، ")}</span>;
      }
    },
    {
      key: "active", header: t("columns.status"), cell: (user) => (
        <span className={`inline-block whitespace-nowrap rounded-[4px] px-2 py-[3px] text-xs font-bold ${user.isActive ? "bg-line text-ink" : "bg-error/10 text-error"}`}>
          {user.isActive ? t("status.active") : t("status.inactive")}
        </span>
      )
    }
  ];

  return (
    <>
      <FilterBar actions={<Button variant="primary" icon={PlusIcon} onClick={() => setInviting(true)}>{t("invite.open")}</Button>}>
        <SearchBox label={t("search")} value={values.search} onChange={(search) => set({ search })} />
        <FilterSelect
          label={t("activeLabel")} value={values.active as (typeof ACTIVE_FILTERS)[number]} onChange={(active) => set({ active })}
          options={ACTIVE_FILTERS.map((entry) => ({ value: entry, label: t(`active.${entry || "all"}`) }))}
        />
      </FilterBar>
      <DataTable label={t("title")} columns={columns} state={tableState} onRetry={users.reload} rowKey={(user) => user.id} empty={t("empty")} />
      {users.state.status === "ready" ? (
        <Pager info={{ page: users.state.data.page, pageSize: users.state.data.pageSize, total: users.state.data.total }} onPage={(next) => set({ page: String(next) })} />
      ) : null}
      <div ref={detailRef} className="scroll-mt-20 empty:hidden">
        {values.user ? (
          <UserDetail
            key={values.user} id={values.user} roles={roleList}
            onChanged={users.reload} onClose={() => set({ user: "", page: values.page })}
          />
        ) : null}
      </div>
      {inviting && roles.state.status === "ready" ? (
        <InviteUserDialog roles={roleList} onClose={() => setInviting(false)} onCreated={(id) => { users.reload(); set({ user: id, page: values.page }); }} />
      ) : null}
    </>
  );
}
