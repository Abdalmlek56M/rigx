import { BoxIcon, GridIcon, RefreshIcon } from "./icons";

/** The icon of a condition choice: a sealed box for "جديد", the refresh arrows for "مستعمل", the grid for "all / any". */
export function conditionIcon(condition: string | null) {
  return condition === "New" ? BoxIcon : condition === "Used" ? RefreshIcon : GridIcon;
}
