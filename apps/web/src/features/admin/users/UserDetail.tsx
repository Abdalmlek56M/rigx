"use client";

import { useCallback, useState } from "react";
import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { useSession } from "@/features/auth/use-session";
import { isConflict } from "@/lib/api/conflict";
import { useLoad } from "@/lib/hooks/use-load";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { formMessage } from "../form-message.ts";
import { UserRestrictions } from "./UserRestrictions";
import { UserRolesCard } from "./UserRolesCard";
import { isSameAccount } from "./user-rules.ts";
import { fetchUser, updateUser, type StaffRole, type StaffUser } from "./users-api";
import { CloseIcon, RefreshIcon, SaveIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

const card = "flex min-w-0 flex-col gap-3 rounded-card border border-line bg-surface p-5";

function ProfileCard({ user, isSelf, onSaved }: { user: StaffUser; isSelf: boolean; onSaved: () => void }) {
  const t = useTranslations("admin.users.detail");
  const submit = useSubmit();
  const [fullName, setFullName] = useState(user.fullName);
  const [phone, setPhone] = useState(user.phoneNumber ?? "");
  const [isActive, setIsActive] = useState(user.isActive);
  const [confirming, setConfirming] = useState(false);
  const valid = fullName.trim() !== "";
  const deactivating = user.isActive && !isActive;

  async function save() {
    await submit.run(async () => {
      try {
        await updateUser(user.id, { fullName: fullName.trim(), phoneNumber: phone.trim() || null, isActive });
      } catch (error) {
        if (isConflict(error)) onSaved();
        throw error;
      }
      onSaved();
    });
  }

  return (
    <form className={card} onSubmit={(event) => { event.preventDefault(); if (!valid) return; if (deactivating) setConfirming(true); else void save(); }}>
      <h3 className="m-0 text-[15px] font-bold">{t("profile")}</h3>
      <p dir="ltr" className="m-0 break-all font-mono text-xs text-ink-muted">{user.email} · {user.username}</p>
      <TextField label={t("fullName")} name="fullName" value={fullName} onChange={setFullName} error={submit.errors.fields.fullName} />
      <TextField label={t("phone")} name="phoneNumber" type="tel" ltr value={phone} onChange={setPhone} error={submit.errors.fields.phoneNumber} />
      {isSelf ? null : (
        <label className="flex min-h-11 items-center gap-3 text-sm font-bold">
          <input type="checkbox" checked={isActive} onChange={(event) => setIsActive(event.target.checked)} className="h-5 w-5 accent-orange" />
          {t("active")}
        </label>
      )}
      <FormMessage message={formMessage(submit.errors, ["fullName", "phoneNumber"])} />
      <Button type="submit" variant="primary" icon={SaveIcon} className="self-start" loading={submit.busy} disabled={!valid}>{t("save")}</Button>
      {submit.dialog}
      <ConfirmDialog
        open={confirming} title={t("deactivateTitle")} body={t("deactivateBody")} confirmLabel={t("deactivate")} danger
        onClose={() => setConfirming(false)} onConfirm={save}
      />
    </form>
  );
}

/** One staff member: profile and active switch, roles (step-up) and restrictions. Your own account hides what could lock you out. */
export function UserDetail({ id, roles, onChanged, onClose }: { id: string; roles: readonly StaffRole[]; onChanged: () => void; onClose: () => void }) {
  const t = useTranslations("admin.users");
  const session = useSession();
  const { state, reload } = useLoad(useCallback(() => fetchUser(id), [id]));

  if (state.status === "loading") return <div aria-hidden="true" className="h-40 animate-pulse rounded-card border border-line bg-surface" />;
  if (state.status === "error") {
    return (
      <div role="alert" className="flex flex-col items-start gap-3 rounded-card border border-line bg-surface p-5">
        <p className="m-0 font-bold">{t("failed")}</p>
        <Button variant="primary" icon={RefreshIcon} onClick={reload}>{t("retry")}</Button>
      </div>
    );
  }

  const user = state.data;
  const isSelf = session.status === "signedIn" && isSameAccount(user.email, session.user.email);
  const changed = () => { reload(); onChanged(); };
  return (
    <section className="flex min-w-0 flex-col gap-4">
      <header className="flex flex-wrap items-center justify-between gap-2">
        <h2 className="m-0 text-[17px] font-bold [overflow-wrap:anywhere]">{user.fullName}</h2>
        <Button icon={CloseIcon} onClick={onClose}>{t("close")}</Button>
      </header>
      {isSelf ? <p className="m-0 rounded-control bg-paper px-3 py-2 text-sm text-ink-muted">{t("detail.self")}</p> : null}
      <div className="grid gap-4 xl:grid-cols-2">
        <ProfileCard key={`${user.fullName}|${user.phoneNumber}|${user.isActive}`} user={user} isSelf={isSelf} onSaved={changed} />
        {isSelf ? null : <UserRolesCard key={user.roleIds.join(",")} user={user} roles={roles} onSaved={changed} />}
      </div>
      {isSelf ? null : <UserRestrictions userId={user.id} />}
    </section>
  );
}
