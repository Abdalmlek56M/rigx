"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextField } from "@/components/ui/TextField";
import type { Category } from "../categories/categories-api";
import type { Partner } from "../expenses/expenses-api";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { PurchaseActions } from "./PurchaseActions";
import { PurchaseLines, newLineKey } from "./PurchaseLines";
import { PurchasePayment } from "./PurchasePayment";
import { SellerPicker } from "./SellerPicker";
import type { Governorate } from "./SellerFormCard";
import type { Purchase, PurchaseReceived, SellerSummary } from "./purchases-api";
import { emptyPurchase, isDraft, PURCHASE_KINDS, purchaseFormValid, purchaseFromView, type PurchaseForm as Form } from "./purchase-rules";

export interface PurchaseLookups {
  tree: readonly Category[];
  /** Active financing partners; `null` when the staff member cannot read partners. */
  partners: readonly Partner[] | null;
  warrantyLabels: readonly string[] | null;
  governorates: readonly Governorate[];
}

/**
 * The purchase form: seller, kind, lines and payment, with save / receive / cancel. A new purchase starts empty; a saved draft starts from its
 * values; a received or cancelled purchase is shown read-only. Remount it (key) to start again from a fresh server copy after a 409.
 */
export function PurchaseForm({ purchase, today, lookups, onUpdated, onReceived, onCancelled, onStale }: {
  purchase: Purchase | null;
  today: string;
  lookups: PurchaseLookups;
  onUpdated: (purchase: Purchase) => void;
  onReceived: (result: PurchaseReceived) => void;
  onCancelled: (purchase: Purchase) => void;
  onStale: () => void;
}) {
  const t = useTranslations("admin.purchases.form");
  const submit = useSubmit();
  const [form, setForm] = useState<Form>(() => (purchase ? purchaseFromView(purchase, newLineKey) : emptyPurchase(today, newLineKey())));
  const [seller, setSeller] = useState<SellerSummary | null>(purchase?.seller ?? null);
  const readOnly = purchase !== null && !isDraft(purchase);
  const set = (patch: Partial<Form>) => setForm((current) => ({ ...current, ...patch }));
  const fullPc = form.kind === "FullPc";
  const error = (field: string) => submit.errors.fields[field];

  return (
    <>
      <SellerPicker
        seller={seller} readOnly={readOnly} governorates={lookups.governorates}
        onPick={(picked) => {
          setSeller(picked);
          set({ sellerId: picked?.id ?? "" });
        }}
      />
      <fieldset disabled={readOnly} className="m-0 flex min-w-0 flex-col gap-4 border-0 p-0">
        <section className="flex flex-col gap-3 rounded-card border border-line bg-surface p-5">
          <h2 className="m-0 text-[17px] font-bold">{t("kindTitle")}</h2>
          <div className="grid gap-3 sm:grid-cols-2">
            <SelectField
              label={t("kind")} name="kind" value={form.kind} error={error("kind")}
              onChange={(kind) => set({ kind, ...(kind === "Parts" ? { keepAsBuild: false, buildName: "" } : {}) })}
              options={PURCHASE_KINDS.map((kind) => ({ value: kind, label: t(`kinds.${kind}`) }))}
            />
          </div>
          {fullPc ? (
            <>
              <label className="flex min-h-11 items-center gap-2 text-sm font-bold">
                <input type="checkbox" checked={form.keepAsBuild} onChange={(event) => set({ keepAsBuild: event.target.checked })} className="h-5 w-5" />
                {t("keepAsBuild")}
              </label>
              {form.keepAsBuild ? <TextField label={t("buildName")} name="buildName" value={form.buildName} onChange={(buildName) => set({ buildName })} error={error("buildName")} /> : null}
            </>
          ) : null}
          <p className="m-0 text-xs text-ink-muted">{t("fullPcHint")}</p>
        </section>
        <PurchaseLines
          lines={form.lines} minLines={fullPc ? 2 : 1} tree={lookups.tree} partners={lookups.partners} warrantyLabels={lookups.warrantyLabels}
          errors={submit.errors.fields} onChange={(lines) => set({ lines })}
        />
        <PurchasePayment form={form} errors={submit.errors.fields} onChange={setForm} />
      </fieldset>
      <FormMessage message={submit.errors.general ?? error("lines") ?? error("sellerId") ?? null} />
      {readOnly ? null : (
        <PurchaseActions
          purchase={purchase} form={form} valid={purchaseFormValid(form)} submit={submit}
          onUpdated={onUpdated} onReceived={onReceived} onCancelled={onCancelled} onStale={onStale}
        />
      )}
      {submit.dialog}
    </>
  );
}
