"use client";

import { useCallback } from "react";
import { useTranslations } from "next-intl";
import { useLoad, type Load } from "@/lib/hooks/use-load";
import { actionsColumn, DataTable, type Column } from "../kit/DataTable";
import { RowAction } from "../kit/RowAction";
import { FilterBar } from "../kit/FilterBar";
import { useReveal } from "../kit/use-reveal";
import { RoleEditor } from "./RoleEditor";
import { useRoleLabel } from "./use-role-label";
import { builtInRole, type UserSet, type UserValues } from "./user-rules.ts";
import { fetchRoles, type StaffRole } from "./users-api";
import { PlusIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

const NEW_ROLE = "new";

/** The roles list (name, code, users, permissions); a new role or one role's permissions open below it. */
export function RolesView({ values, set }: { values: UserValues; set: UserSet }) {
  const t = useTranslations("admin.users");
  const label = useRoleLabel();
  const { state, reload } = useLoad(useCallback(() => fetchRoles(), []));
  const detailRef = useReveal<HTMLDivElement>(values.role || null);
  const tableState: Load<readonly StaffRole[]> = state;
  const picked = state.status === "ready" ? state.data.find((role) => role.id === values.role) ?? null : null;

  const columns: Column<StaffRole>[] = [
    {
      key: "name", header: t("columns.role"), cell: (role) => (
        <button
          type="button" aria-current={values.role === role.id ? "true" : undefined} onClick={() => set({ role: role.id })}
          className={`text-start font-bold [overflow-wrap:anywhere] ${values.role === role.id ? "text-orange-text" : ""}`}
        >
          {label(role)}
        </button>
      )
    },
    { key: "code", header: t("columns.code"), cell: (role) => (builtInRole(role.code) ? null : <span dir="ltr" className="font-mono text-xs">{role.code}</span>) },
    { key: "users", header: t("columns.users"), align: "end", cell: (role) => <span className="font-bold">{role.userCount}</span> },
    { key: "permissions", header: t("columns.permissions"), align: "end", cell: (role) => <span className="font-bold">{role.permissionCodes.length}</span> },
    actionsColumn((role) => <RowAction kind="view" subject={label(role)} current={values.role === role.id} onClick={() => set({ role: role.id })} />)
  ];

  return (
    <>
      <FilterBar actions={<Button variant="primary" icon={PlusIcon} onClick={() => set({ role: NEW_ROLE })}>{t("roleEditor.open")}</Button>}>
        <p className="m-0 text-sm text-ink-muted">{t("rolesHint")}</p>
      </FilterBar>
      <DataTable label={t("views.roles")} columns={columns} state={tableState} onRetry={reload} rowKey={(role) => role.id} empty={t("emptyRoles")} />
      <div ref={detailRef} className="scroll-mt-20 empty:hidden">
        {values.role === NEW_ROLE ? (
          <RoleEditor key={NEW_ROLE} role={null} onClose={() => set({ role: "" })} onSaved={(id) => { reload(); set({ role: id ?? "" }); }} />
        ) : picked ? (
          <RoleEditor key={picked.id} role={picked} onClose={() => set({ role: "" })} onSaved={() => reload()} />
        ) : null}
      </div>
    </>
  );
}
