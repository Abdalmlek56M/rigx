import type { ComponentType, ReactNode } from "react";
import { Link } from "@/i18n/navigation";

export interface BreadcrumbItem {
  label: string;
  /** Omit on the last item (the current page). */
  href?: string;
}

interface PageHeaderProps {
  title: ReactNode;
  /** A short line under the title; one sentence at most. */
  lead?: ReactNode;
  /** The page path, from the home page down to this one. */
  breadcrumb?: readonly BreadcrumbItem[];
  /** The accessible name of the breadcrumb ("مسار الصفحة"). */
  breadcrumbLabel?: string;
  /** Page-level actions on the end side (they wrap under the title on phones). */
  actions?: ReactNode;
  /** Content under the title row, inside the same header (a step indicator, for example). */
  below?: ReactNode;
  /** An icon before the title. */
  icon?: ComponentType<{ size?: number }>;
  /** A small line above the title (for example the invoice kind). */
  eyebrow?: string;
  /** The title's own direction when it is a code or a Latin model name. */
  titleDir?: "ltr" | "rtl" | "auto";
  /** Wrap the header in the page container (default); turn off inside a page that already has one. */
  contained?: boolean;
  className?: string;
}

/**
 * The top of every storefront page: an optional breadcrumb, the 28 px page title, an optional lead line and the page
 * actions. This is the only place a heading sits directly on the page background.
 */
export function PageHeader({ title, lead, breadcrumb, breadcrumbLabel, actions, below, icon: Icon, eyebrow, titleDir, contained = true, className = "" }: PageHeaderProps) {
  return (
    <header className={`${contained ? "shell " : ""}flex flex-col gap-3 pb-5 pt-8 md:pt-10${className ? ` ${className}` : ""}`}>
      {breadcrumb && breadcrumb.length > 0 ? (
        <nav aria-label={breadcrumbLabel} className="text-[13px] text-ink-muted">
          <ol className="m-0 flex list-none flex-wrap items-center gap-x-2 gap-y-1 p-0">
            {breadcrumb.map((item, index) => (
              <li key={`${item.label}-${index}`} className="flex items-center gap-2">
                {index > 0 ? <span aria-hidden="true" className="text-line">/</span> : null}
                {item.href ? (
                  <Link href={item.href} className="hover:text-ink hover:underline">{item.label}</Link>
                ) : (
                  <span aria-current="page" dir={titleDir}>{item.label}</span>
                )}
              </li>
            ))}
          </ol>
        </nav>
      ) : null}
      <div className="flex flex-wrap items-end justify-between gap-x-6 gap-y-3">
        <div className="flex min-w-0 flex-col gap-2">
          {eyebrow ? <span className="type-label font-bold text-orange-text">{eyebrow}</span> : null}
          <h1 dir={titleDir} className="m-0 flex min-w-0 items-center gap-2.5 type-page-title">
            {Icon ? <span aria-hidden="true" className="flex text-ink-muted"><Icon size={26} /></span> : null}
            <span className="min-w-0 [unicode-bidi:isolate]">{title}</span>
          </h1>
          {lead ? <p className="m-0 max-w-[70ch] text-sm leading-7 text-ink-muted">{lead}</p> : null}
        </div>
        {actions ? <div className="flex flex-wrap items-center gap-2">{actions}</div> : null}
      </div>
      {below}
    </header>
  );
}
