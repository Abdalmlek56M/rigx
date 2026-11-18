import type { ComponentType } from "react";
import { BoardIcon, CaseIcon, CpuIcon, FanIcon, GpuIcon, MonitorIcon, PowerIcon, RamIcon, StorageIcon } from "./icons";

type IconComponent = ComponentType<{ size?: number }>;

/** Slug words → icon, first match wins (category slugs are owner-made, so they are matched loosely). */
const RULES: readonly [readonly string[], IconComponent][] = [
  [["peripheral", "monitor", "screen", "keyboard", "mouse"], MonitorIcon],
  [["graphics", "gpu", "vga"], GpuIcon],
  [["motherboard", "board"], BoardIcon],
  [["processor", "cpu"], CpuIcon],
  [["memory", "ram"], RamIcon],
  [["storage", "ssd", "hdd", "nvme"], StorageIcon],
  [["power", "psu"], PowerIcon],
  [["cool", "fan"], FanIcon],
  [["case"], CaseIcon]
];

/** The icon for a category slug; unknown categories get the chip icon. */
export function CategoryIcon({ slug, size }: { slug: string; size?: number }) {
  const Match = RULES.find(([words]) => words.some((word) => slug.includes(word)))?.[1] ?? CpuIcon;
  return <Match size={size} />;
}
