"use client";

import { useLocale, useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { SelectField } from "@/components/ui/SelectField";
import { TextField } from "@/components/ui/TextField";
import { localizedName } from "@/lib/localized-name";
import type { FulfillmentMethod } from "./checkout-request";
import { FormSection, OptionalPill, useFieldError, type SectionProps } from "./form-parts";
import { ChoiceRadioTile } from "@/components/ui/ChoiceTile";
import { MapPinIcon, TruckIcon } from "@/components/ui/icons";

interface FulfillmentSectionProps extends SectionProps {
  pickupEnabled: boolean;
  pickupAddress: string | null;
  governorates: Schemas["GovernorateShippingRate"][];
}

interface OptionProps extends Pick<SectionProps, "form" | "set"> {
  method: FulfillmentMethod;
  title: string;
  body: string;
}

const METHOD_ICONS = { Pickup: MapPinIcon, Delivery: TruckIcon } as const;

function Option({ method, form, set, title, body }: OptionProps) {
  return (
    <ChoiceRadioTile
      name="fulfillment" value={method} icon={METHOD_ICONS[method]} label={title} note={body}
      selected={form.fulfillment === method} onSelect={() => set("fulfillment", method)}
    />
  );
}

/** Pickup (when the owner enabled it) or delivery to any governorate with the address fields. */
export function FulfillmentSection({ form, set, errors, pickupEnabled, pickupAddress, governorates }: FulfillmentSectionProps) {
  const t = useTranslations("checkout.fulfillment");
  const locale = useLocale();
  const error = useFieldError(errors);

  return (
    <FormSection title={t("title")}>
      <div role="radiogroup" aria-label={t("title")} className="grid gap-2.5 sm:grid-cols-2">
        {pickupEnabled ? (
          <Option
            method="Pickup" form={form} set={set}
            title={pickupAddress ? t("pickup", { place: pickupAddress }) : t("pickupNoPlace")} body={t("pickupBody")}
          />
        ) : null}
        <Option method="Delivery" form={form} set={set} title={t("delivery")} body={t("deliveryBody")} />
      </div>
      {form.fulfillment === "Delivery" ? (
        <div className="grid gap-3.5 sm:grid-cols-2">
          <SelectField
            label={t("governorate")} name="governorate" value={form.governorate} onChange={(v) => set("governorate", v)} error={error("governorate")}
            options={[{ value: "", label: t("governoratePlaceholder") }, ...governorates.map((g) => ({ value: g.code, label: localizedName(g, locale) }))]}
          />
          <TextField label={t("area")} name="area" value={form.area} onChange={(v) => set("area", v)} autoComplete="address-level2" required error={error("area")} />
          <TextField label={t("street")} name="street" value={form.street} onChange={(v) => set("street", v)} autoComplete="street-address" required error={error("street")} />
          <TextField label={t("building")} labelEnd={<OptionalPill />} name="building" value={form.building} onChange={(v) => set("building", v)} />
          <div className="sm:col-span-2">
            <TextField label={t("landmark")} labelEnd={<OptionalPill />} name="landmark" value={form.landmark} onChange={(v) => set("landmark", v)} />
          </div>
        </div>
      ) : null}
    </FormSection>
  );
}
