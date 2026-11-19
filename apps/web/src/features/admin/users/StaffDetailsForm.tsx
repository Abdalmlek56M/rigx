"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { isConflict } from "@/lib/api/conflict";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { formMessage } from "../form-message.ts";
import { updateUser, type StaffUser } from "./users-api";
import { SaveIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

/** Edit a staff member's name and phone. The email is how they sign in, so it shows but does not change here. */
export function StaffDetailsForm({ user, onSaved }: { user: StaffUser; onSaved: () => void }) {
  const t = useTranslations("admin.users.detail");
  const submit = useSubmit();
  const [fullName, setFullName] = useState(user.fullName);
  const [phone, setPhone] = useState(user.phoneNumber ?? "");
  const valid = fullName.trim() !== "";
  const unchanged = fullName.trim() === user.fullName && (phone.trim() || null) === (user.phoneNumber ?? null);

  async function save() {
    await submit.run(async () => {
      try {
        await updateUser(user.id, { fullName: fullName.trim(), phoneNumber: phone.trim() || null, isActive: user.isActive });
      } catch (error) {
        if (isConflict(error)) onSaved();
        throw error;
      }
      onSaved();
    });
  }

  return (
    <form className="flex min-w-0 flex-col gap-3" onSubmit={(event) => { event.preventDefault(); if (valid) void save(); }}>
      <p className="m-0 text-sm text-ink-muted">{t("emailNote")} <span dir="ltr" className="break-all font-mono text-xs text-ink">{user.email}</span></p>
      <TextField label={t("fullName")} name="fullName" value={fullName} onChange={setFullName} error={submit.errors.fields.fullName} />
      <TextField label={t("phone")} name="phoneNumber" type="tel" ltr value={phone} onChange={setPhone} error={submit.errors.fields.phoneNumber} />
      <FormMessage message={formMessage(submit.errors, ["fullName", "phoneNumber"])} />
      <Button type="submit" variant="primary" icon={SaveIcon} className="self-start" loading={submit.busy} disabled={!valid || unchanged}>{t("save")}</Button>
      {submit.dialog}
    </form>
  );
}
