import { useTranslations } from "next-intl";
import { groupPermissions, groupSelected, groupTitleKey, setGroup, toggle, systemCodes } from "./user-rules.ts";

/** Permission codes grouped by their prefix, with "select all" per group and a warning (not a block) for `System.*` codes. */
export function PermissionPicker({ codes, selected, onChange }: { codes: readonly string[]; selected: readonly string[]; onChange: (selected: string[]) => void }) {
  const t = useTranslations("admin.users.permissions");
  const groups = groupPermissions(codes);
  const system = systemCodes(selected);

  return (
    <div className="flex min-w-0 flex-col gap-3">
      {groups.map((group) => {
        const key = groupTitleKey(group.prefix);
        const all = groupSelected(group, selected);
        return (
          <fieldset key={group.prefix} className="m-0 flex min-w-0 flex-col gap-1 rounded-control border border-line bg-paper p-3">
            <legend className="px-1 text-sm font-bold">{key ? t(`groups.${key}`) : group.prefix}</legend>
            <label className="flex min-h-11 items-center gap-3 text-xs font-bold text-ink-muted">
              <input type="checkbox" checked={all} onChange={(event) => onChange(setGroup(group, selected, event.target.checked))} className="h-5 w-5 accent-orange" />
              {t("selectGroup")}
            </label>
            {group.codes.map((code) => (
              <label key={code} className="flex min-h-11 items-center gap-3 text-sm">
                <input type="checkbox" checked={selected.includes(code)} onChange={(event) => onChange(toggle(selected, code, event.target.checked))} className="h-5 w-5 accent-orange" />
                <span dir="ltr" className="font-mono text-xs [overflow-wrap:anywhere]">{code}</span>
              </label>
            ))}
          </fieldset>
        );
      })}
      {system.length > 0 ? <p role="status" className="m-0 rounded-control bg-error/10 px-3 py-2 text-sm font-bold text-error">{t("systemWarning")}</p> : null}
    </div>
  );
}
