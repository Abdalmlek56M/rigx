interface SelectFieldProps<T extends string> {
  label: string;
  name: string;
  value: T;
  onChange: (value: T) => void;
  options: readonly { value: T; label: string }[];
  error?: string | null;
}

/** A labelled dropdown that matches `TextField`. */
export function SelectField<T extends string>({ label, name, value, onChange, options, error }: SelectFieldProps<T>) {
  const errorId = `${name}-error`;
  return (
    <label className="flex min-w-0 flex-col gap-1.5 text-[13px] font-bold">
      {label}
      <select
        name={name}
        value={value}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? errorId : undefined}
        onChange={(event) => {
          const chosen = options.find((option) => option.value === event.target.value);
          if (chosen) onChange(chosen.value);
        }}
        className="h-12 w-full rounded-control border border-input-line bg-surface px-2.5 text-sm font-normal text-ink"
      >
        {options.map((option) => (
          <option key={option.value} value={option.value}>{option.label}</option>
        ))}
      </select>
      {error ? (
        <span id={errorId} role="alert" className="text-xs font-normal text-error">
          {error}
        </span>
      ) : null}
    </label>
  );
}
