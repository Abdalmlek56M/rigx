import { unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";
import { buildTree } from "./category-rules";

export type Category = Schemas["CategoryView"];
export type CategoryInput = Schemas["CategoryInput"];
export type SpecDefinition = Schemas["SpecDefinition"];

const api = () => browserApi();

/** The categories as a tree (the endpoint returns them flat). */
export const fetchCategories = async () => buildTree(await required(unwrap(api().GET("/api/v1/admin/catalog/categories"))));

export const createCategory = (input: CategoryInput) =>
  required(unwrap(api().POST("/api/v1/admin/catalog/categories", { body: input })));

export const updateCategory = (id: string, input: CategoryInput) =>
  required(unwrap(api().PUT("/api/v1/admin/catalog/categories/{id}", { params: { path: { id } }, body: input })));

export const saveCategorySpecs = (id: string, specs: SpecDefinition[], rowVersion: number) =>
  required(unwrap(api().PUT("/api/v1/admin/catalog/categories/{id}/specs", { params: { path: { id } }, body: { specs, rowVersion } })));

export const uploadCategoryImage = (id: string, file: File) => {
  const form = new FormData();
  form.set("image", file);
  return required(unwrap(api().POST("/api/v1/admin/catalog/categories/{id}/image", { params: { path: { id } }, bodySerializer: () => form })));
};

export const deleteCategoryImage = (id: string) =>
  required(unwrap(api().DELETE("/api/v1/admin/catalog/categories/{id}/image", { params: { path: { id } } })));
