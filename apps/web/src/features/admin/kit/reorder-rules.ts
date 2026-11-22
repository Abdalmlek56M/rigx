/** Putting a list in a hand-chosen order (drag and drop, arrows): the list is ids; nothing here knows what the ids are. Pure, so it is tested. */

/** The ids with the one at `from` moved to `to` (both clamped to the list); the same list when nothing moves. */
export function moveKey(keys: readonly string[], from: number, to: number): string[] {
  if (from < 0 || from >= keys.length) return [...keys];
  const target = Math.min(Math.max(to, 0), keys.length - 1);
  const next = [...keys];
  const [moved] = next.splice(from, 1);
  next.splice(target, 0, moved!);
  return next;
}

/** The ids after `active` was dropped on `over`: it takes over's place and the ones between shift by one; the same list when either is not in it. */
export function dropOn(keys: readonly string[], active: string, over: string): string[] {
  const from = keys.indexOf(active);
  const to = keys.indexOf(over);
  return from < 0 || to < 0 ? [...keys] : moveKey(keys, from, to);
}

/** Whether two lists name the same ids in the same order (a drop that changed nothing saves nothing). */
export const sameOrder = (left: readonly string[], right: readonly string[]): boolean =>
  left.length === right.length && left.every((key, index) => key === right[index]);

/** The items in the order of `ids`; an item whose id is not listed keeps its place after the listed ones. */
export function inOrder<T extends { id: string }>(items: readonly T[], ids: readonly string[]): T[] {
  const position = (item: T) => {
    const index = ids.indexOf(item.id);
    return index < 0 ? ids.length : index;
  };
  return [...items].sort((left, right) => position(left) - position(right));
}
