"use client";

import { MinusIcon, PlusIcon } from "./icons";
import { IconButton } from "./Button";

interface QuantityStepperProps {
  value: number;
  min?: number;
  max: number;
  onChange: (value: number) => void;
  /** The group's accessible name ("الكمية"). */
  label: string;
  decreaseLabel: string;
  increaseLabel: string;
  /** `sm` fits a cart row; `md` fits a purchase panel. */
  size?: "sm" | "md";
}

/** Minus / count / plus with two labelled icon buttons; it never lets the count leave `min..max`. */
export function QuantityStepper({ value, min = 1, max, onChange, label, decreaseLabel, increaseLabel, size = "md" }: QuantityStepperProps) {
  const countClass = size === "sm" ? "min-w-6 text-sm" : "min-w-8 text-base";
  return (
    <div role="group" aria-label={label} className="flex items-center gap-1.5">
      <IconButton
        icon={MinusIcon}
        label={decreaseLabel}
        variant="secondary"
        size={size === "sm" ? "sm" : "md"}
        disabled={value <= min}
        onClick={() => onChange(Math.max(min, value - 1))}
      />
      <span dir="ltr" className={`text-center font-mono font-semibold tabular-nums ${countClass}`}>{value}</span>
      <IconButton
        icon={PlusIcon}
        label={increaseLabel}
        variant="secondary"
        size={size === "sm" ? "sm" : "md"}
        disabled={value >= max}
        onClick={() => onChange(Math.min(max, value + 1))}
      />
    </div>
  );
}
