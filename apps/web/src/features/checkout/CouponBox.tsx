"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Button, IconButton } from "@/components/ui/Button";
import { fieldClass } from "@/components/ui/field-class";
import { CheckIcon, CloseIcon } from "@/components/ui/icons";
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
        <span className="flex min-w-0 flex-wrap items-center gap-2">
          <span aria-hidden="true" className="flex text-success"><CheckIcon size={16} /></span>
          <span className="text-ink-muted">{t("applied")}</span>
          <b dir="ltr" className="font-mono">{applied.code}</b>
        </span>
        <IconButton icon={CloseIcon} label={t("remove", { code: applied.code })} size="sm" onClick={onRemoved} />
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor="coupon-code" className="text-xs font-bold">{t("label")}</label>
      <div className="flex gap-2">
        <input
          id="coupon-code" dir="ltr" value={draft} maxLength={30} autoComplete="off" placeholder={t("placeholder")}
          onChange={(event) => setDraft(event.target.value.toUpperCase())}
          onKeyDown={(event) => {
            if (event.key !== "Enter") return;
            event.preventDefault();
            void apply();
          }}
          className={`${fieldClass} min-w-0 flex-1 text-start`}
        />
        <Button type="button" variant="secondary" disabled={checking || !draft.trim()} onClick={() => void apply()}>
          {checking ? t("checking") : t("apply")}
        </Button>
      </div>
      {problem ? <p role="alert" className="m-0 text-xs text-error-text">{problemText(problem)}</p> : null}
      {failed ? <p role="alert" className="m-0 text-xs text-error-text">{t("failed")}</p> : null}
    </div>
  );
}
