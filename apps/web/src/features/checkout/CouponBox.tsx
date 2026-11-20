"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { CloseIcon } from "@/components/ui/icons";
import { useCouponProblemText } from "@/lib/coupon-problem-text";
import { checkCoupon } from "./coupon-api";

export interface AppliedCoupon {
  code: string;
  discount: number;
}

interface CouponBoxProps {
  /** The order total the code is checked against (before the coupon). */
  orderTotal: number;
  /** The phone of the order when it is filled and valid. */
  phone: string | null;
  applied: AppliedCoupon | null;
  /** A refusal to show (a problem code from the check or from the order), or null. */
  problem: string | null;
  onApplied: (coupon: AppliedCoupon) => void;
  onRemoved: () => void;
  onProblem: (problem: string | null) => void;
}

/** The coupon field in the order summary: type a code and press apply, then the applied code with a remove button. */
export function CouponBox({ orderTotal, phone, applied, problem, onApplied, onRemoved, onProblem }: CouponBoxProps) {
  const t = useTranslations("checkout.coupon");
  const problemText = useCouponProblemText();
  const [draft, setDraft] = useState("");
  const [checking, setChecking] = useState(false);
  const [failed, setFailed] = useState(false);

  async function apply() {
    const code = draft.trim();
    if (!code || checking) return;
    setChecking(true);
    setFailed(false);
    onProblem(null);
    try {
      const answer = await checkCoupon(code, orderTotal, phone);
      if (answer.problem) onProblem(answer.problem);
      else {
        onApplied({ code: answer.code ?? code.toUpperCase(), discount: answer.discount });
        setDraft("");
      }
    } catch {
      setFailed(true);
    } finally {
      setChecking(false);
    }
  }

  if (applied) {
    return (
      <div className="flex items-center justify-between gap-3 rounded-control bg-paper px-3 py-2 text-sm">
        <span className="flex items-center gap-2">
          <span className="text-ink-muted">{t("applied")}</span>
          <b dir="ltr" className="font-mono">{applied.code}</b>
        </span>
        <button type="button" onClick={onRemoved} aria-label={t("remove", { code: applied.code })} className="flex size-9 items-center justify-center rounded-full text-ink-muted hover:bg-line hover:text-ink">
          <CloseIcon size={16} />
        </button>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor="coupon-code" className="text-[13px] font-bold">{t("label")}</label>
      <div className="flex gap-2">
        <input
          id="coupon-code" dir="ltr" value={draft} maxLength={30} autoComplete="off" placeholder={t("placeholder")}
          onChange={(event) => setDraft(event.target.value.toUpperCase())}
          onKeyDown={(event) => {
            if (event.key !== "Enter") return;
            event.preventDefault();
            void apply();
          }}
          className="h-11 min-w-0 flex-1 rounded-control border border-input-line bg-surface px-3 text-start font-mono text-sm text-ink"
        />
        <button type="button" disabled={checking || !draft.trim()} onClick={() => void apply()} className="h-11 shrink-0 rounded-control border border-input-line px-4 text-sm font-bold hover:bg-paper disabled:cursor-not-allowed disabled:opacity-50">
          {checking ? t("checking") : t("apply")}
        </button>
      </div>
      {problem ? <p role="alert" className="m-0 text-xs text-error">{problemText(problem)}</p> : null}
      {failed ? <p role="alert" className="m-0 text-xs text-error">{t("failed")}</p> : null}
    </div>
  );
}
