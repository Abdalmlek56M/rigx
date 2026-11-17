"use client";

import { useEffect, useState, type FormEvent, type ReactNode } from "react";
import { useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { loginHref } from "@/features/auth/auth-rules";
import { useSession } from "@/features/auth/use-session";
import { useCart } from "@/features/cart/cart-context";
import { Link, useRouter } from "@/i18n/navigation";
import { useMounted } from "@/lib/hooks/use-mounted";
import type { CheckoutSettings, PaymentMethods } from "@/lib/storefront";
import { EMPTY_FORM, toCheckoutRequest, validateCheckout, type CheckoutFormState, type FieldKey } from "./checkout-request";
import { CustomerSection } from "./CustomerSection";
import { ExtrasSection } from "./ExtrasSection";
import { FulfillmentSection } from "./FulfillmentSection";
import type { SetField } from "./form-parts";
import { saveLastOrder } from "./last-order";
import { OrderSummary } from "./OrderSummary";
import { PaymentSection } from "./PaymentSection";
import { submitOrder } from "./submit-order";
import { useQuote } from "./use-quote";

interface CheckoutFormProps {
  settings: CheckoutSettings;
  payment: PaymentMethods;
  pickupEnabled: boolean;
  pickupAddress: string | null;
  governorates: Schemas["GovernorateShippingRate"][];
}

type Problem = { kind: "incomplete" | "gone" | "signIn" | "failed" } | { kind: "invalid"; messages: string[] };

function Notice({ children }: { children: ReactNode }) {
  return <div role="status" className="flex flex-col items-center gap-3 rounded-[10px] border border-line bg-surface p-10 text-center">{children}</div>;
}

const linkClass = "text-orange-text underline hover:text-orange-text-hover";

/** The checkout page body: gates (empty cart, guests not allowed), the form sections and the summary. */
export function CheckoutForm({ settings, payment, pickupEnabled, pickupAddress, governorates }: CheckoutFormProps) {
  const t = useTranslations("checkout");
  const router = useRouter();
  const session = useSession();
  const mounted = useMounted();
  const { lines, subtotal, clear, refresh, removedNotice } = useCart();
  const [form, setForm] = useState<CheckoutFormState>(EMPTY_FORM);
  const [prefilled, setPrefilled] = useState(false);
  const [errors, setErrors] = useState<FieldKey[]>([]);
  const [problem, setProblem] = useState<Problem | null>(null);
  const [busy, setBusy] = useState(false);
  const [placed, setPlaced] = useState(false);
  const quote = useQuote(form.fulfillment === "Pickup", form.governorate, subtotal);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  if (session.status === "signedIn" && !prefilled) {
    setPrefilled(true);
    const { fullName, phoneNumber, email } = session.user;
    setForm((current) => ({ ...current, name: current.name || fullName, phone: current.phone || phoneNumber || "", email: current.email || email }));
  }

  const set: SetField = (key, value) => setForm((current) => ({ ...current, [key]: value }));
  const minimum = settings.minimumOrderAmount > 0 && subtotal < settings.minimumOrderAmount ? settings.minimumOrderAmount : null;

  async function submit(event: FormEvent) {
    event.preventDefault();
    const found = validateCheckout(form);
    setErrors(found);
    if (found.length > 0) return setProblem({ kind: "incomplete" });
    setBusy(true);
    setProblem(null);
    const outcome = await submitOrder(toCheckoutRequest(form, lines));
    if (outcome.status === "placed") {
      saveLastOrder(outcome.order);
      setPlaced(true);
      clear();
      router.push("/checkout/success");
      return;
    }
    if (outcome.status === "gone") await refresh();
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
        <Link href={loginHref("/checkout")} className={linkClass}>{t("loginLink")}</Link>
      </Notice>
    );
  }

  const problemText = problem && (problem.kind === "invalid" ? null : t(`problems.${problem.kind}`));

  return (
    <form onSubmit={submit} noValidate className="grid items-start gap-5 md:grid-cols-[1fr_380px]">
      <div className="flex min-w-0 flex-col gap-4">
        {removedNotice ? <p role="status" className="m-0 rounded-card bg-paper p-3 text-sm text-ink-muted">{t("removedNotice")}</p> : null}
        <CustomerSection form={form} set={set} errors={errors} showLogin={settings.guestCheckoutEnabled && session.status === "signedOut"} />
        <FulfillmentSection
          form={form} set={set} errors={errors} pickupEnabled={pickupEnabled} pickupAddress={pickupAddress} governorates={governorates}
        />
        <ExtrasSection form={form} set={set} errors={errors} leadSources={settings.leadSources} />
        <PaymentSection form={form} set={set} errors={errors} payment={payment} settings={settings} pickupEnabled={pickupEnabled} />
      </div>
      <OrderSummary
        form={form} set={set} errors={errors} lines={lines} subtotal={subtotal} quote={quote} minimum={minimum} busy={busy}
        problem={
          problem ? (
            <div role="alert" className="flex flex-col gap-1 text-sm text-error">
              {problem.kind === "invalid" ? problem.messages.map((message) => <p key={message} className="m-0">{message}</p>) : <p className="m-0">{problemText}</p>}
              {problem.kind === "signIn" ? <Link href={loginHref("/checkout")} className={linkClass}>{t("loginLink")}</Link> : null}
            </div>
          ) : null
        }
      />
    </form>
  );
}
