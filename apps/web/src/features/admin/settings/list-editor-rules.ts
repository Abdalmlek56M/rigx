/** List editor order changes; each returns a new list and leaves the same list untouched. */

export function addItem<T>(items: readonly T[], item: T): T[] {
  return [...items, item];
}

export function replaceItem<T>(items: readonly T[], index: number, item: T): T[] {
  return items.map((current, at) => (at === index ? item : current));
}

export function removeItem<T>(items: readonly T[], index: number): T[] {
  return items.filter((_, at) => at !== index);
}

/** Takes the item at `from` out and puts it at `to` (a drop on another row takes that row's place); the same order when either place is outside the list. */
export function moveItemTo<T>(items: readonly T[], from: number, to: number): T[] {
  if (from < 0 || from >= items.length || to < 0 || to >= items.length) return [...items];
  const next = [...items];
  const [moved] = next.splice(from, 1);
  if (moved === undefined) return next;
  next.splice(to, 0, moved);
  return next;
}

/** Moves one item up (-1) or down (1); an edge move returns the same order. */
export function moveItem<T>(items: readonly T[], index: number, direction: -1 | 1): T[] {
  return moveItemTo(items, index, index + direction);
}
