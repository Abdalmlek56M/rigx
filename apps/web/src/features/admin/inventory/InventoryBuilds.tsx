"use client";

import { Fragment, useCallback, useMemo, useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { BoxIcon, ChevronDownIcon, ChevronStartIcon, PencilIcon, UnlinkIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import { formatEgp } from "@/lib/format";
import { useLoad } from "@/lib/hooks/use-load";
import { BuildStatusBadge } from "../builds/BuildStatusBadge";
import { fetchBuilds, transitionBuild, type Build } from "../builds/builds-api";
import { inStockBuilds } from "../builds/build-rules";
import { Button, buttonClass, buttonIconSize, IconButton } from "../kit/Button";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { ErrorPanel } from "../kit/LoadState";
import { SectionCard } from "../kit/SectionCard";
import { useToast } from "../kit/Toast";
import { usePermissions } from "../use-permissions";

const head = "px-2 py-2 text-start text-xs font-bold text-ink-muted";
const cell = "px-2 py-2 align-middle";

/** "التجميعات في المخزون": each build still in stock (draft or published) as one row that opens to its parts, with edit and disassemble. */
export function InventoryBuilds() {
  const t = useTranslations("admin.inventory.builds");
  const locale = useLocale();
  const toast = useToast();
  const { has } = usePermissions();
  const { state, reload } = useLoad(fetchBuilds);
  const [open, setOpen] = useState(true);
  const [expanded, setExpanded] = useState<ReadonlySet<string>>(new Set());
  const [disassembling, setDisassembling] = useState<Build | null>(null);
  const builds = useMemo(() => (state.status === "ready" ? inStockBuilds(state.data) : []), [state]);
  const showCosts = has("Inventory.ViewCosts");

  const toggle = (id: string) => setExpanded((current) => {
    const next = new Set(current);
    if (!next.delete(id)) next.add(id);
    return next;
  });

  const disassemble = useCallback(async (build: Build) => {
    try {
      await transitionBuild(build.id, "disassemble", build.rowVersion);
      toast(t("disassembleDone"));
      reload();
    } catch (error) {
      toast(t("failed"), "error");
      throw error;
    }
  }, [reload, t, toast]);

  if (state.status === "error") return <ErrorPanel message={t("failed")} retryLabel={t("retry")} onRetry={reload} />;
  const count = state.status === "ready" ? ` (${builds.length})` : "";

  return (
    <SectionCard
      title={`${t("title")}${count}`} icon={BoxIcon}
      actions={<Button size="sm" aria-expanded={open} onClick={() => setOpen((current) => !current)}>{t(open ? "hide" : "show")}</Button>}
    >
      {open ? (
        state.status === "loading" ? <div role="status" aria-label={t("title")} className="h-16 animate-pulse rounded-control bg-line" /> : builds.length === 0 ? (
          <p className="m-0 text-sm text-ink-muted">{t("empty")}</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="data-table w-full border-collapse text-sm">
              <caption className="sr-only">{t("title")}</caption>
              <thead>
                <tr className="border-b border-line">
                  <th scope="col" className={head}><span className="sr-only">{t("columns.expand")}</span></th>
                  <th scope="col" className={head}>{t("columns.code")}</th>
                  <th scope="col" className={head}>{t("columns.name")}</th>
                  <th scope="col" className={head}>{t("columns.parts")}</th>
                  <th scope="col" className={head}>{t("columns.status")}</th>
                  <th scope="col" className={head}>{t("columns.price")}</th>
                  {showCosts ? <th scope="col" className={head}>{t("columns.cost")}</th> : null}
                  <th scope="col" className={head}>{t("columns.actions")}</th>
                </tr>
              </thead>
              <tbody>
                {builds.map((build) => {
                  const isOpen = expanded.has(build.id);
                  return (
                    <Fragment key={build.id}>
                      <tr className="border-b border-line">
                        <td className={cell}>
                          <IconButton
                            icon={isOpen ? ChevronDownIcon : ChevronStartIcon} size="sm" aria-expanded={isOpen}
                            label={t(isOpen ? "collapse" : "expand", { name: build.name })} onClick={() => toggle(build.id)}
                          />
                        </td>
                        <td className={cell}><span dir="ltr" className="whitespace-nowrap font-mono text-[13px] font-bold">{build.code}</span></td>
                        <td className={cell}><span dir="auto" className="font-bold [unicode-bidi:isolate]">{build.name}</span></td>
                        <td className={cell}>{build.units.length}</td>
                        <td className={cell}><BuildStatusBadge status={build.status} /></td>
                        <td className={cell}><span className="whitespace-nowrap font-bold">{formatEgp(build.salePrice, locale)}</span></td>
                        {showCosts ? (
                          <td className={cell}>{build.componentCost === null ? null : <span className="whitespace-nowrap">{formatEgp(build.componentCost, locale)}</span>}</td>
                        ) : null}
                        <td className={cell}>
                          <span className="flex flex-wrap items-center gap-2">
                            <Link href={`/admin/builds/${build.id}`} className={buttonClass({ size: "sm" })}><PencilIcon size={buttonIconSize("sm")} />{t("edit")}</Link>
                            {has("Catalog.ManageProducts") ? (
                              <Button size="sm" variant="dangerGhost" icon={UnlinkIcon} onClick={() => setDisassembling(build)}>{t("disassemble")}</Button>
                            ) : null}
                          </span>
                        </td>
                      </tr>
                      {isOpen ? (
                        <tr className="border-b border-line bg-paper">
                          <td className={cell} />
                          <td className={cell} colSpan={showCosts ? 7 : 6}>
                            {build.units.length === 0 ? <span className="text-ink-muted">{t("noParts")}</span> : (
                              <ul className="m-0 flex list-none flex-col gap-1.5 p-0">
                                {build.units.map((part) => (
                                  <li key={part.unitId} className="flex flex-wrap items-center gap-x-3 gap-y-1">
                                    <Link href={`/admin/inventory?q=${encodeURIComponent(part.stockItemCode)}`} dir="ltr" className="font-mono text-xs font-bold text-orange-text hover:underline">
                                      {part.stockItemCode}
                                    </Link>
                                    <span dir="auto" className="[unicode-bidi:isolate]">{part.name}</span>
                                    <ConditionBadge condition={part.condition === "New" ? "New" : "Used"} />
                                    {part.serialNumber ? (
                                      <span className="text-xs text-ink-muted">{t("serial")} <bdi dir="ltr" className="font-mono">{part.serialNumber}</bdi></span>
                                    ) : null}
                                  </li>
                                ))}
                              </ul>
                            )}
                          </td>
                        </tr>
                      ) : null}
                    </Fragment>
                  );
                })}
              </tbody>
            </table>
          </div>
        )
      ) : null}
      <ConfirmDialog
        open={disassembling !== null} title={t("disassembleTitle")} body={t("disassembleBody")} confirmLabel={t("disassemble")} danger
        onClose={() => setDisassembling(null)}
        onConfirm={() => (disassembling ? disassemble(disassembling) : Promise.resolve())}
      />
    </SectionCard>
  );
}
