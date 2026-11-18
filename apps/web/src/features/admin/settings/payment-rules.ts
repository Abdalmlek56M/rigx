/** Payment: the deposit rule, shipping upfront and the enabled methods. */
import type { Schemas } from "@rigx/api-client";

export type PaymentSettings = Schemas["PaymentSettings"];

export interface PaymentForm {
  depositThreshold: string;
  depositPercent: string;
  shippingPaidUpfront: boolean;
  cashOnDeliveryEnabled: boolean;
  instaPayEnabled: boolean;
  instaPayAddress: string;
  walletEnabled: boolean;
  walletNumber: string;
}

export const paymentForm = (value: PaymentSettings): PaymentForm => ({
  depositThreshold: String(value.depositThreshold ?? 0),
  depositPercent: String(value.depositPercent ?? 0),
  shippingPaidUpfront: value.shippingPaidUpfront ?? true,
  cashOnDeliveryEnabled: value.cashOnDeliveryEnabled ?? true,
  instaPayEnabled: value.instaPayEnabled ?? false,
  instaPayAddress: value.instaPayAddress ?? "",
  walletEnabled: value.walletEnabled ?? false,
  walletNumber: value.walletNumber ?? ""
});

export const paymentValue = (form: PaymentForm): PaymentSettings => ({
  depositThreshold: form.depositThreshold.trim() === "" ? 0 : Number(form.depositThreshold),
  depositPercent: form.depositPercent.trim() === "" ? 0 : Number(form.depositPercent),
  shippingPaidUpfront: form.shippingPaidUpfront,
  cashOnDeliveryEnabled: form.cashOnDeliveryEnabled,
  instaPayEnabled: form.instaPayEnabled,
  instaPayAddress: form.instaPayAddress.trim() || null,
  walletEnabled: form.walletEnabled,
  walletNumber: form.walletNumber.trim() || null
});
