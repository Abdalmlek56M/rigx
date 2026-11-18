import { unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";

export type AuditEntry = Schemas["AuditEntryResponse"];
export type AuditFilters = Schemas["AuditFilterLists"];
export type AuditEntryPage = Schemas["PagedResultOfAuditEntryResponse"];
export type SecurityEvent = Schemas["SecurityAuditItemView"];
export type SecurityEventPage = Schemas["PagedResultOfSecurityAuditItemView"];

const api = () => browserApi();
const PAGE_SIZE = 25;

export const fetchEntries = (query: { module?: string; entity?: string; action?: string; from?: string; to?: string; page: number }) =>
  required(unwrap(api().GET("/api/v1/admin/audit/entries", { params: { query: { ...query, pageSize: PAGE_SIZE } } })));

export const fetchEntry = (id: string) =>
  required(unwrap(api().GET("/api/v1/admin/audit/entries/{id}", { params: { path: { id } } })));

export const fetchAuditFilters = () => required(unwrap(api().GET("/api/v1/admin/audit/filters")));

export const fetchEvents = (query: { fromUtc?: string; toUtc?: string; eventType?: string; outcome?: string; search?: string; page: number }) =>
  required(unwrap(api().GET("/api/v1/admin/audit/events", { params: { query: { ...query, pageSize: PAGE_SIZE } } })));
