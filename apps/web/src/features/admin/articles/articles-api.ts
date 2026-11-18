import { unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";

export type Article = Schemas["ArticleView"];
export type ArticleDetail = Schemas["ArticleDetailView"];
export type ArticleInput = Schemas["ArticleInput"];
export type ArticleRevision = Schemas["ArticleRevisionView"];
export type ArticleBlock = Schemas["ArticleBlock"];

const api = () => browserApi();

export const fetchArticles = (query: { page: number; pageSize: number; status?: string; search?: string }) =>
  required(unwrap(api().GET("/api/v1/admin/content/articles", { params: { query } })));

export const fetchArticle = (id: string) =>
  required(unwrap(api().GET("/api/v1/admin/content/articles/{id}", { params: { path: { id } } })));

export const fetchRevisions = (id: string) =>
  required(unwrap(api().GET("/api/v1/admin/content/articles/{id}/revisions", { params: { path: { id } } })));

export const createArticle = (input: ArticleInput) =>
  required(unwrap(api().POST("/api/v1/admin/content/articles", { body: input })));

export const updateArticle = (id: string, input: ArticleInput) =>
  required(unwrap(api().PUT("/api/v1/admin/content/articles/{id}", { params: { path: { id } }, body: input })));

export const deleteArticle = (id: string) =>
  unwrap(api().DELETE("/api/v1/admin/content/articles/{id}", { params: { path: { id } } }));

export const publishArticle = (id: string, rowVersion: number) =>
  required(unwrap(api().POST("/api/v1/admin/content/articles/{id}/publish", { params: { path: { id } }, body: { rowVersion } })));

export const unpublishArticle = (id: string, rowVersion: number) =>
  required(unwrap(api().POST("/api/v1/admin/content/articles/{id}/unpublish", { params: { path: { id } }, body: { rowVersion } })));
