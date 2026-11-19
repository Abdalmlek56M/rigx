"use client";

import { useState, type ChangeEvent, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { SelectField } from "@/components/ui/SelectField";
import { TextField } from "@/components/ui/TextField";
import { isEgyptMobile } from "@/features/requests/request-fields";
import { addPhotos, MAX_PHOTOS, PHOTO_TYPES, type PhotoProblem } from "@/features/requests/request-photos";
import { submitRequest, type SubmitOutcome } from "@/features/requests/submit-request";
import { uploadRequestPhotos } from "@/features/requests/upload-photos";
import { PART_TYPES, SELL_WANTS, type PartType, type SellWant } from "./services-data";
import { CloseIcon } from "@/components/ui/icons";

type Errors = Partial<Record<"name" | "phone" | "model", string>>;
type Result = { outcome: SubmitOutcome; photosFailed: number };

/** "بيع أو بدّل": sell or trade in a part. The request goes first; its photos follow with the upload token from the receipt. */
export function SellTradeForm() {
  const t = useTranslations("services.sell.form");
  const [partType, setPartType] = useState<PartType>(PART_TYPES[0]);
  const [model, setModel] = useState("");
  const [ownedFor, setOwnedFor] = useState("");
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

  async function submit(event: FormEvent) {
    event.preventDefault();
    const found: Errors = {};
    if (name.trim().length < 2) found.name = t("errors.name");
    if (!model.trim()) found.model = t("errors.model");
    if (!isEgyptMobile(phone)) found.phone = t("errors.phone");
    setErrors(found);
    if (Object.keys(found).length > 0) return;
    setBusy(true);
    const outcome = await submitRequest(want, {
      name: name.trim(),
      phone: phone.trim(),
      modelName: model.trim(),
      ownedFor: ownedFor.trim() || null,
      partType: t(`partTypes.${partType}`)
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
      <SelectField
        label={t("partType")} name="partType" value={partType} onChange={setPartType}
        options={PART_TYPES.map((value) => ({ value, label: t(`partTypes.${value}`) }))}
      />
      <TextField label={t("model")} name="model" value={model} onChange={setModel} placeholder={t("modelPlaceholder")} required error={errors.model ?? null} />
      <TextField label={t("ownedFor")} name="ownedFor" value={ownedFor} onChange={setOwnedFor} placeholder={t("ownedForPlaceholder")} />
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
