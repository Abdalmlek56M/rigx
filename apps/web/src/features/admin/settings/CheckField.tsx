"use client";

interface CheckFieldProps {
  label: string;
  checked: boolean;
  onChange: (checked: boolean) => void;
  hint?: string;
  disabled?: boolean;
}

/** A labelled checkbox with an optional line of what it changes. */
export function CheckField({ label, checked, onChange, hint, disabled = false }: CheckFieldProps) {
  return (
    <label className={`flex min-h-11 items-start gap-2 text-[13px] ${disabled ? "opacity-60" : ""}`}>
      <input
        type="checkbox" checked={checked} disabled={disabled}
        onChange={(event) => onChange(event.target.checked)}
        className="mt-0.5 h-4 w-4 shrink-0 accent-orange"
      />
      <span className="flex flex-col gap-0.5">
        <span>{label}</span>
        {hint ? <span className="text-xs font-normal text-ink-muted">{hint}</span> : null}
      </span>
    </label>
  );
}
