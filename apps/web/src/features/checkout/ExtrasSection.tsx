"use client";

import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { CHECKOUT_USAGES } from "./checkout-request";
import { ChipRadio, FormSection, OptionalPill, useFieldError, type SectionProps } from "./form-parts";

/** Notes, "what will you use it for" and "where did you hear about us": all optional. */
export function ExtrasSection({ form, set, errors, leadSources }: SectionProps & { leadSources: string[] }) {
  const t = useTranslations("checkout.extras");
  const error = useFieldError(errors);

  return (
    <>
      <FormSection title={t("notes")} badge={<OptionalPill />}>
        <textarea
          name="notes" aria-label={t("notes")} rows={3} value={form.notes} onChange={(event) => set("notes", event.target.value)}
          className="w-full rounded-control border border-input-line bg-surface p-3 text-sm text-ink"
        />
      </FormSection>
      <FormSection title={t("usage")} badge={<OptionalPill />}>
        <div role="radiogroup" aria-label={t("usage")} className="flex flex-wrap gap-2">
          {CHECKOUT_USAGES.map((usage) => (
            <ChipRadio
              key={usage} name="usage" label={t(`usages.${usage}`)} selected={form.usage === usage}
              onSelect={() => set("usage", usage)} onClear={() => set("usage", null)}
            />
          ))}
        </div>
        {form.usage === "Other" ? (
          <TextField label={t("usageNote")} name="usageNote" value={form.usageNote} onChange={(v) => set("usageNote", v)} required error={error("usageNote")} />
        ) : null}
      </FormSection>
      {leadSources.length > 0 ? (
        <FormSection title={t("leadSource")} badge={<OptionalPill />} hint={t("leadSourceHint")}>
          <div role="radiogroup" aria-label={t("leadSource")} className="flex flex-wrap gap-2">
            {leadSources.map((source) => (
              <ChipRadio
                key={source} name="leadSource" label={source} selected={form.leadSource === source}
                onSelect={() => set("leadSource", source)} onClear={() => set("leadSource", null)}
              />
            ))}
          </div>
        </FormSection>
      ) : null}
    </>
  );
}
