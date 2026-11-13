import { cache } from "react";
import type {
  CatalogCategory,
  CatalogProduct,
  CatalogProductPage,
} from "@/lib/api-client";

const apiOrigin = (process.env.API_ORIGIN ?? "http://127.0.0.1:8085").replace(/\/+$/, "");

async function readJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    throw new Error(`Catalog server request failed with ${response.status}.`);
  }
  return response.json() as Promise<T>;
}

export const getCatalogProductServer = cache(async (slug: string): Promise<CatalogProduct | null> => {
  const response = await fetch(
    `${apiOrigin}/api/v1/catalog/products/${encodeURIComponent(slug)}`,
    { headers: { Accept: "application/json" }, cache: "no-store" },
  );
  if (response.status === 404) return null;
  return readJson<CatalogProduct>(response);
});

export const getCatalogProductsServer = cache(async (page = 1, pageSize = 100): Promise<CatalogProductPage> => {
  const response = await fetch(
    `${apiOrigin}/api/v1/catalog/products?page=${page}&pageSize=${pageSize}&sort=newest`,
    { headers: { Accept: "application/json" }, next: { revalidate: 300 } },
  );
  return readJson<CatalogProductPage>(response);
});

export const getCatalogCategoriesServer = cache(async (): Promise<CatalogCategory[]> => {
  const response = await fetch(
    `${apiOrigin}/api/v1/catalog/categories`,
    { headers: { Accept: "application/json" }, next: { revalidate: 300 } },
  );
  return readJson<CatalogCategory[]>(response);
});

export function siteUrl(): string {
  const configured = process.env.NEXT_PUBLIC_SITE_URL?.trim();
  if (configured) {
    const url = new URL(configured);
    if (url.protocol !== "http:" && url.protocol !== "https:") {
      throw new Error("NEXT_PUBLIC_SITE_URL must use http or https.");
    }
    if (process.env.NODE_ENV === "production" &&
        (url.hostname === "localhost" || url.hostname === "127.0.0.1")) {
      throw new Error("NEXT_PUBLIC_SITE_URL cannot be localhost in production.");
    }
    return configured.replace(/\/+$/, "");
  }
  if (process.env.NODE_ENV === "production") {
    throw new Error("NEXT_PUBLIC_SITE_URL is required for production builds.");
  }
  return "http://127.0.0.1:3005";
}
