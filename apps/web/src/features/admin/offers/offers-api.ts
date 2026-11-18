import { unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";

export type Offer = Schemas["OfferView"];
export type OfferInput = Schemas["OfferInput"];
export type OfferUpdateRequest = Schemas["OfferUpdateRequest"];
export type OfferItemView = Schemas["OfferItemView"];
export type ProductSummary = Schemas["ProductSummaryView"];

const api = () => browserApi();

export const fetchOffers = () => required(unwrap(api().GET("/api/v1/admin/content/offers")));

export const fetchProducts = () => required(unwrap(api().GET("/api/v1/admin/catalog/products")));

export const createOffer = (input: OfferInput) =>
  required(unwrap(api().POST("/api/v1/admin/content/offers", { body: input })));

export const updateOffer = (id: string, input: OfferUpdateRequest) =>
  required(unwrap(api().PUT("/api/v1/admin/content/offers/{id}", { params: { path: { id } }, body: input })));

export const publishOffer = (id: string, rowVersion: number) =>
  required(unwrap(api().POST("/api/v1/admin/content/offers/{id}/publish", { params: { path: { id } }, body: { rowVersion } })));

export const unpublishOffer = (id: string, rowVersion: number) =>
  required(unwrap(api().POST("/api/v1/admin/content/offers/{id}/unpublish", { params: { path: { id } }, body: { rowVersion } })));

export const cancelOffer = (id: string, rowVersion: number) =>
  required(unwrap(api().POST("/api/v1/admin/content/offers/{id}/cancel", { params: { path: { id } }, body: { rowVersion } })));
