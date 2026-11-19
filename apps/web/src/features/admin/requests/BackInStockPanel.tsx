"use client";

import { useCallback } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatDateTime } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { useSubmit } from "../kit/use-submit";
import { fetchBackInStock, markBackInStockSent } from "./requests-api";

/** Notify-me requests whose model came back in stock and were not messaged yet. */
export function BackInStockPanel({ onChanged }: { onChanged: () => void }) {
  const t = useTranslations("admin.requests.backInStock");
  const tCondition = useTranslations("common.condition");
  const locale = useLocale();
  const submit = useSubmit();
  const load = useCallback(() => fetchBackInStock(), []);
  const { state, reload } = useLoad(load);
  if (state.status !== "ready" || state.data.length === 0) return null;

  async function markSent(id: string) {
    await submit.run(async () => {
      await markBackInStockSent(id);
      reload();
      onChanged();
    }, t("marked"));
  }

  return (
    <section className="flex flex-col gap-3 rounded-card border border-line bg-surface p-5">
      <div className="flex flex-col gap-0.5">
        <h2 className="m-0 text-[17px] font-bold">{t("panelTitle")}</h2>
        <p className="m-0 text-xs text-ink-muted">{t("panelNote")}</p>
      </div>
      <ul className="m-0 flex list-none flex-col gap-2 p-0">
        {state.data.map((item) => (
          <li key={item.id} className="flex flex-wrap items-center gap-3 rounded-control border border-line p-3 text-sm">
            <span className="flex min-w-0 flex-1 flex-col">
              <b dir="auto" className="truncate">{item.itemName}</b>
              <span dir="ltr" className="font-mono text-xs text-orange-text">{item.itemCode}</span>
              <span className="text-xs text-ink-muted">{t("requestLine", { code: item.code, name: item.name, date: formatDateTime(item.availableAtUtc, locale) })}</span>
              <span className="text-xs text-ink-muted">
                <bdi dir="ltr">{item.phone}</bdi>
                {" · "}
                {item.condition === "New" || item.condition === "Used" ? t("wanted", { condition: tCondition(item.condition) }) : t("wantedAny")}
              </span>
            </span>
            <span className="flex flex-wrap gap-2">
              <a href={item.whatsApp.url} target="_blank" rel="noreferrer" className="inline-flex h-11 items-center rounded-control bg-orange px-4 text-sm font-bold text-on-orange">
                {t("openWhatsApp")}
              </a>
              <button type="button" disabled={submit.busy} onClick={() => void markSent(item.id)} className="h-11 rounded-control border border-input-line bg-surface px-4 font-bold disabled:opacity-50">
                {t("markSent")}
              </button>
            </span>
          </li>
        ))}
      </ul>
      {submit.dialog}
    </section>
  );
}
