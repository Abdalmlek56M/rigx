import { unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";
import type { Decision, ModerationStatus } from "./customer-build-rules";

export type CustomerBuild = Schemas["CustomerBuildModerationView"];

export const fetchCustomerBuilds = (status: ModerationStatus, page: number) =>
  required(unwrap(browserApi().GET("/api/v1/admin/customer-builds", { params: { query: { status, page } } })));

export const decideCustomerBuild = (token: string, decision: Decision) =>
  required(unwrap(browserApi().POST("/api/v1/admin/customer-builds/{token}/decision", { params: { path: { token } }, body: { decision } })));
