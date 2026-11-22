"use client";

import type { ComponentType, ReactNode } from "react";
import { useLocale, useTranslations } from "next-intl";
import type { CheckoutSettings, PaymentMethods } from "@/lib/storefront";
import { formatEgp } from "@/lib/format";
import type { PaymentMethod } from "./checkout-request";
import { FieldAlert, FormSection, useFieldError, type SectionProps } from "./form-parts";
import { ChoiceRadioTile } from "@/components/ui/ChoiceTile";
import { BankIcon, CashIcon, WalletIcon } from "@/components/ui/icons";

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

const METHODS: readonly { method: PaymentMethod; icon: ComponentType<{ size?: number }>; enabled: (payment: PaymentMethods) => boolean }[] = [
  { method: "CashOnDelivery", icon: CashIcon, enabled: (p) => p.cash },
  { method: "InstaPay", icon: BankIcon, enabled: (p) => p.instaPay },
  { method: "Wallet", icon: WalletIcon, enabled: (p) => p.wallet }
];

export function PaymentSection({ form, set, errors, payment, settings, pickupEnabled }: PaymentSectionProps) {
  const t = useTranslations("checkout.payment");
  const error = useFieldError(errors);

  return (
    <FormSection title={t("title")}>
      <div role="radiogroup" aria-label={t("title")} className="grid grid-cols-[repeat(auto-fit,minmax(120px,1fr))] gap-2.5">
        {METHODS.filter(({ enabled }) => enabled(payment)).map(({ method, icon }) => (
          <ChoiceRadioTile
            key={method} name="paymentMethod" value={method} icon={icon} label={t(`methods.${method}`)}
            selected={form.paymentMethod === method} onSelect={() => set("paymentMethod", method)}
          />
        ))}
      </div>
      <FieldAlert>{error("paymentMethod")}</FieldAlert>
      <UpfrontNote settings={settings} payment={payment} pickup={pickupEnabled && form.fulfillment === "Pickup"} />
    </FormSection>
  );
}
