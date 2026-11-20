import { unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";

export type Coupon = Schemas["CouponView"];
export type CouponInput = Schemas["CouponInput"];

const api = () => browserApi();

export const fetchCoupons = () => required(unwrap(api().GET("/api/v1/admin/retail/coupons")));

export const createCoupon = (input: CouponInput) =>
  required(unwrap(api().POST("/api/v1/admin/retail/coupons", { body: input })));

export const updateCoupon = (id: string, input: CouponInput) =>
  required(unwrap(api().PUT("/api/v1/admin/retail/coupons/{id}", { params: { path: { id } }, body: input })));

export const deleteCoupon = (id: string) =>
  unwrap(api().DELETE("/api/v1/admin/retail/coupons/{id}", { params: { path: { id } } }));
