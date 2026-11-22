"use client";

import { useEffect, useState } from "react";
import { useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextField } from "@/components/ui/TextField";
import { fetchCategorySpecs, type SpecDefinition } from "../categories/categories-api";
import { foreignSpecs, setSpec, specLabel, specsForSchema, type SpecValues } from "./spec-values";

/**
 * The spec fields of the picked category (its own and inherited ones, e.g. a GPU's VRAM in GB). Choice and yes/no specs are dropdowns,
 * numbers and text are typed. Every spec is optional. Values the category does not define (typed freely before it had fields) are taken
 * out, as the server refuses them, and listed so the owner can type them into the right field before saving.
 */
export function SpecFields({ categoryId, values, onChange, namePrefix, error }: {
  categoryId: string;
  values: SpecValues;
  onChange: (values: SpecValues) => void;
  namePrefix: string;
  error?: string | null;
}) {
  const t = useTranslations("admin.inventory.specs");
  const [loaded, setLoaded] = useState<{ categoryId: string; schema: SpecDefinition[] } | null>(null);
  // What the form opened with: its values the category does not define are listed under the fields (they are not saved).
  const [opened] = useState(values);

  useEffect(() => {
    if (!categoryId) return;
    let current = true;
    fetchCategorySpecs(categoryId).then(
      (schema) => current && setLoaded({ categoryId, schema }),
      () => current && setLoaded({ categoryId, schema: [] })
    );
    return () => {
      current = false;
    };
  }, [categoryId]);

  const schema = categoryId && loaded?.categoryId === categoryId ? loaded.schema : null;
  useEffect(() => {
    if (schema && foreignSpecs(values, schema).length > 0) onChange(specsForSchema(values, schema));
  }, [schema, values, onChange]);

  if (!categoryId || !schema || schema.length === 0) return null;
  const dropped = foreignSpecs(opened, schema);
  const field = (spec: SpecDefinition) => {
    const name = `${namePrefix}-${spec.key}`;
    const value = values[spec.key] ?? "";
    const change = (next: string) => onChange(setSpec(values, spec.key, next));
    if (spec.type === "Choice" || spec.type === "YesNo") {
      const options = spec.type === "YesNo"
        ? [{ value: "yes", label: t("yes") }, { value: "no", label: t("no") }]
        : (spec.options ?? []).map((option) => ({ value: option, label: option }));
      return <SelectField key={spec.key} label={specLabel(spec)} name={name} value={value} onChange={change} options={[{ value: "", label: t("notSet") }, ...options]} />;
    }
    return (
      <TextField
        key={spec.key} label={specLabel(spec)} name={name} value={value} onChange={change}
        type={spec.type === "Number" ? "number" : "text"} ltr={spec.type === "Number"} step={spec.type === "Number" ? "any" : undefined}
      />
    );
  };

  return (
    <fieldset className="m-0 flex min-w-0 flex-col gap-3 rounded-control border border-line p-3">
      <legend className="px-1 text-sm font-bold">{t("title")}</legend>
      <p className="m-0 text-xs text-ink-muted">{t("hint")}</p>
      <div className="grid gap-3 sm:grid-cols-2 xl:grid-cols-3">{schema.map(field)}</div>
      {dropped.length > 0 ? (
        <p role="status" className="m-0 rounded-control bg-orange-soft px-3 py-2 text-xs">
          <span className="font-bold">{t("old")}</span>{" "}
          <span dir="auto" className="[unicode-bidi:isolate]">{dropped.map(([key, value]) => `${key}: ${value}`).join(" · ")}</span>
        </p>
      ) : null}
      {error ? <p role="alert" className="m-0 text-sm font-bold text-error-text">{error}</p> : null}
    </fieldset>
  );
}
