"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { CheckField } from "./CheckField";
import { ListEditor } from "./ListEditor";
import { SectionForm, useSectionSave } from "./SectionForm";
import { saveShipping, type GovernorateRate } from "./settings-api";
import { emptyShippingGroup, shippingForm, shippingValue, toggleGovernorate, type ShippingForm as Form } from "./shipping-rules";
import type { SectionProps } from "./settings-rules";

/** ADM-Settings — shipping: courier, pickup, the default fee/time and a group per governorate set. */
export function ShippingForm({ view, rates, onSaved, onStale }: SectionProps & { rates: readonly GovernorateRate[] }) {
  const t = useTranslations("admin.settings.shipping");
  const { submit, save } = useSectionSave(view.rowVersion, onSaved, onStale);
  const [form, setForm] = useState<Form>(() => shippingForm(view.shipping));

  return (
    <SectionForm
      title={t("title")} hint={t("hint")} busy={submit.busy} error={submit.errors.general} dialog={submit.dialog}
      onSubmit={() => void save((rowVersion) => saveShipping(shippingValue(form), rowVersion))}
    >
      <div className="grid gap-3 sm:grid-cols-2">
        <TextField label={t("courier")} name="courierName" value={form.courierName} onChange={(courierName) => setForm({ ...form, courierName })} />
        <CheckField label={t("pickup")} hint={t("pickupHint")} checked={form.pickupEnabled} onChange={(pickupEnabled) => setForm({ ...form, pickupEnabled })} />
      </div>

      <div className="flex flex-col gap-3 border-t border-line pt-3">
        <h3 className="m-0 text-[15px] font-bold">{t("restTitle")}</h3>
        <div className="grid gap-3 sm:grid-cols-2">
          <TextField label={t("defaultFee")} name="defaultFee" type="number" min="0" max="10000000" step="0.01" ltr value={form.defaultFee} onChange={(defaultFee) => setForm({ ...form, defaultFee })} />
          <TextField label={t("defaultTime")} name="defaultDeliveryTime" value={form.defaultDeliveryTime} onChange={(defaultDeliveryTime) => setForm({ ...form, defaultDeliveryTime })} />
        </div>
        <p className="m-0 text-xs text-ink-muted">{t("restHint")}</p>
      </div>

      <div className="flex flex-col gap-3 border-t border-line pt-3">
        <h3 className="m-0 text-[15px] font-bold">{t("groupsTitle")}</h3>
        <ListEditor
          label={t("groupsLabel")} items={form.groups} onChange={(groups) => setForm({ ...form, groups })}
          create={emptyShippingGroup} addLabel={t("addGroup")}
          renderItem={(group, update) => (
            <div className="flex min-w-0 flex-col gap-3">
              <div className="grid gap-3 sm:grid-cols-3">
                <TextField label={t("groupName")} name="groupName" value={group.name} required onChange={(name) => update({ ...group, name })} />
                <TextField label={t("groupFee")} name="groupFee" type="number" min="0" max="10000000" step="0.01" ltr value={group.fee} onChange={(fee) => update({ ...group, fee })} />
                <TextField label={t("groupTime")} name="groupTime" value={group.deliveryTime} onChange={(deliveryTime) => update({ ...group, deliveryTime })} />
              </div>
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
            </div>
          )}
        />
      </div>
    </SectionForm>
  );
}
