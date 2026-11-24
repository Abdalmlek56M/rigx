"use client";

import { useState } from "react";

/**
 * The editable copy of one settings section. `baseline` is the form as the saved settings give it; the copy starts equal to it, is `dirty`
 * while it differs, `reset` returns to the baseline, and when the saved settings change (after a save, or a reload after a conflict) the copy
 * takes the new baseline, so what the server kept (trimmed text, dropped empty rows) is what the screen shows.
 */
export function useSectionDraft<T extends object>(baseline: T) {
  const saved = JSON.stringify(baseline);
  const [form, setForm] = useState<T>(baseline);
  const [seen, setSeen] = useState(saved);
  if (saved !== seen) {
    setSeen(saved);
    setForm(baseline);
  }
  const set = (patch: Partial<T>) => setForm((current) => ({ ...current, ...patch }));
  return { form, setForm, set, dirty: JSON.stringify(form) !== saved, reset: () => setForm(baseline) };
}
