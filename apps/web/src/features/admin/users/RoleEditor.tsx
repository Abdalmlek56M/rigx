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
import { CloseIcon, PlusIcon, RefreshIcon, SaveIcon } from "@/components/ui/icons";
import { cardClass } from "@/components/ui/Card";
import { Button } from "../kit/Button";

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
    <section className={cardClass({ className: "gap-4" })}>
      <header className="flex flex-wrap items-center justify-between gap-2">
        <h2 className="m-0 text-[17px] font-bold [overflow-wrap:anywhere]">{role ? t("editTitle", { name: label(role) }) : t("newTitle")}</h2>
        <Button icon={CloseIcon} onClick={onClose}>{t("close")}</Button>
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
            <Button variant="primary" icon={RefreshIcon} onClick={permissions.reload}>{t("retry")}</Button>
          </div>
        ) : null}
        {permissions.state.status === "ready" ? <PermissionPicker codes={permissions.state.data} selected={selected} onChange={setSelected} /> : null}
        <p className="m-0 text-xs text-ink-muted">{t("stepUpNote")}</p>
        <FormMessage message={formMessage(submit.errors, ["code", "name"])} />
        <Button type="submit" variant="primary" icon={role ? SaveIcon : PlusIcon} className="self-start" loading={submit.busy} disabled={!valid}>{role ? t("save") : t("create")}</Button>
        {submit.dialog}
      </form>
    </section>
  );
}
