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
import { BuildFacts } from "./BuildFacts";
import { SaveBuildButton } from "./SaveBuildButton";
import { cartLinesOf, partsSummaryText } from "./builder-summary";

interface SharedBuildSummaryProps {
  token: string;
  evaluation: Schemas["BuilderEvaluation"];
  slotNames: Readonly<Record<string, string>>;
  specNames: Readonly<Record<string, string>>;
  whatsApp: { number: string | null; template: string };
  /** The public link of this page for the WhatsApp message ("" when the store has no domain set). */
  pageUrl: string;
  /** False for the build's own owner: it is already in their account. */
  canSave: boolean;
}

/** The total, the compatibility check and what a visitor can do with someone else's build: order it, change it, ask about it, copy it. */
export function SharedBuildSummary({ token, evaluation, slotNames, specNames, whatsApp, pageUrl, canSave }: SharedBuildSummaryProps) {
  const t = useTranslations("builder.summary");
  const ta = useTranslations("builder.actions");
  const ts = useTranslations("sharedBuild");
  const tc = useTranslations("common.condition");
  const locale = useLocale();
  const { add, open } = useCart();
  const [copied, setCopied] = useState(false);
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
    <aside aria-label={t("label")} className="flex flex-col gap-4 rounded-card border border-line bg-surface p-5 shadow-soft lg:sticky lg:top-24">
      <BuildFacts evaluation={evaluation} slotNames={slotNames} specNames={specNames} />
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
      {!evaluation.canOrder ? <span className="text-xs leading-6 text-orange-text">{ta("cannotOrder")}</span> : null}
      {canSave ? <SaveBuildButton token={token} /> : null}
      <ButtonLink href={`/builder?saved=${encodeURIComponent(token)}`} variant="secondary" icon={PencilIcon} className="w-full">{ts("edit")}</ButtonLink>
      {whatsApp.number ? (
        <WhatsAppButton href={whatsAppUrl(whatsApp.number, message)} variant="outline" className="w-full">{ta("whatsApp")}</WhatsAppButton>
      ) : null}
      <Button variant="secondary" icon={copied ? CheckIcon : CopyIcon} onClick={() => void copyLink()} className="w-full">
        {copied ? ts("copied") : ta("copyLink")}
      </Button>
      <span className="text-xs leading-6 text-ink-muted">{ta("note")}</span>
    </aside>
  );
}
