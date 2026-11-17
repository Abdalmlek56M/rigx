"use client";

import { useTranslations } from "next-intl";
import { useState, type FormEvent } from "react";
import { useRouter } from "@/i18n/navigation";
import { lookupHref } from "./warranty-query";

/** The single search box; submitting puts the text in `?q=` so results are a shareable server-rendered page. */
export function WarrantySearchForm({ initial }: { initial: string }) {
  const t = useTranslations("warranty");
  const router = useRouter();
  const [text, setText] = useState(initial);

  function submit(event: FormEvent) {
    event.preventDefault();
    router.push(lookupHref(text));
  }

  return (
    <form onSubmit={submit} role="search" className="flex flex-col gap-2">
      <label htmlFor="warranty-q" className="text-sm font-bold">{t("label")}</label>
      <div className="flex gap-2">
        <input
          id="warranty-q"
          type="search"
          value={text}
          maxLength={64}
          required
          autoComplete="off"
          dir="auto"
          placeholder={t("placeholder")}
          onChange={(event) => setText(event.target.value)}
          className="h-12 min-w-0 flex-1 rounded-control border border-input-line bg-surface px-4 text-base"
        />
        <button type="submit" className="h-12 rounded-control bg-orange px-6 font-bold text-on-orange">{t("submit")}</button>
      </div>
      <span className="text-[13px] text-ink-muted">{t("phoneHint")}</span>
    </form>
  );
}
