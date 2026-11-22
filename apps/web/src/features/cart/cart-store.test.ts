import assert from "node:assert/strict";
import { afterEach, beforeEach, describe, it } from "node:test";
import type { CartLine } from "./cart-rules.ts";

const KEY = "rigx.cart.v1";
const line: CartLine = {
  kind: "Item", code: "RX-0001", modelName: "Ryzen 5 5600", condition: "New", warrantyLabel: null, thumbnailStorageKey: null,
  priceOption: "Alone", unitPrice: 100, quantity: 2, maxQuantity: 5
};

type StorageHandler = (event: StorageEvent) => void;

/** A browser stand-in: an in-memory localStorage (that can be made to fail) and the storage-event registration. */
function fakeWindow(initial: Record<string, string> = {}) {
  const data = new Map(Object.entries(initial));
  const state = { failWrites: false, handlers: new Set<StorageHandler>() };
  const localStorage = {
    getItem: (key: string) => data.get(key) ?? null,
    setItem: (key: string, value: string) => {
      if (state.failWrites) throw new Error("quota");
      data.set(key, value);
    }
  };
  const window = {
    localStorage,
    addEventListener: (_type: string, handler: StorageHandler) => state.handlers.add(handler),
    removeEventListener: (_type: string, handler: StorageHandler) => state.handlers.delete(handler)
  };
  return { data, state, window };
}

let importCount = 0;
/** The store keeps its cart in module state, so each test imports its own copy against its own fake window. */
async function freshStore(initial: Record<string, string> = {}) {
  const env = fakeWindow(initial);
  Object.assign(globalThis, { window: env.window });
  const { cartStore } = (await import(`./cart-store.ts?copy=${importCount++}`)) as typeof import("./cart-store.ts");
  return { cartStore, ...env };
}

describe("cartStore", () => {
  let previous: unknown;
  beforeEach(() => {
    previous = (globalThis as { window?: unknown }).window;
  });
  afterEach(() => {
    if (previous === undefined) delete (globalThis as { window?: unknown }).window;
    else Object.assign(globalThis, { window: previous });
  });

  it("starts from the saved cart and returns the same snapshot until something changes", async () => {
    const { cartStore } = await freshStore({ [KEY]: JSON.stringify([line]) });
    const first = cartStore.getSnapshot();
    assert.equal(first.length, 1);
    assert.equal(first[0]?.code, "RX-0001");
    assert.equal(cartStore.getSnapshot(), first);
  });

  it("reads a broken save as an empty cart", async () => {
    const { cartStore } = await freshStore({ [KEY]: "{not json" });
    assert.deepEqual(cartStore.getSnapshot(), []);
  });

  it("the server snapshot is always empty", async () => {
    const { cartStore } = await freshStore({ [KEY]: JSON.stringify([line]) });
    assert.deepEqual(cartStore.getServerSnapshot(), []);
  });

  it("replace saves to storage and tells every listener", async () => {
    const { cartStore, data } = await freshStore();
    let calls = 0;
    const stop = cartStore.subscribe(() => calls++);
    cartStore.replace([line]);
    assert.equal(calls, 1);
    assert.equal(JSON.parse(data.get(KEY) ?? "[]")[0].code, "RX-0001");
    assert.equal(cartStore.getSnapshot()[0]?.quantity, 2);
    stop();
    cartStore.replace([]);
    assert.equal(calls, 1);
  });

  it("keeps the cart in memory when storage refuses the write", async () => {
    const { cartStore, state } = await freshStore();
    state.failWrites = true;
    cartStore.replace([line]);
    assert.equal(cartStore.getSnapshot().length, 1);
  });

  it("listens for other tabs only while somebody is subscribed", async () => {
    const { cartStore, state } = await freshStore();
    assert.equal(state.handlers.size, 0);
    const stopFirst = cartStore.subscribe(() => undefined);
    const stopSecond = cartStore.subscribe(() => undefined);
    assert.equal(state.handlers.size, 1);
    stopFirst();
    assert.equal(state.handlers.size, 1);
    stopSecond();
    assert.equal(state.handlers.size, 0);
  });

  it("re-reads the cart when another tab changes it, and ignores other keys", async () => {
    const { cartStore, data, state } = await freshStore();
    let calls = 0;
    const stop = cartStore.subscribe(() => calls++);
    const [handler] = [...state.handlers];
    assert.ok(handler);
    data.set(KEY, JSON.stringify([line]));
    handler({ key: "something.else" } as StorageEvent);
    assert.equal(calls, 0);
    handler({ key: KEY } as StorageEvent);
    assert.equal(calls, 1);
    assert.equal(cartStore.getSnapshot().length, 1);
    data.set(KEY, JSON.stringify([]));
    handler({ key: null } as StorageEvent);
    assert.equal(calls, 2);
    assert.equal(cartStore.getSnapshot().length, 0);
    stop();
  });
});
