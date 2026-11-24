"use client";

import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { CheckField } from "./CheckField";
import { FieldGrid, FieldNote, SubSection } from "./FieldGrid";
import { ListEditor } from "./ListEditor";
import { SectionForm, useSectionSave } from "./SectionForm";
import { saveShipping, type GovernorateRate } from "./settings-api";
import { emptyShippingGroup, shippingForm, shippingValue, toggleGovernorate } from "./shipping-rules";
import type { SectionProps } from "./settings-rules";
import { useSectionDraft } from "./use-section-draft";

const ROW = "md:grid-cols-[minmax(0,1.4fr)_minmax(0,1fr)_minmax(0,1.2fr)]";

/** ADM-Settings — shipping: courier, pickup, the default fee/time, free shipping and a compact row per governorate group. */
export function ShippingForm({ view, rates, onSaved, onStale }: SectionProps & { rates: readonly GovernorateRate[] }) {
  const t = useTranslations("admin.settings.shipping");
  const { submit, save } = useSectionSave(view.rowVersion, onSaved, onStale);
  const { form, setForm, dirty, reset } = useSectionDraft(shippingForm(view.shipping));

  return (
    <SectionForm
      title={t("title")} hint={t("hint")} dirty={dirty} onReset={reset} busy={submit.busy} error={submit.errors.general} dialog={submit.dialog}
      onSubmit={() => void save((rowVersion) => saveShipping(shippingValue(form), rowVersion))}
    >
      <FieldGrid>
        <TextField label={t("courier")} name="courierName" value={form.courierName} onChange={(courierName) => setForm({ ...form, courierName })} />
      </FieldGrid>
      <CheckField label={t("pickup")} hint={t("pickupHint")} checked={form.pickupEnabled} onChange={(pickupEnabled) => setForm({ ...form, pickupEnabled })} />

      <SubSection title={t("restTitle")}>
        <FieldGrid>
          <TextField label={t("defaultFee")} name="defaultFee" type="number" min="0" max="10000000" step="0.01" ltr value={form.defaultFee} onChange={(defaultFee) => setForm({ ...form, defaultFee })} />
          <TextField label={t("defaultTime")} name="defaultDeliveryTime" value={form.defaultDeliveryTime} onChange={(defaultDeliveryTime) => setForm({ ...form, defaultDeliveryTime })} />
        </FieldGrid>
        <FieldNote>{t("restHint")}</FieldNote>
      </SubSection>

      <SubSection title={t("freeTitle")}>
        <CheckField label={t("freeEnabled")} hint={t("freeEnabledHint")} checked={form.freeShippingEnabled} onChange={(freeShippingEnabled) => setForm({ ...form, freeShippingEnabled })} />
        {form.freeShippingEnabled ? (
          <FieldGrid>
            <TextField
              label={t("freeMinimum")} name="freeShippingMinimum" type="number" min="1" max="10000000" step="0.01" ltr required
              value={form.freeShippingMinimum} onChange={(freeShippingMinimum) => setForm({ ...form, freeShippingMinimum })}
            />
          </FieldGrid>
        ) : null}
        <FieldNote>{t("freeHint")}</FieldNote>
      </SubSection>

      <SubSection title={t("groupsTitle")}>
        <ListEditor
          label={t("groupsLabel")} items={form.groups} onChange={(groups) => setForm({ ...form, groups })}
          create={emptyShippingGroup} addLabel={t("addGroup")}
          columns={{ labels: [t("groupName"), t("groupFee"), t("groupTime")], className: ROW }}
          renderItem={(group, update) => (
            <div className={`grid gap-2 ${ROW}`}>
              <TextField label={t("groupName")} name="groupName" value={group.name} required labelClassName="md:sr-only" onChange={(name) => update({ ...group, name })} />
              <TextField label={t("groupFee")} name="groupFee" type="number" min="0" max="10000000" step="0.01" ltr value={group.fee} labelClassName="md:sr-only" onChange={(fee) => update({ ...group, fee })} />
              <TextField label={t("groupTime")} name="groupTime" value={group.deliveryTime} labelClassName="md:sr-only" onChange={(deliveryTime) => update({ ...group, deliveryTime })} />
            </div>
          )}
          renderDetails={(group, update) => (
            <div className="flex flex-col gap-1.5">
              <span className="text-[13px] font-bold">{t("governorates")}</span>
              <div role="group" aria-label={t("governorates")} className="flex flex-wrap gap-1.5">
                {rates.map((rate) => {
                  const selected = group.governorates.includes(rate.code);
                  const usedElsewhere = form.groups.some((other) => other !== group && other.governorates.includes(rate.code));
                  return (
                    <label
                      key={rate.code}
                      className={`inline-flex items-center gap-1.5 rounded-control border px-2 py-1 text-xs ${
                        selected ? "border-carbon bg-carbon text-on-carbon" : usedElsewhere ? "border-line bg-paper text-ink-muted" : "border-input-line bg-surface"
                      }`}
                    >
                      <input
                        type="checkbox" checked={selected} disabled={usedElsewhere && !selected}
                        onChange={() => update(toggleGovernorate(group, rate.code))}
                        className="h-3.5 w-3.5 accent-orange"
                      />
                      {rate.name}
                    </label>
                  );
                })}
              </div>
              <span className="text-xs text-ink-muted">{t("governoratesHint")}</span>
            </div>
          )}
        />
      </SubSection>
    </SectionForm>
  );
}
