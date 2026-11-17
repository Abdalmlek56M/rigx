"use client";

import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { loginHref } from "@/features/auth/auth-rules";
import { Link } from "@/i18n/navigation";
import { FormSection, useFieldError, type SectionProps } from "./form-parts";

/** Name, mobile (+ WhatsApp tick) and optional email; guests get a sign-in link. */
export function CustomerSection({ form, set, errors, showLogin }: SectionProps & { showLogin: boolean }) {
  const t = useTranslations("checkout.customer");
  const error = useFieldError(errors);

  return (
    <FormSection
      title={t("title")}
      aside={
        showLogin ? (
          <span className="text-sm text-ink-muted">
            {t("haveAccount")}{" "}
            <Link href={loginHref("/checkout")} className="text-orange-text underline hover:text-orange-text-hover">{t("login")}</Link>
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
          className="h-[18px] w-[18px] accent-carbon"
        />
        {t("whatsApp")}
      </label>
      <TextField label={t("email")} name="email" type="email" ltr autoComplete="email" value={form.email} onChange={(v) => set("email", v)} />
      <p className="m-0 text-[13px] text-ink-muted">{t("note")}</p>
    </FormSection>
  );
}
