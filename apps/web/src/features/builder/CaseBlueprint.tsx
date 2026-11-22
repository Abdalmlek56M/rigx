"use client";

import type { ReactNode } from "react";
import { useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { Card } from "@/components/ui/Card";
import { CaseIcon } from "@/components/ui/icons";
import { BLUEPRINT_HEIGHT, BLUEPRINT_WIDTH, CASE_OUTLINE, SLOT_DRAWINGS, type Shape } from "./blueprint-shapes";
import { blueprintSlots } from "./blueprint-state";

interface CaseBlueprintProps {
  /** The slots the store has, with their display names. */
  slots: readonly { slot: string; name: string }[];
  parts: readonly Schemas["BuilderPartView"][];
  issues: readonly Schemas["BuilderIssue"][];
  active: string | null;
  onSelect: (slot: string) => void;
}

function draw(shape: Shape, key: number, props: Record<string, string | undefined>): ReactNode {
  if (shape.kind === "rect") {
    return <rect key={key} x={shape.x} y={shape.y} width={shape.width} height={shape.height} rx={shape.radius} data-solid={shape.solid ? "true" : undefined} {...props} />;
  }
  if (shape.kind === "circle") return <circle key={key} cx={shape.cx} cy={shape.cy} r={shape.r} {...props} />;
  return <path key={key} d={shape.d} {...props} />;
}

/**
 * The case as a drawing: every slot of the builder is a part inside it, an outline until a part is chosen and filled with the accent once
 * it is. The slot being chosen glows; a part that clashes with another is tinted. A click on a part chooses its slot (the slot list
 * stays the keyboard way, so the drawing is not focusable). On a phone the drawing is small and sits beside the count.
 */
export function CaseBlueprint({ slots, parts, issues, active, onSelect }: CaseBlueprintProps) {
  const t = useTranslations("builder.blueprint");
  const names = Object.fromEntries(slots.map((entry) => [entry.slot, entry.name]));
  const drawn = blueprintSlots(slots.map((entry) => entry.slot), parts, issues, active);
  const filled = drawn.filter((entry) => entry.filled).length;

  return (
    <Card title={t("title")} icon={CaseIcon} padding="sm">
      <div className="flex items-center justify-center gap-5 lg:flex-col lg:gap-3">
        <svg
          role="img"
          aria-label={t("label", { count: filled, total: drawn.length })}
          viewBox={`0 0 ${BLUEPRINT_WIDTH} ${BLUEPRINT_HEIGHT}`}
          className="h-auto w-[132px] shrink-0 lg:w-full lg:max-w-[240px]"
        >
          {draw(CASE_OUTLINE, -1, { className: "bp-case" })}
          {drawn.map((entry) => {
            const drawing = SLOT_DRAWINGS[entry.slot];
            if (!drawing) return null;
            const state = entry.filled ? "filled" : "empty";
            const name = names[entry.slot] ?? entry.slot;
            return (
              <g key={entry.slot} onClick={() => onSelect(entry.slot)}>
                <title>{entry.model ? t("partFilled", { slot: name, model: entry.model }) : t("partEmpty", { slot: name })}</title>
                {drawing.parts.map((shape, index) =>
                  draw(shape, index, { className: "bp-part", "data-state": state, "data-active": entry.active ? "true" : undefined, "data-issue": entry.issue === "none" ? undefined : entry.issue })
                )}
                {drawing.details.map((shape, index) => draw(shape, 100 + index, { className: "bp-detail", "data-state": state }))}
              </g>
            );
          })}
        </svg>
        <span dir="ltr" className="font-mono text-lg font-bold text-ink-muted">{filled} / {drawn.length}</span>
      </div>
    </Card>
  );
}
