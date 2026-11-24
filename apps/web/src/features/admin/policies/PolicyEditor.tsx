"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { isConflict } from "@/lib/api/conflict";
import { PolicyBody } from "@/features/policies/PolicyBody";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { publishPolicy, updatePolicy, type Policy } from "./policies-api";
import { PolicyBodyField } from "./PolicyBodyField";
import { PolicyStatusBadge } from "./PolicyStatusBadge";
import { policyForm, policyPublishable, policySavable, sameForm, toPolicyInput, type PolicyForm } from "./policy-rules";
import { SaveIcon, UploadIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

function Preview({ title, text, dir }: { title: string; text: string; dir: "rtl" | "ltr" }) {
  const t = useTranslations("admin.policies");
  return (
    <section dir={dir} className="flex flex-col gap-3 rounded-card border border-line bg-canvas p-4">
      <h3 className="m-0 text-base font-bold">{title}</h3>
      {text.trim() === "" ? <p className="m-0 text-sm text-ink-muted">{t("previewEmpty")}</p> : <PolicyBody body={text} />}
    </section>
  );
}

/** Edits one draft: Arabic and English title and body, a live preview exactly as the public page renders it, save and publish. */
export function PolicyEditor({ policy, onSaved, onStale }: { policy: Policy; onSaved: (policy: Policy) => void; onStale: () => void }) {
  const t = useTranslations("admin.policies");
  const submit = useSubmit();
  const [form, setForm] = useState<PolicyForm>(() => policyForm(policy));
  const [confirming, setConfirming] = useState(false);
  const set = (patch: Partial<PolicyForm>) => setForm((current) => ({ ...current, ...patch }));
  const dirty = !sameForm(form, policyForm(policy));

  async function guarded(action: () => Promise<Policy>) {
    try {
      onSaved(await action());
    } catch (error) {
      if (isConflict(error)) onStale();
      throw error;
    }
  }

  const save = () => submit.run(() => guarded(() => updatePolicy(policy.id, toPolicyInput(form, policy.rowVersion))), t("saved"));
  /** Saves what is on screen first, so what goes live is what the owner is looking at. */
  const publish = () => guarded(async () => {
    const saved = dirty ? await updatePolicy(policy.id, toPolicyInput(form, policy.rowVersion)) : policy;
    return publishPolicy(saved.id, saved.rowVersion);
  });

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center gap-2">
        <PolicyStatusBadge status={policy.status} />
        <span className="text-sm text-ink-muted">{t("version", { version: policy.versionNumber })}</span>
      </div>
      <form className="flex flex-col gap-4" onSubmit={(event) => { event.preventDefault(); if (policySavable(form)) void save(); }}>
        <p className="m-0 rounded-control bg-canvas p-3 text-xs text-ink-muted">{t("formatHelp")}</p>
        <TextField label={t("fields.titleAr")} name="titleAr" dir="rtl" value={form.titleAr} onChange={(titleAr) => set({ titleAr })} error={submit.errors.fields.titleAr} required />
        <PolicyBodyField label={t("fields.bodyAr")} name="bodyAr" dir="rtl" value={form.bodyAr} onChange={(bodyAr) => set({ bodyAr })} error={submit.errors.fields.bodyAr} />
        <TextField label={t("fields.titleEn")} name="titleEn" value={form.titleEn} onChange={(titleEn) => set({ titleEn })} ltr error={submit.errors.fields.titleEn} required />
        <PolicyBodyField label={t("fields.bodyEn")} name="bodyEn" dir="ltr" value={form.bodyEn} onChange={(bodyEn) => set({ bodyEn })} error={submit.errors.fields.bodyEn} />
        <div className="flex flex-col gap-3">
          <h3 className="m-0 text-[15px] font-bold">{t("preview")}</h3>
          <Preview title={form.titleAr} text={form.bodyAr} dir="rtl" />
          <Preview title={form.titleEn} text={form.bodyEn} dir="ltr" />
        </div>
        <FormMessage message={submit.errors.general} />
        <div className="flex flex-wrap gap-2">
          <Button type="submit" icon={SaveIcon} loading={submit.busy} disabled={!dirty || !policySavable(form)}>{t("save")}</Button>
          <Button variant="primary" icon={UploadIcon} disabled={submit.busy || !policyPublishable(form)} onClick={() => setConfirming(true)}>{t("publish")}</Button>
        </div>
        {!policyPublishable(form) ? <p className="m-0 text-xs text-ink-muted">{t("publishNeeds")}</p> : null}
        {submit.dialog}
      </form>
      <ConfirmDialog
        open={confirming} title={t("publish")} body={t("publishBody")} confirmLabel={t("publish")}
        onClose={() => setConfirming(false)}
        onConfirm={() => submit.run(publish, t("published"))}
      />
    </div>
  );
}
