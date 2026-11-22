"use client";

import { useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { Button, ButtonLink } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { EmptyState } from "@/components/ui/EmptyState";
import { WhatsAppButton } from "@/components/ui/WhatsAppButton";
import { CpuIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import { fillTemplate, siteUrl, whatsAppUrl } from "@/lib/contact";
import { OptionPanel } from "./OptionPanel";
import { SlotList, type SlotInfo } from "./SlotList";
import { SummaryPanel } from "./SummaryPanel";
import { partsParam, type BuilderParams } from "./builder-state";
import { useBuilder } from "./use-builder";

interface BuilderAppProps {
  start: BuilderParams;
  slots: SlotInfo[];
  specNames: Record<string, string>;
  whatsApp: { number: string | null; template: string };
  /** The store's domain, for the public link in the WhatsApp message. */
  domain: string | null;
}

const MOBILE_QUERY = "(max-width: 1023px)";

/** The free builder: slots, the active slot's options and the summary. The chosen parts live in the URL. */
export function BuilderApp({ start, slots, specNames, whatsApp, domain }: BuilderAppProps) {
  const t = useTranslations("builder");
  const b = useBuilder(start);
  const [chosenSlot, setChosenSlot] = useState<string | null>(null);
  const panel = useRef<HTMLDivElement>(null);
  const slotNames = Object.fromEntries(slots.map((entry) => [entry.slot, entry.name]));
  const filled = b.evaluation.parts.map((part) => part.slot);
  const nextEmpty = (taken: readonly string[]) => slots.find((entry) => !taken.includes(entry.slot))?.slot ?? null;
  const active = chosenSlot ?? nextEmpty(filled) ?? slots[0]?.slot ?? null;

  function selectSlot(slot: string) {
    setChosenSlot(slot);
    if (window.matchMedia(MOBILE_QUERY).matches) panel.current?.scrollIntoView({ block: "start" });
  }

  if (!b.ready) return <p role="status" className="shell py-16 text-center text-ink-muted">{t("opening")}</p>;
  if (active === null) {
    return (
      <div className="shell py-16">
        <EmptyState
          icon={CpuIcon}
          title={t("noSlots")}
          help={t("noSlotsBody")}
          action={
            <div className="flex flex-wrap justify-center gap-2.5">
              {whatsApp.number ? (
                <WhatsAppButton href={whatsAppUrl(whatsApp.number, fillTemplate(whatsApp.template, {}))}>{t("noSlotsWhatsApp")}</WhatsAppButton>
              ) : null}
              <ButtonLink href="/builds" variant="secondary">{t("noSlotsBuilds")}</ButtonLink>
            </div>
          }
        />
      </div>
    );
  }

  return (
    <div className="flex shell flex-col gap-4 pb-32 pt-2 lg:pb-16">
      {b.openFailed ? <Card padding="sm"><p className="m-0 text-sm">{t("openFailed")}</p></Card> : null}
      {b.unmatched.length > 0 ? (
        <Card padding="sm" className="gap-2">
          <b className="text-sm">{t("unmatched")}</b>
          <ul className="m-0 flex list-none flex-col gap-1 p-0 text-sm text-ink-muted">
            {b.unmatched.map((part) => (
              <li key={`${part.modelName}-${part.condition}`} dir="auto" className="[unicode-bidi:isolate]">{part.modelName}</li>
            ))}
          </ul>
        </Card>
      ) : null}
      {b.evaluation.missingCodes.length > 0 ? (
        <Card padding="sm" className="gap-3">
          <span className="text-sm">{t("missing")}</span>
          <div className="flex flex-wrap gap-2">
            {b.evaluation.missingCodes.map((code) => (
              <Button key={code} type="button" variant="secondary" size="sm" onClick={() => b.remove(code)} className="font-mono">
                {code} — {t("removeMissing")}
              </Button>
            ))}
          </div>
        </Card>
      ) : null}
      <div className="grid items-start gap-5 lg:grid-cols-[280px_minmax(0,1fr)_340px]">
        <SlotList slots={slots} parts={b.evaluation.parts} active={active} onSelect={selectSlot} onClear={b.clear} />
        <div ref={panel} className="scroll-mt-4">
          <OptionPanel
            key={active}
            slot={active}
            slotName={slotNames[active] ?? active}
            partsKey={partsParam(b.parts)}
            selectedCodes={new Set(b.parts.map((part) => part.code))}
            filledSlots={filled}
            slotNames={slotNames}
            specNames={specNames}
            onPick={(code) => {
              b.pick(active, code);
              setChosenSlot(nextEmpty([...filled, active]) ?? active);
            }}
          />
        </div>
        <SummaryPanel
          evaluation={b.evaluation}
          parts={b.parts}
          checking={b.checking}
          checkFailed={b.checkFailed}
          slotCount={slots.length}
          slotNames={slotNames}
          specNames={specNames}
          whatsApp={whatsApp}
          shared={b.share}
          fallbackLink={siteUrl(domain, b.path)}
          onCopyLink={b.copyLink}
          name={b.name}
          onNameChange={b.setName}
        />
      </div>
      <p className="m-0 text-sm text-ink-muted">
        {t("help")} <Link href="/#recommend" className="text-orange-text underline hover:text-orange-text-hover">{t("helpLink")}</Link>
      </p>
    </div>
  );
}
