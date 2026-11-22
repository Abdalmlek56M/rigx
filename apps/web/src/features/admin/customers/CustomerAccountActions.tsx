"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { Dialog } from "../kit/Dialog";
import { FormMessage } from "../kit/PageHeader";
import { useToast } from "../kit/Toast";
import { useSubmit } from "../kit/use-submit";
import { useUndoDelete } from "../system/use-undo-delete";
import {
  addToBlacklist,
  removeFromBlacklist,
  sendCustomerLoginCode,
  signOutCustomer,
  unlinkCustomerGoogle,
  type BlacklistEntry,
  type CustomerProfile
} from "./customers-api";
import { BanIcon, KeyIcon, LogoutIcon, UndoIcon, UnlinkIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

function BlacklistDialog({ profile, onClose, onSaved }: { profile: CustomerProfile; onClose: () => void; onSaved: () => void }) {
  const t = useTranslations("admin.customers.blacklist");
  const kit = useTranslations("admin.kit");
  const submit = useSubmit();
  const [reason, setReason] = useState("");
  const valid = reason.trim() !== "";

  async function save() {
    if (!valid) return;
    await submit.run(async () => {
      await addToBlacklist({ name: profile.account.name, phone: profile.account.phone, email: profile.account.email }, reason.trim());
      onSaved();
      onClose();
    }, t("saved"));
  }

  return (
    <Dialog open onClose={onClose} title={t("title")}>
      <p className="m-0 text-sm text-ink-muted">{profile.account.name} · {profile.account.phone ?? profile.account.email}</p>
      <form
        className="flex flex-col gap-3"
        onSubmit={(event) => {
          event.preventDefault();
          void save();
        }}
      >
        <TextAreaField label={t("reason")} name="reason" value={reason} onChange={setReason} placeholder={t("reasonPlaceholder")} error={submit.errors.fields.reason} />
        <FormMessage message={submit.errors.general} />
        <div className="flex flex-wrap justify-end gap-2">
          <Button variant="ghost" onClick={onClose}>{kit("cancel")}</Button>
          <Button type="submit" variant="danger" icon={BanIcon} loading={submit.busy} disabled={!valid}>{t("save")}</Button>
        </div>
        {submit.dialog}
      </form>
    </Dialog>
  );
}

/** Sign-in code, Google unlink, sign out everywhere and the blacklist. Merge and suspension stay out of this part. */
export function CustomerAccountActions({ profile, blacklist, canManage, canViewBlacklist, canApprove, onChanged }: {
  profile: CustomerProfile;
  blacklist: BlacklistEntry | null;
  canManage: boolean;
  canViewBlacklist: boolean;
  canApprove: boolean;
  onChanged: () => void;
}) {
  const t = useTranslations("admin.customers.account");
  const blacklistT = useTranslations("admin.customers.blacklist");
  const toast = useToast();
  const undoDelete = useUndoDelete();
  const submit = useSubmit();
  const [unlinking, setUnlinking] = useState(false);
  const [signingOut, setSigningOut] = useState(false);
  const [adding, setAdding] = useState(false);
  const [removing, setRemoving] = useState(false);
  if (!canManage && !canApprove) return null;

  /** `undo`: the entry the action moved to the trash, so its toast can offer to bring it back. */
  async function run(action: () => Promise<unknown>, success: string, undo?: { kind: string; id: string }) {
    try {
      await action();
      if (undo) undoDelete({ ...undo, message: success, onRestored: onChanged });
      else toast(success);
      onChanged();
    } catch {
      toast(blacklistT("failed"), "error");
      throw new Error("action failed");
    }
  }

  return (
    <section className="flex flex-col gap-2.5 rounded-card border border-line bg-surface p-5">
      <h2 className="m-0 text-[17px] font-bold">{t("title")}</h2>
      {canManage ? (
        <>
          <Button
            icon={KeyIcon} className="w-full" loading={submit.busy}
            onClick={() => void submit.run(async () => { await sendCustomerLoginCode(profile.account.id); }, t("codeSent"))}
          >
            {t("sendCode")}
          </Button>
          {profile.loginMethods.googleEmail ? (
            <Button icon={UnlinkIcon} className="w-full" onClick={() => setUnlinking(true)}>
              {t("unlinkGoogle")}
            </Button>
          ) : null}
          <Button icon={LogoutIcon} className="w-full" onClick={() => setSigningOut(true)}>
            {t("signOutAll")}
          </Button>
        </>
      ) : null}
      {profile.sales.blacklisted && canViewBlacklist ? <p className="m-0 rounded-control bg-error/10 px-3 py-2 text-sm font-bold text-error-text">{t("blacklistState")}</p> : null}
      {canApprove && !profile.sales.blacklisted ? (
        <Button variant="danger" icon={BanIcon} className="w-full" onClick={() => setAdding(true)}>
          {t("blacklist")}
        </Button>
      ) : null}
      {canApprove && canViewBlacklist && profile.sales.blacklisted && blacklist ? (
        <Button icon={UndoIcon} className="w-full" onClick={() => setRemoving(true)}>
          {t("blacklistRemove")}
        </Button>
      ) : null}
      <p className="m-0 text-xs text-ink-muted">{t("auditNote")}</p>
      {submit.dialog}

      <ConfirmDialog
        open={unlinking} title={t("unlinkGoogle")} body={t("unlinkConfirm")} confirmLabel={t("unlinkGoogle")} danger
        onClose={() => setUnlinking(false)}
        onConfirm={() => run(() => unlinkCustomerGoogle(profile.account.id), t("unlinkGoogle"))}
      />
      <ConfirmDialog
        open={signingOut} title={t("signOutAll")} body={t("signOutConfirm")} confirmLabel={t("signOutAll")}
        onClose={() => setSigningOut(false)}
        onConfirm={() => run(() => signOutCustomer(profile.account.id), t("signOutAll"))}
      />
      <ConfirmDialog
        open={removing} title={t("blacklistRemove")} body={t("blacklistConfirm")} confirmLabel={t("blacklistRemove")} danger
        onClose={() => setRemoving(false)}
        onConfirm={async () => {
          if (!blacklist) return;
          await run(() => removeFromBlacklist(blacklist.id), blacklistT("removed"), { kind: "blacklist", id: blacklist.id });
        }}
      />
      {adding ? <BlacklistDialog profile={profile} onClose={() => setAdding(false)} onSaved={onChanged} /> : null}
    </section>
  );
}
