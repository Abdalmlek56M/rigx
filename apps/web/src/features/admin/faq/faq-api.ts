import { unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";

export type Faq = Schemas["FaqView"];
export type FaqInput = Schemas["FaqInput"];

const api = () => browserApi();

export const fetchFaqs = (query: { scope?: string; categoryId?: string; itemCode?: string }) =>
  required(unwrap(api().GET("/api/v1/admin/content/faqs", { params: { query } })));

export const createFaq = (input: FaqInput) =>
  required(unwrap(api().POST("/api/v1/admin/content/faqs", { body: input })));

export const updateFaq = (id: string, rowVersion: number, input: FaqInput) =>
  required(unwrap(api().PUT("/api/v1/admin/content/faqs/{id}", { params: { path: { id }, query: { rowVersion } }, body: input })));

export const deleteFaq = (id: string) =>
  unwrap(api().DELETE("/api/v1/admin/content/faqs/{id}", { params: { path: { id } } }));

export const publishFaq = (id: string, rowVersion: number) =>
  required(unwrap(api().POST("/api/v1/admin/content/faqs/{id}/publish", { params: { path: { id } }, body: { rowVersion } })));

export const unpublishFaq = (id: string, rowVersion: number) =>
  required(unwrap(api().POST("/api/v1/admin/content/faqs/{id}/unpublish", { params: { path: { id } }, body: { rowVersion } })));
