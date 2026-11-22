import { multipart, unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";

export type Brand = Schemas["BrandView"];
export type BrandInput = Schemas["BrandInput"];

const api = () => browserApi();

/** Every brand, switched-off ones too, in the order the owner put them (by name until they did). */
export const fetchBrands = () => required(unwrap(api().GET("/api/v1/admin/catalog/brands")));

export const createBrand = (input: BrandInput) => required(unwrap(api().POST("/api/v1/admin/catalog/brands", { body: input })));

export const updateBrand = (id: string, input: BrandInput) =>
  required(unwrap(api().PUT("/api/v1/admin/catalog/brands/{id}", { params: { path: { id } }, body: input })));

/** The brands in this order (every brand once); the API refuses a list that is not the brands as they are now. */
export const reorderBrands = (ids: string[]) =>
  unwrap(api().PUT("/api/v1/admin/catalog/brands/order", { body: { ids } }));

export const uploadBrandLogo = (id: string, file: File) => {
  const form = new FormData();
  form.set("image", file);
  return required(unwrap(api().POST("/api/v1/admin/catalog/brands/{id}/logo", { params: { path: { id } }, ...multipart(form) })));
};

export const deleteBrandLogo = (id: string) =>
  required(unwrap(api().DELETE("/api/v1/admin/catalog/brands/{id}/logo", { params: { path: { id } } })));
