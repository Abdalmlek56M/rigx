"use client";

import { useEffect, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import type { Category } from "../categories/categories-api";
import type { Partner } from "../expenses/expenses-api";
import { Button } from "../kit/Button";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { clearDraft, draftIsFresh, draftKeyFor, readDraft, useFormDraft, type FormDraft } from "../kit/use-form-draft";
import { CloseIcon, UndoIcon } from "@/components/ui/icons";
import { formatDateTime } from "@/lib/format";
import { useSession } from "@/features/auth/use-session";
import { PurchaseIntakeType } from "./PurchaseIntakeType";
import { PurchaseActions } from "./PurchaseActions";
import type { Brand } from "../brands/brands-api";
import type { StockItem } from "../inventory/inventory-api";
import { PurchaseLines, newLineKey } from "./PurchaseLines";
import { PurchasePayment } from "./PurchasePayment";
import { SellerPaymentCard } from "./SellerPaymentCard";
import { SellerPicker } from "./SellerPicker";
import type { Governorate } from "./SellerFormCard";
import type { Purchase, PurchaseReceived, SellerSummary } from "./purchases-api";
import { emptyPurchase, isDraft, lineDisplayName, lineFromItem, missingFields, purchaseFromView, type PurchaseForm as Form } from "./purchase-rules";

export interface PurchaseLookups {
  tree: readonly Category[];
  brands: readonly Brand[];
  /** "نسخ لقطعة جديدة": the inventory item a new purchase starts from (`?copyFrom=` on the new-purchase page). */
  copySource: StockItem | null;
  /** Active financing partners; `null` when the staff member cannot read partners. */
  partners: readonly Partner[] | null;
  warrantyLabels: readonly string[] | null;
  governorates: readonly Governorate[];
}

interface PurchaseDraft {
  form: Form;
  seller: SellerSummary | null;
}

/**
 * The purchase form: seller, intake type, lines and payment, with save / receive / cancel. A new purchase starts empty;
 * a saved draft starts from its values; a received or cancelled purchase is shown read-only. A new purchase keeps a local
 * draft after the last change and offers it when the page is opened again.
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
  const tLines = useTranslations("admin.purchases.lines");
  const tDraft = useTranslations("admin.kit.draft");
  const locale = useLocale();
  const session = useSession();
  const submit = useSubmit();
  const [form, setForm] = useState<Form>(() => (purchase
    ? purchaseFromView(purchase, newLineKey)
    : emptyPurchase(today, newLineKey(), lookups.copySource ? lineFromItem(lookups.copySource, newLineKey()) : undefined)));
  const [seller, setSeller] = useState<SellerSummary | null>(purchase?.seller ?? null);
  const [submitted, setSubmitted] = useState(false);
  const [dirty, setDirty] = useState(false);
  const [created, setCreated] = useState(false);
  const [extraBrands, setExtraBrands] = useState<Brand[]>([]);
  const [reveal, setReveal] = useState<{ key: string | null; nonce: number }>({ key: null, nonce: 0 });
  const [draftOffer, setDraftOffer] = useState<FormDraft<PurchaseDraft> | null>(null);
  const readOnly = purchase !== null && !isDraft(purchase);
  const username = session.status === "signedIn" ? session.user.username : "";
  const draftKey = username ? draftKeyFor(username, "purchase.new") : "";
  const missing = missingFields(form);
  const brands = [...lookups.brands, ...extraBrands.filter((brand) => !lookups.brands.some((known) => known.id === brand.id))];
  const draftEnabled = !readOnly && purchase === null && !created && draftKey !== "" && draftOffer === null && dirty;
  useFormDraft(draftKey, { form, seller }, draftEnabled);

  const changeForm = (next: Form | ((current: Form) => Form)) => {
    setDirty(true);
    setForm(next);
  };
  const set = (patch: Partial<Form>) => changeForm((current) => ({ ...current, ...patch }));
  const error = (field: string) => submit.errors.fields[field];

  useEffect(() => {
    if (purchase || !draftKey) return;
    const timer = window.setTimeout(() => {
      try {
        const draft = readDraft<PurchaseDraft>(window.localStorage, draftKey);
        if (!draft) return;
        if (draftIsFresh(draft)) setDraftOffer(draft);
        else clearDraft(window.localStorage, draftKey);
      } catch {
        // Storage may be disabled; the form keeps working.
      }
    }, 0);
    return () => window.clearTimeout(timer);
  }, [purchase, draftKey]);

  function restoreDraft() {
    if (!draftOffer) return;
    // Fresh keys: the saved ones came from another page load and could clash with lines added from now on.
    const saved = draftOffer.value.form;
    changeForm({ ...saved, lines: saved.lines.map((line) => ({ ...line, key: newLineKey() })) });
    setSeller(draftOffer.value.seller);
    setSubmitted(false);
    setDraftOffer(null);
    setReveal((current) => ({ key: null, nonce: current.nonce + 1 }));
  }

  function ignoreDraft() {
    if (draftKey) {
      try {
        clearDraft(window.localStorage, draftKey);
      } catch {
        // Storage may be disabled; nothing to clear.
      }
    }
    setDirty(false);
    setDraftOffer(null);
  }

  /** Marks every missing field, opens and focuses the first one; false means the API is not called. */
  function validate(): boolean {
    setSubmitted(true);
    const current = missingFields(form);
    if (current.length === 0) return true;
    const first = current[0]!;
    if (first.line !== null) {
      const line = form.lines[first.line];
      if (line) setReveal((state) => ({ key: line.key, nonce: state.nonce + 1 }));
    }
    window.requestAnimationFrame(() => {
      window.requestAnimationFrame(() => {
        const target = first.line !== null
          ? document.querySelector<HTMLElement>(`[name="line${first.line}-${first.field}"]`)
          : first.field === "sellerId"
            ? document.querySelector<HTMLElement>('[data-field="seller"] input')
            : first.field === "lines"
              ? document.querySelector<HTMLElement>('[data-field="lines"]')
              : document.querySelector<HTMLElement>(`[name="${first.field}"]`);
        if (!target) return;
        const reduced = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
        target.scrollIntoView({ behavior: reduced ? "auto" : "smooth", block: "center" });
        target.focus();
      });
    });
    return false;
  }

  const missingLabels = missing.map((entry) => {
    if (entry.line === null) return t(`missing.${entry.label}`);
    const line = form.lines[entry.line];
    const title = line ? lineDisplayName(line, lookups.tree, tLines("lineTitle", { number: entry.line + 1 })) : "";
    return t("missing.inLine", { field: t(`missing.${entry.label}`), line: title });
  });
  const firstFour = missingLabels.slice(0, 4);
  const items = firstFour.join(t("missing.separator")) + (missingLabels.length > 4 ? ` ${t("missing.more", { count: missingLabels.length - 4 })}` : "");

  return (
    <>
      {draftOffer ? (
        <section role="status" className="flex flex-wrap items-center justify-between gap-3 rounded-card border border-orange bg-surface p-4">
          <p className="m-0 text-sm font-bold">{tDraft("found", { time: formatDateTime(draftOffer.savedAt, locale) })}</p>
          <div className="flex flex-wrap gap-2">
            <Button variant="primary" icon={UndoIcon} onClick={restoreDraft}>{tDraft("restore")}</Button>
            <Button variant="secondary" icon={CloseIcon} onClick={ignoreDraft}>{tDraft("ignore")}</Button>
          </div>
        </section>
      ) : null}

      <SellerPicker
        seller={seller} readOnly={readOnly} missing={submitted && !form.sellerId} governorates={lookups.governorates}
        onPick={(picked) => {
          setSeller(picked);
          set({ sellerId: picked?.id ?? "" });
        }}
      />
      <fieldset disabled={readOnly} className="m-0 flex min-w-0 flex-col gap-4 border-0 p-0">
        <PurchaseIntakeType form={form} error={error("buildName")} onChange={set} />
        <PurchaseLines
          key={`${reveal.key ?? "all"}-${reveal.nonce}`}
          lines={form.lines} minLines={form.kind === "FullPc" ? 2 : 1} tree={lookups.tree} brands={brands} partners={lookups.partners}
          warrantyLabels={lookups.warrantyLabels} errors={submit.errors.fields} missing={missing} submitted={submitted} revealKey={reveal.key}
          onChange={(lines) => set({ lines })}
          onBrandCreated={(brand) => setExtraBrands((current) => (current.some((known) => known.id === brand.id) ? current : [...current, brand]))}
        />
        <PurchasePayment
          form={form} errors={submit.errors.fields} missing={missing.filter((entry) => entry.line === null).map((entry) => entry.field)}
          submitted={submitted} onChange={changeForm}
        />
      </fieldset>
      {purchase ? <SellerPaymentCard purchase={purchase} onUpdated={onUpdated} onStale={onStale} /> : null}
      <FormMessage message={submit.errors.general ?? error("lines") ?? error("sellerId") ?? null} />
      {submitted && missing.length > 0 ? (
        <p role="alert" className="m-0 rounded-control bg-paper px-3 py-2 text-sm font-bold text-error">
          {t("missing.summary", { items })}
        </p>
      ) : null}
      {readOnly ? null : (
        <PurchaseActions
          purchase={purchase} form={form} onValidate={validate} submit={submit}
          onCreated={() => {
            setCreated(true);
            if (!draftKey) return;
            try {
              clearDraft(window.localStorage, draftKey);
            } catch {
              // Storage may be disabled; the server already has the purchase.
            }
          }}
          onUpdated={onUpdated} onReceived={onReceived} onCancelled={onCancelled} onStale={onStale}
        />
      )}
      {submit.dialog}
    </>
  );
}
