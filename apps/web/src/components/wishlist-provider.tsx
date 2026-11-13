"use client";

import {
  createContext,
  type ReactNode,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import {
  addWishlistItem,
  clearWishlist,
  getWishlist,
  mergeWishlist,
  removeWishlistItem,
  type WishlistItem,
} from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { useSession } from "@/components/session-provider";

const guestStorageKey = "rigx-wishlist-v1";

export type WishlistProductInput = {
  id: string;
  name: string;
  slug: string;
  sku: string;
  basePrice: number;
  imageStorageKey: string | null;
  categorySlug: string | null;
};

type GuestWishlistItem = WishlistProductInput & { addedAtUtc: string };

type WishlistNotice = { tone: "success" | "error"; text: string };

type WishlistContextValue = {
  ready: boolean;
  loading: boolean;
  items: WishlistItem[];
  ids: ReadonlySet<string>;
  count: number;
  notice: WishlistNotice | null;
  pendingIds: ReadonlySet<string>;
  isSaved: (productId: string) => boolean;
  toggle: (product: WishlistProductInput) => Promise<void>;
  remove: (productId: string) => Promise<void>;
  clear: () => Promise<void>;
  refresh: () => Promise<void>;
  dismissNotice: () => void;
};

const WishlistContext = createContext<WishlistContextValue | null>(null);

function isGuestItem(value: unknown): value is GuestWishlistItem {
  if (!value || typeof value !== "object") return false;
  const candidate = value as Record<string, unknown>;
  return (
    typeof candidate.id === "string" &&
    candidate.id.length === 36 &&
    typeof candidate.name === "string" &&
    typeof candidate.slug === "string" &&
    typeof candidate.sku === "string" &&
    typeof candidate.basePrice === "number"
  );
}

function readGuestItems(): GuestWishlistItem[] {
  try {
    const raw = window.localStorage.getItem(guestStorageKey);
    if (!raw) return [];
    const parsed = JSON.parse(raw) as unknown;
    if (!Array.isArray(parsed)) return [];
    return parsed
      .filter(isGuestItem)
      .map((item) => ({
        id: item.id,
        name: item.name,
        slug: item.slug,
        sku: item.sku,
        basePrice: item.basePrice,
        imageStorageKey: typeof item.imageStorageKey === "string" ? item.imageStorageKey : null,
        categorySlug: typeof item.categorySlug === "string" ? item.categorySlug : null,
        addedAtUtc: typeof item.addedAtUtc === "string" ? item.addedAtUtc : new Date(0).toISOString(),
      }))
      .slice(0, 200);
  } catch {
    return [];
  }
}

function writeGuestItems(items: GuestWishlistItem[]): boolean {
  try {
    window.localStorage.setItem(guestStorageKey, JSON.stringify(items.slice(0, 200)));
    return true;
  } catch {
    return false;
  }
}

function toWishlistItem(item: GuestWishlistItem): WishlistItem {
  return {
    productId: item.id,
    name: item.name,
    slug: item.slug,
    sku: item.sku,
    basePrice: item.basePrice,
    primaryImageStorageKey: item.imageStorageKey,
    categorySlug: item.categorySlug,
    isAvailable: true,
    addedAtUtc: item.addedAtUtc,
  };
}

export function WishlistProvider({ children }: { children: ReactNode }) {
  const { messages } = useLocale();
  const { status, user } = useSession();
  const [accountItems, setAccountItems] = useState<WishlistItem[]>([]);
  const [guestItems, setGuestItems] = useState<GuestWishlistItem[]>([]);
  const [storageReady, setStorageReady] = useState(false);
  const [loading, setLoading] = useState(true);
  const [notice, setNotice] = useState<WishlistNotice | null>(null);
  const [pendingIds, setPendingIds] = useState<string[]>([]);
  const syncedFor = useRef<string | null>(null);

  useEffect(() => {
    setGuestItems(readGuestItems());
    setStorageReady(true);
  }, []);

  useEffect(() => {
    if (!storageReady || status === "authenticated") return;
    writeGuestItems(guestItems);
  }, [guestItems, status, storageReady]);

  const loadAccountWishlist = useCallback(async () => {
    if (status !== "authenticated") {
      setAccountItems([]);
      setLoading(false);
      return;
    }
    setLoading(true);
    try {
      const guest = readGuestItems();
      let next: WishlistItem[];
      if (guest.length > 0) {
        next = await mergeWishlist(guest.map((item) => item.id));
        if (writeGuestItems([])) setGuestItems([]);
      } else {
        next = await getWishlist();
      }
      setAccountItems(next);
      syncedFor.current = user?.email ?? user?.username ?? "authenticated";
    } catch (error) {
      setNotice({ tone: "error", text: apiErrorMessage(error, messages) });
    } finally {
      setLoading(false);
    }
  }, [messages, status, user?.email, user?.username]);

  useEffect(() => {
    if (status === "loading" || !storageReady) return;
    if (status !== "authenticated") {
      syncedFor.current = null;
      setLoading(false);
      setAccountItems([]);
      return;
    }
    const key = user?.email ?? user?.username ?? "authenticated";
    if (syncedFor.current === key) return;
    void loadAccountWishlist();
  }, [status, storageReady, user?.email, user?.username, loadAccountWishlist]);

  const items = useMemo<WishlistItem[]>(
    () => (status === "authenticated" ? accountItems : guestItems.map(toWishlistItem)),
    [accountItems, guestItems, status],
  );

  const ids = useMemo<ReadonlySet<string>>(
    () => new Set(items.map((item) => item.productId)),
    [items],
  );

  const pendingSet = useMemo<ReadonlySet<string>>(() => new Set(pendingIds), [pendingIds]);

  const toggle = useCallback(async (product: WishlistProductInput) => {
    const saved = ids.has(product.id);
    setNotice(null);
    if (status !== "authenticated") {
      let storageFailed = false;
      setGuestItems((current) => {
        const next = saved
          ? current.filter((item) => item.id !== product.id)
          : [{ ...product, addedAtUtc: new Date().toISOString() }, ...current.filter((item) => item.id !== product.id)];
        storageFailed = !writeGuestItems(next);
        return next;
      });
      setNotice(
        storageFailed
          ? { tone: "error", text: messages.wishlistStorageUnavailable }
          : { tone: "success", text: saved ? messages.wishlistRemoved : messages.wishlistSaved },
      );
      return;
    }
    setPendingIds((current) => [...current, product.id]);
    try {
      if (saved) {
        await removeWishlistItem(product.id);
        setAccountItems((current) => current.filter((item) => item.productId !== product.id));
      } else {
        await addWishlistItem(product.id);
        setAccountItems((current) => [
          {
            productId: product.id,
            name: product.name,
            slug: product.slug,
            sku: product.sku,
            basePrice: product.basePrice,
            primaryImageStorageKey: product.imageStorageKey,
            categorySlug: product.categorySlug,
            isAvailable: true,
            addedAtUtc: new Date().toISOString(),
          },
          ...current.filter((item) => item.productId !== product.id),
        ]);
      }
      setNotice({ tone: "success", text: saved ? messages.wishlistRemoved : messages.wishlistSaved });
    } catch (error) {
      setNotice({ tone: "error", text: apiErrorMessage(error, messages) });
    } finally {
      setPendingIds((current) => current.filter((id) => id !== product.id));
    }
  }, [ids, messages, status]);

  const remove = useCallback(async (productId: string) => {
    setNotice(null);
    if (status !== "authenticated") {
      setGuestItems((current) => {
        const next = current.filter((item) => item.id !== productId);
        writeGuestItems(next);
        return next;
      });
      return;
    }
    setPendingIds((current) => [...current, productId]);
    try {
      await removeWishlistItem(productId);
      setAccountItems((current) => current.filter((item) => item.productId !== productId));
      setNotice({ tone: "success", text: messages.wishlistRemoved });
    } catch (error) {
      setNotice({ tone: "error", text: apiErrorMessage(error, messages) });
    } finally {
      setPendingIds((current) => current.filter((id) => id !== productId));
    }
  }, [messages, status]);

  const clear = useCallback(async () => {
    setNotice(null);
    if (status !== "authenticated") {
      setGuestItems([]);
      writeGuestItems([]);
      setNotice({ tone: "success", text: messages.wishlistCleared });
      return;
    }
    try {
      await clearWishlist();
      setAccountItems([]);
      setNotice({ tone: "success", text: messages.wishlistCleared });
    } catch (error) {
      setNotice({ tone: "error", text: apiErrorMessage(error, messages) });
    }
  }, [messages, status]);

  const refresh = useCallback(async () => {
    syncedFor.current = null;
    setGuestItems(readGuestItems());
    await loadAccountWishlist();
  }, [loadAccountWishlist]);

  const value = useMemo<WishlistContextValue>(() => ({
    ready: storageReady && status !== "loading",
    loading,
    items,
    ids,
    count: ids.size,
    notice,
    pendingIds: pendingSet,
    isSaved: (productId: string) => ids.has(productId),
    toggle,
    remove,
    clear,
    refresh,
    dismissNotice: () => setNotice(null),
  }), [clear, ids, items, loading, notice, pendingSet, refresh, remove, storageReady, status, toggle]);

  return <WishlistContext.Provider value={value}>{children}</WishlistContext.Provider>;
}

export function useWishlist(): WishlistContextValue {
  const value = useContext(WishlistContext);
  if (!value) throw new Error("useWishlist must be used inside WishlistProvider.");
  return value;
}
