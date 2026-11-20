"use client";

import { useEffect } from "react";

/** How long after the last change the draft is written. */
export const DRAFT_DELAY_MS = 800;
export const DRAFT_MAX_AGE_MS = 7 * 24 * 60 * 60 * 1000;

/** The part of localStorage the draft needs (tests use a small fake). */
export type DraftStorage = Pick<Storage, "getItem" | "setItem" | "removeItem">;

export interface FormDraft<T> {
  savedAt: string;
  value: T;
}

/** `rigx.draft.{username}.{form}`: one draft per user and form. */
export const draftKeyFor = (username: string, formKey: string) => `rigx.draft.${username}.${formKey}`;

export function readDraft<T>(storage: DraftStorage, key: string): FormDraft<T> | null {
  try {
    const raw = storage.getItem(key);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as Partial<FormDraft<T>> | null;
    if (!parsed || typeof parsed !== "object" || typeof parsed.savedAt !== "string") return null;
    if (parsed.value === null || parsed.value === undefined) return null;
    return { savedAt: parsed.savedAt, value: parsed.value as T };
  } catch {
    return null;
  }
}

export function writeDraft<T>(storage: DraftStorage, key: string, value: T): void {
  try {
    storage.setItem(key, JSON.stringify({ savedAt: new Date().toISOString(), value } satisfies FormDraft<T>));
  } catch {
    // Storage may be disabled (private mode, quota): the form keeps working without a draft.
  }
}

export function clearDraft(storage: DraftStorage, key: string): void {
  try {
    storage.removeItem(key);
  } catch {
    // Storage may be disabled; nothing to clear.
  }
}

/** A draft is offered only when it was saved in the last seven days (a future timestamp is not trusted). */
export function draftIsFresh(draft: FormDraft<unknown>, now: number = Date.now()): boolean {
  const savedAt = Date.parse(draft.savedAt);
  return Number.isFinite(savedAt) && savedAt <= now && now - savedAt <= DRAFT_MAX_AGE_MS;
}

/** Saves `value` 800 ms after the last change, while enabled; clears the pending write on unmount. */
export function useFormDraft<T>(key: string, value: T, enabled: boolean): void {
  useEffect(() => {
    if (!enabled || !key) return;
    const timer = window.setTimeout(() => {
      try {
        writeDraft(window.localStorage, key, value);
      } catch {
        // Storage access itself can throw (blocked storage); the form keeps working.
      }
    }, DRAFT_DELAY_MS);
    return () => window.clearTimeout(timer);
  }, [key, value, enabled]);
}
