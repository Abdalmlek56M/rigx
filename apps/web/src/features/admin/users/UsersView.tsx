"use client";

import { useCallback, useState } from "react";
import { useTranslations } from "next-intl";
import { Link, useRouter } from "@/i18n/navigation";
import { useLoad, type Load } from "@/lib/hooks/use-load";
import { actionsColumn, DataTable, type Column } from "../kit/DataTable";
import { FilterBar, FilterSelect, SearchBox } from "../kit/FilterBar";
import { Pager } from "../kit/Pager";
import { RowAction } from "../kit/RowAction";
import { pageNumber } from "../kit/url-filters";
import { InviteUserDialog } from "./InviteUserDialog";
import { useBuiltInRoleLabel } from "./use-role-label";
import { UserStatusBadge } from "./UserStatusBadge";
import { ACTIVE_FILTERS, activeFilter, roleNames, type UserSet, type UserValues } from "./user-rules.ts";
import { fetchRoles, fetchUsers, type StaffUser } from "./users-api";
import { PlusIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

/** The staff list with search, the active filter and paging; each person opens on their own profile page. */
export function UsersView({ values, set }: { values: UserValues; set: UserSet }) {
  const t = useTranslations("admin.users");
  const router = useRouter();
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

  const columns: Column<StaffUser>[] = [
    {
      key: "name", header: t("columns.name"), width: "24%", cell: (user) => (
        <Link href={`/admin/users/${user.id}`} className="font-bold [overflow-wrap:anywhere] hover:text-orange-text hover:underline">
          {user.fullName}
        </Link>
      )
    },
    { key: "email", header: t("columns.email"), width: "28%", cell: (user) => <span dir="ltr" className="break-all font-mono text-xs">{user.email}</span> },
    {
      key: "roles", header: t("columns.roles"), width: "24%", cell: (user) => {
        const names = roleNames(user, roleList, builtIn);
        return names.length === 0 ? <span className="text-ink-muted">{t("noRoles")}</span> : <span className="[overflow-wrap:anywhere]">{names.join("، ")}</span>;
      }
    },
    {
      key: "active", header: t("columns.status"), width: "10%", cell: (user) => (
        <UserStatusBadge isActive={user.isActive} />
      )
    },
    { ...actionsColumn<StaffUser>((user) => <RowAction kind="edit" subject={user.fullName} href={`/admin/users/${user.id}`} />), width: "14%" }
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
      {inviting && roles.state.status === "ready" ? (
        <InviteUserDialog roles={roleList} onClose={() => setInviting(false)} onCreated={(id) => router.push(`/admin/users/${id}`)} />
      ) : null}
    </>
  );
}
