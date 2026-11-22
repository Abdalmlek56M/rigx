"use client";

import { useTranslations } from "next-intl";
import { IconButton } from "@/components/ui/Button";
import { Card } from "@/components/ui/Card";
import { SelectField } from "@/components/ui/SelectField";
import { TextField } from "@/components/ui/TextField";
import { TrashIcon } from "@/components/ui/icons";
import { PART_CONDITIONS, PART_TYPES, type OfferedPart } from "./services-data";
import { ChoiceRadioTile } from "@/components/ui/ChoiceTile";
import { conditionIcon } from "@/components/ui/condition-icon";

export type SellPart = OfferedPart & { key: number };

interface SellPartFieldsProps {
  part: SellPart;
  index: number;
  /** How many parts the form holds, to show the remove button only when there is more than one. */
  count: number;
  /** The model is required: show its message when the form was submitted with it empty. */
  modelError: boolean;
  onChange: (fields: Partial<OfferedPart>) => void;
  onRemove: () => void;
}

/** One offered part: type, model, condition and how long it was owned; the remove icon appears from the second part. */
export function SellPartFields({ part, index, count, modelError, onChange, onRemove }: SellPartFieldsProps) {
  const t = useTranslations("services.sell.form");

  return (
    <Card as="div" padding="sm" className="part-row gap-3">
      <div className="flex min-h-8 items-center justify-between gap-3">
        <b className="text-sm">{t("partNumber", { number: index + 1 })}</b>
        {count > 1 ? (
          <IconButton
            icon={TrashIcon}
            label={t("removePart", { number: index + 1 })}
            variant="ghost"
            size="sm"
            className="text-ink-muted hover:bg-error/10 hover:text-error-text"
            onClick={onRemove}
          />
        ) : null}
      </div>
      <div className="grid gap-3 sm:grid-cols-2">
        <SelectField
          label={t("partType")} name={`partType-${part.key}`} value={part.type} onChange={(type) => onChange({ type })}
          options={PART_TYPES.map((value) => ({ value, label: t(`partTypes.${value}`) }))}
        />
        <TextField
          label={t("model")} name={`model-${part.key}`} value={part.model} onChange={(model) => onChange({ model })}
          placeholder={t("modelPlaceholder")} autoComplete="off" required error={modelError ? t("errors.model") : null}
        />
        <div className="flex flex-col gap-1.5">
          <span className="text-xs font-bold">{t("condition")}</span>
          <div role="radiogroup" aria-label={t("condition")} className="grid grid-cols-2 gap-2">
            {PART_CONDITIONS.map((condition) => (
              <ChoiceRadioTile
                key={condition} name={`condition-${part.key}`} value={condition} icon={conditionIcon(condition)} label={t(`conditions.${condition}`)}
                selected={part.condition === condition} onSelect={() => onChange({ condition })}
              />
            ))}
          </div>
        </div>
        <TextField
          label={t("ownedFor")} name={`ownedFor-${part.key}`} value={part.ownedFor} onChange={(ownedFor) => onChange({ ownedFor })}
          placeholder={t("ownedForPlaceholder")}
        />
      </div>
    </Card>
  );
}
