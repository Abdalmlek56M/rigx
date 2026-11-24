"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextField } from "@/components/ui/TextField";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { useToast } from "../kit/Toast";
import { CheckField } from "./CheckField";
import { FieldGrid, FieldNote, SubSection } from "./FieldGrid";
import { LogoCard } from "./LogoCard";
import { WatermarkPreview } from "./WatermarkPreview";
import { SectionForm, useSectionSave } from "./SectionForm";
import { useSectionDraft } from "./use-section-draft";
import { reapplyWatermark, saveWatermark } from "./settings-api";
import { watermarkForm, watermarkValue, WATERMARK_POSITIONS, type SectionProps } from "./settings-rules";
import { RefreshIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

/** ADM-Settings — watermark: on/off, placement, opacity/size, the watermark logo and a full reapply to old photos. */
export function WatermarkForm({ view, onSaved, onStale }: SectionProps) {
  const t = useTranslations("admin.settings.watermark");
  const toast = useToast();
  const { submit, save } = useSectionSave(view.rowVersion, onSaved, onStale);
  const { form, set, dirty, reset } = useSectionDraft(watermarkForm(view.watermark));
  const [confirming, setConfirming] = useState(false);
  const [reapplying, setReapplying] = useState(false);

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
        title={t("title")} hint={t("hint")} dirty={dirty} onReset={reset} busy={submit.busy} error={submit.errors.general} dialog={submit.dialog}
        onSubmit={() => void save((rowVersion) => saveWatermark(watermarkValue(form), rowVersion))}
      >
        <div className="grid items-start gap-5 lg:grid-cols-[minmax(0,1fr)_minmax(0,380px)]">
          <div className="flex min-w-0 flex-col gap-4">
        <CheckField label={t("enabled")} checked={form.enabled} onChange={(enabled) => set({ enabled })} />
        <FieldGrid>
          <SelectField
            label={t("position")} name="position" value={form.position} onChange={(position) => set({ position })}
            options={WATERMARK_POSITIONS.map((position) => ({ value: position, label: t(`positions.${position}`) }))}
          />
          <TextField label={t("opacity")} name="opacityPercent" type="number" min="5" max="100" step="1" ltr required value={form.opacityPercent} onChange={(opacityPercent) => set({ opacityPercent })} />
          <TextField label={t("size")} name="sizePercent" type="number" min="5" max="50" step="1" ltr required value={form.sizePercent} onChange={(sizePercent) => set({ sizePercent })} />
        </FieldGrid>
        <CheckField label={t("articles")} checked={form.applyToArticles} onChange={(applyToArticles) => set({ applyToArticles })} />
        <LogoCard kind="watermark" storageKey={view.logos.watermarkLogoKey ?? null} onSaved={onSaved} />
        <FieldNote>{t("fallback")}</FieldNote>
          </div>
          <WatermarkPreview
            logoKey={view.logos.watermarkLogoKey ?? view.logos.storeLogoKey ?? null}
            enabled={form.enabled} position={form.position} opacityText={form.opacityPercent} sizeText={form.sizePercent}
          />
        </div>

        <SubSection title={t("oldImagesTitle")}>
          <FieldNote>{t("oldImagesHint")}</FieldNote>
          <Button icon={RefreshIcon} className="self-start" loading={reapplying} onClick={() => setConfirming(true)}>
            {t("reapply")}
          </Button>
        </SubSection>
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
