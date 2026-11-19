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
import { TrashIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

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
        <Button variant="ghost" onClick={onClose}>{t("cancel")}</Button>
        <Button variant="danger" icon={TrashIcon} loading={submit.busy} disabled={!ready} onClick={() => void remove()}>{t("confirm")}</Button>
      </div>
      {submit.dialog}
    </Dialog>
  );
}
