"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { Button, ButtonLink } from "@/components/ui/Button";
import { WhatsAppButton } from "@/components/ui/WhatsAppButton";
import { CartIcon, CheckIcon, CopyIcon, PencilIcon } from "@/components/ui/icons";
import { fillTemplate, whatsAppUrl } from "@/lib/contact";
import { formatEgp } from "@/lib/format";
import { useCart } from "@/features/cart/cart-context";
import { IssueList } from "./IssueList";
import { cartLinesOf, partsSummaryText, powerShare } from "./builder-summary";

interface SharedBuildSummaryProps {
  token: string;
  evaluation: Schemas["BuilderEvaluation"];
  slotNames: Readonly<Record<string, string>>;
  specNames: Readonly<Record<string, string>>;
  whatsApp: { number: string | null; template: string };
  /** The public link of this page for the WhatsApp message ("" when the store has no domain set). */
  pageUrl: string;
}

const label = "text-[#A9ABB0]";

/** The total, the compatibility check and what a visitor can do with someone else's build: order it, change it, ask about it, copy it. */
export function SharedBuildSummary({ token, evaluation, slotNames, specNames, whatsApp, pageUrl }: SharedBuildSummaryProps) {
  const t = useTranslations("builder.summary");
  const ta = useTranslations("builder.actions");
  const ts = useTranslations("sharedBuild");
  const tc = useTranslations("common.condition");
  const locale = useLocale();
  const { add, open } = useCart();
  const [copied, setCopied] = useState(false);
  const conflicts = evaluation.issues.some((issue) => issue.level === "Conflict");
  const status = conflicts ? "conflict" : evaluation.issues.length > 0 ? "warning" : "ok";
  const share = powerShare(evaluation);
  const message = [
    fillTemplate(whatsApp.template, { item: ta("whatsAppItem"), price: formatEgp(evaluation.total, locale), link: pageUrl }) || ta("whatsAppFallback"),
    partsSummaryText(evaluation.parts, slotNames, tc)
  ].join("\n");

  async function copyLink() {
    try {
      await navigator.clipboard.writeText(window.location.href);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 2000);
    } catch {
      // The clipboard can be blocked; the address bar still has the link.
    }
  }

  return (
    <aside aria-label={t("label")} className="flex flex-col gap-4 rounded-card surface-strong p-5 lg:sticky lg:top-24">
      <div className="flex flex-col gap-2 text-sm">
        <div className="flex justify-between gap-2.5">
          <span className={label}>{t("compatibility")}</span>
          <span className={`font-bold ${status === "conflict" ? "text-[#FF8A7A]" : status === "warning" ? "text-[#FFC94D]" : "text-[#3FD08A]"}`}>
            {t(`status.${status}`)}
          </span>
        </div>
        <div className="flex justify-between gap-2.5">
          <span className={label}>{t("power")}</span>
          <span dir="ltr" className={`font-mono ${share !== null && share > 1 ? "text-[#FF8A7A]" : share !== null && share > 0.8 ? "text-[#FFC94D]" : ""}`}>
            {evaluation.powerEstimateWatts != null ? `~ ${Math.round(evaluation.powerEstimateWatts)}W` : "—"}
            {evaluation.psuCapacityWatts != null ? ` / ${Math.round(evaluation.psuCapacityWatts)}W` : ""}
          </span>
        </div>
      </div>
      <IssueList issues={evaluation.issues} slotNames={slotNames} specNames={specNames} />
      <div className="flex items-baseline justify-between border-t border-dashed border-header-line pt-3.5">
        <span>{t("total")}</span>
        <span className="text-2xl font-bold">{formatEgp(evaluation.total, locale)}</span>
      </div>
      <Button
        variant="accent"
        icon={CartIcon}
        disabled={!evaluation.canOrder}
        onClick={() => {
          for (const line of cartLinesOf(evaluation)) add(line);
          open();
        }}
        className="w-full"
      >
        {ta("addAll")}
      </Button>
      {!evaluation.canOrder ? <span className="text-xs leading-6 text-[#FFC94D]">{ta("cannotOrder")}</span> : null}
      <ButtonLink href={`/builder?saved=${encodeURIComponent(token)}`} variant="secondary" icon={PencilIcon} className="w-full">{ts("edit")}</ButtonLink>
      {whatsApp.number ? (
        <WhatsAppButton href={whatsAppUrl(whatsApp.number, message)} variant="outline" className="w-full">{ta("whatsApp")}</WhatsAppButton>
      ) : null}
      <Button variant="secondary" icon={copied ? CheckIcon : CopyIcon} onClick={() => void copyLink()} className="w-full">
        {copied ? ts("copied") : ta("copyLink")}
      </Button>
      <span className="text-xs leading-6 text-footer-text">{ta("note")}</span>
    </aside>
  );
}
