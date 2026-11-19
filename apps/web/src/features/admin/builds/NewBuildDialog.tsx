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
import { PlusIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

/**
 * Asks for the build's name and, optionally, its first part from free stock; creates the draft and opens its editor. The other parts are
 * added there, from stock or as new pieces that are not in stock yet.
 */
export function NewBuildDialog({ onClose }: { onClose: () => void }) {
  const t = useTranslations("admin.builds.create");
  const router = useRouter();
  const submit = useSubmit();
  const [name, setName] = useState("");
  const [unitId, setUnitId] = useState("");
  const ready = name.trim() !== "";

  async function create() {
    if (!ready) return;
    await submit.run(async () => {
      const build = await createBuild(name.trim(), unitId || null);
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
        <p className="m-0 text-xs text-ink-muted">{t("firstPartHint")}</p>
        <StockUnitPicker pickedId={unitId} actionLabel={t("pick")} onPick={(unit) => setUnitId(unit.id)} />
        <FormMessage message={formMessage(submit.errors, ["name"])} />
        <div className="flex flex-wrap justify-end gap-2">
          <Button variant="ghost" onClick={onClose}>{t("cancel")}</Button>
          <Button type="submit" variant="primary" icon={PlusIcon} loading={submit.busy} disabled={!ready}>{t("submit")}</Button>
        </div>
      </form>
      {submit.dialog}
    </Dialog>
  );
}
