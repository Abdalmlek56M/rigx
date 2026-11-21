"use client";

import { useEffect, useRef, useState, type FormEvent, type ReactNode } from "react";
import { useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { loginHref } from "@/features/auth/auth-rules";
import { useSession } from "@/features/auth/use-session";
import { useCart } from "@/features/cart/cart-context";
import { Link, useRouter } from "@/i18n/navigation";
import { useMounted } from "@/lib/hooks/use-mounted";
import type { CheckoutSettings, PaymentMethods } from "@/lib/storefront";
import {
  FIELD_INPUT, FIELD_ORDER, joinFieldNames, toCheckoutRequest, validateCheckout,
  type CheckoutFormState, type FieldKey
} from "./checkout-request";
import { clearCheckoutDraft, mergeDraft, readCheckoutDraft, saveCheckoutDraft } from "./checkout-draft";
import { isEgyptMobile, normalizePhone } from "../requests/request-fields";
import type { AppliedCoupon } from "./CouponBox";
import { CustomerSection } from "./CustomerSection";
import { ExtrasSection } from "./ExtrasSection";
import { FulfillmentSection } from "./FulfillmentSection";
import type { SetField } from "./form-parts";
import { saveLastOrder } from "./last-order";
import { OrderSummary } from "./OrderSummary";
import { PaymentSection } from "./PaymentSection";
import { SignInSheet } from "./SignInSheet";
import { submitOrder } from "./submit-order";
import { useQuote } from "./use-quote";

interface CheckoutFormProps {
  settings: CheckoutSettings;
  payment: PaymentMethods;
  pickupEnabled: boolean;
  pickupAddress: string | null;
  governorates: Schemas["GovernorateShippingRate"][];
}

type Problem =
  | { kind: "incomplete"; fields: FieldKey[] }
  | { kind: "gone" | "signIn" | "failed" }
  | { kind: "invalid"; messages: string[] };

const AUTOSAVE_MS = 400;

function Notice({ children }: { children: ReactNode }) {
  return <div role="status" className="flex flex-col items-center gap-3 rounded-card border border-line bg-surface p-10 text-center">{children}</div>;
}

const linkClass = "text-orange-text underline hover:text-orange-text-hover";

/** The checkout page body: gates (empty cart, guests not allowed), the form sections and the summary. */
export function CheckoutForm({ settings, payment, pickupEnabled, pickupAddress, governorates }: CheckoutFormProps) {
  const t = useTranslations("checkout");
  const fieldNames = useTranslations("checkout.fieldNames");
  const router = useRouter();
  const session = useSession();
  const mounted = useMounted();
  const { lines, subtotal, clear, refresh, removedNotice } = useCart();
  const [form, setForm] = useState<CheckoutFormState>(() => mergeDraft(readCheckoutDraft()));
  const [prefilled, setPrefilled] = useState(false);
  const [errors, setErrors] = useState<FieldKey[]>([]);
  const [problem, setProblem] = useState<Problem | null>(null);
  const [busy, setBusy] = useState(false);
  const [placed, setPlaced] = useState(false);
  const [signingIn, setSigningIn] = useState(false);
  const [coupon, setCoupon] = useState<(AppliedCoupon & { forTotal: number }) | null>(null);
  const [couponProblem, setCouponProblem] = useState<string | null>(null);
  const formRef = useRef<HTMLFormElement>(null);
  const quote = useQuote(form.fulfillment === "Pickup", form.governorate, subtotal);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  // Keep what the customer typed across a refresh, a crash or a sign-in round trip; the coupon result is never stored.
  useEffect(() => {
    if (!mounted || placed) return;
    const timer = window.setTimeout(() => saveCheckoutDraft(form), AUTOSAVE_MS);
    return () => window.clearTimeout(timer);
  }, [form, mounted, placed]);

  if (session.status === "signedIn" && !prefilled) {
    setPrefilled(true);
    const { fullName, phoneNumber, email } = session.user;
    setForm((current) => {
      const nextEmail = current.email || email;
      return {
        ...current,
        name: current.name || fullName,
        phone: current.phone || phoneNumber || "",
        email: nextEmail,
        // A profile email is already verified; a typed one still needs its confirmation.
        emailConfirm: current.emailConfirm || (current.email ? "" : nextEmail)
      };
    });
  }

  const set: SetField = (key, value) => setForm((current) => ({ ...current, [key]: value }));
  const removeCoupon = () => {
    setCoupon(null);
    setForm((current) => ({ ...current, couponCode: "" }));
  };
  // A discount worked out for an older cart total no longer holds: the customer applies the code again.
  if (coupon && coupon.forTotal !== subtotal) removeCoupon();
  const minimum = settings.minimumOrderAmount > 0 && subtotal < settings.minimumOrderAmount ? settings.minimumOrderAmount : null;

  function focusFirst(fields: readonly FieldKey[]) {
    const first = fields[0];
    const field = first ? formRef.current?.querySelector<HTMLElement>(`[name="${FIELD_INPUT[first]}"]`) : null;
    if (!field) return;
    field.focus({ preventScroll: true });
    const reduce = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    field.scrollIntoView({ block: "center", behavior: reduce ? "auto" : "smooth" });
  }

  async function submit(event: FormEvent) {
    event.preventDefault();
    const found = validateCheckout(form);
    setErrors(found);
    if (found.length > 0) {
      setProblem({ kind: "incomplete", fields: found });
      focusFirst(found);
      return;
    }
    setBusy(true);
    setProblem(null);
    const outcome = await submitOrder(toCheckoutRequest(form, lines));
    if (outcome.status === "placed") {
      saveLastOrder(outcome.order);
      clearCheckoutDraft();
      setPlaced(true);
      clear();
      router.push("/checkout/success");
      return;
    }
    if (outcome.status === "gone") await refresh();
    if (outcome.status === "coupon") {
      removeCoupon();
      setCouponProblem(outcome.problem);
      setBusy(false);
      return;
    }
    setProblem(outcome.status === "invalid" ? { kind: "invalid", messages: outcome.messages } : { kind: outcome.status });
    setBusy(false);
  }

  if (!mounted) return null;
  if (placed) return <Notice><p className="m-0">{t("redirecting")}</p></Notice>;
  if (lines.length === 0) {
    return (
      <Notice>
        <p className="m-0 text-lg font-bold">{t("empty")}</p>
        <Link href="/products" className={linkClass}>{t("browse")}</Link>
      </Notice>
    );
  }
  if (!settings.guestCheckoutEnabled && session.status !== "signedIn") {
    return session.status === "loading" ? null : (
      <Notice>
        <p className="m-0 text-lg font-bold">{t("loginRequired")}</p>
        <Link href={loginHref("/checkout")} className="press inline-flex h-12 items-center justify-center rounded-full bg-orange px-7 font-bold text-on-orange transition-[filter] hover:brightness-105">
          {t("loginLink")}
        </Link>
      </Notice>
    );
  }

  const summaryProblem = problem && problem.kind !== "incomplete" ? problem : null;
  const problemText = summaryProblem && summaryProblem.kind !== "invalid" ? t(`problems.${summaryProblem.kind}`) : null;
  const names = Object.fromEntries(FIELD_ORDER.map((field) => [field, fieldNames(field)])) as Record<FieldKey, string>;

  return (
    <>
      <form ref={formRef} onSubmit={submit} noValidate className="grid grid-cols-1 items-start gap-5 md:grid-cols-[1fr_380px]">
        <div className="flex min-w-0 flex-col gap-4">
          {removedNotice ? <p role="status" className="m-0 rounded-card bg-paper p-3 text-sm text-ink-muted">{t("removedNotice")}</p> : null}
          {problem?.kind === "incomplete" ? (
            <div role="alert" className="rounded-card border border-error bg-error/5 p-4 text-sm font-bold text-error">
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
          <ExtrasSection form={form} set={set} errors={errors} leadSources={settings.leadSources} />
          <PaymentSection form={form} set={set} errors={errors} payment={payment} settings={settings} pickupEnabled={pickupEnabled} />
        </div>
        <OrderSummary
          form={form} set={set} errors={errors} lines={lines} subtotal={subtotal} quote={quote} minimum={minimum} busy={busy}
          coupon={{
            applied: coupon, problem: couponProblem, phone: isEgyptMobile(form.phone) ? normalizePhone(form.phone) : null,
            onApplied: (applied) => {
              setCoupon({ ...applied, forTotal: subtotal });
              setForm((current) => ({ ...current, couponCode: applied.code }));
            },
            onRemoved: removeCoupon,
            onProblem: setCouponProblem
          }}
          problem={
            summaryProblem ? (
              <div role="alert" className="flex flex-col gap-1 text-sm text-error">
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
