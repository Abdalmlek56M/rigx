"use client";

import {
  createContext,
  type ReactNode,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import { useLocale } from "@/lib/i18n";
import {
  cartItemCount,
  cartSubtotal,
  parseCartPreference,
  removeCartItem,
  setCartQuantity,
  tryAddCartItem,
  type CartAddStatus,
  type CartItem,
  type CartItemInput,
} from "@/lib/cart";

type CartToastState = {
  id: number;
  message: string;
  productName: string | null;
  variantName: string | null;
};

type CartContextValue = {
  items: CartItem[];
  ready: boolean;
  count: number;
  subtotal: number;
  notice: string | null;
  noticeTone: "success" | "error";
  toast: CartToastState | null;
  isOpen: boolean;
  addItem: (input: CartItemInput) => CartAddStatus;
  setQuantity: (key: string, quantity: number) => void;
  removeItem: (key: string) => void;
  clear: () => void;
  dismissToast: () => void;
  openCart: () => void;
  closeCart: () => void;
};

const storageKey = "rigx-cart-v1";
const CartContext = createContext<CartContextValue | null>(null);

export function CartProvider({ children }: { children: ReactNode }) {
  const { messages } = useLocale();
  const [items, setItems] = useState<CartItem[]>([]);
  const [ready, setReady] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);
  const [noticeTone, setNoticeTone] = useState<"success" | "error">("success");
  const [toast, setToast] = useState<CartToastState | null>(null);
  const [isOpen, setIsOpen] = useState(false);

  useEffect(() => {
    try {
      setItems(parseCartPreference(window.localStorage.getItem(storageKey)));
    } catch {
      setNoticeTone("error"); setNotice(messages.cartStorageUnavailable);
    } finally {
      setReady(true);
    }
  }, [messages.cartStorageUnavailable]);

  useEffect(() => {
    if (!ready) return;
    try {
      window.localStorage.setItem(storageKey, JSON.stringify(items));
    } catch {
      setNoticeTone("error"); setNotice(messages.cartStorageUnavailable);
    }
  }, [items, messages.cartStorageUnavailable, ready]);

  useEffect(() => {
    if (!notice) return;
    const timer = window.setTimeout(() => setNotice(null), 4000);
    return () => window.clearTimeout(timer);
  }, [notice]);

  useEffect(() => {
    if (!toast) return;
    const timer = window.setTimeout(() => setToast(null), 4600);
    return () => window.clearTimeout(timer);
  }, [toast]);

  const addItem = useCallback((input: CartItemInput): CartAddStatus => {
    const result = tryAddCartItem(items, input);
    setItems(result.items);
    if (result.status === "added" || result.status === "incremented") {
      setToast({
        id: Date.now(),
        message: messages.cartAdded,
        productName: input.name,
        variantName: input.variantName,
      });
    } else if (result.status === "max-lines") {
      setNoticeTone("error"); setNotice(messages.cartLineLimit);
    } else if (result.status === "max-quantity") {
      setNoticeTone("error"); setNotice(messages.cartQuantityLimit);
    } else {
      setNoticeTone("error"); setNotice(messages.cartAddFailed);
    }
    return result.status;
  }, [items, messages.cartAdded, messages.cartAddFailed, messages.cartLineLimit, messages.cartQuantityLimit]);

  const dismissToast = useCallback(() => setToast(null), []);

  const setQuantity = useCallback((key: string, quantity: number) => {
    setItems((current) => setCartQuantity(current, key, quantity));
  }, []);

  const removeItem = useCallback((key: string) => {
    setItems((current) => removeCartItem(current, key));
    setNoticeTone("success"); setNotice(messages.cartRemoved);
  }, [messages.cartRemoved]);

  const clear = useCallback(() => {
    setItems([]);
    setNoticeTone("success"); setNotice(messages.cartCleared);
  }, [messages.cartCleared]);

  const openCart = useCallback(() => setIsOpen(true), []);
  const closeCart = useCallback(() => setIsOpen(false), []);

  const value = useMemo<CartContextValue>(() => ({
    items,
    ready,
    count: cartItemCount(items),
    subtotal: cartSubtotal(items),
    notice,
    noticeTone,
    toast,
    isOpen,
    addItem,
    setQuantity,
    removeItem,
    clear,
    dismissToast,
    openCart,
    closeCart,
  }), [addItem, clear, closeCart, dismissToast, isOpen, items, notice, noticeTone, openCart, ready, removeItem, setQuantity, toast]);

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
}

export function useCart(): CartContextValue {
  const value = useContext(CartContext);
  if (!value) throw new Error("useCart must be used inside CartProvider.");
  return value;
}

