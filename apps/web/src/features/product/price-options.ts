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

const KNOWN: readonly PriceOption[] = ["Alone", "WithBoard", "InBuild"];
/** The order choices are listed in: the cheapest context first. */
const BEST_FIRST: readonly PriceOption[] = ["InBuild", "WithBoard", "Alone"];

/**
 * The sale prices the item's category offers, as the API sends them (`priceOptions`, in order); unknown names are ignored.
 * An answer without the list (an older cached response) means nothing is switched off.
 */
export function offeredOptions(priceOptions: readonly string[] | null | undefined): PriceOption[] {
  if (!priceOptions) return [...KNOWN];
  return KNOWN.filter((option) => priceOptions.includes(option)).sort((a, b) => priceOptions.indexOf(a) - priceOptions.indexOf(b));
}

/** The option a page or a card starts with: the first one offered (`Alone` when everything is offered). */
export function defaultOption(offered: readonly PriceOption[]): PriceOption {
  return offered[0] ?? DEFAULT_OPTION;
}

/** True when the only offered price is "inside a build": the part is not sold on its own. */
export function isBuildOnly(offered: readonly PriceOption[]): boolean {
  return offered.length === 1 && offered[0] === "InBuild";
}

/**
 * The context-price radios for the offered options, best price first. Nothing to choose (so the list is empty and the page just
 * shows the one price) when only one option is offered or the owner priced every offered option the same.
 */
export function priceChoices(prices: Prices, offered: readonly PriceOption[]): PriceChoice[] {
  const choices = BEST_FIRST.filter((option) => offered.includes(option)).map((option) => ({ option, price: priceOf(prices, option) }));
  if (choices.length < 2 || choices.every((choice) => choice.price === choices[0]?.price)) return [];
  return choices;
}

export function priceOf(prices: Prices, option: PriceOption): number {
  if (option === "InBuild") return prices.inBuild;
  return option === "WithBoard" ? prices.withBoard : prices.alone;
}

/** The price a card, a menu entry or the page headline shows: that of the default offered option. */
export function cardPrice(item: { prices: Prices; priceOptions?: readonly string[] | null }): number {
  return priceOf(item.prices, defaultOption(offeredOptions(item.priceOptions)));
}
