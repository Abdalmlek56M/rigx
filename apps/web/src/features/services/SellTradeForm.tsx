"use client";

import { useState, type ChangeEvent, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextField } from "@/components/ui/TextField";
import { isEgyptMobile } from "@/features/requests/request-fields";
import { addPhotos, MAX_PHOTOS, PHOTO_TYPES, type PhotoProblem } from "@/features/requests/request-photos";
import { submitRequest, type SubmitOutcome } from "@/features/requests/submit-request";
import { uploadRequestPhotos } from "@/features/requests/upload-photos";
import { MAX_PARTS, PART_CONDITIONS, PART_TYPES, SELL_WANTS, describeParts, type OfferedPart, type SellWant } from "./services-data";
import { CloseIcon, PlusIcon, TrashIcon } from "@/components/ui/icons";

type Errors = Partial<Record<"name" | "phone", string>> & { models?: number[] };
type Result = { outcome: SubmitOutcome; photosFailed: number };

/** "بيع أو بدّل": sell or trade in one part or several (type, model, condition each). The request goes first; its photos follow with the upload token from the receipt. */
export function SellTradeForm() {
  const t = useTranslations("services.sell.form");
  const [parts, setParts] = useState<(OfferedPart & { key: number })[]>([newPart(0)]);
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
      <div role="status" className="flex flex-1 basis-[480px] flex-col gap-2 rounded-panel bg-paper p-7 text-ink">
        <b className="text-lg">{t("sentTitle")}</b>
        <p className="m-0 text-sm leading-7">{t("sent")}</p>
        {result.photosFailed > 0 ? <p className="m-0 text-sm leading-7 text-error">{t("photosFailed", { count: result.photosFailed })}</p> : null}
      </div>
    );
  }

  return (
    <form
      aria-label={t("label")}
      onSubmit={submit}
      noValidate
      className="grid flex-1 basis-[480px] grid-cols-1 gap-3.5 rounded-panel bg-paper p-6 text-ink sm:grid-cols-2 md:p-7"
    >
      <fieldset className="m-0 flex flex-col gap-3 border-0 p-0 sm:col-span-2">
        <legend className="mb-1 text-[15px] font-bold">{t("parts")}</legend>
        {parts.map((part, index) => (
          <div key={part.key} className="part-row flex flex-col gap-3 rounded-card border border-line bg-surface p-4">
            <div className="flex min-h-8 items-center justify-between gap-3">
              <b className="text-sm">{t("partNumber", { number: index + 1 })}</b>
              {parts.length > 1 ? (
                <button
                  type="button"
                  aria-label={t("removePart", { number: index + 1 })}
                  onClick={() => removePart(part.key)}
                  className="press flex h-9 w-9 items-center justify-center rounded-full border border-line text-ink-muted transition-colors hover:border-error hover:text-error"
                >
                  <TrashIcon size={16} />
                </button>
              ) : null}
            </div>
            <div className="grid gap-3 sm:grid-cols-2">
              <SelectField
                label={t("partType")} name={`partType-${part.key}`} value={part.type} onChange={(type) => updatePart(part.key, { type })}
                options={PART_TYPES.map((value) => ({ value, label: t(`partTypes.${value}`) }))}
              />
              <TextField
                label={t("model")} name={`model-${part.key}`} value={part.model} onChange={(model) => updatePart(part.key, { model })}
                placeholder={t("modelPlaceholder")} autoComplete="off" required error={errors.models?.includes(index) ? t("errors.model") : null}
              />
              <div className="flex flex-col gap-1.5">
                <span className="text-[13px] font-bold">{t("condition")}</span>
                <div role="radiogroup" aria-label={t("condition")} className="grid h-12 grid-cols-2 gap-1 rounded-control border border-input-line bg-surface p-1 focus-within:outline-2 focus-within:outline-offset-2 focus-within:outline-orange">
                  {PART_CONDITIONS.map((condition) => (
                    <label key={condition} className="flex cursor-pointer">
                      <input
                        type="radio"
                        name={`condition-${part.key}`}
                        value={condition}
                        checked={part.condition === condition}
                        onChange={() => updatePart(part.key, { condition })}
                        className="peer sr-only"
                      />
                      <span className="flex flex-1 items-center justify-center rounded-[7px] text-sm font-bold text-ink-muted transition-colors peer-checked:bg-carbon peer-checked:text-on-carbon">
                        {t(`conditions.${condition}`)}
                      </span>
                    </label>
                  ))}
                </div>
              </div>
              <TextField
                label={t("ownedFor")} name={`ownedFor-${part.key}`} value={part.ownedFor} onChange={(ownedFor) => updatePart(part.key, { ownedFor })}
                placeholder={t("ownedForPlaceholder")}
              />
            </div>
          </div>
        ))}
        {parts.length < MAX_PARTS ? (
          <button
            type="button"
            onClick={addPart}
            className="press flex h-12 items-center justify-center gap-2 rounded-control border-2 border-dashed border-input-line text-sm font-bold transition-colors hover:border-orange hover:text-orange-text"
          >
            <PlusIcon size={18} />
            {t("addPart")}
          </button>
        ) : (
          <span className="text-xs text-ink-muted">{t("maxParts", { max: MAX_PARTS })}</span>
        )}
      </fieldset>
      <SelectField
        label={t("want")} name="want" value={want} onChange={setWant}
        options={SELL_WANTS.map((value) => ({ value, label: t(`wants.${value}`) }))}
      />
      <div className="flex flex-col gap-1.5 sm:col-span-2">
        <label htmlFor="sell-photos" className="text-[13px] font-bold">{t("photos")}</label>
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
              <li key={`${photo.name}-${index}`} className="flex items-center gap-2 rounded-control bg-surface px-2.5 py-1 text-xs">
                <span dir="auto" className="max-w-[160px] truncate [unicode-bidi:isolate]">{photo.name}</span>
                <button
                  type="button"
                  aria-label={t("removePhoto", { name: photo.name })}
                  onClick={() => setPhotos(photos.filter((_, position) => position !== index))}
                  className="flex h-8 w-8 items-center justify-center"
                >
                  <CloseIcon size={16} />
                </button>
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
      <button type="submit" disabled={busy} className="h-12 rounded-control bg-orange font-bold text-on-orange disabled:opacity-60 sm:col-span-2">
        {t("submit")}
      </button>
    </form>
  );
}

function newPart(key: number): OfferedPart & { key: number } {
  return { key, type: PART_TYPES[0], model: "", condition: "Used", ownedFor: "" };
}
