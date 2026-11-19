"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { isConflict } from "@/lib/api/conflict";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { RoleCheckboxes } from "./RoleCheckboxes";
import { sameSet } from "./user-rules.ts";
import { assignRoles, type StaffRole, type StaffUser } from "./users-api";
import { SaveIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

/** The roles of a staff member; saving needs the step-up code, and taking away every role asks first. */
export function UserRolesCard({ user, roles, onSaved }: { user: StaffUser; roles: readonly StaffRole[]; onSaved: () => void }) {
  const t = useTranslations("admin.users.rolesCard");
  const submit = useSubmit();
  const [selected, setSelected] = useState<string[]>(user.roleIds);
  const [confirming, setConfirming] = useState(false);
  const unchanged = sameSet(selected, user.roleIds);

  async function save() {
    await submit.run(async (headers) => {
      try {
        await assignRoles(user.id, selected, headers);
      } catch (error) {
        if (isConflict(error)) onSaved();
        throw error;
      }
      onSaved();
    });
  }

  return (
    <form
      className="flex min-w-0 flex-col gap-3 rounded-card border border-line bg-surface p-5"
      onSubmit={(event) => { event.preventDefault(); if (selected.length === 0) setConfirming(true); else void save(); }}
    >
      <h3 className="m-0 text-[15px] font-bold">{t("title")}</h3>
      <RoleCheckboxes roles={roles} selected={selected} onChange={setSelected} />
      <p className="m-0 text-xs text-ink-muted">{t("stepUpNote")}</p>
      <FormMessage message={submit.errors.general ?? submit.errors.fields.roleIds ?? null} />
      <Button type="submit" variant="primary" icon={SaveIcon} className="self-start" loading={submit.busy} disabled={unchanged}>{t("save")}</Button>
      {submit.dialog}
      <ConfirmDialog
        open={confirming} title={t("emptyTitle")} body={t("emptyBody")} confirmLabel={t("save")} danger
        onClose={() => setConfirming(false)} onConfirm={save}
      />
    </form>
  );
}
