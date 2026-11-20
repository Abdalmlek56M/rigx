import { unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";

export type SessionUser = Schemas["CurrentIdentityUser"];
export type Session = { status: "loading" } | { status: "signedOut" } | { status: "signedIn"; user: SessionUser };

const LOADING: Session = { status: "loading" };
const SIGNED_OUT: Session = { status: "signedOut" };

let session: Session = LOADING;
let started = false;
const listeners = new Set<() => void>();

function set(next: Session) {
  session = next;
  listeners.forEach((listener) => listener());
}

/** Reads the current user; any failure (401 included) means signed out. */
export async function refreshSession(): Promise<void> {
  try {
    const user = await required(unwrap(browserApi().GET("/api/v1/identity/me")));
    set({ status: "signedIn", user });
  } catch {
    set(SIGNED_OUT);
  }
}

export async function signOut(): Promise<void> {
  try {
    await unwrap(browserApi().DELETE("/api/v1/identity/sessions/current"));
  } finally {
    set(SIGNED_OUT);
  }
}

/** One session per page load, shared by every component; the first subscriber triggers the load. */
export const sessionStore = {
  getSnapshot(): Session {
    return session;
  },
  getServerSnapshot(): Session {
    return LOADING;
  },
  subscribe(listener: () => void): () => void {
    listeners.add(listener);
    if (!started) {
      started = true;
      // A 401 on any call means the session ended (idle or signed out elsewhere); the shells then send the user to sign in.
      browserApi().use({
        onResponse({ response }) {
          if (response.status === 401 && session.status === "signedIn") set(SIGNED_OUT);
          return response;
        }
      });
      void refreshSession();
    }
    return () => {
      listeners.delete(listener);
    };
  }
};
