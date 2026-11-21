"use client";

import { useTranslations } from "next-intl";
import { useState, type FormEvent } from "react";
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
    <form onSubmit={submit} role="search" className="flex flex-col gap-3 rounded-card border border-line bg-surface p-4 md:p-5">
      <div className="flex flex-col gap-1">
        <label htmlFor="warranty-q" className="text-sm font-bold">{t("label")}</label>
        <p className="m-0 text-[13px] text-ink-muted">{t("hint")}</p>
      </div>
      <div className="flex gap-2">
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
          className="h-12 min-w-0 flex-1 rounded-control border border-input-line bg-surface px-4 text-start text-base"
        />
        <button type="submit" className="flex h-12 items-center gap-2 rounded-control bg-orange px-5 font-bold text-on-orange">
          <SearchIcon size={18} />
          {t("submit")}
        </button>
      </div>
    </form>
  );
}
