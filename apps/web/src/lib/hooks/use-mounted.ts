import { useSyncExternalStore } from "react";

const subscribe = () => () => undefined;

/** `false` while rendering on the server and during hydration, `true` afterwards (for data that only exists in the browser, like the cart). */
export function useMounted(): boolean {
  return useSyncExternalStore(subscribe, () => true, () => false);
}
