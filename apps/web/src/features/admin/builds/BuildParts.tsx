"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { Link } from "@/i18n/navigation";
import { formatEgp } from "@/lib/format";
import type { Brand } from "../brands/brands-api";
import type { Category } from "../categories/categories-api";
import type { Partner } from "../expenses/expenses-api";
import { partnerShare } from "../stock/stock-fields";
import { AddNewPartPanel } from "./AddNewPartPanel";
import { AddStockPartPanel } from "./AddStockPartPanel";
import { PartEditDialog } from "./PartEditDialog";
import { RemovePartDialog } from "./RemovePartDialog";
import { ReplacePartDialog } from "./ReplacePartDialog";
import type { BuildPart } from "./builds-api";
import type { BuildActionProps } from "./use-build-action";
import { BoxIcon, PencilIcon, PlusIcon, SwapIcon, TrashIcon } from "@/components/ui/icons";
import { Button, IconButton } from "../kit/Button";
import { SectionCard } from "../kit/SectionCard";

export interface BuildLookups {
  tree: readonly Category[];
  brands: readonly Brand[];
  /** Every partner, or `null` when the staff member cannot read partners (the partner column and fields are then left out). */
  partners: readonly Partner[] | null;
  warrantyLabels: readonly string[] | null;
}

type Panel = { kind: "edit" | "replace" | "remove"; part: BuildPart } | { kind: "stock" | "new" } | null;

const CELL = "truncate px-2 py-2.5 align-middle";
const HEAD = "whitespace-nowrap px-2 py-2.5 text-start text-[13px] font-bold text-ink-muted";

/** The build's parts with their money, and the actions on them: edit, replace, remove, add from stock, add a new part. */
export function BuildParts({ canEdit, lookups, ...action }: BuildActionProps & { canEdit: boolean; lookups: BuildLookups }) {
  const t = useTranslations("admin.builds.parts");
  const locale = useLocale();
  const { build } = action;
  const [panel, setPanel] = useState<Panel>(null);
  const close = () => setPanel(null);
  const none = <span className="text-ink-muted">{t("none")}</span>;
  const hasPartner = Boolean(lookups.partners);
  const widths = ["200px", "90px", "105px", "105px", "130px", ...(hasPartner ? ["110px"] : []), "110px", ...(canEdit ? ["132px"] : [])];

  const partnerOf = (part: BuildPart) => lookups.partners?.find((entry) => entry.id === part.partnerId) ?? null;
  const actions = (part: BuildPart) => (
    <span className="flex items-center gap-1">
      <IconButton icon={PencilIcon} label={t("edit")} size="sm" onClick={() => setPanel({ kind: "edit", part })} />
      <IconButton icon={SwapIcon} label={t("replace")} size="sm" onClick={() => setPanel({ kind: "replace", part })} />
      <IconButton icon={TrashIcon} label={t("remove")} size="sm" variant="dangerGhost" onClick={() => setPanel({ kind: "remove", part })} />
    </span>
  );
  const partnerValue = (part: BuildPart) => {
    const partner = partnerOf(part);
    if (!partner) return none;
    return (
      <span className="flex min-w-0 flex-col gap-0.5">
        <span dir="auto" className="truncate [unicode-bidi:isolate]">{partner.name}</span>
        <span className={`text-xs ${part.partnerPercentOverride === null ? "text-ink-muted" : "font-bold text-orange-text"}`}>{t("percent", { percent: partnerShare(partner, part.partnerPercentOverride) })}</span>
      </span>
    );
  };
  const warrantyValue = (part: BuildPart) => (
    <span className="flex min-w-0 flex-col gap-0.5">
      <span className="truncate">{part.warrantyLabel ?? none}</span>
      {part.warrantyOverridden ? <span className="text-xs text-orange-text">{t("ownWarranty")}</span> : null}
    </span>
  );

  const addButtons = (
    <>
      <Button size="sm" icon={BoxIcon} onClick={() => setPanel({ kind: "stock" })}>{t("addFromStock")}</Button>
      <Button size="sm" icon={PlusIcon} onClick={() => setPanel({ kind: "new" })}>{t("addNew")}</Button>
    </>
  );

  return (
    <SectionCard
      title={t("title", { count: build.units.length })}
      actions={canEdit && build.units.length > 0 ? addButtons : null}
    >
      {build.units.length === 0 ? (
        <div className="flex flex-col items-center gap-3 py-8 text-center">
          <span aria-hidden="true" className="flex size-11 items-center justify-center rounded-full bg-paper text-ink-muted"><BoxIcon size={22} /></span>
          <p className="m-0 text-sm font-bold text-ink-muted">{t("empty")}</p>
          {canEdit ? <div className="flex flex-wrap justify-center gap-2">{addButtons}</div> : null}
        </div>
      ) : (
        <>
          <div className="hidden overflow-x-auto lg:block">
            <table className="w-full table-fixed border-collapse text-sm">
              <colgroup>{widths.map((width, index) => <col key={index} style={{ width }} />)}</colgroup>
              <thead className="bg-paper">
                <tr>
                  <th scope="col" className={HEAD}>{t("columns.part")}</th>
                  <th scope="col" className={HEAD}>{t("columns.condition")}</th>
                  <th scope="col" className={`${HEAD} text-end`}>{t("columns.cost")}</th>
                  <th scope="col" className={`${HEAD} text-end`}>{t("columns.price")}</th>
                  <th scope="col" className={HEAD}>{t("columns.warranty")}</th>
                  {hasPartner ? <th scope="col" className={HEAD}>{t("columns.partner")}</th> : null}
                  <th scope="col" className={`${HEAD} text-end`}>{t("columns.allocation")}</th>
                  {canEdit ? <th scope="col" className={`${HEAD} text-end`}>{t("columns.actions")}</th> : null}
                </tr>
              </thead>
              <tbody>
                {build.units.map((part) => (
                  <tr key={part.unitId} className="border-t border-line">
                    <td className={CELL}>
                      <span className="flex min-w-0 flex-col gap-0.5">
                        <span dir="auto" className="truncate font-bold [unicode-bidi:isolate]">{part.name}</span>
                        <Link href={`/admin/inventory?q=${encodeURIComponent(part.stockItemCode)}`} dir="ltr" className="font-mono text-xs text-ink-muted hover:underline">{part.stockItemCode}</Link>
                      </span>
                    </td>
                    <td className={CELL}><ConditionBadge condition={part.condition === "New" ? "New" : "Used"} /></td>
                    <td className={`${CELL} whitespace-nowrap text-end`}>{part.acquisitionCost === null ? none : formatEgp(part.acquisitionCost, locale)}</td>
                    <td className={`${CELL} whitespace-nowrap text-end font-bold`}>{formatEgp(part.partPrice, locale)}</td>
                    <td className={CELL}>{warrantyValue(part)}</td>
                    {lookups.partners ? <td className={CELL}>{partnerValue(part)}</td> : null}
                    <td className={`${CELL} whitespace-nowrap text-end`}>
                      <span className="font-bold">{t("percent", { percent: part.allocationPercent })}</span>
                      {part.allocatedAmount === null ? null : <span className="block text-xs text-ink-muted">{formatEgp(part.allocatedAmount, locale)}</span>}
                    </td>
                    {canEdit ? <td className={`${CELL} text-end`}>{actions(part)}</td> : null}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <ul className="m-0 flex list-none flex-col gap-2 p-0 lg:hidden">
            {build.units.map((part) => (
              <li key={part.unitId} className="flex flex-col gap-3 rounded-control border border-line bg-paper p-3">
                <div className="flex min-w-0 items-start justify-between gap-2">
                  <span className="flex min-w-0 flex-col gap-0.5">
                    <span dir="auto" className="font-bold [unicode-bidi:isolate]">{part.name}</span>
                    <Link href={`/admin/inventory?q=${encodeURIComponent(part.stockItemCode)}`} dir="ltr" className="font-mono text-xs text-ink-muted hover:underline">{part.stockItemCode}</Link>
                  </span>
                  <ConditionBadge condition={part.condition === "New" ? "New" : "Used"} />
                </div>
                <dl className="m-0 grid grid-cols-2 gap-2 text-sm">
                  <div><dt className="text-xs text-ink-muted">{t("columns.cost")}</dt><dd className="m-0 font-bold">{part.acquisitionCost === null ? none : formatEgp(part.acquisitionCost, locale)}</dd></div>
                  <div><dt className="text-xs text-ink-muted">{t("columns.price")}</dt><dd className="m-0 font-bold">{formatEgp(part.partPrice, locale)}</dd></div>
                  <div><dt className="text-xs text-ink-muted">{t("columns.warranty")}</dt><dd className="m-0">{warrantyValue(part)}</dd></div>
                  {lookups.partners ? <div><dt className="text-xs text-ink-muted">{t("columns.partner")}</dt><dd className="m-0">{partnerValue(part)}</dd></div> : null}
                  <div><dt className="text-xs text-ink-muted">{t("columns.allocation")}</dt><dd className="m-0 font-bold">{t("percent", { percent: part.allocationPercent })}{part.allocatedAmount === null ? null : <span className="block text-xs font-normal text-ink-muted">{formatEgp(part.allocatedAmount, locale)}</span>}</dd></div>
                </dl>
                {canEdit ? <div className="flex items-center justify-end gap-1 border-t border-line pt-2">{actions(part)}</div> : null}
              </li>
            ))}
          </ul>
        </>
      )}
      <p className="m-0 text-xs text-ink-muted">{t("hint")}</p>
      {panel?.kind === "stock" ? <AddStockPartPanel {...action} onClose={close} /> : null}
      {panel?.kind === "new" ? <AddNewPartPanel {...action} lookups={lookups} onClose={close} /> : null}
      {panel?.kind === "edit" ? <PartEditDialog key={panel.part.unitId} {...action} part={panel.part} lookups={lookups} onClose={close} /> : null}
      {panel?.kind === "replace" ? <ReplacePartDialog key={panel.part.unitId} {...action} part={panel.part} tree={lookups.tree} onClose={close} /> : null}
      {panel?.kind === "remove" ? <RemovePartDialog key={panel.part.unitId} {...action} part={panel.part} onClose={close} /> : null}
    </SectionCard>
  );
}
