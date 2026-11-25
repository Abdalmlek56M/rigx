export type SpecIconKey =
  | "wifi" | "sparkle" | "fan" | "cooling" | "board" | "case" | "layers" | "gauge" | "ruler" | "power" | "plug" | "memory"
  | "storage" | "monitor" | "gpu" | "cpu" | "tag";

/**
 * Spec key words → icon, first match wins. Spec keys are owner-made (snake case, e.g. `vram_type`), so they are matched
 * loosely; every starter spec of the base categories has a fitting rule, and an unknown key gets the tag.
 */
const RULES: readonly [readonly string[], SpecIconKey][] = [
  [["wifi", "bluetooth", "wireless"], "wifi"],
  [["rgb", "light"], "sparkle"],
  [["fan"], "fan"],
  [["cooler", "cooling", "radiator"], "cooling"],
  [["socket", "chipset", "form_factor", "slot", "board"], "board"],
  [["side_panel", "case"], "case"],
  [["type"], "layers"],
  [["clock", "speed", "refresh", "response", "latency"], "gauge"],
  [["length", "size", "height", "width"], "ruler"],
  [["power", "watt", "tdp", "efficiency"], "power"],
  [["connector", "port", "interface", "modular", "pcie"], "plug"],
  [["vram", "memory", "ram", "cache", "module"], "memory"],
  [["capacity", "storage"], "storage"],
  [["panel", "resolution", "curved", "screen"], "monitor"],
  [["gpu", "graphics"], "gpu"],
  [["core", "thread"], "cpu"]
];

/** The icon of a spec, picked from its key. */
export function specIconKey(key: string): SpecIconKey {
  const lower = key.toLowerCase();
  return RULES.find(([words]) => words.some((word) => lower.includes(word)))?.[1] ?? "tag";
}
