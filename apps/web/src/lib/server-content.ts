import type { ContentArticle, ContentPage, ContentPolicy } from "@/lib/api-client";

const apiOrigin = (process.env.API_ORIGIN ?? "http://127.0.0.1:8085").replace(/\/+$/, "");

async function readJson<T>(response: Response): Promise<T> {
  if (!response.ok) {
    throw new Error(`Content server request failed with ${response.status}.`);
  }
  return response.json() as Promise<T>;
}

export async function getPublishedArticlesServer(
  page = 1,
  pageSize = 20,
): Promise<ContentPage<ContentArticle>> {
  const response = await fetch(
    `${apiOrigin}/api/v1/content/articles?page=${page}&pageSize=${pageSize}`,
    { headers: { Accept: "application/json" }, next: { revalidate: 60 } },
  );
  return readJson<ContentPage<ContentArticle>>(response);
}

export async function getPublishedArticleServer(slug: string): Promise<ContentArticle | null> {
  const response = await fetch(
    `${apiOrigin}/api/v1/content/articles/${encodeURIComponent(slug)}`,
    { headers: { Accept: "application/json" }, next: { revalidate: 60 } },
  );
  if (response.status === 404) return null;
  return readJson<ContentArticle>(response);
}

export async function getPublishedPolicyServer(kind: string): Promise<ContentPolicy | null> {
  const response = await fetch(
    `${apiOrigin}/api/v1/content/policies/${encodeURIComponent(kind)}`,
    { headers: { Accept: "application/json" }, next: { revalidate: 60 } },
  );
  if (response.status === 404) return null;
  return readJson<ContentPolicy>(response);
}
