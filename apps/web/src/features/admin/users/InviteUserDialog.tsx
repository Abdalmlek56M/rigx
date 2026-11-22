"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { Dialog } from "../kit/Dialog";
import { FormMessage } from "../kit/PageHeader";
import { useToast } from "../kit/Toast";
import { useSubmit } from "../kit/use-submit";
import { formMessage } from "../form-message.ts";
import { RoleCheckboxes } from "./RoleCheckboxes";
import { usernameFromEmail } from "./user-rules.ts";
import { createUser, type StaffRole } from "./users-api";
import { SendIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

const SHOWN = ["fullName", "email", "phoneNumber", "username", "roleIds"];

/** Invite a staff member: the server emails them a sign-in invitation (no password). */
export function InviteUserDialog({ roles, onClose, onCreated }: { roles: readonly StaffRole[]; onClose: () => void; onCreated: (id: string) => void }) {
  const t = useTranslations("admin.users.invite");
  const kit = useTranslations("admin.kit");
  const toast = useToast();
  const submit = useSubmit();
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [username, setUsername] = useState("");
  const [usernameEdited, setUsernameEdited] = useState(false);
  const [roleIds, setRoleIds] = useState<string[]>([]);
  const valid = fullName.trim() !== "" && email.trim() !== "" && username.trim() !== "";

  function changeEmail(next: string) {
    setEmail(next);
    if (!usernameEdited) setUsername(usernameFromEmail(next));
  }

  async function save() {
    if (!valid) return;
    await submit.run(async () => {
      const result = await createUser({
        fullName: fullName.trim(), email: email.trim(), username: username.trim(), phoneNumber: phone.trim() || null, roleIds
      });
      if (result.emailDispatchQueued) toast(t("queued"));
      onCreated(result.user.id);
      onClose();
    }, t("created"));
  }

  return (
    <Dialog open onClose={onClose} title={t("title")}>
      <form className="flex flex-col gap-3" onSubmit={(event) => { event.preventDefault(); void save(); }}>
        <TextField label={t("fullName")} name="fullName" value={fullName} onChange={setFullName} error={submit.errors.fields.fullName} />
        <TextField label={t("email")} name="email" type="email" ltr value={email} onChange={changeEmail} error={submit.errors.fields.email} />
        <TextField label={t("phone")} name="phoneNumber" type="tel" ltr value={phone} onChange={setPhone} error={submit.errors.fields.phoneNumber} />
        <TextField
          label={t("username")} name="username" ltr value={username} error={submit.errors.fields.username}
          onChange={(next) => { setUsername(next); setUsernameEdited(true); }}
        />
        <fieldset className="m-0 flex min-w-0 flex-col gap-2 border-0 p-0">
          <legend className="mb-1.5 p-0 text-[13px] font-bold">{t("roles")}</legend>
          <RoleCheckboxes roles={roles} selected={roleIds} onChange={setRoleIds} />
          {submit.errors.fields.roleIds ? <p role="alert" className="m-0 text-xs font-bold text-error-text">{submit.errors.fields.roleIds}</p> : null}
        </fieldset>
        <p className="m-0 text-xs text-ink-muted">{t("hint")}</p>
        <FormMessage message={formMessage(submit.errors, SHOWN)} />
        <div className="flex flex-wrap justify-end gap-2">
          <Button variant="ghost" onClick={onClose}>{kit("cancel")}</Button>
          <Button type="submit" variant="primary" icon={SendIcon} loading={submit.busy} disabled={!valid}>{t("submit")}</Button>
        </div>
        {submit.dialog}
      </form>
    </Dialog>
  );
}
