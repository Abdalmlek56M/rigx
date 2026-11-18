"use client";

import { useState } from "react";
import { useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { TextField } from "@/components/ui/TextField";
import { ARTICLE_BLOCK_TYPES, addBlock, blockTypeKey, changeBlockType, moveBlock, removeBlock, type BlockRow } from "./article-blocks";

function Check({ label, checked, onChange }: { label: string; checked: boolean; onChange: (value: boolean) => void }) {
  return (
    <label className="flex min-h-11 items-center gap-2 text-[13px]">
      <input type="checkbox" checked={checked} onChange={(event) => onChange(event.target.checked)} className="h-4 w-4 accent-orange" />
      {label}
    </label>
  );
}

/** One language's body: an ordered list of simple blocks with add / remove / move and the fields its type uses. */
/** `dir` is the body's language direction (the Arabic body stays right-to-left in the English admin and the other way round). */
export function ArticleBlocksEditor({ title, rows, dir, onChange }: { title: string; rows: readonly BlockRow[]; dir: "ltr" | "rtl"; onChange: (rows: BlockRow[]) => void }) {
  const t = useTranslations("admin.articles.blocks");
  const [adding, setAdding] = useState<string>("paragraph");
  const update = (id: string, patch: Partial<BlockRow>) => onChange(rows.map((row) => row.id === id ? { ...row, ...patch } : row));
  const indexOf = (id: string) => rows.findIndex((row) => row.id === id);

  return (
    <section className="flex flex-col gap-3">
      <h3 className="m-0 text-[15px] font-bold">{title}</h3>
      {rows.map((row) => {
        const index = indexOf(row.id);
        return (
          <div key={row.id} className="flex flex-col gap-2 rounded-control border border-line bg-paper p-3">
            <div className="flex flex-wrap items-end gap-2">
              <div className="min-w-40 flex-1">
                <SelectField
                  label={t("type")} name={`type-${row.id}`} value={row.type}
                  onChange={(type) => update(row.id, changeBlockType(row, blockTypeKey(type)))}
                  options={ARTICLE_BLOCK_TYPES.map((type) => ({ value: type, label: t(`types.${type}`) }))}
                />
              </div>
              <button type="button" disabled={index === 0} onClick={() => onChange(moveBlock(rows, index, -1))} aria-label={t("up")} className="h-11 w-11 rounded-control border border-input-line bg-surface disabled:opacity-40">↑</button>
              <button type="button" disabled={index === rows.length - 1} onClick={() => onChange(moveBlock(rows, index, 1))} aria-label={t("down")} className="h-11 w-11 rounded-control border border-input-line bg-surface disabled:opacity-40">↓</button>
              <button type="button" onClick={() => onChange(removeBlock(rows, index))} className="h-11 rounded-control border border-error/40 bg-surface px-3 text-sm font-bold text-error">{t("remove")}</button>
            </div>

            {row.type === "heading" ? (
              <div className="grid gap-2 sm:grid-cols-[120px_minmax(0,1fr)]">
                <SelectField label={t("level")} name={`level-${row.id}`} value={row.level} onChange={(level) => update(row.id, { level })}
                  options={[{ value: "2", label: "H2" }, { value: "3", label: "H3" }]} />
                <TextField label={t("text")} name={`text-${row.id}`} dir={dir} value={row.text} onChange={(text) => update(row.id, { text })} />
              </div>
            ) : null}
            {row.type === "paragraph" || row.type === "callout" ? (
              <TextAreaField label={t("text")} name={`text-${row.id}`} dir={dir} value={row.text} onChange={(text) => update(row.id, { text })} rows={3} />
            ) : null}
            {row.type === "callout" ? (
              <SelectField label={t("variant")} name={`variant-${row.id}`} value={row.variant} onChange={(variant) => update(row.id, { variant })}
                options={[
                  { value: "info", label: t("variants.info") },
                  { value: "tip", label: t("variants.tip") },
                  { value: "warning", label: t("variants.warning") }
                ]} />
            ) : null}
            {row.type === "list" ? (
              <div className="flex flex-col gap-2">
                <TextAreaField label={t("text")} name={`text-${row.id}`} dir={dir} value={row.text} onChange={(text) => update(row.id, { text })} rows={4} hint={t("listHint")} />
                <Check label={t("ordered")} checked={row.ordered} onChange={(ordered) => update(row.id, { ordered })} />
              </div>
            ) : null}
            {row.type === "table" ? (
              <TextAreaField label={t("table")} name={`text-${row.id}`} dir={dir} value={row.text} onChange={(text) => update(row.id, { text })} rows={4} hint={t("tableHint")} />
            ) : null}
            {row.type === "image" ? (
              <div className="grid gap-2 sm:grid-cols-2">
                <TextField label={t("imageKey")} name={`image-${row.id}`} value={row.imageKey} onChange={(imageKey) => update(row.id, { imageKey })} ltr />
                <TextField label={t("alt")} name={`alt-${row.id}`} dir={dir} value={row.alt} onChange={(alt) => update(row.id, { alt })} />
                <TextField label={t("caption")} name={`caption-${row.id}`} dir={dir} value={row.caption} onChange={(caption) => update(row.id, { caption })} />
              </div>
            ) : null}
            {row.type === "product" ? <TextField label={t("itemCode")} name={`code-${row.id}`} value={row.itemCode} onChange={(itemCode) => update(row.id, { itemCode: itemCode.toUpperCase() })} ltr placeholder="RX-0154" /> : null}
            {row.type === "youtube" ? <TextField label={t("youTube")} name={`video-${row.id}`} value={row.youTube} onChange={(youTube) => update(row.id, { youTube })} ltr placeholder="https://youtu.be/…" /> : null}
          </div>
        );
      })}
      <div className="flex flex-wrap items-end gap-2">
        <div className="min-w-40">
          <SelectField label={t("newBlock")} name="newBlockType" value={adding} onChange={setAdding}
            options={ARTICLE_BLOCK_TYPES.map((type) => ({ value: type, label: t(`types.${type}`) }))} />
        </div>
        <button type="button" onClick={() => onChange(addBlock(rows, blockTypeKey(adding)))} className="h-11 rounded-control border border-carbon bg-surface px-4 text-sm font-bold">{t("add")}</button>
      </div>
    </section>
  );
}
