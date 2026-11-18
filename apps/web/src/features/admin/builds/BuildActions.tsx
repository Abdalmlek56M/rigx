"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { formMessage } from "../form-message";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { FormMessage } from "../kit/PageHeader";
import { transitionBuild, type BuildTransition } from "./builds-api";
import { canDisassemble, canPublish, canUnpublish } from "./build-rules";
import { useBuildAction, type BuildActionProps } from "./use-build-action";

const button = "h-11 rounded-control border border-input-line bg-surface px-5 font-bold disabled:opacity-50";

/** Publish / unpublish / disassemble (disassemble confirms: every part goes back to stock), and "preview in store" for a published build. */
export function BuildActions({ canEdit, ...action }: BuildActionProps & { canEdit: boolean }) {
  const t = useTranslations("admin.builds.actions");
  const { build } = action;
  const { submit, run } = useBuildAction(action);
  const [confirming, setConfirming] = useState(false);
  const go = (transition: BuildTransition) => run(() => transitionBuild(build.id, transition, build.rowVersion), t(`${transition}Done`));

  return (
    <div className="flex flex-col gap-2">
      <div className="flex flex-wrap items-center gap-2">
        {build.status === "Published" ? (
          <Link href={`/builds/${build.code}`} target="_blank" className={`${button} inline-flex items-center`}>{t("preview")}</Link>
        ) : null}
        {canEdit && canPublish(build) ? <button type="button" disabled={submit.busy} onClick={() => void go("publish")} className="h-11 rounded-control bg-carbon px-5 font-bold text-on-carbon disabled:opacity-50">{t("publish")}</button> : null}
        {canEdit && canUnpublish(build) ? <button type="button" disabled={submit.busy} onClick={() => void go("unpublish")} className={button}>{t("unpublish")}</button> : null}
        {canEdit && canDisassemble(build) ? <button type="button" disabled={submit.busy} onClick={() => setConfirming(true)} className={`${button} text-error`}>{t("disassemble")}</button> : null}
      </div>
      <FormMessage message={formMessage(submit.errors)} />
      <ConfirmDialog
        open={confirming} title={t("disassembleTitle")} body={t("disassembleBody")} confirmLabel={t("disassemble")} danger
        onClose={() => setConfirming(false)}
        onConfirm={() => go("disassemble")}
      />
      {submit.dialog}
    </div>
  );
}
