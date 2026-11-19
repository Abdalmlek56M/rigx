import { unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";

export type Brand = Schemas["BrandView"];
export type BrandInput = Schemas["BrandInput"];

const api = () => browserApi();

/** Every brand, switched-off ones too, by name. */
export const fetchBrands = () => required(unwrap(api().GET("/api/v1/admin/catalog/brands")));

export const createBrand = (input: BrandInput) => required(unwrap(api().POST("/api/v1/admin/catalog/brands", { body: input })));

export const updateBrand = (id: string, input: BrandInput) =>
  required(unwrap(api().PUT("/api/v1/admin/catalog/brands/{id}", { params: { path: { id } }, body: input })));
