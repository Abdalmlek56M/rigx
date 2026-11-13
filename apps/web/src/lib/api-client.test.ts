import { afterEach, describe, expect, it, vi } from "vitest";
import {
  ApiError,
  catalogMediaUrl,
  getCatalogProducts,
  getCurrentUser,
  login,
  uploadCatalogImage,
} from "./api-client";

describe("identity API client", () => {
  afterEach(() => vi.restoreAllMocks());

  it("reads the antiforgery token and sends same-origin credentialed mutations", async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(Response.json({ requestToken: "csrf-value" }))
      .mockResolvedValueOnce(new Response(null, { status: 204 }));
    vi.stubGlobal("fetch", fetchMock);

    await login("Customer.Name", "a sufficiently long password");

    expect(fetchMock).toHaveBeenCalledTimes(2);
    const [path, options] = fetchMock.mock.calls[1] as [string, RequestInit];
    expect(path).toBe("/api/v1/identity/sessions");
    expect(options.credentials).toBe("include");
    expect(options.cache).toBe("no-store");
    const headers = new Headers(options.headers);
    expect(headers.get("X-CSRF-TOKEN")).toBe("csrf-value");
    expect(options.body).toBe(JSON.stringify({
      identifier: "Customer.Name",
      password: "a sufficiently long password",
    }));
  });

  it("treats a missing authenticated session as anonymous without making a CSRF request", async () => {
    const fetchMock = vi.fn().mockResolvedValue(new Response(null, { status: 401 }));
    vi.stubGlobal("fetch", fetchMock);

    await expect(getCurrentUser()).resolves.toBeNull();
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it("preserves validation problem details from the backend", async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(Response.json({ requestToken: "csrf-value" }))
      .mockResolvedValueOnce(Response.json({
        status: 400,
        title: "Validation Failed",
        errors: { email: ["Invalid email"] },
      }, { status: 400 }));
    vi.stubGlobal("fetch", fetchMock);

    const error = await login("name", "a sufficiently long password").catch((caught: unknown) => caught);
    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).payload?.errors?.email).toEqual(["Invalid email"]);
  });

  it("uses anonymous same-origin catalog reads and encodes media paths", async () => {
    const fetchMock = vi.fn().mockResolvedValue(Response.json({
      items: [],
      page: 1,
      pageSize: 24,
      totalCount: 0,
    }));
    vi.stubGlobal("fetch", fetchMock);

    await getCatalogProducts({
      category: "graphics cards",
      search: "rtx",
      minPrice: 100,
      maxPrice: 900,
      specifications: ["Platform|Socket|AM5"],
      sort: "price-asc",
      page: 2,
    });

    expect(fetchMock).toHaveBeenCalledTimes(1);
    const [path, options] = fetchMock.mock.calls[0] as [string, RequestInit];
    expect(path).toBe(
      "/api/v1/catalog/products?category=graphics+cards&search=rtx&minimumPrice=100&maximumPrice=900&specification=Platform%7CSocket%7CAM5&sort=price-asc&page=2",
    );
    expect(options.credentials).toBe("include");
    expect(new Headers(options.headers).get("X-CSRF-TOKEN")).toBeNull();
    expect(catalogMediaUrl("products/abc/image.webp")).toBe("/api/v1/catalog/media/products/abc/image.webp");
  });

  it("sends catalog media uploads as multipart with CSRF protection", async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(Response.json({ requestToken: "csrf-value" }))
      .mockResolvedValueOnce(Response.json({
        id: "image-id",
        storageKey: "products/p/image.webp",
        thumbnailStorageKey: "products/p/image-thumb.webp",
        altText: "GPU",
        sortOrder: 0,
        isPrimary: true,
      }, { status: 201 }));
    vi.stubGlobal("fetch", fetchMock);

    await uploadCatalogImage(
      "product-id",
      new Blob(["image"], { type: "image/webp" }),
      "GPU",
      true,
    );

    const [path, options] = fetchMock.mock.calls[1] as [string, RequestInit];
    expect(path).toBe("/api/v1/admin/catalog/products/product-id/images");
    expect(options.method).toBe("POST");
    expect(options.credentials).toBe("include");
    expect(new Headers(options.headers).get("X-CSRF-TOKEN")).toBe("csrf-value");
    expect(options.body).toBeInstanceOf(FormData);
    expect((options.body as FormData).get("isPrimary")).toBe("true");
  });
});
