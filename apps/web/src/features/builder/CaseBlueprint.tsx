"use client";

import { useId, type CSSProperties, type ReactNode } from "react";
import { useLocale, useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { Card } from "@/components/ui/Card";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { CaseIcon } from "@/components/ui/icons";
import { joinValues } from "@/lib/format";
import { BLUEPRINT_HEIGHT, BLUEPRINT_WIDTH, type Shape } from "./blueprint-geometry";
import { SLOT_DRAWINGS } from "./blueprint-shapes";
import { blueprintSlots, litPieces, type BlueprintSlot } from "./blueprint-state";
import { takesQuantity } from "./slot-limits";

interface CaseBlueprintProps {
  /** The slots the store has, with their display names. */
  slots: readonly { slot: string; name: string }[];
  parts: readonly Schemas["BuilderPartView"][];
  issues: readonly Schemas["BuilderIssue"][];
  active: string | null;
  onSelect: (slot: string) => void;
}

function draw(shape: Shape, key: string, props: Record<string, string | undefined>): ReactNode {
  const common = { "data-role": shape.role, ...props };
  if (shape.kind === "rect") return <rect key={key} {...common} x={shape.x} y={shape.y} width={shape.width} height={shape.height} rx={shape.radius} />;
  if (shape.kind === "circle") return <circle key={key} {...common} cx={shape.cx} cy={shape.cy} r={shape.r} />;
  return <path key={key} {...common} d={shape.d} />;
}

/** The name of a slot on the drawing: "رامات ×2" when several pieces of a quantity slot are chosen. */
function slotLabel(name: string, entry: BlueprintSlot): string {
  return entry.count > 1 && takesQuantity(entry.slot) ? `${name} ×${entry.count}` : name;
}

/**
 * The case as a layered drawing: a steel shell with a glass side panel, the board with its slots, and every slot of the builder as a part
 * on it. A part that is not chosen is a dim outline; a chosen one is filled with the accent, glows and carries its name. Fans, drives and
 * sticks of memory light one by one as many as are chosen. The slot being chosen glows; a part that clashes with another is tinted. A click
 * on a part chooses its slot (the slot list stays the keyboard way, so the drawing is not focusable).
 */
export function CaseBlueprint({ slots, parts, issues, active, onSelect }: CaseBlueprintProps) {
  const t = useTranslations("builder.blueprint");
  const locale = useLocale();
  const id = useId();
  const names = Object.fromEntries(slots.map((entry) => [entry.slot, entry.name]));
  const drawn = blueprintSlots(slots.map((entry) => entry.slot), parts, issues, active);
  const filled = drawn.filter((entry) => entry.filled).length;
  const style = { "--bp-on": `url(#${id}-on)`, "--bp-glass": `url(#${id}-glass)` } as CSSProperties;

  return (
    <Card
      title={t("title")}
      icon={CaseIcon}
      padding="sm"
      actions={<span dir="ltr"><StatusBadge tone={filled === drawn.length && filled > 0 ? "success" : "neutral"}>{filled} / {drawn.length}</StatusBadge></span>}
    >
      <div className="flex flex-col items-center justify-center gap-3">
        <svg
          role="img"
          aria-label={t("label", { count: filled, total: drawn.length })}
          viewBox={`0 0 ${BLUEPRINT_WIDTH} ${BLUEPRINT_HEIGHT}`}
          className="bp-svg h-auto w-full max-w-[320px] shrink-0"
          style={style}
        >
          <defs>
            <linearGradient id={`${id}-on`} x1="0" y1="0" x2="0" y2="1">
              <stop offset="0" style={{ stopColor: "var(--orange)", stopOpacity: 0.55 }} />
              <stop offset="1" style={{ stopColor: "var(--orange)", stopOpacity: 0.2 }} />
            </linearGradient>
            <linearGradient id={`${id}-glass`} x1="0" y1="0" x2="1" y2="1">
              <stop offset="0" style={{ stopColor: "var(--ink)", stopOpacity: 0.09 }} />
              <stop offset="1" style={{ stopColor: "var(--ink)", stopOpacity: 0.02 }} />
            </linearGradient>
          </defs>
          {drawn.map((entry) => {
            const drawing = SLOT_DRAWINGS[entry.slot];
            if (!drawing) return null;
            const name = names[entry.slot] ?? entry.slot;
            const lit = litPieces(entry.slot, entry.count);
            return (
              <g key={entry.slot} onClick={() => onSelect(entry.slot)}>
                <title>{entry.filled ? t("partFilled", { slot: name, model: joinValues(entry.models, locale) }) : t("partEmpty", { slot: name })}</title>
                {drawing.pieces.map((piece, index) => {
                  const state = index < lit ? "filled" : "empty";
                  const key = `${entry.slot}-${index}`;
                  return (
                    <g key={key}>
                      {piece.parts.map((shape, at) =>
                        draw(shape, `${key}-p${at}`, {
                          className: "bp-part",
                          "data-slot": entry.slot,
                          "data-state": state,
                          "data-active": entry.active ? "true" : undefined,
                          "data-issue": entry.issue === "none" ? undefined : entry.issue
                        })
                      )}
                      {piece.details.map((shape, at) => draw(shape, `${key}-d${at}`, { className: "bp-detail", "data-slot": entry.slot, "data-state": state }))}
                      {state === "filled" && piece.label ? <text x={piece.label.x} y={piece.label.y} textAnchor="middle" className="bp-label">{name}</text> : null}
                    </g>
                  );
                })}
                {entry.filled && drawing.label ? (
                  <text x={drawing.label.x} y={drawing.label.y} textAnchor="middle" className="bp-label">{slotLabel(name, entry)}</text>
                ) : null}
              </g>
            );
          })}
        </svg>
        <ul className="m-0 flex list-none items-center justify-center gap-4 p-0 text-xs text-ink-muted">
          <li className="flex items-center gap-1.5"><span aria-hidden="true" className="bp-key" data-state="filled" />{t("legendOn")}</li>
          <li className="flex items-center gap-1.5"><span aria-hidden="true" className="bp-key" data-state="empty" />{t("legendOff")}</li>
        </ul>
      </div>
    </Card>
  );
}
