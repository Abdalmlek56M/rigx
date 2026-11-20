"use client";

import { useRef, useState } from "react";
import { useTranslations } from "next-intl";
import { multipart, unwrap } from "@rigx/api-client";
import { refreshSession, type SessionUser } from "@/features/auth/session-store";
import { browserApi } from "@/lib/api/browser";
import { mediaUrl } from "@/lib/media";
import { avatarProblem, initials } from "./profile-rules";

/** The profile picture (initials when there is none) with upload and remove. */
export function AvatarEditor({ user }: { user: SessionUser }) {
  const t = useTranslations("account.profile.avatar");
  const input = useRef<HTMLInputElement>(null);
  const [problem, setProblem] = useState<"type" | "size" | "failed" | null>(null);
  const [busy, setBusy] = useState(false);

  async function run(action: () => Promise<unknown>) {
    setBusy(true);
    setProblem(null);
    try {
      await action();
      await refreshSession();
    } catch {
      setProblem("failed");
    } finally {
      setBusy(false);
    }
  }

  function pick(file: File | undefined) {
    if (!file) return;
    const refused = avatarProblem(file);
    if (refused) return setProblem(refused);
    const form = new FormData();
    form.set("image", file);
    void run(() => unwrap(browserApi().POST("/api/v1/identity/profile/avatar", multipart(form))));
    if (input.current) input.current.value = "";
  }

  const button = "inline-flex min-h-11 items-center rounded-control border border-input-line bg-surface px-4 text-sm font-bold disabled:opacity-60";

  return (
    <div className="flex flex-wrap items-center gap-4">
      <span className="flex h-16 w-16 shrink-0 items-center justify-center overflow-hidden rounded-full bg-carbon text-xl font-bold text-on-carbon">
        {user.avatarStorageKey ? (
          // eslint-disable-next-line @next/next/no-img-element -- media is served by the API through the same-origin rewrite
          <img src={mediaUrl(user.avatarStorageKey)} alt="" className="h-full w-full object-cover" />
        ) : (
          <span aria-hidden="true">{initials(user.fullName || user.email)}</span>
        )}
      </span>
      <div className="flex flex-col gap-1.5">
        <div className="flex flex-wrap gap-2">
          <label className={`${button} cursor-pointer`}>
            {user.avatarStorageKey ? t("change") : t("upload")}
            <input ref={input} type="file" accept="image/jpeg,image/png,image/webp" disabled={busy} onChange={(event) => pick(event.target.files?.[0])} className="sr-only" />
          </label>
          {user.avatarStorageKey ? (
            <button type="button" disabled={busy} onClick={() => void run(() => unwrap(browserApi().DELETE("/api/v1/identity/profile/avatar")))} className={button}>{t("remove")}</button>
          ) : null}
        </div>
        {problem ? <p role="alert" className="m-0 text-xs text-error">{t(`errors.${problem}`)}</p> : null}
      </div>
    </div>
  );
}
