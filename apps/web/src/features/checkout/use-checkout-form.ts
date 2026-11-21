"use client";

import { useEffect, useRef, useState, type FormEvent } from "react";
import { useRouter } from "@/i18n/navigation";
import { useMounted } from "@/lib/hooks/use-mounted";
import type { CheckoutSettings } from "@/lib/storefront";
import { useCart } from "@/features/cart/cart-context";
import { useSession } from "@/features/auth/use-session";
import {
  FIELD_INPUT, toCheckoutRequest, validateCheckout, type CheckoutFormState, type FieldKey
} from "./checkout-request";
import { clearCheckoutDraft, mergeDraft, readCheckoutDraft, saveCheckoutDraft } from "./checkout-draft";
import { isEgyptMobile, normalizePhone } from "../requests/request-fields";
import type { AppliedCoupon } from "./CouponBox";
import type { SetField } from "./form-parts";
import { saveLastOrder } from "./last-order";
import { submitOrder } from "./submit-order";
import { useQuote } from "./use-quote";

export type CheckoutProblem =
  | { kind: "incomplete"; fields: FieldKey[] }
  | { kind: "gone" | "signIn" | "failed" }
  | { kind: "invalid"; messages: string[] };

const AUTOSAVE_MS = 400;

/** The checkout form's state and actions: draft autosave, profile prefill, validation/focus, coupon and order submit. */
export function useCheckoutForm(settings: CheckoutSettings) {
  const router = useRouter();
  const session = useSession();
  const mounted = useMounted();
  const { lines, subtotal, clear, refresh, removedNotice } = useCart();
  const [form, setForm] = useState<CheckoutFormState>(() => mergeDraft(readCheckoutDraft()));
  const [prefilled, setPrefilled] = useState(false);
  const [errors, setErrors] = useState<FieldKey[]>([]);
  const [problem, setProblem] = useState<CheckoutProblem | null>(null);
  const [busy, setBusy] = useState(false);
  const [placed, setPlaced] = useState(false);
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

  // Fill the empty fields from the profile once; a field the customer typed always wins.
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
  const applyCoupon = (applied: AppliedCoupon) => {
    setCoupon({ ...applied, forTotal: subtotal });
    setForm((current) => ({ ...current, couponCode: applied.code }));
  };
  // A discount worked out for an older cart total no longer holds: the old code is ignored until it is applied again.
  const appliedCoupon = coupon && coupon.forTotal === subtotal ? coupon : null;
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
    const outcome = await submitOrder(toCheckoutRequest({ ...form, couponCode: appliedCoupon?.code ?? "" }, lines));
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

  return {
    mounted,
    placed,
    lines,
    subtotal,
    removedNotice,
    form,
    set,
    errors,
    problem,
    busy,
    coupon: appliedCoupon,
    couponProblem,
    couponPhone: isEgyptMobile(form.phone) ? normalizePhone(form.phone) : null,
    minimum,
    quote,
    formRef,
    submit,
    setCouponProblem,
    applyCoupon,
    removeCoupon
  };
}
