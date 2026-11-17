import type { ReactNode } from "react";
import { useTranslations } from "next-intl";

/** The top of every admin page: "Admin" above the page title, actions at the end (they wrap under it on phones). */
export function PageHeader({ title, actions }: { title: string; actions?: ReactNode }) {
  const t = useTranslations("admin.shell");
  return (
    <div className="flex flex-wrap items-center justify-between gap-3">
      <div className="flex flex-col gap-0.5">
        <span className="text-[13px] text-ink-muted">{t("eyebrow")}</span>
        <h1 className="m-0 text-[26px] font-bold">{title}</h1>
      </div>
      {actions ? <div className="flex flex-wrap items-center gap-2">{actions}</div> : null}
    </div>
  );
}

/** A form's message for errors that belong to no field. */
export function FormMessage({ message }: { message: string | null }) {
  return message ? <p role="alert" className="m-0 rounded-control bg-paper px-3 py-2 text-sm font-bold text-error">{message}</p> : null;
}
