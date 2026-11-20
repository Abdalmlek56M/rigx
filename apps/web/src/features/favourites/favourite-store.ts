import { unwrap } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";
import { sessionStore, type Session } from "@/features/auth/session-store";
import { parseStoredCodes, toggleCode } from "./favourite-rules";

const STORAGE_KEY = "rigx.favourites.v1";
const EMPTY: string[] = [];

let codes: string[] | undefined;
let signedIn = false;
let seenStatus: Session["status"] = "loading";
let unsubscribeSession: (() => void) | undefined;
const listeners = new Set<() => void>();

function readLocal(): string[] {
  try {
    return parseStoredCodes(window.localStorage.getItem(STORAGE_KEY));
  } catch {
    return EMPTY;
  }
}

function writeLocal(next: string[]) {
  try {
    if (next.length === 0) window.localStorage.removeItem(STORAGE_KEY);
    else window.localStorage.setItem(STORAGE_KEY, JSON.stringify(next));
  } catch {
    // Storage unavailable (private mode, quota): keep the in-memory list.
  }
}

function set(next: string[]) {
  codes = next;
  listeners.forEach((listener) => listener());
}

/**
 * Signing in adds the guest's local hearts to the account (then clears them locally) and the account list takes over;
 * signing out goes back to the (now empty) local list.
 */
async function onSessionChange() {
  const status = sessionStore.getSnapshot().status;
  if (status === seenStatus) return;
  seenStatus = status;
  signedIn = status === "signedIn";
  if (!signedIn) {
    set(readLocal());
    return;
  }
  const local = readLocal();
  try {
    const list = await required(unwrap(local.length > 0
      ? browserApi().POST("/api/v1/account/favourites/merge", { body: { codes: local } })
      : browserApi().GET("/api/v1/account/favourites")));
    if (local.length > 0) writeLocal(EMPTY);
    set([...list.codes]);
  } catch {
    set(local);
  }
}

function onStorage(event: StorageEvent) {
  if (signedIn || (event.key !== STORAGE_KEY && event.key !== null)) return;
  set(readLocal());
}

/** The hearts: local for guests, the account's list when signed in. */
export const favouriteStore = {
  getSnapshot(): string[] {
    codes ??= readLocal();
    return codes;
  },
  getServerSnapshot(): string[] {
    return EMPTY;
  },
  subscribe(listener: () => void): () => void {
    if (listeners.size === 0) {
      window.addEventListener("storage", onStorage);
      unsubscribeSession = sessionStore.subscribe(() => void onSessionChange());
      void onSessionChange();
    }
    listeners.add(listener);
    return () => {
      listeners.delete(listener);
      if (listeners.size > 0) return;
      window.removeEventListener("storage", onStorage);
      unsubscribeSession?.();
    };
  },
  /** Flips one heart at once; when the account call fails the heart flips back and the promise rejects. */
  async toggle(code: string): Promise<void> {
    const before = favouriteStore.getSnapshot();
    const adding = !before.includes(code);
    const next = toggleCode(before, code);
    set(next);
    if (!signedIn) {
      writeLocal(next);
      return;
    }
    try {
      const path = { params: { path: { code } } };
      await unwrap(adding ? browserApi().PUT("/api/v1/account/favourites/{code}", path) : browserApi().DELETE("/api/v1/account/favourites/{code}", path));
    } catch (error) {
      set(before);
      throw error;
    }
  }
};
