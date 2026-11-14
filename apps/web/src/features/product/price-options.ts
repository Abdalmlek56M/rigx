import type { PriceOption } from "../cart/cart-rules.ts";

export interface Prices {
  inBuild: number;
  withBoard: number;
  alone: number;
}

export interface PriceChoice {
  option: PriceOption;
  price: number;
}

/** What to take it with: a full build, a board from us, or alone (the default, the standalone price). */
export const DEFAULT_OPTION: PriceOption = "Alone";

/**
 * The context-price radios, best price first. When the owner priced all three the same there is nothing to choose,
 * so the list is empty and the page just shows the one price.
 */
export function priceChoices(prices: Prices): PriceChoice[] {
  if (prices.inBuild === prices.withBoard && prices.withBoard === prices.alone) return [];
  return [
    { option: "InBuild", price: prices.inBuild },
    { option: "WithBoard", price: prices.withBoard },
    { option: "Alone", price: prices.alone }
  ];
}

export function priceOf(prices: Prices, option: PriceOption): number {
  if (option === "InBuild") return prices.inBuild;
  return option === "WithBoard" ? prices.withBoard : prices.alone;
}
