"use client";

import { useCallback, useState } from "react";
import { useTranslations } from "next-intl";
import { DownloadIcon, PrinterIcon } from "@/components/ui/icons";
import { SelectField } from "@/components/ui/SelectField";
import { DateField } from "../kit/DateField";
import { Button } from "../kit/Button";
import { FormMessage } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { ExportPrintSheet } from "./ExportPrintSheet";
import { parseCsv, type ExportSheet } from "./export-sheet";
import { SectionCard } from "./SectionCard";
import { downloadExport, fetchExportCsv } from "./system-api";
import { EXPORT_CHOICES, type ExportChoice, type ExportDataset } from "./system-rules.ts";

interface Printed {
  dataset: ExportDataset;
  sheet: ExportSheet;
  title: string;
  range: string;
  generatedAtUtc: string;
}

/**
 * Export: one dataset, a format and a Cairo date range (empty = the server's last 30 days); customers asks for the step-up code.
 * PDF reads the CSV and opens the print dialog on an Arabic table (save as PDF there); the other formats download the server's file.
 */
export function ExportSection({ datasets }: { datasets: readonly ExportDataset[] }) {
  const t = useTranslations("admin.system.export");
  const submit = useSubmit();
  const [dataset, setDataset] = useState<ExportDataset>(datasets[0] ?? "orders");
  const [format, setFormat] = useState<ExportChoice>("xlsx");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [printed, setPrinted] = useState<Printed | null>(null);
  const badRange = from !== "" && to !== "" && from > to;
  const donePrinting = useCallback(() => setPrinted(null), []);

  async function save() {
    if (format !== "pdf") {
      await submit.run((headers) => downloadExport({ dataset, format, from, to }, headers), t("downloaded"));
      return;
    }
    await submit.run(async (headers) => {
      const sheet = parseCsv(await fetchExportCsv({ dataset, from, to }, headers));
      const range = from || to ? t("pdf.range", { from: from || "…", to: to || "…" }) : t("pdf.lastDays");
      setPrinted({ dataset, sheet, title: t(`datasets.${dataset}`), range, generatedAtUtc: new Date().toISOString() });
    }, t("pdf.ready"));
  }

  return (
    <SectionCard title={t("title")} hint={t("hint")}>
      <form className="flex flex-col gap-3" onSubmit={(event) => { event.preventDefault(); void save(); }}>
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <SelectField label={t("dataset")} name="dataset" value={dataset} onChange={setDataset} options={datasets.map((name) => ({ value: name, label: t(`datasets.${name}`) }))} />
          <SelectField label={t("format")} name="format" value={format} onChange={setFormat} options={EXPORT_CHOICES.map((name) => ({ value: name, label: t(`formats.${name}`) }))} />
          <DateField label={t("from")} name="from" value={from} onChange={setFrom} error={submit.errors.fields.from} />
          <DateField label={t("to")} name="to" value={to} onChange={setTo} error={submit.errors.fields.to} />
        </div>
        {badRange ? <FormMessage message={t("badRange")} /> : null}
        {dataset === "customers" ? <p className="m-0 text-xs text-ink-muted">{t("stepUpNote")}</p> : null}
        {format === "pdf" ? <p className="m-0 text-xs text-ink-muted">{t("pdf.note")}</p> : null}
        <FormMessage message={submit.errors.general ?? submit.errors.fields.format ?? null} />
        <Button type="submit" variant="primary" icon={format === "pdf" ? PrinterIcon : DownloadIcon} loading={submit.busy} disabled={badRange} className="self-start">
          {format === "pdf" ? t("pdf.button") : t("download")}
        </Button>
        {submit.dialog}
      </form>
      <p className="m-0 text-xs text-ink-muted">{t("audited")}</p>
      {printed ? <ExportPrintSheet {...printed} onDone={donePrinting} /> : null}
    </SectionCard>
  );
}
