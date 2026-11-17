import type { Schemas } from "@rigx/api-client";
import { isEgyptMobile, normalizePhone } from "../requests/request-fields.ts";

export interface AddressForm {
  label: string;
  recipientName: string;
  phone: string;
  governorate: string;
  city: string;
  street: string;
  building: string;
  floor: string;
  apartment: string;
  landmark: string;
  isDefault: boolean;
}

export type AddressField = "label" | "recipientName" | "phone" | "governorate" | "city" | "street";

type CreateRequest = Schemas["CustomerAddressCreateRequest"];
type UpdateRequest = Schemas["CustomerAddressUpdateRequest"];

export function emptyAddressForm(person: { name: string; phone: string }, isDefault: boolean): AddressForm {
  return {
    label: "", recipientName: person.name, phone: person.phone, governorate: "", city: "", street: "",
    building: "", floor: "", apartment: "", landmark: "", isDefault
  };
}

export function formFromAddress(address: Schemas["CustomerAddressView"]): AddressForm {
  return {
    label: address.label, recipientName: address.recipientName, phone: address.phoneNumber, governorate: address.governorate,
    city: address.city, street: address.street, building: address.building ?? "", floor: address.floor ?? "",
    apartment: address.apartment ?? "", landmark: address.landmark ?? "", isDefault: address.isDefault
  };
}

/** The required fields that are empty or invalid (the backend requires all of these; the phone must be an Egyptian mobile). */
export function addressErrors(form: AddressForm): AddressField[] {
  const errors: AddressField[] = [];
  if (!form.label.trim()) errors.push("label");
  if (!form.recipientName.trim()) errors.push("recipientName");
  if (!isEgyptMobile(form.phone)) errors.push("phone");
  if (!form.governorate.trim()) errors.push("governorate");
  if (!form.city.trim()) errors.push("city");
  if (!form.street.trim()) errors.push("street");
  return errors;
}

const optional = (value: string): string | null => value.trim() || null;

/** The request body; the governorate is sent as the name the person picked (the API stores free text). */
export function toAddressRequest(form: AddressForm): CreateRequest {
  return {
    label: form.label.trim(),
    recipientName: form.recipientName.trim(),
    phoneNumber: normalizePhone(form.phone),
    countryCode: "EG",
    countryCallingCode: "+20",
    governorate: form.governorate.trim(),
    city: form.city.trim(),
    district: null,
    street: form.street.trim(),
    building: optional(form.building),
    floor: optional(form.floor),
    apartment: optional(form.apartment),
    landmark: optional(form.landmark),
    postalCode: null,
    instructions: null,
    isDefault: form.isDefault
  };
}

export function toUpdateRequest(form: AddressForm, rowVersion: number): UpdateRequest {
  return { ...toAddressRequest(form), rowVersion };
}

/** Dropdown options: the shipping governorates, plus the saved one when it is no longer in the list. */
export function governorateOptions(names: readonly string[], current: string): { value: string; label: string }[] {
  const all = current && !names.includes(current) ? [current, ...names] : [...names];
  return all.map((name) => ({ value: name, label: name }));
}

type AddressParts = Pick<Schemas["CustomerAddressView"], "city" | "street" | "building" | "floor" | "apartment" | "landmark">;

/** One line for the address card: area, street, building, floor, apartment, landmark. */
export function addressLine(address: AddressParts, labels: { floor: string; apartment: string }): string {
  return [
    address.city, address.street, address.building,
    address.floor ? `${labels.floor} ${address.floor}` : null,
    address.apartment ? `${labels.apartment} ${address.apartment}` : null,
    address.landmark
  ].filter(Boolean).join("، ");
}
