"use client";

import dynamic from "next/dynamic";
import { useTheme } from "@/components/layout/use-theme";

// The canvas code is only downloaded in the dark theme, after the page is interactive.
const CircuitCanvas = dynamic(() => import("./CircuitCanvas"), { ssr: false });

/**
 * The dark theme's circuit board canvas for a hero or a page header (the faint lights behind the whole page are `.page-glow`, CSS only).
 * The parent must be `isolate` (the layer sits at z-index -1 inside it). Nothing shows in the light theme. `cover` fades it out only at
 * the very edges, for a page whose whole visible area is the backdrop (sign-in).
 */
export function Atmosphere({ cover = false }: { cover?: boolean }) {
  const theme = useTheme();
  return <div aria-hidden="true" className={cover ? "ambient ambient-cover" : "ambient"}>{theme === "dark" ? <CircuitCanvas /> : null}</div>;
}
