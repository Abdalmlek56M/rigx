import type { ButtonHTMLAttributes, ComponentType, ReactNode } from "react";
import { Link } from "@/i18n/navigation";

/**
 * The storefront button set. Every action on the site is one of these variants:
 * - `primary`: the one main action of a screen (graphite).
 * - `accent`: a buying action (orange).
 * - `secondary`: other actions (outlined).
 * - `ghost`: quiet actions inside rows and toolbars.
 * - `danger`: deletes, cancellations and other destructive actions.
 * A disabled button turns flat grey (`.btn-off` in globals.css), whatever its variant; the rules are in docs/design/storefront-kit.md.
 *
 * Use `IconButton` when the action is clear from its icon (it requires a `label`), and `ButtonLink` for a link that
 * looks like a button. Words stay for the one main action of a screen; everything else can be an icon with a tip.
 */
export type ButtonVariant = "primary" | "accent" | "secondary" | "ghost" | "danger";
export type ButtonSize = "md" | "sm";
export type ButtonIcon = ComponentType<{ size?: number }>;

const BASE =
  "press inline-flex shrink-0 items-center justify-center gap-2 whitespace-nowrap rounded-control font-bold select-none max-sm:min-h-11 " +
  "[transition:background-color_150ms_ease,border-color_150ms_ease,color_150ms_ease,box-shadow_150ms_ease,transform_110ms_var(--ease-out)] " +
  "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-orange " +
  "disabled:cursor-not-allowed aria-disabled:pointer-events-none";

const VARIANTS: Record<ButtonVariant, string> = {
  primary: "btn-off surface-strong hover:brightness-110",
  accent: "btn-off accent-strong buy-sweep",
  secondary: "btn-off border border-input-line bg-surface text-ink hover:border-ink-muted hover:bg-paper",
  ghost: "btn-off-quiet text-ink-muted hover:bg-line hover:text-ink",
  danger: "btn-off bg-error text-white hover:bg-error-hover"
};

const SIZES: Record<ButtonSize, { label: string; square: string; icon: number }> = {
  md: { label: "h-11 px-4 text-sm", square: "size-11", icon: 18 },
  sm: { label: "h-9 px-3 text-[13px]", square: "size-9 max-sm:size-11", icon: 16 }
};

/** The classes of a button-looking control (for a file `label`, a `summary` or another element that cannot be a button). */
export function buttonClass({ variant = "primary", size = "md", iconOnly = false }: { variant?: ButtonVariant; size?: ButtonSize; iconOnly?: boolean } = {}): string {
  return `${BASE} ${VARIANTS[variant]} ${iconOnly ? SIZES[size].square : SIZES[size].label}`;
}

/** The icon size that matches a button size, for an icon used outside `Button`. */
export const buttonIconSize = (size: ButtonSize = "md") => SIZES[size].icon;

function Spinner({ size }: { size: number }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" aria-hidden="true" className="shrink-0 animate-spin motion-reduce:animate-none">
      <circle cx="12" cy="12" r="9" stroke="currentColor" strokeOpacity="0.25" strokeWidth="3" />
      <path d="M21 12a9 9 0 0 0-9-9" stroke="currentColor" strokeWidth="3" strokeLinecap="round" />
    </svg>
  );
}

type NativeButton = Omit<ButtonHTMLAttributes<HTMLButtonElement>, "children">;

interface ButtonProps extends NativeButton {
  variant?: ButtonVariant;
  size?: ButtonSize;
  icon?: ButtonIcon;
  /** Shows a spinner in place of the icon and disables the button while the action runs. */
  loading?: boolean;
  /** Drop the label and show only the icon; `label` then becomes the accessible name and the hover tip. */
  iconOnly?: boolean;
  /** The accessible name of an icon-only button. */
  label?: string;
  children?: ReactNode;
}

/** A storefront action: icon + label. Defaults to `type="button"`, so only a real submit says `type="submit"`. */
export function Button({ variant = "primary", size = "md", icon: Icon, loading = false, iconOnly = false, label, disabled, className, type = "button", children, ...rest }: ButtonProps) {
  const iconSize = SIZES[size].icon;
  const accessible = iconOnly ? label : undefined;
  return (
    <button
      type={type}
      aria-label={accessible}
      data-tip={iconOnly ? label : undefined}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      className={`${iconOnly ? "tip " : ""}${buttonClass({ variant, size, iconOnly })}${className ? ` ${className}` : ""}`}
      {...rest}
    >
      {loading ? <Spinner size={iconSize} /> : Icon ? <Icon size={iconSize} /> : null}
      {iconOnly ? null : children}
    </button>
  );
}

interface IconButtonProps extends NativeButton {
  icon: ButtonIcon;
  /** Read by screen readers and shown as the hover tip. */
  label: string;
  variant?: ButtonVariant;
  size?: ButtonSize;
  loading?: boolean;
}

/** An icon-only action, with the label as `aria-label` and as the hover tip. */
export function IconButton({ icon: Icon, label, variant = "ghost", size = "md", loading = false, disabled, className, type = "button", ...rest }: IconButtonProps) {
  const iconSize = SIZES[size].icon;
  return (
    <button
      type={type}
      aria-label={label}
      data-tip={label}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      className={`tip ${buttonClass({ variant, size, iconOnly: true })}${className ? ` ${className}` : ""}`}
      {...rest}
    >
      {loading ? <Spinner size={iconSize} /> : <Icon size={iconSize} />}
    </button>
  );
}

interface ButtonLinkProps {
  href: string;
  variant?: ButtonVariant;
  size?: ButtonSize;
  icon?: ButtonIcon;
  iconOnly?: boolean;
  /** The accessible name of an icon-only link. */
  label?: string;
  /** Opens in a new tab with `rel="noreferrer"` (WhatsApp, social links). */
  external?: boolean;
  /** A file download link. */
  download?: boolean;
  prefetch?: boolean;
  className?: string;
  /** Runs after the link is followed (closing a sheet, for example). */
  onClick?: () => void;
  children?: ReactNode;
}

/** A link that looks like a button (navigation never needs a `<button>`). */
export function ButtonLink({ href, variant = "primary", size = "md", icon: Icon, iconOnly = false, label, external = false, download = false, prefetch, className, onClick, children }: ButtonLinkProps) {
  const iconSize = SIZES[size].icon;
  const classes = `${iconOnly ? "tip " : ""}${buttonClass({ variant, size, iconOnly })}${className ? ` ${className}` : ""}`;
  const body = (
    <>
      {Icon ? <Icon size={iconSize} /> : null}
      {iconOnly ? null : children}
    </>
  );
  if (external || download) {
    return (
      <a
        href={href}
        aria-label={iconOnly ? label : undefined}
        data-tip={iconOnly ? label : undefined}
        {...(external ? { target: "_blank", rel: "noreferrer" } : {})}
        {...(download ? { download: true } : {})}
        onClick={onClick}
        className={classes}
      >
        {body}
      </a>
    );
  }
  return (
    <Link href={href} prefetch={prefetch} aria-label={iconOnly ? label : undefined} data-tip={iconOnly ? label : undefined} onClick={onClick} className={classes}>
      {body}
    </Link>
  );
}
