/** The ready icon set the owner picks a category icon from (the same keys as `CategoryIcons.Keys` in the Catalog module). */
export const CATEGORY_ICON_KEYS = [
  "cpu", "gpu", "motherboard", "ram", "storage", "power", "cooling", "case", "monitor", "keyboard", "mouse",
  "headset", "gamepad", "laptop", "network", "cable", "accessories"
] as const;

export type CategoryIconKey = (typeof CATEGORY_ICON_KEYS)[number];

/**
 * Slug words → icon, first match wins, for a category the owner gave no icon. Category slugs are owner-made, so they are
 * matched loosely; every base category slug (`BaseCatalog` in the Catalog module) has a rule here.
 */
const SLUG_RULES: readonly [readonly string[], CategoryIconKey][] = [
  [["keyboard"], "keyboard"],
  [["mouse", "mice"], "mouse"],
  [["accessor", "peripheral", "headset"], "headset"],
  [["monitor", "screen", "display"], "monitor"],
  [["graphics", "gpu", "vga"], "gpu"],
  [["motherboard", "board"], "motherboard"],
  [["processor", "cpu", "intel", "amd"], "cpu"],
  [["memory", "ram"], "ram"],
  [["storage", "ssd", "hdd", "nvme", "hard"], "storage"],
  [["power", "psu"], "power"],
  [["cool", "fan"], "cooling"],
  [["case"], "case"],
  [["laptop", "notebook"], "laptop"],
  [["network", "wifi", "router"], "network"],
  [["cable"], "cable"],
  [["game", "controller"], "gamepad"]
];

const isKey = (value: string): value is CategoryIconKey => (CATEGORY_ICON_KEYS as readonly string[]).includes(value);

/** The icon the owner picked, else one guessed from the slug, else the chip. */
export function categoryIconKey(slug: string, picked?: string | null): CategoryIconKey {
  if (picked && isKey(picked)) return picked;
  return SLUG_RULES.find(([words]) => words.some((word) => slug.includes(word)))?.[1] ?? "cpu";
}
