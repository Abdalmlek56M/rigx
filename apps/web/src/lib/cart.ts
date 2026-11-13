export type CartItem = {
  productId: string;
  assemblyId: string | null;
  slug: string;
  name: string;
  sku: string;
  variantId: string | null;
  variantName: string | null;
  unitPrice: number;
  imageStorageKey: string | null;
  categorySlug: string;
  quantity: number;
};

export type CartItemInput = Omit<CartItem, "quantity" | "assemblyId"> & { quantity?: number; assemblyId?: string | null };

export const CART_MAX_QUANTITY = 99;
export const CART_MAX_LINES = 50;

export type CartAddStatus = "added" | "incremented" | "max-lines" | "max-quantity" | "invalid";

export type CartAddResult = {
  items: CartItem[];
  status: CartAddStatus;
};

export function cartLineKey(item: Pick<CartItem, "productId" | "variantId" | "assemblyId">): string {
  if (item.assemblyId) return `assembly:${item.assemblyId}`;
  return `${item.productId}::${item.variantId ?? "base"}`;
}

export function normalizeCartQuantity(value: number): number {
  if (!Number.isFinite(value)) return 1;
  return Math.min(CART_MAX_QUANTITY, Math.max(1, Math.trunc(value)));
}

export function tryAddCartItem(items: CartItem[], input: CartItemInput): CartAddResult {
  if (
    typeof input.productId !== "string" ||
    input.productId.length === 0 ||
    typeof input.name !== "string" ||
    input.name.length === 0 ||
    typeof input.sku !== "string" ||
    input.sku.length === 0 ||
    !Number.isFinite(input.unitPrice) ||
    input.unitPrice < 0 ||
    (input.variantId !== null && typeof input.variantId !== "string") ||
    (input.assemblyId != null && typeof input.assemblyId !== "string") ||
    (input.quantity !== undefined && !Number.isFinite(input.quantity))
  ) {
    return { items, status: "invalid" };
  }

  const quantity = normalizeCartQuantity(input.quantity ?? 1);
  const normalized: Omit<CartItem, "quantity"> = { ...input, assemblyId: input.assemblyId ?? null };
  const key = cartLineKey(normalized);
  const existing = items.find((item) => cartLineKey(item) === key);

  if (!existing) {
    if (items.length >= CART_MAX_LINES) return { items, status: "max-lines" };
    return { items: [...items, { ...normalized, quantity }], status: "added" };
  }

  if (existing.quantity >= CART_MAX_QUANTITY) {
    return { items, status: "max-quantity" };
  }

  return {
    items: items.map((item) =>
      cartLineKey(item) === key
        ? { ...item, quantity: normalizeCartQuantity(item.quantity + quantity) }
        : item,
    ),
    status: "incremented",
  };
}

export function addCartItem(items: CartItem[], input: CartItemInput): CartItem[] {
  return tryAddCartItem(items, input).items;
}

export function setCartQuantity(items: CartItem[], key: string, quantity: number): CartItem[] {
  if (quantity <= 0) return removeCartItem(items, key);
  return items.map((item) =>
    cartLineKey(item) === key ? { ...item, quantity: normalizeCartQuantity(quantity) } : item,
  );
}

export function removeCartItem(items: CartItem[], key: string): CartItem[] {
  return items.filter((item) => cartLineKey(item) !== key);
}

export function cartItemCount(items: CartItem[]): number {
  return items.reduce((total, item) => total + item.quantity, 0);
}

export function cartSubtotal(items: CartItem[]): number {
  const total = items.reduce((sum, item) => sum + item.unitPrice * item.quantity, 0);
  return Math.round(total * 100) / 100;
}

function isCartItem(value: unknown): value is CartItem {
  if (typeof value !== "object" || value === null) return false;
  const item = value as Partial<CartItem>;
  return (
    typeof item.productId === "string" &&
    item.productId.length > 0 &&
    (item.assemblyId === undefined || item.assemblyId === null || typeof item.assemblyId === "string") &&
    typeof item.slug === "string" &&
    item.slug.length > 0 &&
    typeof item.name === "string" &&
    item.name.length > 0 &&
    typeof item.sku === "string" &&
    (item.variantId === null || typeof item.variantId === "string") &&
    (item.variantName === null || typeof item.variantName === "string") &&
    typeof item.unitPrice === "number" &&
    Number.isFinite(item.unitPrice) &&
    item.unitPrice >= 0 &&
    (item.imageStorageKey === null || typeof item.imageStorageKey === "string") &&
    typeof item.categorySlug === "string" &&
    typeof item.quantity === "number" &&
    Number.isFinite(item.quantity) &&
    item.quantity >= 1
  );
}

export function parseCartPreference(raw: string | null): CartItem[] {
  if (!raw) return [];
  try {
    const parsed = JSON.parse(raw) as unknown;
    if (!Array.isArray(parsed)) return [];

    const merged: CartItem[] = [];
    for (const candidate of parsed) {
      if (!isCartItem(candidate)) continue;
      const item: CartItem = {
        ...candidate,
        assemblyId: candidate.assemblyId ?? null,
        quantity: normalizeCartQuantity(candidate.quantity),
      };
      const index = merged.findIndex((entry) => cartLineKey(entry) === cartLineKey(item));
      if (index >= 0) {
        merged[index] = {
          ...merged[index],
          quantity: normalizeCartQuantity(merged[index].quantity + item.quantity),
        };
      } else if (merged.length < CART_MAX_LINES) {
        merged.push(item);
      }
    }
    return merged;
  } catch {
    return [];
  }
}
