interface SelectFieldProps<T extends string> {
  label: string;
  name: string;
  value: T;
  onChange: (value: T) => void;
  options: readonly { value: T; label: string }[];
}

/** A labelled dropdown that matches `TextField`. */
export function SelectField<T extends string>({ label, name, value, onChange, options }: SelectFieldProps<T>) {
  return (
    <label className="flex min-w-0 flex-col gap-1.5 text-[13px] font-bold">
      {label}
      <select
        name={name}
        value={value}
        onChange={(event) => {
          const chosen = options.find((option) => option.value === event.target.value);
          if (chosen) onChange(chosen.value);
        }}
        className="h-11 w-full rounded-control border border-input-line bg-surface px-2.5 text-sm font-normal text-ink"
      >
        {options.map((option) => (
          <option key={option.value} value={option.value}>{option.label}</option>
        ))}
      </select>
    </label>
  );
}
