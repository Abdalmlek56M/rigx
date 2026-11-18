"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { TextField } from "@/components/ui/TextField";
import { suggestModels, type ModelSuggestion } from "./purchases-api";

const MIN_CHARS = 2;
const DELAY_MS = 300;

/**
 * The model name: free text, with existing models suggested while typing (debounced). Picking one fills the model and its product;
 * ignoring them keeps the typed name. A failed suggestion call simply shows none.
 */
export function ModelInput({ value, categoryId, name, error, onType, onPick }: {
  value: string;
  categoryId: string;
  name: string;
  error?: string | null;
  onType: (text: string) => void;
  onPick: (suggestion: ModelSuggestion) => void;
}) {
  const t = useTranslations("admin.purchases.lines");
  const [open, setOpen] = useState(false);
  const [found, setFound] = useState<{ query: string; items: ModelSuggestion[] }>({ query: "", items: [] });
  const query = value.trim();

  useEffect(() => {
    if (query.length < MIN_CHARS) return;
    let current = true;
    const timer = window.setTimeout(() => {
      suggestModels(query, categoryId || undefined).then(
        (items) => current && setFound({ query, items }),
        () => current && setFound({ query, items: [] })
      );
    }, DELAY_MS);
    return () => {
      current = false;
      window.clearTimeout(timer);
    };
  }, [query, categoryId]);

  const items = open && found.query === query ? found.items.filter((item) => item.modelName !== value) : [];

  return (
    <div
      className="relative min-w-0"
      onFocus={() => setOpen(true)}
      onBlur={(event) => {
        if (!event.currentTarget.contains(event.relatedTarget)) setOpen(false);
      }}
      onKeyDown={(event) => {
        if (event.key === "Escape") setOpen(false);
      }}
    >
      <TextField label={t("model")} name={name} value={value} onChange={onType} error={error} placeholder={t("modelPlaceholder")} />
      {items.length > 0 ? (
        <ul aria-label={t("suggestions")} className="absolute inset-x-0 top-full z-10 m-0 mt-1 flex max-h-56 list-none flex-col overflow-y-auto rounded-control border border-input-line bg-surface p-1 shadow-[0_6px_24px_rgba(21,23,27,.15)]">
          {items.map((item) => (
            <li key={`${item.modelName}-${item.productId ?? ""}`}>
              <button
                type="button" onClick={() => { onPick(item); setOpen(false); }}
                className="flex min-h-11 w-full items-center justify-between gap-2 rounded-control px-2 text-start text-sm hover:bg-paper"
              >
                <span dir="auto" className="[unicode-bidi:isolate]">{item.modelName}</span>
                <span className="text-xs text-ink-muted">{t("inStock", { count: item.itemCount })}</span>
              </button>
            </li>
          ))}
        </ul>
      ) : null}
    </div>
  );
}
