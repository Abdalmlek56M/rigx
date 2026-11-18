"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { FormSection } from "@/features/checkout/form-parts";
import { BUTTON, usePosError, type SetForm } from "./pos-parts";
import { findCustomer, type PosCustomer as FoundCustomer } from "./pos-api";
import { posPhone, posPhoneValid, type PosField, type PosForm } from "./pos-rules";

interface PosCustomerProps {
  form: PosForm;
  set: SetForm;
  errors: readonly PosField[];
}

type Lookup = { status: "idle" | "busy" | "none" | "failed" } | { status: "found"; customer: FoundCustomer };

/** The customer's name, phone, WhatsApp and email; a phone that was used before offers the details it had. */
export function PosCustomer({ form, set, errors }: PosCustomerProps) {
  const t = useTranslations("admin.pos.customer");
  const error = usePosError(errors);
  const [lookup, setLookup] = useState<Lookup>({ status: "idle" });

  async function find() {
    if (!posPhoneValid(form.phone) || lookup.status === "busy") return;
    setLookup({ status: "busy" });
    try {
      const customer = await findCustomer(posPhone(form.phone));
      setLookup(customer ? { status: "found", customer } : { status: "none" });
    } catch {
      setLookup({ status: "failed" });
    }
  }

  /** Fills only what is still empty, so nothing already typed is lost. */
  function applyDetails(customer: FoundCustomer) {
    const fill = <K extends "name" | "email" | "governorate" | "area" | "street" | "building" | "landmark">(key: K, value: string | null | undefined) => {
      if (value && !form[key].trim()) set(key, value);
    };
    fill("name", customer.name);
    fill("email", customer.email);
    fill("governorate", customer.address?.governorate);
    fill("area", customer.address?.area);
    fill("street", customer.address?.street);
    fill("building", customer.address?.building);
    fill("landmark", customer.address?.landmark);
    set("phoneHasWhatsApp", customer.phoneHasWhatsApp);
  }

  return (
    <FormSection title={t("title")}>
      <div className="grid gap-3.5 sm:grid-cols-2">
        <TextField label={t("name")} name="name" value={form.name} onChange={(value) => set("name", value)} autoComplete="off" required error={error("name")} />
        <div className="flex flex-col gap-2">
          <div onBlur={() => { if (lookup.status === "idle") void find(); }}>
            <TextField
              label={t("phone")} name="phone" type="tel" ltr value={form.phone} autoComplete="off" required error={error("phone")}
              onChange={(value) => { set("phone", value); setLookup({ status: "idle" }); }}
            />
          </div>
          <label className="flex min-h-11 items-center gap-2.5 text-sm">
            <input type="checkbox" checked={form.phoneHasWhatsApp} onChange={(event) => set("phoneHasWhatsApp", event.target.checked)} className="h-[18px] w-[18px] accent-carbon" />
            {t("whatsapp")}
          </label>
        </div>
        <TextField label={t("email")} name="email" type="email" ltr value={form.email} onChange={(value) => set("email", value)} autoComplete="off" error={error("email")} />
        <div className="flex flex-col items-start justify-end gap-2">
          <button type="button" disabled={!posPhoneValid(form.phone) || lookup.status === "busy"} onClick={() => void find()} className={BUTTON}>
            {lookup.status === "busy" ? t("finding") : t("find")}
          </button>
        </div>
      </div>
      {lookup.status === "found" ? (
        <div role="status" className="flex flex-wrap items-center justify-between gap-3 rounded-control bg-paper px-3 py-2 text-sm">
          <span>{t("found", { name: lookup.customer.name, orders: t("orders", { count: lookup.customer.orderCount }) })}</span>
          <button type="button" onClick={() => applyDetails(lookup.customer)} className={BUTTON}>{t("use")}</button>
        </div>
      ) : null}
      {lookup.status === "none" ? <p role="status" className="m-0 text-sm text-ink-muted">{t("none")}</p> : null}
      {lookup.status === "failed" ? <p role="alert" className="m-0 text-sm text-error">{t("failed")}</p> : null}
    </FormSection>
  );
}
