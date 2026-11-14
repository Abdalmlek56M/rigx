"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { BUDGETS, type Budget } from "@/features/builds/build-query";
import { fillTemplate, whatsAppUrl } from "@/lib/contact";
import { USAGES, type Usage } from "@/lib/usage";

const chip = "h-11 rounded-full px-4 text-sm";

/** "رشحولي تجميعة": usage and budget chips that become a prefilled WhatsApp message (no API call). */
export function RecommendWidget({ whatsApp }: { whatsApp: { number: string; template: string } }) {
  const t = useTranslations("home.recommend");
  const tu = useTranslations("usage");
  const tb = useTranslations("home.recommend.budgets");
  const [usage, setUsage] = useState<Usage | null>(null);
  const [budget, setBudget] = useState<Budget | null>(null);

  const text = [
    fillTemplate(whatsApp.template, {}),
    [t("message.base"), usage && t("message.usage", { usage: tu(usage) }), budget && t("message.budget", { budget: tb(budget) })]
      .filter(Boolean)
      .join(" ")
  ]
    .filter(Boolean)
    .join("\n");

  return (
    <div id="recommend" className="flex scroll-mt-4 flex-1 basis-[420px] flex-col gap-4 rounded-panel border border-line bg-surface p-6 md:p-8">
      <span className="text-[13px] font-bold text-orange-text">{t("eyebrow")}</span>
      <h2 className="m-0 text-[28px] font-bold">{t("title")}</h2>
      <fieldset className="m-0 border-0 p-0">
        <legend className="mb-2.5 text-[15px] font-bold">{t("usageLegend")}</legend>
        <div className="flex flex-wrap gap-2">
          {USAGES.map((entry) => (
            <button
              key={entry}
              type="button"
              aria-pressed={usage === entry}
              onClick={() => setUsage(usage === entry ? null : entry)}
              className={`${chip} ${usage === entry ? "bg-carbon text-on-carbon" : "border border-input-line bg-surface"}`}
            >
              {tu(entry)}
            </button>
          ))}
        </div>
      </fieldset>
      <fieldset className="m-0 border-0 p-0">
        <legend className="mb-2.5 text-[15px] font-bold">{t("budgetLegend")}</legend>
        <div className="flex flex-wrap gap-2">
          {BUDGETS.map((entry) => (
            <button
              key={entry}
              type="button"
              aria-pressed={budget === entry}
              onClick={() => setBudget(budget === entry ? null : entry)}
              className={`${chip} ${budget === entry ? "bg-carbon text-on-carbon" : "border border-input-line bg-surface"}`}
            >
              {tb(entry)}
            </button>
          ))}
        </div>
      </fieldset>
      <a
        href={whatsAppUrl(whatsApp.number, text)}
        target="_blank"
        rel="noopener noreferrer"
        className="flex h-[50px] items-center self-start rounded-control bg-orange px-6 font-bold text-on-orange"
      >
        {t("send")}
      </a>
    </div>
  );
}
