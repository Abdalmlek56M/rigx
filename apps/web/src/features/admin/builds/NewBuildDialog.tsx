"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { useRouter } from "@/i18n/navigation";
import { formMessage } from "../form-message";
import { Dialog } from "../kit/Dialog";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { createBuild } from "./builds-api";
import { StockUnitPicker } from "./StockUnitPicker";

/** Asks for the build's name and its first part from free stock, creates the draft and opens its editor (the other parts are added there). */
export function NewBuildDialog({ onClose }: { onClose: () => void }) {
  const t = useTranslations("admin.builds.create");
  const router = useRouter();
  const submit = useSubmit();
  const [name, setName] = useState("");
  const [unitId, setUnitId] = useState("");
  const ready = name.trim() !== "" && unitId !== "";

  async function create() {
    if (!ready) return;
    await submit.run(async () => {
      const build = await createBuild(name.trim(), unitId);
      router.push(`/admin/builds/${build.id}`);
    }, t("created"));
  }

  return (
    <Dialog open onClose={onClose} title={t("title")}>
      <form
        className="flex flex-col gap-4"
        onSubmit={(event) => {
          event.preventDefault();
          void create();
        }}
      >
        <TextField label={t("name")} name="name" value={name} onChange={setName} error={submit.errors.fields.name} />
        <p className="m-0 text-sm font-bold">{t("firstPart")}</p>
        <StockUnitPicker pickedId={unitId} actionLabel={t("pick")} onPick={(unit) => setUnitId(unit.id)} />
        <FormMessage message={formMessage(submit.errors, ["name"])} />
        <div className="flex flex-wrap justify-end gap-2">
          <button type="button" onClick={onClose} className="h-11 rounded-control border border-input-line bg-surface px-5 font-bold">{t("cancel")}</button>
          <button type="submit" disabled={submit.busy || !ready} className="h-11 rounded-control bg-carbon px-5 font-bold text-on-carbon disabled:opacity-60">{t("submit")}</button>
        </div>
      </form>
      {submit.dialog}
    </Dialog>
  );
}
