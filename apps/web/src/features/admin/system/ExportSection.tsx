"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextField } from "@/components/ui/TextField";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { SectionCard } from "./SectionCard";
import { downloadExport } from "./system-api";
import { EXPORT_FORMATS, type ExportDataset, type ExportFormat } from "./system-rules.ts";

/** Export: one dataset, a format and a Cairo date range (empty = the server's last 30 days); customers asks for the step-up code. */
export function ExportSection({ datasets }: { datasets: readonly ExportDataset[] }) {
  const t = useTranslations("admin.system.export");
  const submit = useSubmit();
  const [dataset, setDataset] = useState<ExportDataset>(datasets[0] ?? "orders");
  const [format, setFormat] = useState<ExportFormat>("xlsx");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const badRange = from !== "" && to !== "" && from > to;

  async function save() {
    await submit.run((headers) => downloadExport({ dataset, format, from, to }, headers), t("downloaded"));
  }

  return (
    <SectionCard title={t("title")} hint={t("hint")}>
      <form className="flex flex-col gap-3" onSubmit={(event) => { event.preventDefault(); void save(); }}>
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <SelectField label={t("dataset")} name="dataset" value={dataset} onChange={setDataset} options={datasets.map((name) => ({ value: name, label: t(`datasets.${name}`) }))} />
          <SelectField label={t("format")} name="format" value={format} onChange={setFormat} options={EXPORT_FORMATS.map((name) => ({ value: name, label: name.toUpperCase() }))} />
          <TextField label={t("from")} name="from" type="date" ltr value={from} onChange={setFrom} error={submit.errors.fields.from} />
          <TextField label={t("to")} name="to" type="date" ltr value={to} onChange={setTo} error={submit.errors.fields.to} />
        </div>
        {badRange ? <FormMessage message={t("badRange")} /> : null}
        {dataset === "customers" ? <p className="m-0 text-xs text-ink-muted">{t("stepUpNote")}</p> : null}
        <FormMessage message={submit.errors.general ?? submit.errors.fields.format ?? null} />
        <button type="submit" disabled={submit.busy || badRange} className="h-11 self-start rounded-control bg-carbon px-5 font-bold text-on-carbon disabled:opacity-50">{t("download")}</button>
        {submit.dialog}
      </form>
      <p className="m-0 text-xs text-ink-muted">{t("audited")}</p>
    </SectionCard>
  );
}
