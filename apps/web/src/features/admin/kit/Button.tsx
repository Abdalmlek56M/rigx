import type { ButtonHTMLAttributes, ComponentType, ReactNode } from "react";

/**
 * The admin button set (4.10b C). Every admin action is one of these: an icon and a label in one of four variants, or an icon-only
 * button with a hover tip. Links that look like buttons use `buttonClass` (plus the icon as their first child).
 *
 * - primary: the one main action of a form or screen (graphite).
 * - secondary: other actions (outlined).
 * - danger: deletes, cancellations, resets (red).
 * - ghost: quiet actions inside rows and toolbars (no border until hover).
 */
export type ButtonVariant = "primary" | "secondary" | "danger" | "ghost";
export type ButtonSize = "md" | "sm";
export type ButtonIcon = ComponentType<{ size?: number }>;

const BASE =
  "press inline-flex shrink-0 items-center justify-center gap-2 whitespace-nowrap rounded-control font-bold select-none " +
  // Colours ease on hover; the press scale (0.97, from `.press`) uses the site's strong ease-out so it answers at once.
  "[transition:background-color_150ms_ease,border-color_150ms_ease,color_150ms_ease,box-shadow_150ms_ease,transform_140ms_var(--ease-out)] " +
  "focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-orange " +
  "disabled:cursor-not-allowed disabled:opacity-50 aria-disabled:pointer-events-none aria-disabled:opacity-50";

const VARIANTS: Record<ButtonVariant, string> = {
  primary: "bg-carbon text-on-carbon hover:bg-carbon-hover hover:shadow-soft disabled:hover:bg-carbon disabled:hover:shadow-none",
  secondary: "border border-input-line bg-surface text-ink hover:border-ink-muted hover:bg-paper disabled:hover:border-input-line disabled:hover:bg-surface",
  danger: "bg-error text-white hover:bg-error-hover hover:shadow-soft disabled:hover:bg-error disabled:hover:shadow-none",
  ghost: "text-ink-muted hover:bg-line hover:text-ink disabled:hover:bg-transparent"
};

const SIZES: Record<ButtonSize, { label: string; square: string; icon: number }> = {
  md: { label: "h-11 px-4 text-sm", square: "size-11", icon: 18 },
  sm: { label: "h-9 px-3 text-[13px]", square: "size-9", icon: 16 }
};

/** The classes of a button-looking control (for a `Link`, a file `label` or a `summary`). */
export function buttonClass({ variant = "secondary", size = "md", iconOnly = false }: { variant?: ButtonVariant; size?: ButtonSize; iconOnly?: boolean } = {}): string {
  return `${BASE} ${VARIANTS[variant]} ${iconOnly ? SIZES[size].square : SIZES[size].label}`;
}

/** The icon size that matches a button size, for links built with `buttonClass`. */
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
  /** Shows a spinner in place of the icon and disables the button while an action runs. */
  loading?: boolean;
  children: ReactNode;
}

/** An admin action: icon + label. Defaults to `type="button"`, so only a real submit says `type="submit"`. */
export function Button({ variant = "secondary", size = "md", icon: Icon, loading = false, disabled, className, type = "button", children, ...rest }: ButtonProps) {
  const iconSize = SIZES[size].icon;
  return (
    <button
      type={type}
      disabled={disabled || loading}
      aria-busy={loading || undefined}
      className={`${buttonClass({ variant, size })}${className ? ` ${className}` : ""}`}
      {...rest}
    >
      {loading ? <Spinner size={iconSize} /> : Icon ? <Icon size={iconSize} /> : null}
      <span>{children}</span>
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

/** An icon-only admin action, with its label as `aria-label` and the storefront's hover tip (`.tip` + `data-tip`). */
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
