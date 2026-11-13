import type { MetadataRoute } from "next";
import { getCatalogCategoriesServer, getCatalogProductsServer, siteUrl } from "@/lib/server-catalog";
import { getPublishedArticlesServer } from "@/lib/server-content";

export default async function sitemap(): Promise<MetadataRoute.Sitemap> {
  const base = siteUrl();
  const [products, categories, articles] = await Promise.all([
    getAllProducts().catch(() => []),
    getCatalogCategoriesServer().catch(() => []),
    getAllArticles().catch(() => []),
  ]);

  const staticRoutes: MetadataRoute.Sitemap = [
    { url: `${base}/`, changeFrequency: "daily", priority: 1 },
    { url: `${base}/products`, changeFrequency: "daily", priority: 0.9 },
    { url: `${base}/categories`, changeFrequency: "weekly", priority: 0.8 },
    { url: `${base}/articles`, changeFrequency: "weekly", priority: 0.7 },
    { url: `${base}/faq`, changeFrequency: "monthly", priority: 0.5 },
  ];
  const productRoutes: MetadataRoute.Sitemap = products.map((product) => ({
    url: `${base}/products/${encodeURIComponent(product.slug)}`,
    changeFrequency: "weekly",
    priority: 0.7,
  }));
  const categoryRoutes: MetadataRoute.Sitemap = flatten(categories).map((category) => ({
    url: `${base}/products?category=${encodeURIComponent(category.slug)}`,
    changeFrequency: "weekly",
    priority: 0.6,
  }));
  const articleRoutes: MetadataRoute.Sitemap = articles.map((article) => ({
    url: `${base}/articles/${encodeURIComponent(article.slug)}`,
    lastModified: article.publishedAtUtc ?? undefined,
    changeFrequency: "monthly",
    priority: 0.6,
  }));

  return [...staticRoutes, ...categoryRoutes, ...productRoutes, ...articleRoutes];
}

async function getAllProducts() {
  const items: Awaited<ReturnType<typeof getCatalogProductsServer>>["items"] = [];
  for (let page = 1; page <= 50; page++) {
    const batch = await getCatalogProductsServer(page, 100);
    items.push(...batch.items);
    if (batch.items.length === 0 || items.length >= batch.totalCount) break;
  }
  return items;
}

async function getAllArticles() {
  const items: Awaited<ReturnType<typeof getPublishedArticlesServer>>["items"] = [];
  for (let page = 1; page <= 50; page++) {
    const batch = await getPublishedArticlesServer(page, 100);
    items.push(...batch.items);
    if (batch.items.length === 0 || items.length >= batch.total) break;
  }
  return items;
}

function flatten(categories: Awaited<ReturnType<typeof getCatalogCategoriesServer>>): Awaited<ReturnType<typeof getCatalogCategoriesServer>> {
  return categories.flatMap((category) => [category, ...flatten(category.children)]);
}
