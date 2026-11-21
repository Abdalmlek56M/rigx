"use client";

import { useState, type ChangeEvent, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { Button, IconButton } from "@/components/ui/Button";
import { Card, cardClass } from "@/components/ui/Card";
import { SelectField } from "@/components/ui/SelectField";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { TextField } from "@/components/ui/TextField";
import { CheckIcon, CloseIcon, PlusIcon } from "@/components/ui/icons";
import { isEgyptMobile } from "@/features/requests/request-fields";
import { addPhotos, MAX_PHOTOS, PHOTO_TYPES, type PhotoProblem } from "@/features/requests/request-photos";
import { submitRequest, type SubmitOutcome } from "@/features/requests/submit-request";
import { uploadRequestPhotos } from "@/features/requests/upload-photos";
import { MAX_PARTS, PART_TYPES, SELL_WANTS, describeParts, type OfferedPart, type SellWant } from "./services-data";
import { SellPartFields, type SellPart } from "./SellPartFields";

type Errors = Partial<Record<"name" | "phone", string>> & { models?: number[] };
type Result = { outcome: SubmitOutcome; photosFailed: number };

/** "بِع أو بدّل": sell or trade in one part or several (type, model, condition each). The request goes first; its photos follow. */
export function SellTradeForm() {
  const t = useTranslations("services.sell.form");
  const [parts, setParts] = useState<SellPart[]>([newPart(0)]);
  const [want, setWant] = useState<SellWant>(SELL_WANTS[0]);
  const [name, setName] = useState("");
  const [phone, setPhone] = useState("");
  const [photos, setPhotos] = useState<File[]>([]);
  const [photoProblem, setPhotoProblem] = useState<PhotoProblem | null>(null);
  const [errors, setErrors] = useState<Errors>({});
  const [busy, setBusy] = useState(false);
  const [result, setResult] = useState<Result | null>(null);

  function choosePhotos(event: ChangeEvent<HTMLInputElement>) {
    const added = addPhotos(photos, Array.from(event.target.files ?? []));
    setPhotos(added.photos);
    setPhotoProblem(added.problem);
    event.target.value = "";
  }

  const updatePart = (key: number, fields: Partial<OfferedPart>) =>
    setParts((current) => current.map((part) => (part.key === key ? { ...part, ...fields } : part)));
  const addPart = () => setParts((current) => (current.length >= MAX_PARTS ? current : [...current, newPart(Math.max(...current.map((part) => part.key)) + 1)]));
  const removePart = (key: number) => setParts((current) => (current.length > 1 ? current.filter((part) => part.key !== key) : current));

  async function submit(event: FormEvent) {
    event.preventDefault();
    const found: Errors = {};
    if (name.trim().length < 2) found.name = t("errors.name");
    const missing = parts.flatMap((part, index) => (part.model.trim() ? [] : [index]));
    if (missing.length > 0) found.models = missing;
    if (!isEgyptMobile(phone)) found.phone = t("errors.phone");
    setErrors(found);
    if (Object.keys(found).length > 0) {
      const form = event.currentTarget;
      requestAnimationFrame(() => form.querySelector<HTMLElement>("[aria-invalid=true]")?.focus());
      return;
    }
    setBusy(true);
    const described = describeParts(parts, {
      type: (type) => t(`partTypes.${type}`),
      condition: (condition) => t(`conditions.${condition}`),
      ownedFor: (value) => t("ownedForLine", { value })
    });
    const single = parts.length === 1 ? (parts[0]?.ownedFor.trim() ?? "") : "";
    const outcome = await submitRequest(want, {
      name: name.trim(),
      phone: phone.trim(),
      modelName: described.modelName,
      partType: described.partType,
      ownedFor: single || null,
      message: described.message
    });
    const upload = outcome.status === "sent" && outcome.photoUploadToken && photos.length > 0
      ? await uploadRequestPhotos(outcome.photoUploadToken, photos)
      : { failed: 0 };
    setResult({ outcome, photosFailed: upload.failed });
    setBusy(false);
  }

  if (result?.outcome.status === "sent") {
    return (
      <Card padding="lg" className="flex-1 basis-[480px] gap-3">
        <StatusBadge tone="success" icon={CheckIcon}>{t("sentTitle")}</StatusBadge>
        <p className="m-0 text-sm leading-7">{t("sent")}</p>
        {result.photosFailed > 0 ? <p className="m-0 text-sm leading-7 text-error">{t("photosFailed", { count: result.photosFailed })}</p> : null}
      </Card>
    );
  }

  return (
    <form
      aria-label={t("label")}
      onSubmit={submit}
      noValidate
      className={`${cardClass({ padding: "lg", layout: "grid" })} flex-1 basis-[480px] grid-cols-1 gap-3.5 sm:grid-cols-2`}
    >
      <fieldset className="m-0 flex flex-col gap-3 border-0 p-0 sm:col-span-2">
        <legend className="mb-1 text-xs font-bold">{t("parts")}</legend>
        {parts.map((part, index) => (
          <SellPartFields
            key={part.key}
            part={part}
            index={index}
            count={parts.length}
            modelError={errors.models?.includes(index) ?? false}
            onChange={(fields) => updatePart(part.key, fields)}
            onRemove={() => removePart(part.key)}
          />
        ))}
        {parts.length < MAX_PARTS ? (
          <Button type="button" variant="secondary" icon={PlusIcon} onClick={addPart} className="w-full border-dashed">{t("addPart")}</Button>
        ) : (
          <span className="text-xs text-ink-muted">{t("maxParts", { max: MAX_PARTS })}</span>
        )}
      </fieldset>
      <SelectField
        label={t("want")} name="want" value={want} onChange={setWant}
        options={SELL_WANTS.map((value) => ({ value, label: t(`wants.${value}`) }))}
      />
      <div className="flex flex-col gap-1.5 sm:col-span-2">
        <label htmlFor="sell-photos" className="text-xs font-bold">{t("photos")}</label>
        <label
          htmlFor="sell-photos"
          className="flex min-h-[90px] cursor-pointer items-center justify-center rounded-card border-2 border-dashed border-input-line bg-surface px-3 text-center text-sm text-ink-muted"
        >
          {t("photosDrop", { max: MAX_PHOTOS })}
        </label>
        <input id="sell-photos" type="file" multiple accept={PHOTO_TYPES.join(",")} onChange={choosePhotos} className="sr-only" />
        {photoProblem ? <span role="alert" className="text-xs text-error">{t(`photoProblems.${photoProblem}`, { max: MAX_PHOTOS })}</span> : null}
        {photos.length > 0 ? (
          <ul className="m-0 flex list-none flex-wrap gap-2 p-0">
            {photos.map((photo, index) => (
              <li key={`${photo.name}-${index}`} className="flex items-center gap-2 rounded-control bg-paper px-2.5 py-1 text-xs">
                <span dir="auto" className="max-w-[160px] truncate [unicode-bidi:isolate]">{photo.name}</span>
                <IconButton
                  icon={CloseIcon}
                  label={t("removePhoto", { name: photo.name })}
                  variant="ghost"
                  size="sm"
                  onClick={() => setPhotos(photos.filter((_, position) => position !== index))}
                />
              </li>
            ))}
          </ul>
        ) : null}
      </div>
      <TextField label={t("name")} name="name" value={name} onChange={setName} autoComplete="name" required error={errors.name ?? null} />
      <TextField
        label={t("phone")} name="phone" type="tel" ltr autoComplete="tel" placeholder="01xxxxxxxxx" required
        value={phone} onChange={setPhone} error={errors.phone ?? null}
      />
      {result ? (
        <p role="alert" className="m-0 text-sm text-error sm:col-span-2">{t(result.outcome.status === "invalid" ? "errors.invalid" : "errors.failed")}</p>
      ) : null}
      <Button type="submit" variant="accent" loading={busy} className="w-full sm:col-span-2">{t("submit")}</Button>
    </form>
  );
}

function newPart(key: number): OfferedPart & { key: number } {
  return { key, type: PART_TYPES[0], model: "", condition: "Used", ownedFor: "" };
}
