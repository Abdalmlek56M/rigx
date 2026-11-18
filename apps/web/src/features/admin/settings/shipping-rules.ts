/** Shipping: governorate groups with a fee and a delivery time, and the default for every other governorate. */
import type { Schemas } from "@rigx/api-client";

export type ShippingSettings = Schemas["ShippingSettings"];

export interface ShippingGroupForm {
  name: string;
  fee: string;
  deliveryTime: string;
  governorates: string[];
}

export interface ShippingForm {
  courierName: string;
  pickupEnabled: boolean;
  defaultFee: string;
  defaultDeliveryTime: string;
  groups: ShippingGroupForm[];
}

export const emptyShippingGroup = (): ShippingGroupForm => ({ name: "", fee: "0", deliveryTime: "", governorates: [] });

export const shippingForm = (value: ShippingSettings): ShippingForm => ({
  courierName: value.courierName ?? "",
  pickupEnabled: value.pickupEnabled ?? true,
  defaultFee: String(value.defaultFee ?? 0),
  defaultDeliveryTime: value.defaultDeliveryTime ?? "",
  groups: (value.groups ?? []).map((group) => ({
    name: group.name ?? "",
    fee: String(group.fee ?? 0),
    deliveryTime: group.deliveryTime ?? "",
    governorates: [...(group.governorates ?? [])]
  }))
});

export const shippingValue = (form: ShippingForm): ShippingSettings => ({
  courierName: form.courierName.trim() || null,
  pickupEnabled: form.pickupEnabled,
  defaultFee: form.defaultFee.trim() === "" ? 0 : Number(form.defaultFee),
  defaultDeliveryTime: form.defaultDeliveryTime.trim() || null,
  groups: form.groups.map((group) => ({
    name: group.name.trim(),
    fee: group.fee.trim() === "" ? 0 : Number(group.fee),
    deliveryTime: group.deliveryTime.trim() || null,
    governorates: [...group.governorates]
  }))
});

/** Adds or removes one governorate code, keeping the chosen order. */
export function toggleGovernorate(group: ShippingGroupForm, code: string): ShippingGroupForm {
  const has = group.governorates.includes(code);
  return { ...group, governorates: has ? group.governorates.filter((item) => item !== code) : [...group.governorates, code] };
}
