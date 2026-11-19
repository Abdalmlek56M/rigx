import type { ComponentType } from "react";
import { BoardIcon, CaseIcon, CpuIcon, FanIcon, GpuIcon, HeadsetIcon, MonitorIcon, PowerIcon, RamIcon, StorageIcon } from "./icons";

type IconComponent = ComponentType<{ size?: number }>;

/**
 * Slug words → icon, first match wins. Category slugs are owner-made, so they are matched loosely; every base category slug
 * (`BaseCatalog` in the Catalog module) has a rule here.
 */
const RULES: readonly [readonly string[], IconComponent][] = [
  [["accessor", "peripheral", "keyboard", "mouse", "headset"], HeadsetIcon],
  [["monitor", "screen", "display"], MonitorIcon],
  [["graphics", "gpu", "vga"], GpuIcon],
  [["motherboard", "board"], BoardIcon],
  [["processor", "cpu", "intel", "amd"], CpuIcon],
  [["memory", "ram"], RamIcon],
  [["storage", "ssd", "hdd", "nvme", "hard"], StorageIcon],
  [["power", "psu"], PowerIcon],
  [["cool", "fan"], FanIcon],
  [["case"], CaseIcon]
];

/** The icon for a category slug; unknown categories get the chip icon. */
export function CategoryIcon({ slug, size }: { slug: string; size?: number }) {
  const Match = RULES.find(([words]) => words.some((word) => slug.includes(word)))?.[1] ?? CpuIcon;
  return <Match size={size} />;
}
