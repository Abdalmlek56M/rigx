"use client";

import { useLocale, useTranslations } from "next-intl";
import { Button } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { ImagePlaceholderIcon } from "@/components/ui/icons";
import { formatEgp } from "@/lib/format";
import { mediaUrl } from "@/lib/media";
import { describeIssue } from "./issue-text";
import type { Choice, OptionGroup } from "./option-groups";

interface OptionRowProps {
  group: OptionGroup;
  slotNames: Readonly<Record<string, string>>;
  specNames: Readonly<Record<string, string>>;
  onPick: (code: string) => void;
}

/** One model with its جديد and مستعمل choices; an unavailable or incompatible choice is struck through and locked with the reason. */
export function OptionRow({ group, slotNames, specNames, onPick }: OptionRowProps) {
  const t = useTranslations("builder.options");
  const ti = useTranslations("builder.issues");
  const tc = useTranslations("common.condition");
  const locale = useLocale();
  const choices = [group.new, group.used].filter((choice): choice is Choice => choice !== null);
  const chosen = choices.some((choice) => choice.selected);
  const warranties = choices.filter((choice) => choice.warrantyLabel).map((choice) => `${tc(choice.condition)}: ${choice.warrantyLabel}`);
  const reasons = choices.flatMap((choice) =>
    choice.block === "incompatible"
      ? choice.issues
          .filter((issue) => issue.level === "Conflict")
          .slice(0, 1)
          .map((issue) => {
            const text = describeIssue(issue, slotNames, specNames);
            return `${tc(choice.condition)}: ${ti(text.key, text.values)}`;
          })
      : []
  );

  return (
    <Card as="li" padding="sm" className={`@container gap-3 ${chosen ? "border-orange bg-orange/10" : ""}`}>
      <div className="flex flex-col gap-3 @md:flex-row @md:items-center @md:gap-3.5">
        <div className="flex min-w-0 flex-1 items-center gap-3.5">
          <div className="flex aspect-square h-14 w-14 shrink-0 items-center justify-center overflow-hidden rounded-control bg-paper text-muted">
            {group.thumbnailStorageKey ? (
              // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
              <img src={mediaUrl(group.thumbnailStorageKey)} alt="" width={56} height={56} loading="lazy" className="h-full w-full object-contain p-1" />
            ) : (
              <ImagePlaceholderIcon size={32} />
            )}
          </div>
          <div className="flex min-w-0 flex-1 flex-col gap-1">
            <span dir="ltr" className="text-start font-bold [unicode-bidi:isolate]">{group.modelName}</span>
            {warranties.length > 0 ? <span className="text-xs text-ink-muted">{warranties.join(" · ")}</span> : null}
            {reasons.map((reason) => (
              <span key={reason} className="text-xs text-error">{reason}</span>
            ))}
          </div>
        </div>
        <div role="radiogroup" aria-label={t("conditionGroup", { model: group.modelName })} className="grid grid-cols-2 gap-2 @md:flex @md:flex-wrap">
          {choices.map((choice) => (
            <Button
              key={choice.code}
              type="button"
              role="radio"
              aria-checked={choice.selected}
              aria-disabled={choice.block !== null}
              disabled={choice.block !== null}
              variant={choice.selected ? "primary" : "secondary"}
              onClick={() => onPick(choice.code)}
              className={`h-auto min-h-[52px] @md:min-w-32 flex-col gap-0.5 px-3.5 py-2 ${choice.block ? "line-through opacity-60" : ""}`}
            >
              <span className="text-xs font-bold">{tc(choice.condition)}</span>
              <span className="text-sm font-bold">
                {choice.block === "unavailable" ? t("unavailable") : choice.block === "incompatible" ? t("incompatible") : formatEgp(choice.price, locale)}
              </span>
            </Button>
          ))}
        </div>
      </div>
    </Card>
  );
}
