import { unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";
import type { DateRange } from "./dashboard-rules";

export type AdminDashboard = Schemas["AdminDashboardView"];
export type DashboardData = Schemas["DashboardView"];

export const fetchDashboard = ({ from, to }: DateRange) =>
  required(unwrap(browserApi().GET("/api/v1/admin/dashboard", { params: { query: { from, to } } })));
