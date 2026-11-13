"use client";

import { useRef, useState } from "react";
import { catalogMediaUrl, deleteProfileAvatar, uploadProfileAvatar } from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { useSession } from "@/components/session-provider";
import { Message, SecondaryButton } from "@/components/ui";

export function ProfileAvatar({
  size = "md",
  editable = false,
}: {
  size?: "sm" | "md" | "lg";
  editable?: boolean;
}) {
  const { messages } = useLocale();
  const { user, refresh } = useSession();
  const fileRef = useRef<HTMLInputElement>(null);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const initials = (user?.fullName || user?.username || "R")
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part.charAt(0).toUpperCase())
    .join("");
  const dimension = size === "lg" ? "h-24 w-24 text-3xl" : size === "sm" ? "h-9 w-9 text-xs" : "h-16 w-16 text-xl";

  async function upload(file: File | undefined) {
    if (!file) return;
    setPending(true);
    setError(null);
    setNotice(null);
    try {
      await uploadProfileAvatar(file);
      await refresh();
      setNotice(messages.accountAvatarSaved);
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setPending(false);
      if (fileRef.current) fileRef.current.value = "";
    }
  }

  async function remove() {
    setPending(true);
    setError(null);
    setNotice(null);
    try {
      await deleteProfileAvatar();
      await refresh();
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setPending(false);
    }
  }

  return (
    <div className="grid gap-2">
      <div className="flex flex-wrap items-center gap-3">
        {user?.avatarStorageKey ? (
          <img
            src={catalogMediaUrl(user.avatarStorageKey)}
            alt={user.fullName || user.username}
            className={`${dimension} rounded-full border border-[var(--ds-border)] object-cover`}
          />
        ) : (
          <span
            aria-hidden="true"
            className={`${dimension} grid place-items-center rounded-full border border-[var(--ds-border)] bg-[var(--ds-accent-soft)] font-black text-[var(--ds-accent-strong)]`}
          >
            {initials || "R"}
          </span>
        )}
        {editable ? (
          <div className="flex flex-wrap gap-2">
            <input
              ref={fileRef}
              type="file"
              accept="image/jpeg,image/png,image/webp"
              className="sr-only"
              onChange={(event) => void upload(event.target.files?.[0])}
            />
            <SecondaryButton type="button" disabled={pending} onClick={() => fileRef.current?.click()}>
              {messages.accountAvatarUpload}
            </SecondaryButton>
            {user?.avatarStorageKey ? (
              <SecondaryButton type="button" disabled={pending} onClick={() => void remove()}>
                {messages.accountAvatarRemove}
              </SecondaryButton>
            ) : null}
          </div>
        ) : null}
      </div>
      {editable ? <p className="text-xs text-[var(--ds-text-muted)]">{messages.accountAvatarHint}</p> : null}
      {notice ? <Message tone="success">{notice}</Message> : null}
      {error ? <Message tone="error">{error}</Message> : null}
    </div>
  );
}
