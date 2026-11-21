"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { BUDGETS, type Budget } from "@/features/builds/build-query";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { BookIcon, BroadcastIcon, CheckIcon, FilmIcon, GamepadIcon, HelpIcon } from "@/components/ui/icons";
import { WhatsAppButton } from "@/components/ui/WhatsAppButton";
import { fillTemplate, whatsAppUrl } from "@/lib/contact";
import { USAGES, type Usage } from "@/lib/usage";

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
    <Card id="recommend" icon={HelpIcon} title={t("title")} padding="lg" className="flex-1 basis-[420px] scroll-mt-4 gap-4">
      <p className="m-0 text-[15px] text-ink-muted">{t("eyebrow")}</p>
      <fieldset className="m-0 border-0 p-0">
        <legend className="mb-2.5 text-xs font-bold">{t("usageLegend")}</legend>
        <div className="flex flex-wrap gap-2">
          {USAGES.map((entry) => {
            const UsageIcon = USAGE_ICONS[entry];
            return (
              <Button
                key={entry}
                type="button"
                size="sm"
                variant={usage === entry ? "primary" : "secondary"}
                icon={UsageIcon}
                aria-pressed={usage === entry}
                onClick={() => setUsage(usage === entry ? null : entry)}
              >
                {tu(entry)}
              </Button>
            );
          })}
        </div>
      </fieldset>
      <fieldset className="m-0 border-0 p-0">
        <legend className="mb-2.5 text-xs font-bold">{t("budgetLegend")}</legend>
        <div className="flex flex-wrap gap-2">
          {BUDGETS.map((entry) => (
            <Button
              key={entry}
              type="button"
              size="sm"
              variant={budget === entry ? "primary" : "secondary"}
              icon={budget === entry ? CheckIcon : undefined}
              aria-pressed={budget === entry}
              onClick={() => setBudget(budget === entry ? null : entry)}
            >
              {tb(entry)}
            </Button>
          ))}
        </div>
      </fieldset>
      <WhatsAppButton href={whatsAppUrl(whatsApp.number, text)} className="mt-1 self-start">{t("send")}</WhatsAppButton>
    </Card>
  );
}
