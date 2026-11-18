import { toggle } from "./user-rules.ts";
import { useRoleLabel } from "./use-role-label";
import type { StaffRole } from "./users-api";

/** One checkbox per role (name, with the code in mono under it). */
export function RoleCheckboxes({ roles, selected, onChange, disabled = false }: {
  roles: readonly StaffRole[];
  selected: readonly string[];
  onChange: (selected: string[]) => void;
  disabled?: boolean;
}) {
  const label = useRoleLabel();
  return (
    <ul className="m-0 flex list-none flex-col gap-2 p-0">
      {roles.map((role) => (
        <li key={role.id}>
          <label className="flex min-h-11 items-center gap-3 rounded-control border border-line bg-paper px-3 py-1.5 text-sm">
            <input
              type="checkbox" disabled={disabled} checked={selected.includes(role.id)}
              onChange={(event) => onChange(toggle(selected, role.id, event.target.checked))} className="h-5 w-5 accent-orange"
            />
            <span className="flex min-w-0 flex-col">
              <span className="font-bold">{label(role)}</span>
              <span dir="ltr" className="font-mono text-[11px] text-ink-muted">{role.code}</span>
            </span>
          </label>
        </li>
      ))}
    </ul>
  );
}
