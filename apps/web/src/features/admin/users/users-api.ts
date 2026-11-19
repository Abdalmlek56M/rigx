import { unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";

export type StaffUser = Schemas["AdminIdentityUser"];
export type StaffProfile = Schemas["StaffProfile"];
export type StaffUserPage = Schemas["PagedResultOfAdminIdentityUser"];
export type StaffRole = Schemas["AdminIdentityRole"];
export type Restriction = Schemas["RestrictionView"];
export type RestrictionInput = Schemas["RestrictionRequest"];
export type NewStaffUser = Schemas["CreateAdminIdentityUserRequest"];
export type NewRole = Schemas["CreateAdminIdentityRoleRequest"];

/** The step-up headers a protected save runs with (empty on the first try). */
type Headers = Record<string, string>;

const api = () => browserApi();
const PAGE_SIZE = 25;

export const fetchUsers = (query: { search?: string; isActive?: boolean; page: number }) =>
  required(unwrap(api().GET("/api/v1/admin/identity/users", { params: { query: { ...query, pageSize: PAGE_SIZE } } })));

export const fetchUser = (id: string) =>
  required(unwrap(api().GET("/api/v1/admin/identity/users/{id}", { params: { path: { id } } })));

export const fetchStaffProfile = (id: string) =>
  required(unwrap(api().GET("/api/v1/admin/identity/users/{id}/profile", { params: { path: { id } } })));

export const saveStaffNote = (id: string, note: string) =>
  unwrap(api().PUT("/api/v1/admin/identity/users/{id}/note", { params: { path: { id } }, body: { note } }));

export const signOutStaff = (id: string) =>
  unwrap(api().POST("/api/v1/admin/identity/users/{id}/sign-out-all", { params: { path: { id } } }));

export const createUser = (body: NewStaffUser) =>
  required(unwrap(api().POST("/api/v1/admin/identity/users", { body })));

export const updateUser = (id: string, body: { fullName: string; phoneNumber: string | null; isActive: boolean }) =>
  required(unwrap(api().PATCH("/api/v1/admin/identity/users/{id}", { params: { path: { id } }, body })));

export const assignRoles = (id: string, roleIds: string[], headers: Headers) =>
  required(unwrap(api().PUT("/api/v1/admin/identity/users/{id}/roles", { params: { path: { id } }, body: { roleIds }, headers })));

export const fetchRestrictions = (id: string) =>
  required(unwrap(api().GET("/api/v1/admin/identity/users/{id}/restrictions", { params: { path: { id } } })));

export const addRestriction = (id: string, body: RestrictionInput, headers: Headers) =>
  required(unwrap(api().POST("/api/v1/admin/identity/users/{id}/restrictions", { params: { path: { id } }, body, headers })));

export const reinstateRestriction = (id: string, reason: string, rowVersion: number) =>
  required(unwrap(api().POST("/api/v1/admin/identity/restrictions/{id}/reinstate", { params: { path: { id } }, body: { reason, rowVersion } })));

export const fetchRoles = () => required(unwrap(api().GET("/api/v1/admin/identity/roles")));

export const fetchPermissions = () => required(unwrap(api().GET("/api/v1/admin/identity/permissions")));

export const createRole = (body: NewRole, headers: Headers) =>
  required(unwrap(api().POST("/api/v1/admin/identity/roles", { body, headers })));

export const setRolePermissions = (id: string, permissionCodes: string[], headers: Headers) =>
  required(unwrap(api().PUT("/api/v1/admin/identity/roles/{id}/permissions", { params: { path: { id } }, body: { permissionCodes }, headers })));
