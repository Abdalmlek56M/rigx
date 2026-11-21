"use client";

import { useTranslations } from "next-intl";
import { useState, type FormEvent } from "react";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { fieldClass } from "@/components/ui/field-class";
import { SearchIcon } from "@/components/ui/icons";
import { useRouter } from "@/i18n/navigation";
import { lookupHref } from "./warranty-query";

/** The single search box in a compact card; submitting puts the text in `?q=` so results are a shareable page. */
export function WarrantySearchForm({ initial }: { initial: string }) {
  const t = useTranslations("warranty");
  const router = useRouter();
  const [text, setText] = useState(initial);

  function submit(event: FormEvent) {
    event.preventDefault();
    router.push(lookupHref(text));
  }

  return (
    <Card as="div" padding="md" className="gap-3">
      <form onSubmit={submit} role="search" className="flex flex-col gap-3">
        <div className="flex flex-col gap-1">
          <label htmlFor="warranty-q" className="text-xs font-bold">{t("label")}</label>
          <p className="m-0 text-xs text-ink-muted">{t("hint")}</p>
        </div>
        <div className="flex flex-col gap-2 sm:flex-row">
          <input
            id="warranty-q"
            type="search"
            value={text}
            maxLength={64}
            required
            autoComplete="off"
            dir="ltr"
            placeholder={t("placeholder")}
            onChange={(event) => setText(event.target.value)}
            className={`${fieldClass} min-w-0 flex-1 text-start`}
          />
          <Button type="submit" variant="accent" icon={SearchIcon}>{t("submit")}</Button>
        </div>
      </form>
    </Card>
  );
}
