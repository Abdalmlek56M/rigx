import { unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";

export type PartnerDetail = Schemas["PartnerDetailView"];
export type FinancedPart = Schemas["FinancedPartView"];
export type PartnerRequest = Schemas["PartnerRequest"];
export type CapitalEntry = Schemas["PartnerCapitalEntryView"];
export type CapitalRequest = Schemas["PartnerCapitalRequest"];
export type Payout = Schemas["PartnerSettlementView"];
export type PayoutRequest = Schemas["PartnerPayoutRequest"];
export type Statement = Schemas["PartnerStatementView"];
export type MonthClose = Schemas["PartnerMonthCloseView"];

const api = () => browserApi();
const path = (id: string) => ({ params: { path: { id } } });

export const fetchPartner = (id: string) => required(unwrap(api().GET("/api/v1/admin/retail/partners/{id}", path(id))));

export const createPartner = (body: PartnerRequest) => required(unwrap(api().POST("/api/v1/admin/retail/partners", { body })));

export const updatePartner = (id: string, body: PartnerRequest) =>
  required(unwrap(api().PUT("/api/v1/admin/retail/partners/{id}", { ...path(id), body })));

export const fetchCapital = (id: string) => required(unwrap(api().GET("/api/v1/admin/retail/partners/{id}/capital", path(id))));

export const addCapital = (id: string, body: CapitalRequest) =>
  required(unwrap(api().POST("/api/v1/admin/retail/partners/{id}/capital", { ...path(id), body })));

export const fetchPayouts = (id: string) => required(unwrap(api().GET("/api/v1/admin/retail/partners/{id}/payouts", path(id))));

export const addPayout = (id: string, body: PayoutRequest) =>
  required(unwrap(api().POST("/api/v1/admin/retail/partners/{id}/payouts", { ...path(id), body })));

export const fetchStatement = (id: string, from: string, to: string) =>
  required(unwrap(api().GET("/api/v1/admin/retail/partners/{id}/statement", { params: { path: { id }, query: { from, to } } })));

export const closeMonth = (year: number, month: number) =>
  required(unwrap(api().POST("/api/v1/admin/retail/partners/month-close", { body: { year, month } })));
