import type { ReactNode } from "react";
import { HelpIcon } from "@/components/ui/icons";
import { Link } from "@/i18n/navigation";

/** A small outlined pill that points a visitor who is unsure to help (the recommender). A link, never an underlined sentence. */
export function HelpLink({ href, children }: { href: string; children: ReactNode }) {
  return (
    <Link
      href={href}
      className="press inline-flex min-h-10 w-fit items-center gap-2 rounded-full border border-line bg-surface px-4 text-sm font-medium transition-colors hover:border-orange/50 hover:text-orange-text"
    >
      <span aria-hidden="true" className="text-orange-text"><HelpIcon size={16} /></span>
      {children}
    </Link>
  );
}
