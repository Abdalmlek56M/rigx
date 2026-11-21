import type { Schemas } from "@rigx/api-client";
import { localizedName } from "../../lib/localized-name.ts";
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

/** The request body; the governorate is the Arabic name picked from the list (the API accepts only the 27 governorates). */
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

export interface GovernorateOption {
  /** The Arabic name the API stores on the address. */
  name: string;
  nameEn?: string | null;
}

/** Dropdown options: the 27 governorates (from the shipping rates), plus a saved legacy value so the form shows it until it is re-picked. */
export function governorateOptions(governorates: readonly GovernorateOption[], current: string, locale: string): { value: string; label: string }[] {
  const all = current && !governorates.some((governorate) => governorate.name === current)
    ? [{ name: current, nameEn: null }, ...governorates]
    : [...governorates];
  return all.map((governorate) => ({ value: governorate.name, label: localizedName(governorate, locale) }));
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
