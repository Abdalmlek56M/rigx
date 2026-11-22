"use client";

import { useState, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { unwrap } from "@rigx/api-client";
import { Button } from "@/components/ui/Button";
import { cardClass } from "@/components/ui/Card";
import { textAreaClass } from "@/components/ui/field-class";
import { browserApi } from "@/lib/api/browser";

const SCORES = [1, 2, 3, 4, 5] as const;

/** One to five and an optional comment; once sent (or already rated) it only shows the score. */
export function RatingForm({ token, canRate, score }: { token: string; canRate: boolean; score: number | null }) {
  const t = useTranslations("tracking.rating");
  const [chosen, setChosen] = useState<number | null>(null);
  const [comment, setComment] = useState("");
  const [sent, setSent] = useState<number | null>(score);
  const [busy, setBusy] = useState(false);
  const [failed, setFailed] = useState(false);

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (chosen === null) return;
    setBusy(true);
    setFailed(false);
    try {
      await unwrap(browserApi().POST("/api/v1/track/{token}/rating", {
        params: { path: { token } }, body: { score: chosen, comment: comment.trim() || null }
      }));
      setSent(chosen);
    } catch {
      setFailed(true);
    }
    setBusy(false);
  }

  if (sent !== null) {
    return (
      <p role="status" className={`${cardClass({ padding: "md" })} text-sm`}>
        {score === null ? t("thanks") : t("already")} <b dir="ltr" className="font-mono">{sent}/5</b>
      </p>
    );
  }
  if (!canRate) return null;

  return (
    <form onSubmit={submit} className={`${cardClass({ padding: "md" })} gap-3`}>
      <fieldset className="m-0 flex flex-col gap-2 border-0 p-0">
        <legend className="mb-2 p-0 type-section-title">{t("title")}</legend>
        <div className="flex gap-2">
          {SCORES.map((value) => (
            <label key={value} className="relative">
              <input type="radio" name="score" value={value} checked={chosen === value} onChange={() => setChosen(value)} className="peer sr-only" />
              <span className="flex h-11 w-11 cursor-pointer items-center justify-center rounded-control border border-input-line bg-surface font-mono font-bold text-ink transition-colors peer-checked:border-transparent peer-checked:accent-strong peer-focus-visible:outline-2 peer-focus-visible:outline-offset-2 peer-focus-visible:outline-orange motion-reduce:transition-none">
                {value}
              </span>
            </label>
          ))}
        </div>
      </fieldset>
      <textarea
        name="comment" aria-label={t("comment")} placeholder={t("comment")} rows={3} value={comment} onChange={(event) => setComment(event.target.value)}
        className={textAreaClass}
      />
      {failed ? <p role="alert" className="m-0 text-sm text-error-text">{t("failed")}</p> : null}
      <Button type="submit" variant="primary" loading={busy} disabled={chosen === null} className="self-start">{t("submit")}</Button>
    </form>
  );
}
