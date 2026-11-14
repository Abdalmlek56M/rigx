"use client";

import { createContext, useCallback, useContext, useMemo, useState, useSyncExternalStore, type ReactNode } from "react";
import { addLine, itemCount, removeLine, setLineQuantity, subtotal, type CartLine, type PriceOption } from "./cart-rules";
import { cartStore } from "./cart-store";
import { refreshLines } from "./refresh-lines";

interface CartApi {
  lines: CartLine[];
  count: number;
  subtotal: number;
  isOpen: boolean;
  /** Set when the last refresh dropped lines that sold out or no longer exist. */
  removedNotice: boolean;
  /** Model name of the item just added (drives the toast); null when no toast is showing. */
  addedModel: string | null;
  add: (line: CartLine) => void;
  remove: (code: string, priceOption: PriceOption) => void;
  setQuantity: (code: string, priceOption: PriceOption, quantity: number) => void;
  clear: () => void;
  open: () => void;
  close: () => void;
  dismissToast: () => void;
}

const CartContext = createContext<CartApi | null>(null);

export function CartProvider({ children }: { children: ReactNode }) {
  const lines = useSyncExternalStore(cartStore.subscribe, cartStore.getSnapshot, cartStore.getServerSnapshot);
  const [isOpen, setIsOpen] = useState(false);
  const [removedNotice, setRemovedNotice] = useState(false);
  const [addedModel, setAddedModel] = useState<string | null>(null);

  const open = useCallback(() => {
    setIsOpen(true);
    setRemovedNotice(false);
    setAddedModel(null);
    void refreshLines(cartStore.getSnapshot()).then(({ lines: fresh, removed }) => {
      cartStore.replace(fresh);
      setRemovedNotice(removed > 0);
    });
  }, []);

  const api = useMemo<CartApi>(
    () => ({
      lines,
      count: itemCount(lines),
      subtotal: subtotal(lines),
      isOpen,
      removedNotice,
      addedModel,
      add: (line) => {
        cartStore.replace(addLine(cartStore.getSnapshot(), line));
        setAddedModel(line.modelName);
      },
      remove: (code, priceOption) => cartStore.replace(removeLine(cartStore.getSnapshot(), code, priceOption)),
      setQuantity: (code, priceOption, quantity) =>
        cartStore.replace(setLineQuantity(cartStore.getSnapshot(), code, priceOption, quantity)),
      clear: () => cartStore.replace([]),
      open,
      close: () => setIsOpen(false),
      dismissToast: () => setAddedModel(null)
    }),
    [lines, isOpen, removedNotice, addedModel, open]
  );

  return <CartContext value={api}>{children}</CartContext>;
}

export function useCart(): CartApi {
  const context = useContext(CartContext);
  if (!context) throw new Error("useCart must be used inside <CartProvider>.");
  return context;
}
