import type { ReactNode } from "react";
import { WhatsAppIcon } from "./icons";
import { buttonClass, type ButtonSize } from "./Button";

interface WhatsAppButtonProps {
  href: string;
  children?: ReactNode;
  /** The accessible name of an icon-only button. */
  label?: string;
  size?: ButtonSize;
  variant?: "solid" | "outline";
  className?: string;
  onClick?: () => void;
}

/**
 * The one WhatsApp action style: green for "order on WhatsApp", an outline for a quiet question. Always opens a new tab;
 * the shared look is what makes the green read as WhatsApp rather than a second brand colour.
 */
export function WhatsAppButton({ href, children, label, size = "md", variant = "solid", className, onClick }: WhatsAppButtonProps) {
  const iconOnly = children === undefined;
  const tone = variant === "solid"
    ? "bg-whatsapp text-white hover:brightness-105"
    : "bg-surface text-ink hover:text-whatsapp";
  // The outline option is a secondary button: the ghost base would mute its text on the dark surface.
  const base = buttonClass({ variant: variant === "solid" ? "ghost" : "secondary", size, iconOnly });
  return (
    <a
      href={href}
      target="_blank"
      rel="noreferrer"
      aria-label={iconOnly ? label : undefined}
      data-tip={iconOnly ? label : undefined}
      onClick={onClick}
      className={`${iconOnly ? "tip " : ""}${base} ${tone}${className ? ` ${className}` : ""}`}
    >
      <WhatsAppIcon size={size === "sm" ? 16 : 18} />
      {iconOnly ? null : children}
    </a>
  );
}
