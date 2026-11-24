"use client";

import { useRef } from "react";
import { useTranslations } from "next-intl";
import { BulletListIcon, HeadingIcon } from "@/components/ui/icons";
import { TextAreaField } from "@/components/ui/TextAreaField";
import { IconButton } from "../kit/Button";
import { applyPolicyFormat, type PolicyFormat } from "./policy-format";

/**
 * A policy's body field with a small toolbar for the two formats the public page understands: a heading and a bulleted list. The buttons
 * work on the lines the selection touches and write the same marks (`# `, `- `) one could type by hand.
 */
export function PolicyBodyField({ label, name, dir, value, onChange, error }: {
  label: string;
  name: string;
  dir: "rtl" | "ltr";
  value: string;
  onChange: (value: string) => void;
  error?: string | null;
}) {
  const t = useTranslations("admin.policies.toolbar");
  const area = useRef<HTMLTextAreaElement>(null);

  function format(kind: PolicyFormat) {
    const field = area.current;
    if (!field) return;
    const edit = applyPolicyFormat(value, field.selectionStart, field.selectionEnd, kind);
    onChange(edit.text);
    // The text is replaced on the next render; the selection and the focus are put back after it.
    requestAnimationFrame(() => {
      field.focus();
      field.setSelectionRange(edit.selectionStart, edit.selectionEnd);
    });
  }

  return (
    <div className="flex flex-col gap-1.5">
      <div role="toolbar" aria-label={t("label", { field: label })} className="flex items-center gap-1">
        <IconButton icon={HeadingIcon} label={t("heading")} variant="secondary" size="sm" onClick={() => format("heading")} />
        <IconButton icon={BulletListIcon} label={t("list")} variant="secondary" size="sm" onClick={() => format("list")} />
      </div>
      <TextAreaField label={label} name={name} dir={dir} value={value} onChange={onChange} rows={14} maxLength={40000} error={error} required={dir === "rtl"} textareaRef={area} />
    </div>
  );
}
