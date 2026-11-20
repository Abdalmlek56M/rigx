/** The list under a ComboBox: matches anywhere in the label, case-insensitive in Arabic and English. */
export interface ComboOption {
  value: string;
  label: string;
}

export function filterComboOptions<T extends ComboOption>(options: readonly T[], query: string): T[] {
  const needle = query.trim().toLocaleLowerCase();
  if (!needle) return [...options];
  return options.filter((option) => option.label.toLocaleLowerCase().includes(needle));
}
