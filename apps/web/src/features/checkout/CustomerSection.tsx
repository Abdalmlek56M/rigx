"use client";

import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { FormSection, OptionalPill, useFieldError, type SectionProps } from "./form-parts";

/** Name, mobile (+ WhatsApp tick), optional email with its confirmation; guests can sign in without leaving the page. */
export function CustomerSection({ form, set, errors, showLogin, onLogin }: SectionProps & { showLogin: boolean; onLogin: () => void }) {
  const t = useTranslations("checkout.customer");
  const error = useFieldError(errors);

  return (
    <FormSection
      title={t("title")}
      aside={
        showLogin ? (
          <span className="text-sm text-ink-muted">
            {t("haveAccount")}{" "}
            <button type="button" onClick={onLogin} className="text-orange-text underline hover:text-orange-text-hover">
              {t("login")}
            </button>
          </span>
        ) : undefined
      }
    >
      <div className="grid gap-3.5 sm:grid-cols-2">
        <TextField label={t("name")} name="name" value={form.name} onChange={(v) => set("name", v)} autoComplete="name" required error={error("name")} />
        <TextField
          label={t("phone")} name="phone" type="tel" ltr autoComplete="tel" placeholder="01xxxxxxxxx" required
          value={form.phone} onChange={(v) => set("phone", v)} error={error("phone")}
        />
      </div>
      <label className="flex min-h-11 items-center gap-2.5 text-sm">
        <input
          type="checkbox" checked={form.phoneHasWhatsApp} onChange={(event) => set("phoneHasWhatsApp", event.target.checked)}
          className="h-[18px] w-[18px] accent-orange"
        />
        {t("whatsApp")}
      </label>
      <div className="grid gap-3.5 sm:grid-cols-2">
        <TextField
          label={t("email")} labelEnd={<OptionalPill />} name="email" type="email" ltr autoComplete="email"
          value={form.email} onChange={(v) => set("email", v)}
        />
        {form.email.trim() ? (
          <TextField
            label={t("emailConfirm")} name="emailConfirm" type="email" ltr autoComplete="off"
            value={form.emailConfirm} onChange={(v) => set("emailConfirm", v)} error={error("emailConfirm")}
          />
        ) : null}
      </div>
      <p className="m-0 text-[13px] text-ink-muted">{t("note")}</p>
    </FormSection>
  );
}
