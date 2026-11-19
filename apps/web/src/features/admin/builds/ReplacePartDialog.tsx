"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { formatEgp } from "@/lib/format";
import { formMessage } from "../form-message";
import { Dialog } from "../kit/Dialog";
import { FormMessage } from "../kit/PageHeader";
import { StockUnitPicker } from "./StockUnitPicker";
import { replacePart, type BuildPart } from "./builds-api";
import { buildOption } from "./build-rules";
import { useBuildAction, type BuildActionProps } from "./use-build-action";
import { useOtherBuilds } from "./use-other-builds";
import { SwapIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

type Source = "stock" | "build";

const tab = (active: boolean) => `h-11 flex-1 rounded-control border px-3 text-sm font-bold ${active ? "border-carbon bg-carbon text-on-carbon" : "border-input-line bg-surface"}`;

/** Swaps a part for one from free stock, or for a part of another open build (the two builds trade places; the other's row version travels with the call). */
export function ReplacePartDialog({ part, onClose, ...action }: BuildActionProps & { part: BuildPart; onClose: () => void }) {
  const t = useTranslations("admin.builds.replaceDialog");
  const locale = useLocale();
  const { build } = action;
  const { submit, runOwn } = useBuildAction(action);
  const others = useOtherBuilds(build.id);
  const [source, setSource] = useState<Source>("stock");
  const [targetId, setTargetId] = useState("");
  const [pickedId, setPickedId] = useState("");
  const other = others.state.status === "ready" ? others.state.data.find((entry) => entry.id === targetId) : undefined;
  const choose = (next: Source) => {
    setSource(next);
    setPickedId("");
  };

  async function replace() {
    if (!pickedId) return;
    await runOwn(async () => {
      const saved = await replacePart(build.id, part.unitId, pickedId, build.rowVersion, source === "build" ? other?.rowVersion : undefined);
      onClose();
      return saved;
    }, t("done"));
  }

  return (
    <Dialog open onClose={onClose} title={t("title", { name: part.name })}>
      <div className="flex gap-2" role="group" aria-label={t("source")}>
        <button type="button" aria-pressed={source === "stock"} onClick={() => choose("stock")} className={tab(source === "stock")}>{t("fromStock")}</button>
        <button type="button" aria-pressed={source === "build"} onClick={() => choose("build")} className={tab(source === "build")}>{t("fromBuild")}</button>
      </div>
      {source === "stock" ? (
        <StockUnitPicker categoryId={part.categoryId ?? undefined} pickedId={pickedId} actionLabel={t("pick")} onPick={(unit) => setPickedId(unit.id)} />
      ) : others.state.status === "ready" ? (
        <div className="flex flex-col gap-2">
          <SelectField
            label={t("otherBuild")} name="otherBuild" value={targetId} onChange={(id) => { setTargetId(id); setPickedId(""); }}
            options={[{ value: "", label: t("pickBuild") }, ...others.state.data.map(buildOption)]}
          />
          <ul className="m-0 flex max-h-64 list-none flex-col gap-1.5 overflow-y-auto p-0">
            {(other?.units ?? []).map((unit) => (
              <li key={unit.unitId} className="flex flex-wrap items-center justify-between gap-2 rounded-control border border-line bg-paper p-2.5 text-sm">
                <span className="flex min-w-0 flex-col">
                  <span dir="auto" className="font-bold [unicode-bidi:isolate]">{unit.name}</span>
                  <span className="text-xs text-ink-muted"><span dir="ltr" className="font-mono">{unit.stockItemCode}</span> · {formatEgp(unit.partPrice, locale)}</span>
                </span>
                <button
                  type="button" aria-pressed={pickedId === unit.unitId} onClick={() => setPickedId(unit.unitId)}
                  className={`h-10 rounded-control border px-4 text-sm font-bold ${pickedId === unit.unitId ? "border-carbon bg-carbon text-on-carbon" : "border-input-line bg-surface"}`}
                >
                  {pickedId === unit.unitId ? t("picked") : t("pick")}
                </button>
              </li>
            ))}
          </ul>
        </div>
      ) : others.state.status === "error" ? (
        <p role="alert" className="m-0 text-sm font-bold text-error">{t("loadFailed")}</p>
      ) : (
        <div role="status" aria-label={t("loading")} className="h-11 animate-pulse rounded-control bg-line" />
      )}
      <p className="m-0 text-xs text-ink-muted">{source === "stock" ? t("stockNote", { code: part.stockItemCode }) : t("buildNote")}</p>
      <FormMessage message={formMessage(submit.errors)} />
      <div className="flex flex-wrap justify-end gap-2">
        <Button variant="ghost" onClick={onClose}>{t("cancel")}</Button>
        <Button variant="primary" icon={SwapIcon} loading={submit.busy} disabled={!pickedId} onClick={() => void replace()}>{t("confirm")}</Button>
      </div>
      {submit.dialog}
    </Dialog>
  );
}
