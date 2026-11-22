"use client";

import { useState, type ChangeEvent } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { FormMessage, PageHeader } from "../kit/PageHeader";
import { useSubmit } from "../kit/use-submit";
import { SectionCard } from "../system/SectionCard";
import { downloadImportTemplate, importStockFile, type StockImportResult } from "./stock-import-api";
import { canImport, conditionKey, fileProblem, importSummary, rowHasErrors } from "./stock-import-rules";
import { BoxIcon, DownloadIcon, UploadIcon } from "@/components/ui/icons";
import { Button, buttonClass, buttonIconSize } from "../kit/Button";

const COLUMNS = ["row", "model", "condition", "quantity", "items", "errors"] as const;

/** Opening stock from Excel: download the template, upload the filled file (a dry run shows every row), then import it in one go. */
export function StockImport() {
  const t = useTranslations("admin.stockImport");
  const condition = useTranslations("common.condition");
  const template = useSubmit();
  const check = useSubmit();
  const commit = useSubmit();
  const [file, setFile] = useState<File | null>(null);
  const [result, setResult] = useState<StockImportResult | null>(null);
  const [fileError, setFileError] = useState<string | null>(null);

  async function choose(event: ChangeEvent<HTMLInputElement>) {
    const chosen = event.target.files?.[0] ?? null;
    const problem = chosen ? fileProblem(chosen) : null;
    setResult(null);
    setFile(null);
    setFileError(problem ? t(`fileProblems.${problem}`) : null);
    if (!chosen || problem) return;
    setFile(chosen);
    await check.run(async () => setResult(await importStockFile(chosen, true)), t("checked"));
  }

  async function startImport() {
    if (!file) return;
    await commit.run(async () => setResult(await importStockFile(file, false)), t("imported"));
  }

  const summary = result ? importSummary(result) : null;
  const commitMessage = commit.errors.general ?? (Object.keys(commit.errors.fields).length > 0 ? t("rowsChanged") : null);

  return (
    <div className="flex flex-col gap-5">
      <PageHeader title={t("title")} />

      <SectionCard title={t("step1.title")} hint={t("step1.hint")}>
        <FormMessage message={template.errors.general} />
        <Button variant="primary" icon={DownloadIcon} className="self-start" loading={template.busy} onClick={() => void template.run(() => downloadImportTemplate(), t("step1.downloaded"))}>{t("step1.button")}</Button>
      </SectionCard>

      <SectionCard title={t("step2.title")} hint={t("step2.hint")}>
        <div className="flex flex-wrap items-center gap-3">
          <label className={`${buttonClass({ variant: "primary" })} cursor-pointer ${check.busy || commit.busy ? "opacity-60" : ""}`}>
            <UploadIcon size={buttonIconSize()} />
            {t("step2.choose")}
            <input type="file" accept=".xlsx" disabled={check.busy || commit.busy} onChange={(event) => void choose(event)} className="sr-only" />
          </label>
          <span dir="auto" className="text-sm text-ink-muted [unicode-bidi:isolate]">{file ? file.name : t("step2.noFile")}</span>
        </div>
        <FormMessage message={fileError ?? check.errors.general} />
        {result && summary ? (
          <>
            <div className="overflow-x-auto rounded-card border border-line bg-surface">
              <table aria-label={t("table.label")} className="w-full border-collapse text-sm">
                <thead className="bg-paper text-[13px] text-ink-muted">
                  <tr>
                    {COLUMNS.map((column) => <th key={column} scope="col" className="whitespace-nowrap px-3 py-2.5 text-start font-bold">{t(`table.${column}`)}</th>)}
                  </tr>
                </thead>
                <tbody>
                  {result.rows.map((row) => {
                    const known = conditionKey(row.condition);
                    return (
                      <tr key={row.row} className={`border-t border-line ${rowHasErrors(row) ? "bg-paper" : ""}`}>
                        <td dir="ltr" className="px-3 py-2.5 text-start font-mono">{row.row}</td>
                        <td dir="auto" className="px-3 py-2.5 [unicode-bidi:isolate]">{row.modelName || t("none")}</td>
                        <td className="whitespace-nowrap px-3 py-2.5">{known ? condition(known) : t("none")}</td>
                        <td className="px-3 py-2.5">{row.quantity}</td>
                        <td className="px-3 py-2.5">{row.itemsToCreate}</td>
                        <td className="px-3 py-2.5">
                          {rowHasErrors(row) ? (
                            <ul className="m-0 flex list-none flex-col gap-0.5 p-0 font-bold text-error">
                              {row.errors.map((error) => <li key={error}>{error}</li>)}
                            </ul>
                          ) : t("none")}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
            <p role="status" className="m-0 text-sm font-bold">{t(`summary.${summary.key}`, summary.values)}</p>
          </>
        ) : null}
      </SectionCard>

      <SectionCard title={t("step3.title")}>
        <FormMessage message={commitMessage} />
        {result?.imported ? (
          <div role="status" className="flex flex-col gap-3">
            <p className="m-0 font-bold">{t("step3.done", { count: result.items?.length ?? result.itemsToCreate })}</p>
            <p className="m-0 text-sm text-ink-muted">{t("step3.photosHint")}</p>
            <Link href="/admin/inventory" className={`${buttonClass({ variant: "primary" })} self-start`}><BoxIcon size={buttonIconSize()} />{t("step3.openStock")}</Link>
          </div>
        ) : (
          <Button variant="primary" icon={UploadIcon} className="self-start" loading={commit.busy} disabled={!canImport(result)} onClick={() => void startImport()}>
            {t("step3.button", { count: result?.itemsToCreate ?? 0 })}
          </Button>
        )}
      </SectionCard>
    </div>
  );
}
