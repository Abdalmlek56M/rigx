"use client";

import { useLocale, useTranslations } from "next-intl";
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
    <li className={`flex flex-wrap items-center gap-3.5 rounded-card border p-3.5 ${chosen ? "border-orange bg-orange/10" : "border-line bg-surface"}`}>
      <div className="flex h-14 w-[84px] shrink-0 items-center justify-center overflow-hidden rounded bg-sand text-muted">
        {group.thumbnailStorageKey ? (
          // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
          <img src={mediaUrl(group.thumbnailStorageKey)} alt="" width={84} height={56} loading="lazy" className="h-full w-full object-cover" />
        ) : (
          <ImagePlaceholderIcon size={32} />
        )}
      </div>
      <div className="flex min-w-0 flex-1 basis-[200px] flex-col gap-1">
        <span dir="ltr" className="text-start font-bold [unicode-bidi:isolate]">{group.modelName}</span>
        {warranties.length > 0 ? <span className="text-xs text-ink-muted">{warranties.join(" · ")}</span> : null}
        {reasons.map((reason) => (
          <span key={reason} className="text-xs text-error">{reason}</span>
        ))}
      </div>
      <div role="radiogroup" aria-label={t("conditionGroup", { model: group.modelName })} className="flex flex-wrap gap-2">
        {choices.map((choice) => (
          <button
            key={choice.code}
            type="button"
            role="radio"
            aria-checked={choice.selected}
            aria-disabled={choice.block !== null}
            onClick={() => (choice.block === null ? onPick(choice.code) : undefined)}
            className={`flex h-[52px] min-w-32 flex-col items-center justify-center rounded-control px-3.5 ${
              choice.selected
                ? "border-2 border-carbon bg-carbon text-on-carbon"
                : choice.block
                  ? "cursor-not-allowed border border-dashed border-input-line bg-paper text-muted"
                  : "border-[1.5px] border-ink bg-surface"
            }`}
          >
            <span className={`text-xs font-bold ${choice.block ? "line-through" : ""}`}>{tc(choice.condition)}</span>
            <span className={`text-sm font-bold ${choice.block ? "line-through" : ""}`}>
              {choice.block === "unavailable" ? t("unavailable") : choice.block === "incompatible" ? t("incompatible") : formatEgp(choice.price, locale)}
            </span>
          </button>
        ))}
      </div>
    </li>
  );
}
