"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { BUDGETS, type Budget } from "@/features/builds/build-query";
import { BookIcon, BroadcastIcon, CheckIcon, FilmIcon, GamepadIcon, WhatsAppIcon } from "@/components/ui/icons";
import { fillTemplate, whatsAppUrl, NEW_TAB } from "@/lib/contact";
import { USAGES, type Usage } from "@/lib/usage";

const chip = "chip inline-flex h-11 cursor-pointer items-center gap-2 rounded-full border border-input-line bg-surface px-4 text-sm font-medium";

const USAGE_ICONS: Record<Usage, (p: { size?: number }) => React.JSX.Element> = {
  Gaming: GamepadIcon,
  Editing: FilmIcon,
  StudyWork: BookIcon,
  Streaming: BroadcastIcon
};

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
    <div id="recommend" className="flex scroll-mt-4 flex-1 basis-[420px] flex-col gap-4 rounded-panel border border-line bg-surface p-6 shadow-soft md:p-8">
      <div className="flex flex-col gap-1">
        <h2 className="m-0 text-[28px] font-bold">{t("title")}</h2>
        <p className="m-0 text-[15px] text-ink-muted">{t("eyebrow")}</p>
      </div>
      <fieldset className="m-0 border-0 p-0">
        <legend className="mb-2.5 text-[15px] font-bold">{t("usageLegend")}</legend>
        <div className="flex flex-wrap gap-2">
          {USAGES.map((entry) => {
            const UsageIcon = USAGE_ICONS[entry];
            return (
              <button key={entry} type="button" aria-pressed={usage === entry} onClick={() => setUsage(usage === entry ? null : entry)} className={chip}>
                <UsageIcon size={18} />
                {tu(entry)}
              </button>
            );
          })}
        </div>
      </fieldset>
      <fieldset className="m-0 border-0 p-0">
        <legend className="mb-2.5 text-[15px] font-bold">{t("budgetLegend")}</legend>
        <div className="flex flex-wrap gap-2">
          {BUDGETS.map((entry) => (
            <button key={entry} type="button" aria-pressed={budget === entry} onClick={() => setBudget(budget === entry ? null : entry)} className={chip}>
              {budget === entry ? <CheckIcon size={16} /> : null}
              {tb(entry)}
            </button>
          ))}
        </div>
      </fieldset>
      <a
        {...NEW_TAB} href={whatsAppUrl(whatsApp.number, text)}
        className="press mt-1 flex h-[50px] items-center gap-2 self-start rounded-full bg-whatsapp px-6 font-bold text-white shadow-[0_10px_24px_-10px_rgba(31,168,85,.6)] transition-[filter] hover:brightness-105"
      >
        <WhatsAppIcon size={20} />
        {t("send")}
      </a>
    </div>
  );
}
