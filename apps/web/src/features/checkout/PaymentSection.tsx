"use client";

import type { ReactNode } from "react";
import { useLocale, useTranslations } from "next-intl";
import type { CheckoutSettings, PaymentMethods } from "@/lib/storefront";
import { formatEgp } from "@/lib/format";
import type { PaymentMethod } from "./checkout-request";
import { FieldAlert, FormSection, useFieldError, type SectionProps } from "./form-parts";

interface PaymentSectionProps extends SectionProps {
  payment: PaymentMethods;
  settings: CheckoutSettings;
  pickupEnabled: boolean;
}

/** What is paid before the order is prepared, for the fulfilment the customer chose, worded from the owner's settings. */
function UpfrontNote({ settings, payment, pickup }: Pick<PaymentSectionProps, "settings" | "payment"> & { pickup: boolean }) {
  const t = useTranslations("checkout.payment.note");
  const locale = useLocale();
  if (pickup) return <NoteBox>{t("pickup")}</NoteBox>;
  const deposit = settings.depositPercent > 0;
  if (!settings.shippingPaidUpfront && !deposit) return null;
  const transfer = payment.instaPay || payment.wallet;
  const percent = String(settings.depositPercent);
  const depositText = settings.depositThreshold > 0
    ? t("deposit", { threshold: formatEgp(settings.depositThreshold, locale), percent })
    : t("depositAll", { percent });

  return (
    <NoteBox>
      {settings.shippingPaidUpfront ? <b className="block text-sm text-ink">{t("shipping")}</b> : null}
      {transfer ? `${t("transfer")} ` : ""}
      {deposit ? `${depositText} ` : ""}
      {payment.cash ? t("rest") : ""}
    </NoteBox>
  );
}

function NoteBox({ children }: { children: ReactNode }) {
  return <div className="flex flex-col gap-1.5 rounded-card bg-paper p-3.5 text-[13px] leading-6 text-ink-muted">{children}</div>;
}

const METHODS: readonly { method: PaymentMethod; enabled: (payment: PaymentMethods) => boolean }[] = [
  { method: "CashOnDelivery", enabled: (p) => p.cash },
  { method: "InstaPay", enabled: (p) => p.instaPay },
  { method: "Wallet", enabled: (p) => p.wallet }
];

export function PaymentSection({ form, set, errors, payment, settings, pickupEnabled }: PaymentSectionProps) {
  const t = useTranslations("checkout.payment");
  const error = useFieldError(errors);

  return (
    <FormSection title={t("title")}>
      <div role="radiogroup" aria-label={t("title")} className="flex flex-col gap-2.5 sm:flex-row">
        {METHODS.filter(({ enabled }) => enabled(payment)).map(({ method }) => (
          <label
            key={method}
            className="flex min-h-12 flex-1 cursor-pointer items-center gap-2.5 rounded-card border border-input-line px-3.5 transition-colors duration-150 has-[:checked]:border-carbon has-[:checked]:bg-paper motion-reduce:transition-none"
          >
            <input
              type="radio" name="paymentMethod" value={method} checked={form.paymentMethod === method}
              onChange={() => set("paymentMethod", method)} className="h-[18px] w-[18px] accent-carbon"
            />
            <span className="text-sm font-bold">{t(`methods.${method}`)}</span>
          </label>
        ))}
      </div>
      <FieldAlert>{error("paymentMethod")}</FieldAlert>
      <UpfrontNote settings={settings} payment={payment} pickup={pickupEnabled && form.fulfillment === "Pickup"} />
    </FormSection>
  );
}
