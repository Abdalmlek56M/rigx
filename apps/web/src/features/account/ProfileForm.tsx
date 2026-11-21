"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { unwrap } from "@rigx/api-client";
import { Button } from "@/components/ui/Button";
import { TextField } from "@/components/ui/TextField";
import { refreshSession, type SessionUser } from "@/features/auth/session-store";
import { browserApi } from "@/lib/api/browser";
import { profileErrors, toProfileRequest, type ProfileField } from "./profile-rules";

/** Name and mobile; saved to the account, and the shared session is refreshed so the greeting updates. */
export function ProfileForm({ user }: { user: SessionUser }) {
  const t = useTranslations("account.profile.form");
  const [form, setForm] = useState({ fullName: user.fullName, phone: user.phoneNumber ?? "" });
  const [errors, setErrors] = useState<ProfileField[]>([]);
  const [state, setState] = useState<"idle" | "busy" | "saved" | "failed">("idle");

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    const found = profileErrors(form);
    setErrors(found);
    if (found.length > 0) return;
    setState("busy");
    try {
      await unwrap(browserApi().PATCH("/api/v1/identity/profile", { body: toProfileRequest(form) }));
      await refreshSession();
      setState("saved");
    } catch {
      setState("failed");
    }
  }

  const error = (field: ProfileField) => (errors.includes(field) ? t(`errors.${field}`) : null);

  return (
    <form onSubmit={submit} noValidate className="flex flex-col gap-3.5">
      <div className="grid gap-3.5 sm:grid-cols-2">
        <TextField label={t("fullName")} name="fullName" value={form.fullName} onChange={(v) => setForm((f) => ({ ...f, fullName: v }))} autoComplete="name" required error={error("fullName")} />
        <TextField label={t("phone")} name="phone" type="tel" ltr value={form.phone} onChange={(v) => setForm((f) => ({ ...f, phone: v }))} autoComplete="tel" placeholder="01xxxxxxxxx" error={error("phone")} />
      </div>
      <p className="m-0 text-[13px] text-ink-muted">{t("phoneNote")}</p>
      {state === "saved" ? <p role="status" className="m-0 text-sm text-success">{t("saved")}</p> : null}
      {state === "failed" ? <p role="alert" className="m-0 text-sm text-error">{t("failed")}</p> : null}
      <div>
        <Button type="submit" variant="accent" loading={state === "busy"}>{t("save")}</Button>
      </div>
    </form>
  );
}
