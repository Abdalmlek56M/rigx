interface TextAreaFieldProps {
  label: string;
  name: string;
  value: string;
  onChange: (value: string) => void;
  rows?: number;
  placeholder?: string;
  required?: boolean;
  error?: string | null;
  hint?: string;
}

/** A labelled multi-line input that matches `TextField`; `error` is announced and linked to the textarea. */
export function TextAreaField({ label, name, value, onChange, rows = 3, placeholder, required, error, hint }: TextAreaFieldProps) {
  const errorId = `${name}-error`;
  const hintId = `${name}-hint`;
  const describedBy = error ? errorId : hint ? hintId : undefined;
  return (
    <label className="flex min-w-0 flex-col gap-1.5 text-[13px] font-bold">
      {label}
      <textarea
        name={name}
        value={value}
        rows={rows}
        required={required}
        placeholder={placeholder}
        aria-invalid={error ? true : undefined}
        aria-describedby={describedBy}
        onChange={(event) => onChange(event.target.value)}
        className="w-full resize-y rounded-control border border-input-line bg-surface px-3 py-2.5 text-sm font-normal text-ink"
      />
      {hint && !error ? <span id={hintId} className="text-xs font-normal text-ink-muted">{hint}</span> : null}
      {error ? <span id={errorId} role="alert" className="text-xs font-normal text-error">{error}</span> : null}
    </label>
  );
}
