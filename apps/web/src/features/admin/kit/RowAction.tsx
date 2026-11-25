"use client";

import { useTranslations } from "next-intl";
import { EyeIcon, PencilIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";
import { Button, buttonClass, buttonIconSize } from "./Button";

/**
 * The visible way to open a list row (its name stays a shortcut): a labelled button, or a link when the row has its own page.
 * `subject` names the row for screen readers; `current` marks the row whose panel is open under the list.
 */
export function RowAction({ kind, subject, href, onClick, current }: {
  kind: "view" | "edit" | "viewEdit";
  subject: string;
  href?: string;
  onClick?: () => void;
  current?: boolean;
}) {
  const t = useTranslations("admin.kit.rows");
  const Icon = kind === "view" ? EyeIcon : PencilIcon;
  const name = `${t(kind)}: ${subject}`;
  if (href) {
    return (
      <Link href={href} aria-label={name} className={buttonClass({ size: "sm" })}>
        <Icon size={buttonIconSize("sm")} />
        {t(kind)}
      </Link>
    );
  }
  return <Button size="sm" icon={Icon} aria-label={name} aria-current={current ? "true" : undefined} onClick={onClick}>{t(kind)}</Button>;
}
