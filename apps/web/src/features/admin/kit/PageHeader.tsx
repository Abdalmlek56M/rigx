import type { ReactNode } from "react";
import { useTranslations } from "next-intl";

/** The top of every admin page: "Admin" above the page title (the store's 28 px page title), actions at the end (they wrap under it on phones). */
export function PageHeader({ title, actions }: { title: string; actions?: ReactNode }) {
  const t = useTranslations("admin.shell");
  return (
    <div className="flex flex-wrap items-center justify-between gap-3">
      <div className="flex min-w-0 flex-col gap-1">
        <span className="type-label">{t("eyebrow")}</span>
        <h1 className="m-0 type-page-title">{title}</h1>
      </div>
      {actions ? <div className="flex flex-wrap items-center gap-2">{actions}</div> : null}
    </div>
  );
}

/** A form's message for errors that belong to no field. */
export function FormMessage({ message }: { message: string | null }) {
  return message ? <p role="alert" className="m-0 rounded-control bg-paper px-3 py-2 text-sm font-bold text-error-text">{message}</p> : null;
}
