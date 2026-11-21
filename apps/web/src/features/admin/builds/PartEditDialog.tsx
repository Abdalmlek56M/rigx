"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { formMessage } from "../form-message";
import { Dialog } from "../kit/Dialog";
import { FormMessage } from "../kit/PageHeader";
import { PartnerFields } from "../stock/PartnerFields";
import { financingPartners } from "../stock/stock-fields";
import { WarrantyField } from "../stock/WarrantyField";
import type { BuildLookups } from "./BuildParts";
import { editPart, type BuildPart } from "./builds-api";
import { partFormValid, toPartBody, toPartForm, type PartForm } from "./build-rules";
import { useBuildAction, type BuildActionProps } from "./use-build-action";
import { SaveIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

const SHOWN_FIELDS = ["partPrice", "warrantyLabel", "partnerId", "partnerPercentOverride"];

/** One part's price in this build, its own warranty (empty = the build's) and who financed it with what share. */
export function PartEditDialog({ part, lookups, onClose, ...action }: BuildActionProps & { part: BuildPart; lookups: BuildLookups; onClose: () => void }) {
  const t = useTranslations("admin.builds.partDialog");
  const { build } = action;
  const { submit, run } = useBuildAction(action);
  const [form, setForm] = useState<PartForm>(() => toPartForm(part));
  const set = (patch: Partial<PartForm>) => setForm((current) => ({ ...current, ...patch }));
  const error = (field: string) => submit.errors.fields[field];
  // The part's current partner stays in the select even when it is no longer offered for new financing.
  const financing = lookups.partners ? financingPartners(lookups.partners) : [];
  const current = lookups.partners?.find((partner) => partner.id === part.partnerId);
  const partners = current && !financing.some((partner) => partner.id === current.id) ? [...financing, current] : financing;

  async function save() {
    if (!partFormValid(form)) return;
    await run(async () => {
      const saved = await editPart(build.id, part.unitId, toPartBody(form, build.rowVersion));
      onClose();
      return saved;
    });
  }

  return (
    <Dialog open onClose={onClose} title={t("title", { name: part.name })}>
      <form
        className="flex flex-col gap-3"
        onSubmit={(event) => {
          event.preventDefault();
          void save();
        }}
      >
        <TextField label={t("price")} name="partPrice" type="number" ltr value={form.partPrice} onChange={(partPrice) => set({ partPrice })} error={error("partPrice")} />
        <WarrantyField value={form.warrantyLabel} onChange={(warrantyLabel) => set({ warrantyLabel })} options={lookups.warrantyLabels} error={error("warrantyLabel")} emptyLabel={t("buildWarranty")} />
        {lookups.partners ? (
          <PartnerFields
            partners={partners} partnerId={form.partnerId} percent={form.partnerPercent} namePrefix="partPartner"
            onChange={(patch) => set({ ...(patch.partnerId !== undefined ? { partnerId: patch.partnerId } : {}), ...(patch.percent !== undefined ? { partnerPercent: patch.percent } : {}) })}
            errors={{ partnerId: error("partnerId"), percent: error("partnerPercentOverride") }}
          />
        ) : null}
        <FormMessage message={formMessage(submit.errors, SHOWN_FIELDS)} />
        <div className="flex flex-wrap justify-end gap-2">
          <Button variant="ghost" onClick={onClose}>{t("cancel")}</Button>
          <Button type="submit" variant="primary" icon={SaveIcon} loading={submit.busy} disabled={!partFormValid(form)}>{t("save")}</Button>
        </div>
      </form>
      {submit.dialog}
    </Dialog>
  );
}
