"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { ApiError } from "@rigx/api-client";
import { BanIcon, SaveIcon } from "@/components/ui/icons";
import { SelectField } from "@/components/ui/SelectField";
import { TextField } from "@/components/ui/TextField";
import { isConflict } from "@/lib/api/conflict";
import { Button } from "../kit/Button";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { toFormErrors } from "../kit/form-errors";
import { FormMessage } from "../kit/PageHeader";
import { useToast } from "../kit/Toast";
import { useSubmit } from "../kit/use-submit";
import {
  canDelete, couponForm, couponValid, DISCOUNT_KINDS, EMPTY_COUPON_FORM, endsBeforeStart, normalizeCode, toCouponInput, type CouponForm as Form
} from "./coupon-rules";
import { createCoupon, deleteCoupon, updateCoupon, type Coupon } from "./coupons-api";

const CheckRow = ({ label, checked, onChange }: { label: string; checked: boolean; onChange: (checked: boolean) => void }) => (
  <label className="flex items-center gap-2.5 text-sm font-bold">
    <input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)} className="h-5 w-5 accent-orange" />
    {label}
  </label>
);

/** The coupon form: code, source, discount, limits, dates and on/off; plus delete (unused code) or turn off (used code). */
export function CouponForm({ coupon, onSaved, onStale, onCancel, onDeleted }: {
  coupon: Coupon | null;
  onSaved: (coupon: Coupon) => void;
  onStale: () => void;
  onCancel: () => void;
  onDeleted?: () => void;
}) {
  const t = useTranslations("admin.coupons");
  const kit = useTranslations("admin.kit");
  const submit = useSubmit();
  const toast = useToast();
  const [form, setForm] = useState<Form>(() => (coupon ? couponForm(coupon) : EMPTY_COUPON_FORM));
  const [deleting, setDeleting] = useState(false);
  const set = (patch: Partial<Form>) => setForm((current) => ({ ...current, ...patch }));
  /** The server's English message is replaced by the Arabic sentence of its field. */
  const fieldError = (key: "code" | "sourceName" | "discountKind" | "discountValue" | "maxDiscount" | "minOrderTotal" | "maxUses" | "endsAtUtc") => (submit.errors.fields[key] ? t(`errors.${key}`) : undefined);
  const percent = form.kind === "Percent";

  async function save(next: Form = form) {
    if (!couponValid(next)) return;
    await submit.run(async () => {
      try {
        const input = toCouponInput(next, coupon?.rowVersion ?? 0);
        onSaved(coupon ? await updateCoupon(coupon.id, input) : await createCoupon(input));
      } catch (error) {
        if (isConflict(error)) onStale();
        throw error;
      }
    }, t("saved"));
  }

  async function remove() {
    if (!coupon) return;
    try {
      await deleteCoupon(coupon.id);
      toast(t("deleted"));
      onDeleted?.();
    } catch (error) {
      toast(error instanceof ApiError && error.status === 409 ? t("deleteUsed") : toFormErrors(error, kit("failed")).general ?? kit("failed"), "error");
      throw error;
    }
  }

  return (
    <form className="flex flex-col gap-4" onSubmit={(event) => { event.preventDefault(); void save(); }}>
      <div className="grid gap-3 sm:grid-cols-2">
        <TextField label={t("fields.code")} name="code" ltr value={form.code} onChange={(code) => set({ code: normalizeCode(code) })} error={fieldError("code")} maxLength={30} required />
        <TextField label={t("fields.sourceName")} name="sourceName" value={form.sourceName} onChange={(sourceName) => set({ sourceName })} error={fieldError("sourceName")} placeholder={t("sourcePlaceholder")} required />
        <SelectField label={t("fields.discountKind")} name="discountKind" value={form.kind} onChange={(kind) => set({ kind })} options={DISCOUNT_KINDS.map((kind) => ({ value: kind, label: t(`kinds.${kind}`) }))} error={fieldError("discountKind")} />
        <TextField label={percent ? t("fields.percent") : t("fields.amount")} name="discountValue" type="number" ltr min="0" step={percent ? "1" : "0.01"} value={form.value} onChange={(value) => set({ value })} error={fieldError("discountValue")} required />
        {percent ? <TextField label={t("fields.maxDiscount")} name="maxDiscount" type="number" ltr min="0" step="0.01" value={form.maxDiscount} onChange={(maxDiscount) => set({ maxDiscount })} error={fieldError("maxDiscount")} /> : null}
        <TextField label={t("fields.minOrderTotal")} name="minOrderTotal" type="number" ltr min="0" step="0.01" value={form.minOrderTotal} onChange={(minOrderTotal) => set({ minOrderTotal })} error={fieldError("minOrderTotal")} />
        <TextField label={t("fields.maxUses")} name="maxUses" type="number" ltr min="1" step="1" value={form.maxUses} onChange={(maxUses) => set({ maxUses })} error={fieldError("maxUses")} />
        <TextField label={t("fields.startDate")} name="startDate" type="date" ltr value={form.startDate} onChange={(startDate) => set({ startDate })} />
        <TextField label={t("fields.endDate")} name="endDate" type="date" ltr value={form.endDate} onChange={(endDate) => set({ endDate })} error={endsBeforeStart(form) ? t("endBeforeStart") : fieldError("endsAtUtc")} />
      </div>
      <p className="m-0 text-xs text-ink-muted">{t("hint")}</p>
      <div className="flex flex-wrap gap-x-6 gap-y-2">
        <CheckRow label={t("fields.oncePerCustomer")} checked={form.oncePerCustomer} onChange={(oncePerCustomer) => set({ oncePerCustomer })} />
        <CheckRow label={t("fields.isActive")} checked={form.isActive} onChange={(isActive) => set({ isActive })} />
      </div>
      <FormMessage message={submit.errors.general} />
      <div className="flex flex-wrap gap-2">
        <Button type="submit" variant="primary" icon={SaveIcon} loading={submit.busy} disabled={!couponValid(form)}>{t("save")}</Button>
        {coupon && canDelete(coupon) ? <Button variant="danger" onClick={() => setDeleting(true)}>{t("delete")}</Button> : null}
        {coupon && !canDelete(coupon) && form.isActive ? (
          <Button icon={BanIcon} loading={submit.busy} onClick={() => { const next = { ...form, isActive: false }; setForm(next); void save(next); }}>{t("turnOff")}</Button>
        ) : null}
        {coupon ? null : <Button variant="ghost" onClick={onCancel}>{kit("cancel")}</Button>}
      </div>
      {coupon && !canDelete(coupon) ? <p className="m-0 text-xs text-ink-muted">{t("usedNote")}</p> : null}
      {submit.dialog}
      <ConfirmDialog open={deleting} title={t("delete")} body={t("deleteBody")} confirmLabel={t("delete")} danger onClose={() => setDeleting(false)} onConfirm={remove} />
    </form>
  );
}
