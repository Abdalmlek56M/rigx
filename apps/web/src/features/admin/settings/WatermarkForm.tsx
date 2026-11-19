"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextField } from "@/components/ui/TextField";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { useToast } from "../kit/Toast";
import { CheckField } from "./CheckField";
import { LogoCard } from "./LogoCard";
import { SectionForm, useSectionSave } from "./SectionForm";
import { reapplyWatermark, saveWatermark } from "./settings-api";
import { watermarkForm, watermarkValue, WATERMARK_POSITIONS, type SectionProps, type WatermarkForm as Form } from "./settings-rules";
import { RefreshIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

/** ADM-Settings — watermark: on/off, placement, opacity/size, the watermark logo and a full reapply to old photos. */
export function WatermarkForm({ view, onSaved, onStale }: SectionProps) {
  const t = useTranslations("admin.settings.watermark");
  const toast = useToast();
  const { submit, save } = useSectionSave(view.rowVersion, onSaved, onStale);
  const [form, setForm] = useState<Form>(() => watermarkForm(view.watermark));
  const [confirming, setConfirming] = useState(false);
  const [reapplying, setReapplying] = useState(false);
  const set = (patch: Partial<Form>) => setForm((current) => ({ ...current, ...patch }));

  async function reapply() {
    setReapplying(true);
    try {
      await reapplyWatermark();
      toast(t("reapplyStarted"));
    } catch (error) {
      toast(error instanceof Error ? error.message : t("reapplyFailed"), "error");
    } finally {
      setReapplying(false);
    }
  }

  return (
    <>
      <SectionForm
        title={t("title")} hint={t("hint")} busy={submit.busy} error={submit.errors.general} dialog={submit.dialog}
        onSubmit={() => void save((rowVersion) => saveWatermark(watermarkValue(form), rowVersion))}
      >
        <CheckField label={t("enabled")} checked={form.enabled} onChange={(enabled) => set({ enabled })} />
        <div className="grid gap-3 sm:grid-cols-3">
          <SelectField
            label={t("position")} name="position" value={form.position} onChange={(position) => set({ position })}
            options={WATERMARK_POSITIONS.map((position) => ({ value: position, label: t(`positions.${position}`) }))}
          />
          <TextField label={t("opacity")} name="opacityPercent" type="number" min="5" max="100" step="1" ltr required value={form.opacityPercent} onChange={(opacityPercent) => set({ opacityPercent })} />
          <TextField label={t("size")} name="sizePercent" type="number" min="5" max="50" step="1" ltr required value={form.sizePercent} onChange={(sizePercent) => set({ sizePercent })} />
        </div>
        <CheckField label={t("articles")} checked={form.applyToArticles} onChange={(applyToArticles) => set({ applyToArticles })} />
        <LogoCard kind="watermark" storageKey={view.logos.watermarkLogoKey ?? null} onSaved={onSaved} />
        <p className="m-0 text-xs text-ink-muted">{t("fallback")}</p>

        <div className="flex flex-col gap-2 border-t border-line pt-3">
          <h3 className="m-0 text-[15px] font-bold">{t("oldImagesTitle")}</h3>
          <p className="m-0 text-xs text-ink-muted">{t("oldImagesHint")}</p>
          <Button icon={RefreshIcon} className="self-start" loading={reapplying} onClick={() => setConfirming(true)}>
            {t("reapply")}
          </Button>
        </div>
      </SectionForm>
      <ConfirmDialog
        open={confirming} title={t("reapplyTitle")} body={t("reapplyBody")} confirmLabel={t("reapply")}
        onClose={() => setConfirming(false)}
        onConfirm={async () => {
          await reapply();
        }}
      />
    </>
  );
}
