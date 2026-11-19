"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { isConflict } from "@/lib/api/conflict";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { useToast } from "../kit/Toast";
import { signOutStaff, updateUser, type StaffUser } from "./users-api";
import { BanIcon, LogoutIcon, UndoIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

type Pending = "disable" | "enable" | "signOut" | null;

/**
 * Disable / enable the account and sign it out of every device. Changing the role lives in the roles tab (it needs the
 * step-up code). Your own account shows a note instead, so nobody locks themselves out.
 */
export function StaffAccountControls({ user, isSelf, onChanged }: { user: StaffUser; isSelf: boolean; onChanged: () => void }) {
  const t = useTranslations("admin.users.controls");
  const toast = useToast();
  const [pending, setPending] = useState<Pending>(null);

  async function run(action: () => Promise<unknown>, success: string) {
    try {
      await action();
      toast(success);
    } catch (error) {
      toast(isConflict(error) ? t("conflict") : t("failed"), "error");
    }
    onChanged();
  }

  const setActive = (isActive: boolean) =>
    run(() => updateUser(user.id, { fullName: user.fullName, phoneNumber: user.phoneNumber ?? null, isActive }), isActive ? t("enabled") : t("disabled"));

  return (
    <section className="flex flex-col gap-2.5 rounded-card border border-line bg-surface p-5">
      <h2 className="m-0 text-[17px] font-bold">{t("title")}</h2>
      {isSelf ? (
        <p className="m-0 rounded-control bg-paper px-3 py-2 text-sm text-ink-muted">{t("self")}</p>
      ) : (
        <>
          {user.isActive ? (
            <Button variant="danger" icon={BanIcon} className="w-full" onClick={() => setPending("disable")}>{t("disable")}</Button>
          ) : (
            <Button variant="primary" icon={UndoIcon} className="w-full" onClick={() => setPending("enable")}>{t("enable")}</Button>
          )}
          <Button icon={LogoutIcon} className="w-full" onClick={() => setPending("signOut")}>{t("signOutAll")}</Button>
          <p className="m-0 text-xs text-ink-muted">{t("auditNote")}</p>
        </>
      )}
      <ConfirmDialog
        open={pending === "disable"} title={t("disable")} body={t("disableBody")} confirmLabel={t("disable")} danger
        onClose={() => setPending(null)} onConfirm={() => setActive(false)}
      />
      <ConfirmDialog
        open={pending === "enable"} title={t("enable")} body={t("enableBody")} confirmLabel={t("enable")}
        onClose={() => setPending(null)} onConfirm={() => setActive(true)}
      />
      <ConfirmDialog
        open={pending === "signOut"} title={t("signOutAll")} body={t("signOutBody")} confirmLabel={t("signOutAll")}
        onClose={() => setPending(null)} onConfirm={() => run(() => signOutStaff(user.id), t("signedOut"))}
      />
    </section>
  );
}
