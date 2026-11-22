"use client";

import { useEffect, useRef, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { ApiError, unwrap, type Schemas } from "@rigx/api-client";
import { Button, ButtonLink } from "@/components/ui/Button";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { Logo } from "@/components/ui/Logo";
import { ArrowBackIcon, PrinterIcon } from "@/components/ui/icons";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";
import { parseCondition } from "@/lib/condition";
import { formatDate, formatEgp } from "@/lib/format";
import { parseUsage } from "@/lib/usage";
import { QuoteImageButton } from "./QuoteImageButton";
import { builderPath, partsOf, type PartInput } from "./builder-state";

type Quote = Schemas["QuoteDocument"];
type Outcome =
  | { status: "ready"; document: Quote; parts: PartInput[]; slotByCode: ReadonlyMap<string, string> }
  | { status: "unavailable" }
  | { status: "failed" };

const CONFLICT = 409;
const DESIGN_WIDTH = 1080;
const DESIGN_HEIGHT = 1350;

/** Prices a build into a quote: from explicit parts or from a saved build (its name and usage carry over). */
async function loadQuote(parts: PartInput[], saved: string | null): Promise<Outcome> {
  try {
    const api = browserApi();
    const source = saved
      ? await required(unwrap(api.GET("/api/v1/builder/saved/{token}", { params: { path: { token: saved } } })))
      : null;
    const quoteParts = source ? partsOf(source.evaluation) : parts;
    const [document, evaluation] = await Promise.all([
      required(unwrap(api.POST("/api/v1/builder/quote", { body: { name: source?.name ?? null, usage: source?.usage ?? null, parts: quoteParts } }))),
      required(unwrap(api.POST("/api/v1/builder/check", { body: { parts: quoteParts } })))
    ]);
    return { status: "ready", document, parts: quoteParts, slotByCode: new Map(evaluation.parts.map((part) => [part.item.code, part.slot])) };
  } catch (error) {
    return error instanceof ApiError && (error.status === CONFLICT || error.status === 404) ? { status: "unavailable" } : { status: "failed" };
  }
}

/** Scale that fits the fixed-size card into the width it has (never above 1). */
function useFitScale() {
  const frame = useRef<HTMLDivElement>(null);
  const [scale, setScale] = useState(1);

  useEffect(() => {
    const element = frame.current;
    if (!element) return;
    const observer = new ResizeObserver(() => setScale(Math.min(1, element.clientWidth / DESIGN_WIDTH)));
    observer.observe(element);
    return () => observer.disconnect();
  }, []);

  return { frame, scale };
}

interface QuoteSheetProps {
  parts: PartInput[];
  saved: string | null;
  slotNames: Readonly<Record<string, string>>;
  /** The store's domain, shown on the card ("" when not set). */
  domain: string;
}

/** The quote as a dark share card (1080×1350, scaled to fit): no invoice number and no customer data; print or save as PDF from the browser. */
export function QuoteSheet({ parts, saved, slotNames, domain }: QuoteSheetProps) {
  const t = useTranslations("builder.quote");
  const tu = useTranslations("usage");
  const locale = useLocale();
  const { frame, scale } = useFitScale();
  const card = useRef<HTMLElement>(null);
  const [outcome, setOutcome] = useState<Outcome | null>(null);

  useEffect(() => {
    let cancelled = false;
    loadQuote(parts, saved).then((result) => {
      if (!cancelled) setOutcome(result);
    });
    return () => {
      cancelled = true;
    };
  }, [parts, saved]);

  if (outcome === null) return <p role="status" className="px-4 py-16 text-center text-ink-muted">{t("loading")}</p>;
  if (outcome.status !== "ready") {
    return (
      <div role="alert" className="mx-auto flex max-w-[640px] flex-col items-center gap-3 px-4 py-16 text-center">
        <b className="text-lg">{t(outcome.status === "unavailable" ? "unavailable" : "failed")}</b>
        <ButtonLink href="/builder" variant="secondary" icon={ArrowBackIcon}>{t("backToBuilder")}</ButtonLink>
      </div>
    );
  }

  const { quote } = outcome.document;
  const usage = parseUsage(quote.usage);
  const count = quote.lines.reduce((sum, line) => sum + line.quantity, 0);
  const meta = usage ? t("metaWithUsage", { usage: tu(usage), count }) : t("meta", { count });
  const modelAlign = locale === "ar" ? "text-right" : "text-left";

  return (
    <div data-print-document className="mx-auto flex max-w-[1080px] flex-col gap-4 px-4 py-8 md:py-12 print:max-w-none print:p-0">
      <div className="flex flex-wrap items-center justify-between gap-3 print:hidden">
        <ButtonLink href={builderPath(outcome.parts)} variant="secondary" icon={ArrowBackIcon}>{t("backToBuilder")}</ButtonLink>
        <QuoteImageButton card={card} width={DESIGN_WIDTH} height={DESIGN_HEIGHT} fileLabel={quote.title ?? ""} />
        <Button type="button" variant="primary" icon={PrinterIcon} onClick={() => window.print()}>{t("print")}</Button>
      </div>
      <div ref={frame} data-quote-frame className="relative w-full" style={{ height: DESIGN_HEIGHT * scale }}>
        <article
          ref={card}
          data-quote-card
          className="absolute top-0 flex origin-top-left flex-col gap-8 overflow-hidden bg-[#15171B] p-16 text-[#F4F2ED]"
          style={{
            left: 0,
            width: DESIGN_WIDTH,
            height: DESIGN_HEIGHT,
            transform: `scale(${scale})`,
            printColorAdjust: "exact",
            backgroundImage: "linear-gradient(rgba(244,242,237,.035) 1px, transparent 1px), linear-gradient(90deg, rgba(244,242,237,.035) 1px, transparent 1px)",
            backgroundSize: "54px 54px"
          }}
        >
          <div aria-hidden="true" className="absolute -bottom-[60px] -left-[120px] -rotate-[24deg] opacity-5">
            <Logo width={900} />
          </div>
          <div className="relative flex items-center justify-between">
            <Logo width={200} />
            <span dir="ltr" className="font-mono text-lg tracking-[2px] text-orange">BUILD QUOTE</span>
          </div>
          <div className="relative flex flex-col gap-2.5">
            <span className="text-[22px] text-[#A9ABB0]">{t("label")}</span>
            <h1 dir="auto" className="m-0 text-[56px] leading-[1.2] font-bold [unicode-bidi:isolate]">{quote.title || t("label")}</h1>
            <span className="text-[22px] text-[#BFC1C5]">{meta}</span>
          </div>
          <ul className="relative m-0 flex list-none flex-col border-t border-[#2E3036] p-0">
            {quote.lines.map((line) => {
              const condition = parseCondition(line.condition);
              const slot = line.code ? outcome.slotByCode.get(line.code) : undefined;
              return (
                <li key={`${line.code}-${line.name}`} className="grid grid-cols-[170px_1fr_auto_170px] items-center gap-[18px] border-b border-[#2E3036] py-[18px]">
                  <span className="text-xl text-[#A9ABB0]">{slot ? (slotNames[slot] ?? slot) : ""}</span>
                  <span dir="ltr" className={`text-2xl font-bold [unicode-bidi:isolate] ${modelAlign}`}>
                    {line.name}{line.quantity > 1 ? ` ×${line.quantity}` : ""}
                  </span>
                  {condition ? <ConditionBadge condition={condition} /> : <span />}
                  <span dir="ltr" className={`text-[22px] ${locale === "ar" ? "text-left" : "text-right"}`}>{formatEgp(line.lineTotal, locale)}</span>
                </li>
              );
            })}
          </ul>
          <div className="relative mt-auto flex items-end justify-between">
            <div className="flex flex-col gap-1.5">
              <span className="text-[22px] text-[#A9ABB0]">{t("total")}</span>
              <span className="text-[64px] leading-none font-bold text-orange">{formatEgp(quote.total, locale)}</span>
              <span className="text-lg text-[#A9ABB0]">{t("pricedOn", { date: formatDate(quote.pricedAtUtc, locale) })}</span>
            </div>
            <div className="flex flex-col items-end gap-2">
              {domain ? <span dir="ltr" className="font-mono text-2xl">{domain}</span> : null}
              <span className="text-lg text-[#A9ABB0]">{t("cta")}</span>
            </div>
          </div>
        </article>
      </div>
    </div>
  );
}
