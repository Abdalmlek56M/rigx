"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { useRouter } from "@/i18n/navigation";
import { formMessage } from "../form-message";
import { Dialog } from "../kit/Dialog";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { copyBuild, type Build } from "./builds-api";
import { copyCount } from "./build-rules";
import { PlusIcon } from "@/components/ui/icons";
import { Button } from "../kit/Button";

/** Asks how many identical builds to add from this one (each gets its own new parts), then lands on the builds list where they appear. */
export function BuildCopiesDialog({ build, onClose }: { build: Build; onClose: () => void }) {
  const t = useTranslations("admin.builds.copies");
  const router = useRouter();
  const submit = useSubmit();
  const [text, setText] = useState("1");
  const count = copyCount(text);

  async function add() {
    if (count === null) return;
    await submit.run(async () => {
      await copyBuild(build.id, count);
      router.push("/admin/builds");
    }, t("done", { count }));
  }

  return (
    <Dialog open onClose={onClose} title={t("title")}>
      <form
        className="flex flex-col gap-4"
        onSubmit={(event) => {
          event.preventDefault();
          void add();
        }}
      >
        <p className="m-0 text-sm font-bold">{t("question")}</p>
        <TextField label={t("count")} name="count" type="number" ltr value={text} onChange={setText} error={submit.errors.fields.count} />
        <p className="m-0 text-xs text-ink-muted">{t("hint")}</p>
        <FormMessage message={formMessage(submit.errors, ["count"])} />
        <div className="flex flex-wrap justify-end gap-2">
          <Button variant="ghost" onClick={onClose}>{t("cancel")}</Button>
          <Button type="submit" variant="primary" icon={PlusIcon} loading={submit.busy} disabled={count === null}>{t("submit")}</Button>
        </div>
      </form>
      {submit.dialog}
    </Dialog>
  );
}
