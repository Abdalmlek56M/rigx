"use client";

import { useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { TextField } from "@/components/ui/TextField";
import { ChipRadio, FormSection } from "@/features/checkout/form-parts";
import { usePosError, type SetForm } from "./pos-parts";
import { POS_USAGES, type PosField, type PosForm } from "./pos-rules";

export interface Governorate {
  code: string;
  name: string;
}

interface PosDeliveryProps {
  form: PosForm;
  set: SetForm;
  errors: readonly PosField[];
  governorates: readonly Governorate[];
  leadSources: readonly string[];
}

const METHODS = ["Pickup", "Delivery"] as const;

/** How the customer gets it (at the store, always offered at the counter; or shipping with the address), and the optional "how did you hear / what for / notes". */
export function PosDelivery({ form, set, errors, governorates, leadSources }: PosDeliveryProps) {
  const t = useTranslations("admin.pos.fulfillment");
  const kinds = useTranslations("admin.orders.fulfillmentKinds");
  const address = useTranslations("checkout.fulfillment");
  const extras = useTranslations("checkout.extras");
  const error = usePosError(errors);

  return (
    <>
      <FormSection title={t("title")}>
        <div role="radiogroup" aria-label={t("title")} className="flex flex-wrap gap-2">
          {METHODS.map((method) => (
            <ChipRadio key={method} name="pos-fulfillment" label={kinds(method)} selected={form.fulfillment === method} onSelect={() => set("fulfillment", method)} />
          ))}
        </div>
        {form.fulfillment === "Delivery" ? (
          <>
            {governorates.length === 0 ? <p role="status" className="m-0 text-sm font-bold text-error-text">{t("noRates")}</p> : null}
            <div className="grid gap-3.5 sm:grid-cols-2">
              <SelectField
                label={address("governorate")} name="governorate" value={form.governorate} onChange={(value) => set("governorate", value)} error={error("governorate")}
                options={[{ value: "", label: address("governoratePlaceholder") }, ...governorates.map((governorate) => ({ value: governorate.code, label: governorate.name }))]}
              />
              <TextField label={address("area")} name="area" value={form.area} onChange={(value) => set("area", value)} required error={error("area")} />
              <TextField label={address("street")} name="street" value={form.street} onChange={(value) => set("street", value)} required error={error("street")} />
              <TextField label={address("building")} name="building" value={form.building} onChange={(value) => set("building", value)} />
              <TextField label={address("landmark")} name="landmark" value={form.landmark} onChange={(value) => set("landmark", value)} />
              <div className="flex flex-col gap-1">
                <TextField label={t("deliveryFee")} name="deliveryFee" ltr value={form.deliveryFee} onChange={(value) => set("deliveryFee", value)} error={error("deliveryFee")} />
                <span className="text-xs text-ink-muted">{t("deliveryFeeHint")}</span>
              </div>
            </div>
          </>
        ) : null}
      </FormSection>
      {leadSources.length > 0 ? (
        <FormSection title={extras("leadSource")}>
          <div role="radiogroup" aria-label={extras("leadSource")} className="flex flex-wrap gap-2">
            {leadSources.map((source) => (
              <ChipRadio key={source} name="pos-leadSource" label={source} selected={form.leadSource === source} onSelect={() => set("leadSource", source)} onClear={() => set("leadSource", null)} />
            ))}
          </div>
        </FormSection>
      ) : null}
      <FormSection title={extras("usage")}>
        <div role="radiogroup" aria-label={extras("usage")} className="flex flex-wrap gap-2">
          {POS_USAGES.map((usage) => (
            <ChipRadio key={usage} name="pos-usage" label={extras(`usages.${usage}`)} selected={form.usage === usage} onSelect={() => set("usage", usage)} onClear={() => set("usage", null)} />
          ))}
        </div>
        {form.usage === "Other" ? <TextField label={extras("usageNote")} name="usageNote" value={form.usageNote} onChange={(value) => set("usageNote", value)} required error={error("usageNote")} /> : null}
        <TextAreaField label={extras("notes")} name="notes" value={form.notes} onChange={(value) => set("notes", value)} />
      </FormSection>
    </>
  );
}
