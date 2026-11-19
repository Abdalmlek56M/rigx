import { unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";

export type Policy = Schemas["PolicyView"];
export type PolicyInput = Schemas["PolicyInput"];

const api = () => browserApi();

/** Every version of every policy kind, newest version first within a kind. */
export const fetchPolicies = () => required(unwrap(api().GET("/api/v1/admin/content/policies", { params: { query: {} } })));

export const createPolicyVersion = (kind: string) =>
  required(unwrap(api().POST("/api/v1/admin/content/policies/{kind}/versions", { params: { path: { kind } } })));

export const updatePolicy = (id: string, input: PolicyInput) =>
  required(unwrap(api().PUT("/api/v1/admin/content/policies/{id}", { params: { path: { id } }, body: input })));

export const publishPolicy = (id: string, rowVersion: number) =>
  required(unwrap(api().POST("/api/v1/admin/content/policies/{id}/publish", { params: { path: { id } }, body: { rowVersion } })));
