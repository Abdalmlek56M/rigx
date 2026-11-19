"use client";

import { useCallback, useState } from "react";
import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { isConflict } from "@/lib/api/conflict";
import { useLoad } from "@/lib/hooks/use-load";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { useRoleLabel } from "./use-role-label";
import { formMessage } from "../form-message.ts";
import { PermissionPicker } from "./PermissionPicker";
import { roleCodeValid, sameSet } from "./user-rules.ts";
import { createRole, fetchPermissions, setRolePermissions, type StaffRole } from "./users-api";

/** Create a role (`role` = null) or change the permissions of an existing one; both need the step-up code. */
export function RoleEditor({ role, onSaved, onClose }: { role: StaffRole | null; onSaved: (id: string | null) => void; onClose: () => void }) {
  const t = useTranslations("admin.users.roleEditor");
  const label = useRoleLabel();
  const permissions = useLoad(useCallback(() => fetchPermissions(), []));
  const submit = useSubmit();
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [selected, setSelected] = useState<string[]>(role?.permissionCodes ?? []);
  const valid = role ? !sameSet(selected, role.permissionCodes) : roleCodeValid(code) && name.trim() !== "";

  async function save() {
    if (!valid) return;
    await submit.run(async (headers) => {
      try {
        if (role) {
          await setRolePermissions(role.id, selected, headers);
          onSaved(null);
        } else {
          const created = await createRole({ code: code.trim(), name: name.trim(), permissionCodes: selected }, headers);
          onSaved(created.id);
        }
      } catch (error) {
        if (isConflict(error)) onSaved(null);
        throw error;
      }
    }, role ? t("saved") : t("created"));
  }

  return (
    <section className="flex min-w-0 flex-col gap-4 rounded-card border border-line bg-surface p-5">
      <header className="flex flex-wrap items-center justify-between gap-2">
        <h2 className="m-0 text-[17px] font-bold [overflow-wrap:anywhere]">{role ? t("editTitle", { name: label(role) }) : t("newTitle")}</h2>
        <button type="button" onClick={onClose} className="h-11 rounded-control border border-input-line bg-surface px-4 text-sm font-bold">{t("close")}</button>
      </header>
      <form className="flex flex-col gap-3" onSubmit={(event) => { event.preventDefault(); void save(); }}>
        {role ? null : (
          <div className="grid gap-3 sm:grid-cols-2">
            <TextField label={t("name")} name="name" value={name} onChange={setName} error={submit.errors.fields.name} />
            <TextField label={t("code")} name="code" ltr value={code} onChange={setCode} error={submit.errors.fields.code} />
          </div>
        )}
        <h3 className="m-0 text-[15px] font-bold">{t("permissions")}</h3>
        {permissions.state.status === "loading" ? <div aria-hidden="true" className="h-32 animate-pulse rounded-control bg-line" /> : null}
        {permissions.state.status === "error" ? (
          <div role="alert" className="flex flex-col items-start gap-2">
            <p className="m-0 font-bold">{t("failed")}</p>
            <button type="button" onClick={permissions.reload} className="h-11 rounded-control bg-carbon px-5 font-bold text-on-carbon">{t("retry")}</button>
          </div>
        ) : null}
        {permissions.state.status === "ready" ? <PermissionPicker codes={permissions.state.data} selected={selected} onChange={setSelected} /> : null}
        <p className="m-0 text-xs text-ink-muted">{t("stepUpNote")}</p>
        <FormMessage message={formMessage(submit.errors, ["code", "name"])} />
        <button type="submit" disabled={submit.busy || !valid} className="h-11 self-start rounded-control bg-carbon px-5 font-bold text-on-carbon disabled:opacity-50">{role ? t("save") : t("create")}</button>
        {submit.dialog}
      </form>
    </section>
  );
}
