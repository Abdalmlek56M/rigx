"use client";

import { useCallback, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { TextField } from "@/components/ui/TextField";
import { formatDateTime } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { ReinstateDialog } from "./ReinstateDialog";
import { RESTRICTION_TYPES, restrictionRequest, restrictionType, restrictionValid, type RestrictionForm } from "./user-rules.ts";
import { addRestriction, fetchRestrictions, type Restriction } from "./users-api";

const EMPTY: RestrictionForm = { type: "Suspension", reason: "", until: "" };

function AddRestriction({ userId, onAdded }: { userId: string; onAdded: () => void }) {
  const t = useTranslations("admin.users.restrictions");
  const submit = useSubmit();
  const [form, setForm] = useState<RestrictionForm>(EMPTY);
  const [confirming, setConfirming] = useState(false);

  async function save() {
    await submit.run(async (headers) => {
      await addRestriction(userId, restrictionRequest(form), headers);
      setForm(EMPTY);
      onAdded();
    }, t("added"));
  }

  return (
    <form className="flex flex-col gap-3 border-t border-line pt-3" onSubmit={(event) => { event.preventDefault(); if (restrictionValid(form)) setConfirming(true); }}>
      <h4 className="m-0 text-sm font-bold">{t("add")}</h4>
      <SelectField
        label={t("type")} name="type" value={form.type} error={submit.errors.fields.type}
        onChange={(type) => setForm({ ...form, type: restrictionType(type) })}
        options={RESTRICTION_TYPES.map((type) => ({ value: type, label: t(`types.${type}`) }))}
      />
      {form.type === "Suspension" ? (
        <TextField label={t("until")} name="expiresAtUtc" type="date" ltr value={form.until} onChange={(until) => setForm({ ...form, until })} error={submit.errors.fields.expiresAtUtc} />
      ) : null}
      <TextAreaField label={t("reason")} name="reason" value={form.reason} onChange={(reason) => setForm({ ...form, reason })} error={submit.errors.fields.reason} maxLength={1000} />
      <FormMessage message={submit.errors.general} />
      <button type="submit" disabled={submit.busy || !restrictionValid(form)} className="h-11 self-start rounded-control bg-error px-5 font-bold text-surface disabled:opacity-50">{t(`submit.${form.type}`)}</button>
      {submit.dialog}
      <ConfirmDialog
        open={confirming} title={t(`submit.${form.type}`)} body={t(`confirm.${form.type}`)} confirmLabel={t(`submit.${form.type}`)} danger
        onClose={() => setConfirming(false)} onConfirm={save}
      />
    </form>
  );
}

/** The account's restrictions (suspension / ban), add one (step-up) and reinstate an active one. */
export function UserRestrictions({ userId }: { userId: string }) {
  const t = useTranslations("admin.users.restrictions");
  const locale = useLocale();
  const { state, reload } = useLoad(useCallback(() => fetchRestrictions(userId), [userId]));
  const [reinstating, setReinstating] = useState<Restriction | null>(null);

  return (
    <section className="flex min-w-0 flex-col gap-3 rounded-[10px] border border-line bg-surface p-5">
      <h3 className="m-0 text-[15px] font-bold">{t("title")}</h3>
      {state.status === "loading" ? <div aria-hidden="true" className="h-16 animate-pulse rounded-control bg-line" /> : null}
      {state.status === "error" ? (
        <div role="alert" className="flex flex-col items-start gap-2">
          <p className="m-0 font-bold">{t("failed")}</p>
          <button type="button" onClick={reload} className="h-11 rounded-control bg-carbon px-5 font-bold text-on-carbon">{t("retry")}</button>
        </div>
      ) : null}
      {state.status === "ready" && state.data.length === 0 ? <p className="m-0 text-sm text-ink-muted">{t("empty")}</p> : null}
      {state.status === "ready" && state.data.length > 0 ? (
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {state.data.map((item) => (
            <li key={item.id} className="flex flex-col gap-1.5 rounded-control border border-line bg-paper p-3 text-sm">
              <div className="flex flex-wrap items-center justify-between gap-2">
                <span className="flex flex-wrap items-center gap-2">
                  <span className="font-bold">{t(`types.${restrictionType(item.type)}`)}</span>
                  <span className={`rounded-[4px] px-2 py-[3px] text-xs font-bold ${item.isActive ? "bg-error/10 text-error" : "bg-line text-ink-muted"}`}>{item.isActive ? t("active") : t("ended")}</span>
                </span>
                {item.isActive ? (
                  <button type="button" onClick={() => setReinstating(item)} className="h-11 rounded-control border border-input-line bg-surface px-4 text-sm font-bold">{t("reinstate")}</button>
                ) : null}
              </div>
              <p className="m-0 [overflow-wrap:anywhere]">{item.reason}</p>
              <p className="m-0 text-xs text-ink-muted">
                {t("from")}: <span dir="ltr">{formatDateTime(item.effectiveAtUtc, locale)}</span> · {t("untilLabel")}: <span dir="ltr">{item.expiresAtUtc ? formatDateTime(item.expiresAtUtc, locale) : t("forever")}</span>
              </p>
              {item.reinstatementReason ? <p className="m-0 text-xs text-ink-muted [overflow-wrap:anywhere]">{t("reinstatedBecause", { reason: item.reinstatementReason })}</p> : null}
            </li>
          ))}
        </ul>
      ) : null}
      <AddRestriction userId={userId} onAdded={reload} />
      {reinstating ? <ReinstateDialog restriction={reinstating} onClose={() => setReinstating(null)} onDone={reload} /> : null}
    </section>
  );
}
