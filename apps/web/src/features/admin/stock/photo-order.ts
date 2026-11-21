/** The order of a photo list: the first photo is the main one. Pure, so it is tested. */

/** The keys with the one at `from` moved to `to` (both clamped to the list); the same list when nothing moves. */
export function moveKey(keys: readonly string[], from: number, to: number): string[] {
  if (from < 0 || from >= keys.length) return [...keys];
  const target = Math.min(Math.max(to, 0), keys.length - 1);
  const next = [...keys];
  const [moved] = next.splice(from, 1);
  next.splice(target, 0, moved!);
  return next;
}

/** The keys with `key` first (the main photo), the others in their order; unchanged when `key` is not in the list. */
export function makeMain(keys: readonly string[], key: string): string[] {
  const from = keys.indexOf(key);
  return from < 0 ? [...keys] : moveKey(keys, from, 0);
}
