"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { loginHref } from "@/features/auth/auth-rules";
import { useSession } from "@/features/auth/use-session";
import { Link } from "@/i18n/navigation";
import type { CheckoutSettings, PaymentMethods } from "@/lib/storefront";
import { ButtonLink } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { EmptyState } from "@/components/ui/EmptyState";
import { CartIcon, UserIcon } from "@/components/ui/icons";
import { FIELD_ORDER, joinFieldNames, type FieldKey } from "./checkout-request";
import { CustomerSection } from "./CustomerSection";
import { ExtrasSection } from "./ExtrasSection";
import { FulfillmentSection } from "./FulfillmentSection";
import { OrderSummary } from "./OrderSummary";
import { PaymentSection } from "./PaymentSection";
import { SignInSheet } from "./SignInSheet";
import { useCheckoutForm } from "./use-checkout-form";

interface CheckoutFormProps {
  settings: CheckoutSettings;
  payment: PaymentMethods;
  pickupEnabled: boolean;
  pickupAddress: string | null;
  governorates: Schemas["GovernorateShippingRate"][];
}

const linkClass = "text-orange-text underline hover:text-orange-text-hover";

/** The checkout page body: gates (empty cart, guests not allowed), the form sections and the summary. */
export function CheckoutForm({ settings, payment, pickupEnabled, pickupAddress, governorates }: CheckoutFormProps) {
  const t = useTranslations("checkout");
  const fieldNames = useTranslations("checkout.fieldNames");
  const session = useSession();
  const [signingIn, setSigningIn] = useState(false);
  const {
    mounted, placed, lines, subtotal, removedNotice, form, set, errors, problem, busy,
    coupon, couponProblem, couponPhone, minimum, quote, formRef, submit, applyCoupon, setCouponProblem, removeCoupon
  } = useCheckoutForm(settings);

  if (!mounted) return null;
  if (placed) return <Card padding="lg"><p className="m-0 text-center text-sm">{t("redirecting")}</p></Card>;
  if (lines.length === 0) {
    return (
      <EmptyState
        icon={CartIcon}
        title={t("empty")}
        action={<ButtonLink href="/products" variant="secondary">{t("browse")}</ButtonLink>}
      />
    );
  }
  if (!settings.guestCheckoutEnabled && session.status !== "signedIn") {
    return session.status === "loading" ? null : (
      <EmptyState
        icon={UserIcon}
        title={t("loginRequired")}
        action={<ButtonLink href={loginHref("/checkout")} variant="accent">{t("loginLink")}</ButtonLink>}
      />
    );
  }

  const summaryProblem = problem && problem.kind !== "incomplete" ? problem : null;
  const problemText = summaryProblem && summaryProblem.kind !== "invalid" ? t(`problems.${summaryProblem.kind}`) : null;
  const names = Object.fromEntries(FIELD_ORDER.map((field) => [field, fieldNames(field)])) as Record<FieldKey, string>;

  return (
    <>
      <form ref={formRef} onSubmit={submit} noValidate className="grid grid-cols-1 items-start gap-5 lg:grid-cols-[1fr_380px]">
        <div className="flex min-w-0 flex-col gap-4">
          {removedNotice ? <p role="status" className="m-0 rounded-card bg-paper p-3 text-sm text-ink-muted">{t("removedNotice")}</p> : null}
          {problem?.kind === "incomplete" ? (
            <div role="alert" className="rounded-card border border-error bg-error/5 p-4 text-sm font-bold text-error-text">
              <p className="m-0">{t("missingFields", { fields: joinFieldNames(problem.fields, names) })}</p>
            </div>
          ) : null}
          <CustomerSection
            form={form} set={set} errors={errors} showLogin={settings.guestCheckoutEnabled && session.status === "signedOut"}
            onLogin={() => setSigningIn(true)}
          />
          <FulfillmentSection
            form={form} set={set} errors={errors} pickupEnabled={pickupEnabled} pickupAddress={pickupAddress} governorates={governorates}
          />
          <PaymentSection form={form} set={set} errors={errors} payment={payment} settings={settings} pickupEnabled={pickupEnabled} />
          <ExtrasSection form={form} set={set} errors={errors} leadSources={settings.leadSources} />
        </div>
        <OrderSummary
          form={form} set={set} errors={errors} lines={lines} subtotal={subtotal} quote={quote} minimum={minimum} busy={busy}
          freeShippingMinimum={settings.freeShippingMinimum}
          coupon={{
            applied: coupon, problem: couponProblem, phone: couponPhone,
            onApplied: applyCoupon,
            onRemoved: removeCoupon,
            onProblem: setCouponProblem
          }}
          problem={
            summaryProblem ? (
              <div role="alert" className="flex flex-col gap-1 text-sm text-error-text">
                {summaryProblem.kind === "invalid" ? summaryProblem.messages.map((message) => <p key={message} className="m-0">{message}</p>) : <p className="m-0">{problemText}</p>}
                {summaryProblem.kind === "signIn" ? <Link href={loginHref("/checkout")} className={linkClass}>{t("loginLink")}</Link> : null}
              </div>
            ) : null
          }
        />
      </form>
      <SignInSheet open={signingIn} onClose={() => setSigningIn(false)} />
    </>
  );
}
