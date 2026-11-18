"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { formMessage } from "../form-message";
import { Dialog } from "../kit/Dialog";
import { FormMessage } from "../kit/PageHeader";
import { removePart, type BuildPart } from "./builds-api";
import { buildOption } from "./build-rules";
import { useBuildAction, type BuildActionProps } from "./use-build-action";
import { useOtherBuilds } from "./use-other-builds";

const radio = "flex min-h-11 items-center gap-2 text-sm font-bold";

/** Takes a part out: back to free stock, or straight into another open build (its current row version travels with the call). */
export function RemovePartDialog({ part, onClose, ...action }: BuildActionProps & { part: BuildPart; onClose: () => void }) {
  const t = useTranslations("admin.builds.removeDialog");
  const { build } = action;
  const { submit, runOwn } = useBuildAction(action);
  const others = useOtherBuilds(build.id);
  const [toBuild, setToBuild] = useState(false);
  const [targetId, setTargetId] = useState("");
  const target = others.state.status === "ready" ? others.state.data.find((entry) => entry.id === targetId) : undefined;
  const ready = !toBuild || target !== undefined;

  async function remove() {
    if (!ready) return;
    await runOwn(async () => {
      const saved = await removePart(build.id, part.unitId, build.rowVersion, toBuild && target ? { assemblyId: target.id, rowVersion: target.rowVersion } : undefined);
      onClose();
      return saved;
    }, t("done"));
  }

  return (
    <Dialog open onClose={onClose} title={t("title", { name: part.name })}>
      <div className="flex flex-col gap-1">
        <label className={radio}>
          <input type="radio" name="removeTo" checked={!toBuild} onChange={() => setToBuild(false)} className="h-5 w-5" />
          {t("toStock")}
        </label>
        <label className={radio}>
          <input type="radio" name="removeTo" checked={toBuild} onChange={() => setToBuild(true)} className="h-5 w-5" />
          {t("toBuild")}
        </label>
      </div>
      {toBuild ? (
        others.state.status === "ready" ? (
          <SelectField
            label={t("targetBuild")} name="targetAssemblyId" value={targetId} onChange={setTargetId} error={submit.errors.fields.targetAssemblyId}
            options={[{ value: "", label: t("pickBuild") }, ...others.state.data.map(buildOption)]}
          />
        ) : others.state.status === "error" ? (
          <p role="alert" className="m-0 text-sm font-bold text-error">{t("loadFailed")}</p>
        ) : (
          <div role="status" aria-label={t("loading")} className="h-11 animate-pulse rounded-control bg-line" />
        )
      ) : null}
      <FormMessage message={formMessage(submit.errors, ["targetAssemblyId"])} />
      <div className="flex flex-wrap justify-end gap-2">
        <button type="button" onClick={onClose} className="h-11 rounded-control border border-input-line bg-surface px-5 font-bold">{t("cancel")}</button>
        <button type="button" disabled={submit.busy || !ready} onClick={() => void remove()} className="h-11 rounded-control bg-error px-5 font-bold text-surface disabled:opacity-60">{t("confirm")}</button>
      </div>
      {submit.dialog}
    </Dialog>
  );
}
