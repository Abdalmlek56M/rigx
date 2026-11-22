"use client";

import dynamic from "next/dynamic";
import { useTheme } from "@/components/layout/use-theme";

// The canvas code is only downloaded in the dark theme, after the page is interactive.
const CircuitCanvas = dynamic(() => import("./CircuitCanvas"), { ssr: false });

/**
 * The dark theme's backdrop for a hero or a page header: two slow lights in CSS plus the circuit board canvas. The parent must
 * be `isolate` (the layer sits at z-index -1 inside it). Nothing shows in the light theme.
 */
export function Atmosphere() {
  const theme = useTheme();
  return <div aria-hidden="true" className="ambient">{theme === "dark" ? <CircuitCanvas /> : null}</div>;
}
