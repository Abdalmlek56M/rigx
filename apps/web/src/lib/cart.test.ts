import { describe, expect, it } from "vitest";
import {
  addCartItem,
  cartItemCount,
  cartLineKey,
  cartSubtotal,
  parseCartPreference,
  removeCartItem,
  setCartQuantity,
  tryAddCartItem,
  type CartItem,
  type CartItemInput,
} from "@/lib/cart";

const product = (id: string, overrides: Partial<CartItemInput> = {}): CartItemInput => ({
  productId: id,
  slug: `product-${id}`,
  name: `Product ${id}`,
  sku: `SKU-${id}`,
  variantId: null,
  variantName: null,
  unitPrice: 100,
  imageStorageKey: null,
  categorySlug: "graphics-cards",
  ...overrides,
});

describe("cart state", () => {
  it("merges the same product and variant while keeping variants separate", () => {
    let items: CartItem[] = [];
    items = addCartItem(items, product("1"));
    items = addCartItem(items, product("1", { quantity: 2 }));
    items = addCartItem(items, product("1", { variantId: "v1", variantName: "8 GB", unitPrice: 120 }));
    expect(items).toHaveLength(2);
    expect(items[0].quantity).toBe(3);
    expect(items[1].quantity).toBe(1);
    expect(cartItemCount(items)).toBe(4);
  });

  it("caps quantities and rounds the subtotal", () => {
    let items = addCartItem([], product("1", { unitPrice: 19.99, quantity: 99 }));
    items = addCartItem(items, product("1", { unitPrice: 19.99, quantity: 99 }));
    expect(items[0].quantity).toBe(99);
    expect(cartSubtotal(items)).toBe(1979.01);
  });

  it("updates and removes lines", () => {
    const items = addCartItem([], product("1"));
    const key = cartLineKey(items[0]);
    expect(setCartQuantity(items, key, 4)[0].quantity).toBe(4);
    expect(setCartQuantity(items, key, 0)).toEqual([]);
    expect(removeCartItem(items, key)).toEqual([]);
  });

  it("sanitizes stale, duplicate, and invalid stored lines", () => {
    const parsed = parseCartPreference(JSON.stringify([
      { ...product("1"), quantity: 2 },
      { ...product("1"), quantity: 3 },
      { ...product("2"), quantity: 0 },
      { ...product("3"), unitPrice: Number.NaN, quantity: 1 },
      { ...product("4"), quantity: 2.8 },
      { broken: true },
    ]));
    expect(parsed).toHaveLength(2);
    expect(parsed[0].quantity).toBe(5);
    expect(parsed[1].quantity).toBe(2);
  });

  it("returns an empty cart for malformed storage", () => {
    expect(parseCartPreference(null)).toEqual([]);
    expect(parseCartPreference("{")).toEqual([]);
    expect(parseCartPreference(JSON.stringify({}))).toEqual([]);
  });

  it("reports limits and rejects invalid additions without changing the cart", () => {
    let items: CartItem[] = [];
    for (let index = 0; index < 50; index += 1) {
      const result = tryAddCartItem(items, product(`line-${index}`));
      expect(result.status).toBe("added");
      items = result.items;
    }
    const overflow = tryAddCartItem(items, product("line-overflow"));
    expect(overflow.status).toBe("max-lines");
    expect(overflow.items).toBe(items);

    const fullQuantity = tryAddCartItem([], product("qty", { quantity: 99 }));
    const capped = tryAddCartItem(fullQuantity.items, product("qty", { quantity: 1 }));
    expect(capped.status).toBe("max-quantity");
    expect(capped.items[0].quantity).toBe(99);

    const invalid = tryAddCartItem(items, product("bad", { unitPrice: Number.NaN }));
    expect(invalid.status).toBe("invalid");
    expect(invalid.items).toBe(items);
  });
});
