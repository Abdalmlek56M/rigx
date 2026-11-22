export type CircuitMode = "off" | "static" | "live";

export interface Capabilities {
  reducedMotion: boolean;
  saveData: boolean;
  /** `navigator.hardwareConcurrency`; 0 when the browser does not say. */
  cores: number;
  /** A phone-sized screen: a battery is worth more there than a moving pulse. */
  narrow: boolean;
}

/** Cores at or below this count are a low-end device: they get the still picture. */
export const LOW_END_CORES = 4;

/**
 * How the circuit board is shown: not at all when the visitor asked to save data, as a still picture for reduced motion, phone-sized
 * screens and low-end devices (an unknown core count is treated as capable), and animated otherwise.
 */
export function circuitMode({ reducedMotion, saveData, cores, narrow }: Capabilities): CircuitMode {
  if (saveData) return "off";
  if (reducedMotion || narrow || (cores > 0 && cores <= LOW_END_CORES)) return "static";
  return "live";
}

/** A `#rrggbb` token as red, green and blue (a fallback grey when the value is not that). */
export function hexToRgb(value: string): readonly [number, number, number] {
  const match = /^#([0-9a-f]{6})$/i.exec(value.trim());
  if (!match) return [200, 200, 200];
  const number = Number.parseInt(match[1] as string, 16);
  return [(number >> 16) & 255, (number >> 8) & 255, number & 255];
}
