"use client";

import { useState, type FormEvent } from "react";
import { useTranslations } from "next-intl";
import { unwrap } from "@rigx/api-client";
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
      <p role="status" className="m-0 rounded-card border border-line bg-surface p-5 text-sm">
        {score === null ? t("thanks") : t("already")} <b dir="ltr" className="font-mono">{sent}/5</b>
      </p>
    );
  }
  if (!canRate) return null;

  return (
    <form onSubmit={submit} className="flex flex-col gap-3 rounded-card border border-line bg-surface p-5">
      <fieldset className="m-0 flex flex-col gap-2 border-0 p-0">
        <legend className="mb-2 p-0 font-bold">{t("title")}</legend>
        <div className="flex gap-2">
          {SCORES.map((value) => (
            <label key={value} className="relative">
              <input type="radio" name="score" value={value} checked={chosen === value} onChange={() => setChosen(value)} className="peer sr-only" />
              <span className="flex h-11 w-11 cursor-pointer items-center justify-center rounded-control border border-input-line bg-surface font-mono font-bold peer-checked:border-orange peer-checked:bg-orange peer-checked:text-on-orange peer-focus-visible:outline-2 peer-focus-visible:outline-offset-2 peer-focus-visible:outline-orange">
                {value}
              </span>
            </label>
          ))}
        </div>
      </fieldset>
      <textarea
        name="comment" aria-label={t("comment")} placeholder={t("comment")} rows={3} value={comment} onChange={(event) => setComment(event.target.value)}
        className="w-full rounded-control border border-input-line bg-surface p-3 text-sm text-ink"
      />
      {failed ? <p role="alert" className="m-0 text-sm text-error">{t("failed")}</p> : null}
      <button type="submit" disabled={busy || chosen === null} className="h-11 rounded-control bg-carbon font-bold text-on-carbon disabled:opacity-50">
        {t("submit")}
      </button>
    </form>
  );
}
