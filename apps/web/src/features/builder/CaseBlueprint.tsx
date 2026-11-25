"use client";

import { useId, type CSSProperties, type ReactNode } from "react";
import { useLocale, useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { Card } from "@/components/ui/Card";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { CaseIcon, WifiIcon } from "@/components/ui/icons";
import { joinValues } from "@/lib/format";
import { BLUEPRINT_HEIGHT, BLUEPRINT_WIDTH, type Shape } from "./blueprint-geometry";
import { GLASS, slotDrawings } from "./blueprint-shapes";
import { blueprintSlots, litPieces, type BlueprintSlot } from "./blueprint-state";
import { pcLayout, type PcLayout } from "./pc-layout";
import { takesQuantity } from "./slot-limits";
import { useTilt } from "./use-tilt";

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

const BOARD_NAMES = { atx: "ATX", matx: "Micro-ATX", itx: "Mini-ITX" } as const;

/** How many pieces of a slot are in the build: memory counts sticks (a kit of two is two), every other slot its quantity. */
const piecesOf = (entry: BlueprintSlot, layout: PcLayout) => (entry.slot === "RAM" ? layout.sticks : entry.count);

/** The name of a slot on the drawing: "رامات ×2" when several pieces of a quantity slot are chosen. */
function slotLabel(name: string, entry: BlueprintSlot, layout: PcLayout): string {
  const pieces = piecesOf(entry, layout);
  return pieces > 1 && takesQuantity(entry.slot) ? `${name} ×${pieces}` : name;
}

/**
 * The PC seen through its tempered-glass side panel: a dark steel case with its back wall, cable grommets and fan frames; the board and every
 * part in its real place; the glass on a layer of its own over them. A part that is not chosen is a faint outline on the back wall; a chosen
 * one is drawn in its real colours with an orange light (fan rings, memory and card strips) and flares once as it is added. Fans, drives and
 * sticks of memory appear one by one as many as are chosen, and the board, the card's fans, the cooler and the antennas follow the parts'
 * specs (`pcLayout`). The slot being chosen glows; a part that clashes with another is tinted. The picture turns a little with the mouse or
 * a sideways drag (`useTilt`), the glass moving a touch more than the parts. A click on a part chooses its slot (the slot list stays the
 * keyboard way, so the drawing is not focusable).
 */
export function CaseBlueprint({ slots, parts, issues, active, onSelect }: CaseBlueprintProps) {
  const t = useTranslations("builder.blueprint");
  const locale = useLocale();
  const id = useId();
  const names = Object.fromEntries(slots.map((entry) => [entry.slot, entry.name]));
  const layout = pcLayout(parts);
  const drawings = slotDrawings(layout);
  const drawn = blueprintSlots(slots.map((entry) => entry.slot), parts, issues, active);
  const filled = drawn.filter((entry) => entry.filled).length;
  const tilt = useTilt<HTMLDivElement>();
  const style = {
    "--bp-metal": `url(#${id}-metal)`,
    "--bp-part-fill": `url(#${id}-part)`,
    "--bp-pcb": `url(#${id}-pcb)`,
    "--bp-pane": `url(#${id}-pane)`
  } as CSSProperties;
  const viewBox = `0 0 ${BLUEPRINT_WIDTH} ${BLUEPRINT_HEIGHT}`;

  return (
    <Card
      title={t("title")}
      icon={CaseIcon}
      padding="sm"
      actions={<span dir="ltr"><StatusBadge tone={filled === drawn.length && filled > 0 ? "success" : "neutral"}>{filled} / {drawn.length}</StatusBadge></span>}
    >
      <div className="flex flex-col items-center justify-center gap-3">
        <div
          ref={tilt.ref}
          className="bp-stage w-full max-w-[320px]"
          style={style}
          onPointerDown={tilt.onPointerDown}
          onPointerMove={tilt.onPointerMove}
          onPointerLeave={tilt.onPointerLeave}
          onPointerUp={tilt.onPointerUp}
          onPointerCancel={tilt.onPointerCancel}
        >
        <div className="bp-rig">
        <svg aria-hidden="true" viewBox={viewBox} className="bp-svg bp-layer bp-layer-back">
          <defs>
            <linearGradient id={`${id}-metal`} x1="0" y1="0" x2="1" y2="1">
              <stop offset="0" stopColor="#3a3f47" />
              <stop offset="0.55" stopColor="#202329" />
              <stop offset="1" stopColor="#14161a" />
            </linearGradient>
            <linearGradient id={`${id}-part`} x1="0" y1="0" x2="0" y2="1">
              <stop offset="0" stopColor="#4a5059" />
              <stop offset="1" stopColor="#262a31" />
            </linearGradient>
            <linearGradient id={`${id}-pcb`} x1="0" y1="0" x2="1" y2="1">
              <stop offset="0" stopColor="#262b33" />
              <stop offset="1" stopColor="#15181d" />
            </linearGradient>
            <linearGradient id={`${id}-pane`} x1="0" y1="0" x2="1" y2="1">
              <stop offset="0" stopColor="#dfe9f3" stopOpacity="0.16" />
              <stop offset="0.5" stopColor="#dfe9f3" stopOpacity="0.04" />
              <stop offset="1" stopColor="#dfe9f3" stopOpacity="0.1" />
            </linearGradient>
          </defs>
          <rect x={40} y={361} width={240} height={8} rx={4} className="bp-floor" />
        </svg>
        <svg
          role="img"
          aria-label={t("label", { count: filled, total: drawn.length })}
          viewBox={viewBox}
          className="bp-svg bp-layer bp-layer-parts"
        >
          {drawn.map((entry) => {
            const drawing = drawings[entry.slot];
            if (!drawing) return null;
            const name = names[entry.slot] ?? entry.slot;
            const lit = litPieces(drawing, piecesOf(entry, layout));
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
                  <text x={drawing.label.x} y={drawing.label.y} textAnchor="middle" className="bp-label">{slotLabel(name, entry, layout)}</text>
                ) : null}
              </g>
            );
          })}
        </svg>
        <svg aria-hidden="true" viewBox={viewBox} className="bp-svg bp-layer bp-layer-glass">
          {GLASS.map((shape, at) => draw(shape, `glass-${at}`, { className: "bp-glass" }))}
        </svg>
        </div>
        </div>
        {parts.some((part) => part.slot === "BOARD") ? (
          <ul aria-label={t("boardFacts")} className="m-0 flex list-none flex-wrap items-center justify-center gap-1.5 p-0 text-xs font-semibold">
            <li dir="ltr" className="rounded-full bg-paper px-2.5 py-1">{BOARD_NAMES[layout.board]}</li>
            <li className="rounded-full bg-paper px-2.5 py-1">{t("ramSlots", { used: layout.sticks, total: layout.ramSlots })}</li>
            {layout.wifi ? (
              <li className="inline-flex items-center gap-1 rounded-full bg-orange-soft px-2.5 py-1 text-orange-text">
                <span aria-hidden="true" className="flex"><WifiIcon size={13} /></span>
                {t("wifi")}
              </li>
            ) : null}
          </ul>
        ) : null}
        <ul className="m-0 flex list-none items-center justify-center gap-4 p-0 text-xs text-ink-muted">
          <li className="flex items-center gap-1.5"><span aria-hidden="true" className="bp-key" data-state="filled" />{t("legendOn")}</li>
          <li className="flex items-center gap-1.5"><span aria-hidden="true" className="bp-key" data-state="empty" />{t("legendOff")}</li>
        </ul>
      </div>
    </Card>
  );
}
