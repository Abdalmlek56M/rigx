import type { ComponentType, ReactNode } from "react";

export type CardPadding = "none" | "sm" | "md" | "lg";

const PADDING: Record<CardPadding, string> = {
  none: "",
  sm: "p-4",
  md: "p-5 md:p-6",
  lg: "p-6 md:p-8"
};

export interface CardClassOptions {
  padding?: CardPadding;
  /** Lifts the card under a mouse and marks it as a whole-card link. */
  interactive?: boolean;
  /** `grid` for a card that lays its own content out in columns (a form); the default is a flex column. */
  layout?: "flex" | "grid";
  className?: string;
}

/** The shared surface of every storefront card; use it when a card must be a `Link` or another element `Card` cannot be. */
export function cardClass({ padding = "md", interactive = false, layout = "flex", className = "" }: CardClassOptions = {}): string {
  const base = layout === "grid" ? "grid min-w-0 rounded-card" : "flex min-w-0 flex-col rounded-card";
  return `${base} border border-line bg-surface ${PADDING[padding]}${interactive ? " lift" : ""}${className ? ` ${className}` : ""}`;
}

type CardElement = "section" | "article" | "div" | "li" | "aside" | "nav";

interface CardProps {
  /** The card's title row. Omit for a card that only groups content. */
  title?: ReactNode;
  /** An icon before the title, drawn muted so the title stays the loudest thing in the row. */
  icon?: ComponentType<{ size?: number }>;
  /** Buttons or links on the end of the title row (they wrap under it on phones). */
  actions?: ReactNode;
  padding?: CardPadding;
  /** Lifts the card under a mouse (for a card that opens something). */
  interactive?: boolean;
  /** The element to render; `li` for a card inside a list. */
  as?: CardElement;
  /** An id for the title, so a section can be labelled by it. */
  titleId?: string;
  /** An id for the card itself (a link target such as `#recommend`). */
  id?: string;
  /** The accessible name when the card is a nav or another labelled landmark. */
  "aria-label"?: string;
  className?: string;
  children?: ReactNode;
}

/**
 * A calm card: a bordered surface with an optional title row (icon + title + actions), then its content. Every block of
 * content on a page belongs in one of these, so nothing floats on the page background.
 */
export function Card({ title, icon: Icon, actions, padding = "md", interactive = false, as: Element = "section", titleId, id, "aria-label": ariaLabel, className, children }: CardProps) {
  const hasTitle = title !== undefined;
  return (
    <Element id={id} aria-label={ariaLabel} className={cardClass({ padding, interactive, className: `${hasTitle ? "gap-3 " : ""}${className ?? ""}` })}>
      {hasTitle || actions ? (
        <div className="flex flex-wrap items-center justify-between gap-x-3 gap-y-2">
          {hasTitle ? (
            <h2 id={titleId} className="m-0 flex min-w-0 items-center gap-2 type-section-title">
              {Icon ? <span aria-hidden="true" className="flex text-ink-muted"><Icon size={18} /></span> : null}
              <span className="min-w-0">{title}</span>
            </h2>
          ) : <span />}
          {actions ? <div className="flex flex-wrap items-center gap-2">{actions}</div> : null}
        </div>
      ) : null}
      {children}
    </Element>
  );
}
