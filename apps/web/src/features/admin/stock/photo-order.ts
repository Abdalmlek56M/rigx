/** The order of a photo list: the first photo is the main one. Pure, so it is tested. */
import { moveKey } from "../kit/reorder-rules.ts";

/** The keys with `key` first (the main photo), the others in their order; unchanged when `key` is not in the list. */
export function makeMain(keys: readonly string[], key: string): string[] {
  const from = keys.indexOf(key);
  return from < 0 ? [...keys] : moveKey(keys, from, 0);
}
