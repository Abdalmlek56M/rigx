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
/** The change log groups a page by day and kind, so it reads more rows at once. */
const ENTRIES_PAGE_SIZE = 50;

export const fetchEntries = (query: { module?: string; entity?: string; entityId?: string; action?: string; from?: string; to?: string; page: number }) =>
  required(unwrap(api().GET("/api/v1/admin/audit/entries", { params: { query: { ...query, pageSize: ENTRIES_PAGE_SIZE } } })));

export const fetchEntry = (id: string) =>
  required(unwrap(api().GET("/api/v1/admin/audit/entries/{id}", { params: { path: { id } } })));

export const fetchAuditFilters = () => required(unwrap(api().GET("/api/v1/admin/audit/filters")));

export const fetchEvents = (query: { fromUtc?: string; toUtc?: string; eventType?: string; outcome?: string; search?: string; page: number }) =>
  required(unwrap(api().GET("/api/v1/admin/audit/events", { params: { query: { ...query, pageSize: PAGE_SIZE } } })));

let linkedNames: Promise<ReadonlyMap<string, string>> | null = null;

/** The names of brands and categories by id, read once: the change log stores a link as an id, and "Brand: Intel" reads better than "Brand: set". A viewer without catalog access gets none. */
export function fetchLinkedNames(): Promise<ReadonlyMap<string, string>> {
  linkedNames ??= Promise.all([
    unwrap(api().GET("/api/v1/admin/catalog/brands")).catch(() => []),
    unwrap(api().GET("/api/v1/admin/catalog/categories")).catch(() => [])
  ]).then(([brands, categories]) => new Map([...(brands ?? []), ...(categories ?? [])].map((record) => [record.id, record.name] as const)));
  return linkedNames;
}

export const restoreEntry = (id: string, headers: Record<string, string>) =>
  unwrap(browserApi().POST("/api/v1/admin/audit/entries/{id}/restore", { params: { path: { id } }, headers }));
