"use client";

import { useEffect, useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { BUDGETS, type Budget } from "@/features/builds/build-query";
import { Button, IconButton } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { ArrowBackIcon, BookIcon, BroadcastIcon, CheckIcon, FilmIcon, GamepadIcon, HelpIcon, PencilIcon, WalletIcon } from "@/components/ui/icons";
import { WhatsAppButton } from "@/components/ui/WhatsAppButton";
import { fillTemplate, whatsAppUrl } from "@/lib/contact";
import { USAGES, type Usage } from "@/lib/usage";
import { RECOMMEND_STEPS, slideDirection, stepBy, type RecommendStep, type SlideDirection } from "./recommend-steps";
import { ChoiceTile } from "@/components/ui/ChoiceTile";

const USAGE_ICONS: Record<Usage, (p: { size?: number }) => React.JSX.Element> = {
  Gaming: GamepadIcon,
  Editing: FilmIcon,
  StudyWork: BookIcon,
  Streaming: BroadcastIcon
};

/** A choice that was just made stays lit this long before the next step slides in. */
const ADVANCE_MS = 280;

/**
 * "اقترح لي تجميعة": a three-step wizard (usage, budget, send). Choosing a card lights it for a moment and slides to the next step; both
 * choices can be skipped. The last step turns the choices into a prefilled WhatsApp message (no API call).
 */
export function RecommendWidget({ whatsApp }: { whatsApp: { number: string; template: string } }) {
  const t = useTranslations("home.recommend");
  const tu = useTranslations("usage");
  const tb = useTranslations("home.recommend.budgets");
  const [step, setStep] = useState<RecommendStep>("usage");
  const [direction, setDirection] = useState<SlideDirection | undefined>(undefined);
  const [usage, setUsage] = useState<Usage | null>(null);
  const [budget, setBudget] = useState<Budget | null>(null);
  const timer = useRef<number | undefined>(undefined);

  useEffect(() => () => window.clearTimeout(timer.current), []);

  function goTo(next: RecommendStep) {
    window.clearTimeout(timer.current);
    setDirection(slideDirection(step, next));
    setStep(next);
  }

  function choose(apply: () => void) {
    apply();
    window.clearTimeout(timer.current);
    timer.current = window.setTimeout(() => goTo(stepBy(step, 1)), ADVANCE_MS);
  }

  const text = [
    fillTemplate(whatsApp.template, {}),
    [t("message.base"), usage && t("message.usage", { usage: tu(usage) }), budget && t("message.budget", { budget: tb(budget) })].filter(Boolean).join(" ")
  ]
    .filter(Boolean)
    .join("\n");
  const current = RECOMMEND_STEPS.indexOf(step);

  return (
    <Card id="recommend" icon={HelpIcon} title={t("title")} padding="lg" className="flex-1 basis-[420px] scroll-mt-4 gap-4">
      <ol aria-label={t("stepsLabel")} className="m-0 flex list-none items-center gap-2 p-0">
        {RECOMMEND_STEPS.map((entry, index) => (
          <li key={entry} aria-current={index === current ? "step" : undefined} className="flex items-center gap-2 text-[13px]">
            <span
              aria-hidden="true"
              className={`flex size-6 items-center justify-center rounded-full text-xs font-bold ${index < current ? "bg-orange text-on-orange" : index === current ? "border-2 border-orange text-orange-text" : "border border-line text-ink-muted"}`}
            >
              {index < current ? <CheckIcon size={13} /> : index + 1}
            </span>
            <span className={index === current ? "font-bold" : "text-ink-muted max-sm:sr-only"}>{t(`steps.${entry}`)}</span>
            {index < RECOMMEND_STEPS.length - 1 ? <span aria-hidden="true" className="h-px w-5 bg-line" /> : null}
          </li>
        ))}
      </ol>

      <div key={step} data-direction={direction} className="wizard-panel flex min-h-[220px] flex-col gap-4">
        {step === "usage" ? (
          <fieldset className="m-0 flex flex-col gap-3 border-0 p-0">
            <legend className="mb-3 text-[15px] text-ink-muted">{t("eyebrow")} <b className="text-ink">{t("usageLegend")}</b></legend>
            <div className="grid grid-cols-2 gap-2.5">
              {USAGES.map((entry) => (
                <ChoiceTile key={entry} label={tu(entry)} icon={USAGE_ICONS[entry]} selected={usage === entry} onSelect={() => choose(() => setUsage(entry))} />
              ))}
            </div>
          </fieldset>
        ) : null}

        {step === "budget" ? (
          <fieldset className="m-0 flex flex-col gap-3 border-0 p-0">
            <legend className="mb-3 text-[15px] font-bold">{t("budgetLegend")}</legend>
            <div className="grid grid-cols-1 gap-2.5 sm:grid-cols-3">
              {BUDGETS.map((entry) => (
                <ChoiceTile key={entry} label={tb(entry)} icon={WalletIcon} selected={budget === entry} onSelect={() => choose(() => setBudget(entry))} />
              ))}
            </div>
          </fieldset>
        ) : null}

        {step === "send" ? (
          <div className="flex flex-col gap-3">
            <p className="m-0 text-[15px] font-bold">{t("sendLegend")}</p>
            <ul className="m-0 flex list-none flex-col gap-2 p-0 text-sm">
              {([
                { key: "usage" as const, label: t("steps.usage"), value: usage ? tu(usage) : t("any") },
                { key: "budget" as const, label: t("steps.budget"), value: budget ? tb(budget) : t("any") }
              ]).map((row) => (
                <li key={row.key} className="flex items-center justify-between gap-3 rounded-card border border-line bg-paper px-3.5 py-2.5">
                  <span className="flex flex-col gap-0.5"><span className="type-label">{row.label}</span><span className="type-value">{row.value}</span></span>
                  <IconButton icon={PencilIcon} label={t("edit", { step: row.label })} size="sm" onClick={() => goTo(row.key)} />
                </li>
              ))}
            </ul>
            <WhatsAppButton href={whatsAppUrl(whatsApp.number, text)} className="self-start">{t("send")}</WhatsAppButton>
          </div>
        ) : null}
      </div>

      <div className="flex items-center justify-between gap-3">
        {current > 0 ? <Button variant="ghost" size="sm" icon={ArrowBackIcon} onClick={() => goTo(stepBy(step, -1))}>{t("back")}</Button> : <span />}
        {step !== "send" ? <Button variant="ghost" size="sm" onClick={() => goTo(stepBy(step, 1))}>{t("skip")}</Button> : null}
      </div>
    </Card>
  );
}
