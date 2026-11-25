/**
 * The one grid every page lays item cards in (catalog, home, favourites, sold-out alternatives): two cards side by side on a phone,
 * then as many 240 px-or-wider columns as fit, so a card keeps the same proportions wherever it appears.
 */
export const ITEM_GRID_CLASS = "grid-cols-2 sm:grid-cols-[repeat(auto-fill,minmax(240px,1fr))]";

/** The width of one card in a sideways rail (home GPU rail, related items): about a grid column, with the next card peeking in. */
export const ITEM_RAIL_ITEM_CLASS = "flex w-[172px] shrink-0 sm:w-[236px] md:w-[280px]";
