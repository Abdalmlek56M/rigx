export type CurrentUser = {
  username: string;
  email: string;
  fullName: string;
  phoneNumber: string | null;
  roles: string[];
  permissions: string[];
  isEmailVerified: boolean;
  avatarStorageKey: string | null;
};

export type ProblemPayload = {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
};

export class ApiError extends Error {
  readonly status: number;
  readonly payload: ProblemPayload | null;

  constructor(status: number, payload: ProblemPayload | null) {
    super(payload?.title || `Request failed with status ${status}`);
    this.name = "ApiError";
    this.status = status;
    this.payload = payload;
  }
}

type ApiRequestOptions = {
  method?: "GET" | "POST" | "PUT" | "PATCH" | "DELETE";
  body?: unknown;
};

async function readJson(response: Response): Promise<unknown> {
  if (response.status === 204) return undefined;
  const contentType = response.headers.get("content-type") ?? "";
  if (!contentType.includes("json")) return undefined;
  return response.json();
}

async function getCsrfToken(): Promise<string> {
  const response = await fetch("/api/v1/identity/antiforgery", {
    method: "GET",
    credentials: "include",
    cache: "no-store",
  });
  const payload = await readJson(response) as { requestToken?: string } | undefined;
  if (!response.ok || !payload?.requestToken) {
    throw new ApiError(response.status, payload as ProblemPayload | null);
  }
  return payload.requestToken;
}

async function apiRequest<T>(path: string, options: ApiRequestOptions = {}): Promise<T> {
  const method = options.method ?? "GET";
  const headers = new Headers({ Accept: "application/json" });
  if (options.body !== undefined) headers.set("Content-Type", "application/json");
  if (method !== "GET") headers.set("X-CSRF-TOKEN", await getCsrfToken());

  const response = await fetch(path, {
    method,
    credentials: "include",
    cache: "no-store",
    headers,
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
  });
  const payload = await readJson(response);
  if (!response.ok) throw new ApiError(response.status, payload as ProblemPayload | null);
  return payload as T;
}

export async function getCurrentUser(): Promise<CurrentUser | null> {
  const response = await fetch("/api/v1/identity/me", {
    method: "GET",
    credentials: "include",
    cache: "no-store",
    headers: { Accept: "application/json" },
  });
  if (response.status === 401) return null;
  const payload = await readJson(response);
  if (!response.ok) throw new ApiError(response.status, payload as ProblemPayload | null);
  return payload as CurrentUser;
}

export async function registerCustomer(input: {
  fullName: string;
  phoneNumber?: string;
  username: string;
  email: string;
}): Promise<void> {
  await apiRequest("/api/v1/identity/registrations", { method: "POST", body: input });
}

export async function verifyEmail(token: string, newPassword: string): Promise<void> {
  await apiRequest("/api/v1/identity/email-verifications", {
    method: "POST",
    body: { token, newPassword },
  });
}

export async function resendVerification(email: string): Promise<void> {
  await apiRequest("/api/v1/identity/email-verification-resends", {
    method: "POST",
    body: { email },
  });
}

export async function login(identifier: string, password: string): Promise<void> {
  await apiRequest("/api/v1/identity/sessions", {
    method: "POST",
    body: { identifier, password },
  });
}

export async function requestEmailLoginOtp(email: string): Promise<{ status: string }> {
  return apiRequest("/api/v1/identity/email-otp/login/request", { method: "POST", body: { email } });
}

export async function verifyEmailLoginOtp(email: string, code: string): Promise<{ sessionScope: string; method: string }> {
  return apiRequest("/api/v1/identity/email-otp/login/verify", { method: "POST", body: { email, code } });
}

export async function requestPasswordResetOtp(email: string): Promise<{ status: string }> {
  return apiRequest("/api/v1/identity/password-reset/otp/request", { method: "POST", body: { email } });
}

export async function resetPasswordWithOtp(email: string, code: string, newPassword: string): Promise<{ status: string }> {
  return apiRequest("/api/v1/identity/password-reset/otp/verify", { method: "POST", body: { email, code, newPassword } });
}

export async function requestPasswordReset(email: string): Promise<void> {
  await apiRequest("/api/v1/identity/password-reset-requests", {
    method: "POST",
    body: { email },
  });
}

export async function resetPassword(token: string, newPassword: string): Promise<void> {
  await apiRequest("/api/v1/identity/password-resets", {
    method: "POST",
    body: { token, newPassword },
  });
}

export async function changePassword(currentPassword: string, newPassword: string): Promise<void> {
  await apiRequest("/api/v1/identity/password-changes", {
    method: "POST",
    body: { currentPassword, newPassword },
  });
}

export async function logoutCurrent(): Promise<void> {
  await apiRequest("/api/v1/identity/sessions/current", { method: "DELETE" });
}

export async function logoutAll(): Promise<void> {
  await apiRequest("/api/v1/identity/sessions", { method: "DELETE" });
}

export type CatalogProductStatus = "Draft" | "Published";
export type CatalogProductCondition = "New" | "Used";

export type CatalogCategory = {
  id: string;
  parentId: string | null;
  name: string;
  slug: string;
  description: string | null;
  iconStorageKey: string | null;
  sortOrder: number;
  isActive: boolean;
  isEffectivelyActive: boolean;
  rowVersion: number;
  children: CatalogCategory[];
};

export type CatalogBrand = {
  id: string;
  name: string;
  slug: string;
  description: string | null;
  logoStorageKey: string | null;
  supportUrl: string | null;
  isActive: boolean;
  rowVersion: number;
};

export type CatalogProductSummary = {
  id: string;
  name: string;
  slug: string;
  sku: string;
  basePrice: number;
  fromPrice: number;
  status: CatalogProductStatus;
  condition: CatalogProductCondition;
  categoryName: string;
  categorySlug: string;
  brandName: string | null;
  specifications: Array<{
    groupName: string;
    name: string;
    value: string;
  }>;
  primaryImageStorageKey: string | null;
};

export type CatalogVariant = {
  id: string;
  name: string;
  sku: string;
  barcode: string | null;
  price: number | null;
  isActive: boolean;
};

export type CatalogSpecification = {
  id: string;
  variantId: string | null;
  groupName: string;
  name: string;
  value: string;
  sortOrder: number;
};

export type CatalogImage = {
  id: string;
  storageKey: string;
  thumbnailStorageKey: string;
  altText: string;
  sortOrder: number;
  isPrimary: boolean;
  width: number;
  height: number;
  thumbnailWidth: number;
  thumbnailHeight: number;
};

export type CatalogProduct = {
  id: string;
  name: string;
  slug: string;
  sku: string;
  barcode: string | null;
  categoryId: string;
  categoryName: string;
  categorySlug: string;
  brandId: string | null;
  brandName: string | null;
  shortDescription: string | null;
  description: string | null;
  basePrice: number;
  status: CatalogProductStatus;
  condition: CatalogProductCondition;
  rowVersion: number;
  variants: CatalogVariant[];
  specifications: CatalogSpecification[];
  images: CatalogImage[];
};

export type CatalogProductPage = {
  items: CatalogProductSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type CatalogSpecificationFilter = {
  groupName: string;
  name: string;
  values: string[];
};

export type CatalogProductSort =
  | "newest"
  | "oldest"
  | "price-asc"
  | "price-desc"
  | "name-asc"
  | "name-desc";

export type CategoryInput = {
  name: string;
  slug: string;
  parentId: string | null;
  description?: string;
  iconStorageKey?: string;
  sortOrder: number;
  isActive: boolean;
  rowVersion?: number;
};

export type BrandInput = {
  name: string;
  slug: string;
  description?: string;
  logoStorageKey?: string;
  supportUrl?: string;
  isActive: boolean;
  rowVersion?: number;
};

export type ProductInput = {
  name: string;
  slug: string;
  sku: string;
  barcode?: string;
  categoryId: string;
  brandId: string | null;
  shortDescription?: string;
  description?: string;
  basePrice: number;
  status: CatalogProductStatus;
  condition: CatalogProductCondition;
  variants: Array<Omit<CatalogVariant, "id"> & { id?: string }>;
  specifications: Array<{
    id?: string;
    variantId?: string | null;
    variantSku?: string | null;
    groupName: string;
    name: string;
    value: string;
    sortOrder: number;
  }>;
  rowVersion?: number;
};

export function catalogMediaUrl(storageKey: string): string {
  const encoded = storageKey.split("/").map(encodeURIComponent).join("/");
  return `/api/v1/catalog/media/${encoded}`;
}

export async function getCatalogCategories(): Promise<CatalogCategory[]> {
  return apiRequest("/api/v1/catalog/categories");
}

export async function getCatalogBrands(): Promise<CatalogBrand[]> {
  return apiRequest("/api/v1/catalog/brands");
}

export async function getCatalogSpecificationFilters(): Promise<CatalogSpecificationFilter[]> {
  return apiRequest("/api/v1/catalog/specification-filters");
}

export async function getCatalogProducts(params: {
  category?: string;
  brand?: string;
  search?: string;
  condition?: CatalogProductCondition;
  minPrice?: number;
  maxPrice?: number;
  specifications?: string[];
  sort?: CatalogProductSort;
  page?: number;
  pageSize?: number;
} = {}): Promise<CatalogProductPage> {
  const query = new URLSearchParams();
  if (params.category) query.set("category", params.category);
  if (params.brand) query.set("brand", params.brand);
  if (params.search) query.set("search", params.search);
  if (params.condition) query.set("condition", params.condition.toLowerCase());
  if (params.minPrice !== undefined) query.set("minimumPrice", String(params.minPrice));
  if (params.maxPrice !== undefined) query.set("maximumPrice", String(params.maxPrice));
  for (const specification of params.specifications ?? []) {
    query.append("specification", specification);
  }
  if (params.sort) query.set("sort", params.sort);
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  return apiRequest(`/api/v1/catalog/products?${query}`);
}

export async function getCatalogProduct(slug: string): Promise<CatalogProduct> {
  return apiRequest(`/api/v1/catalog/products/${encodeURIComponent(slug)}`);
}

export async function getAdminCategories(): Promise<CatalogCategory[]> {
  return apiRequest("/api/v1/admin/catalog/categories");
}

export async function getAdminBrands(): Promise<CatalogBrand[]> {
  return apiRequest("/api/v1/admin/catalog/brands");
}

export async function getAdminProducts(): Promise<CatalogProductSummary[]> {
  return apiRequest("/api/v1/admin/catalog/products");
}

export async function getAdminProduct(id: string): Promise<CatalogProduct> {
  return apiRequest(`/api/v1/admin/catalog/products/${id}`);
}

export async function saveCategory(id: string | null, input: CategoryInput): Promise<CatalogCategory> {
  return apiRequest(id ? `/api/v1/admin/catalog/categories/${id}` : "/api/v1/admin/catalog/categories", {
    method: id ? "PUT" : "POST",
    body: input,
  });
}

export async function saveBrand(id: string | null, input: BrandInput): Promise<CatalogBrand> {
  return apiRequest(id ? `/api/v1/admin/catalog/brands/${id}` : "/api/v1/admin/catalog/brands", {
    method: id ? "PUT" : "POST",
    body: input,
  });
}

export async function saveProduct(id: string | null, input: ProductInput): Promise<CatalogProduct> {
  return apiRequest(id ? `/api/v1/admin/catalog/products/${id}` : "/api/v1/admin/catalog/products", {
    method: id ? "PUT" : "POST",
    body: input,
  });
}

export async function setCatalogProductStatus(
  id: string,
  status: CatalogProductStatus,
  rowVersion: number,
): Promise<CatalogProduct> {
  return apiRequest(`/api/v1/admin/catalog/products/${id}/status`, {
    method: "PATCH",
    body: { status, rowVersion },
  });
}

export async function uploadCatalogImage(
  productId: string,
  image: Blob,
  altText: string,
  isPrimary: boolean,
): Promise<CatalogImage> {
  const form = new FormData();
  form.append("image", image, "product.webp");
  form.append("altText", altText);
  form.append("isPrimary", String(isPrimary));
  const response = await fetch(`/api/v1/admin/catalog/products/${productId}/images`, {
    method: "POST",
    credentials: "include",
    cache: "no-store",
    headers: { "X-CSRF-TOKEN": await getCsrfToken() },
    body: form,
  });
  const payload = await readJson(response);
  if (!response.ok) throw new ApiError(response.status, payload as ProblemPayload | null);
  return payload as CatalogImage;
}

export async function uploadCategoryImage(categoryId: string, image: Blob): Promise<CatalogCategory> {
  return uploadAssetImage(`/api/v1/admin/catalog/categories/${categoryId}/image`, image);
}

export async function deleteCategoryImage(categoryId: string): Promise<CatalogCategory> {
  return apiRequest(`/api/v1/admin/catalog/categories/${categoryId}/image`, { method: "DELETE" });
}

export async function uploadBrandLogo(brandId: string, image: Blob): Promise<CatalogBrand> {
  return uploadAssetImage(`/api/v1/admin/catalog/brands/${brandId}/logo`, image);
}

export async function deleteBrandLogo(brandId: string): Promise<CatalogBrand> {
  return apiRequest(`/api/v1/admin/catalog/brands/${brandId}/logo`, { method: "DELETE" });
}

async function uploadAssetImage<T>(path: string, image: Blob): Promise<T> {
  const form = new FormData();
  form.append("image", image, "catalog-asset");
  const response = await fetch(path, {
    method: "POST",
    credentials: "include",
    cache: "no-store",
    headers: { "X-CSRF-TOKEN": await getCsrfToken() },
    body: form,
  });
  const payload = await readJson(response);
  if (!response.ok) throw new ApiError(response.status, payload as ProblemPayload | null);
  return payload as T;
}

export async function updateCatalogImage(
  productId: string,
  imageId: string,
  input: { altText: string; sortOrder: number; isPrimary: boolean },
): Promise<CatalogImage> {
  return apiRequest(`/api/v1/admin/catalog/products/${productId}/images/${imageId}`, {
    method: "PUT",
    body: input,
  });
}

export async function deleteCatalogImage(productId: string, imageId: string): Promise<void> {
  await apiRequest(`/api/v1/admin/catalog/products/${productId}/images/${imageId}`, {
    method: "DELETE",
  });
}

export type CatalogMediaAudit = {
  referencedKeyCount: number;
  storedFileCount: number;
  missingStorageKeys: string[];
  orphanStorageKeys: string[];
};

export type CatalogMediaCleanup = {
  audit: CatalogMediaAudit;
  deletedStorageKeys: string[];
};

export async function getCatalogMediaAudit(): Promise<CatalogMediaAudit> {
  return apiRequest("/api/v1/admin/catalog/media-audit");
}

export async function cleanupCatalogMedia(): Promise<CatalogMediaCleanup> {
  return apiRequest("/api/v1/admin/catalog/media-audit/cleanup", { method: "POST" });
}

export type AdminIdentityUser = {
  id: string;
  username: string;
  email: string;
  fullName: string;
  phoneNumber: string | null;
  isActive: boolean;
  isEmailVerified: boolean;
  lockoutEndUtc: string | null;
  authorizationVersion: number;
  roleIds: string[];
  roleCodes: string[];
};

export type AdminIdentityUserPage = {
  items: AdminIdentityUser[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type AdminIdentityRole = {
  id: string;
  code: string;
  name: string;
  userCount: number;
  permissionCodes: string[];
};

export async function getAdminIdentityUsers(params: {
  search?: string;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
} = {}): Promise<AdminIdentityUserPage> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.isActive !== undefined) query.set("isActive", String(params.isActive));
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const suffix = query.toString();
  return apiRequest(`/api/v1/admin/identity/users${suffix ? `?${suffix}` : ""}`);
}

export async function getAdminIdentityUser(id: string): Promise<AdminIdentityUser> {
  return apiRequest(`/api/v1/admin/identity/users/${id}`);
}

export async function createAdminIdentityUser(input: {
  username: string;
  email: string;
  fullName: string;
  phoneNumber: string | null;
  roleIds: string[];
}): Promise<{ user: AdminIdentityUser; emailDispatchQueued: boolean }> {
  return apiRequest("/api/v1/admin/identity/users", { method: "POST", body: input });
}

export async function requestAdminIdentityPasswordReset(id: string): Promise<void> {
  await apiRequest(`/api/v1/admin/identity/users/${id}/password-reset`, { method: "POST" });
}

export type IdentitySession = {
  id: string;
  isCurrent: boolean;
  device: string;
  browser: string;
  createdAtUtc: string;
  lastActivityAtUtc: string;
  idleExpiresAtUtc: string;
  absoluteExpiresAtUtc: string;
};

export async function getIdentitySessions(): Promise<IdentitySession[]> {
  return apiRequest("/api/v1/identity/sessions");
}

export async function revokeIdentitySession(id: string): Promise<void> {
  await apiRequest(`/api/v1/identity/sessions/${id}`, { method: "DELETE" });
}

export async function revokeOtherIdentitySessions(): Promise<void> {
  await apiRequest("/api/v1/identity/sessions/others", { method: "DELETE" });
}

export async function updateIdentityProfile(input: {
  fullName: string;
  phoneNumber: string | null;
}): Promise<CurrentUser> {
  return apiRequest("/api/v1/identity/profile", { method: "PATCH", body: input });
}

export type CustomerAddress = {
  id: string;
  label: string;
  recipientName: string;
  phoneNumber: string;
  countryCode: string;
  countryCallingCode: string;
  governorate: string;
  city: string;
  district: string | null;
  street: string;
  building: string | null;
  floor: string | null;
  apartment: string | null;
  landmark: string | null;
  postalCode: string | null;
  instructions: string | null;
  isDefault: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
  rowVersion: number;
};

export type CustomerAddressInput = Omit<CustomerAddress, "id" | "createdAtUtc" | "updatedAtUtc" | "rowVersion">;

export async function getCustomerAddresses(): Promise<CustomerAddress[]> {
  return apiRequest("/api/v1/identity/addresses");
}

export async function createCustomerAddress(input: CustomerAddressInput): Promise<CustomerAddress> {
  return apiRequest("/api/v1/identity/addresses", { method: "POST", body: input });
}

export async function updateCustomerAddress(
  id: string,
  input: CustomerAddressInput & { rowVersion: number },
): Promise<CustomerAddress> {
  return apiRequest(`/api/v1/identity/addresses/${id}`, { method: "PUT", body: input });
}

export async function deleteCustomerAddress(id: string, rowVersion: number): Promise<void> {
  await apiRequest(`/api/v1/identity/addresses/${id}?rowVersion=${rowVersion}`, { method: "DELETE" });
}

export async function updateAdminIdentityUser(
  id: string,
  input: { fullName: string; phoneNumber: string | null; isActive: boolean },
): Promise<AdminIdentityUser> {
  return apiRequest(`/api/v1/admin/identity/users/${id}`, { method: "PATCH", body: input });
}

export async function assignAdminIdentityRoles(
  id: string,
  roleIds: string[],
): Promise<AdminIdentityUser> {
  return apiRequest(`/api/v1/admin/identity/users/${id}/roles`, {
    method: "PUT",
    body: { roleIds },
  });
}

export async function getAdminIdentityRoles(): Promise<AdminIdentityRole[]> {
  return apiRequest("/api/v1/admin/identity/roles");
}

export async function getAdminIdentityPermissions(): Promise<string[]> {
  return apiRequest("/api/v1/admin/identity/permissions");
}

export async function createAdminIdentityRole(input: {
  code: string;
  name: string;
  permissionCodes: string[];
}): Promise<AdminIdentityRole> {
  return apiRequest("/api/v1/admin/identity/roles", { method: "POST", body: input });
}

export async function updateAdminIdentityRolePermissions(
  id: string,
  permissionCodes: string[],
): Promise<AdminIdentityRole> {
  return apiRequest(`/api/v1/admin/identity/roles/${id}/permissions`, {
    method: "PUT",
    body: { permissionCodes },
  });
}

export type Supplier = {
  id: string;
  personId: string;
  personCode: string;
  companyName: string;
  phoneNumber: string | null;
  email: string | null;
  taxIdNumber: string | null;
  commercialRegistration: string | null;
  contactPersonName: string | null;
  contactPersonPhone: string | null;
  paymentTermsDays: number;
  notes: string | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
  rowVersion: number;
};

export type SupplierPage = {
  items: Supplier[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type SupplierCreateInput = {
  companyName: string;
  phoneNumber: string | null;
  email: string | null;
  taxIdNumber: string | null;
  commercialRegistration: string | null;
  contactPersonName: string | null;
  contactPersonPhone: string | null;
  paymentTermsDays: number;
  notes: string | null;
  isActive: boolean;
};

export type SupplierUpdateInput = SupplierCreateInput & {
  rowVersion: number;
};

export type SupplierFinancials = {
  supplierId: string;
  bankName: string | null;
  bankAccountNumber: string | null;
  bankIban: string | null;
  rowVersion: number;
};

export type SupplierFinancialWriteInput = {
  bankName: string | null;
  bankAccountNumber: string | null;
  bankIban: string | null;
  rowVersion: number;
};

export async function getSuppliers(params: {
  search?: string;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
} = {}): Promise<SupplierPage> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.isActive !== undefined) query.set("isActive", String(params.isActive));
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const suffix = query.toString();
  return apiRequest(`/api/v1/admin/purchasing/suppliers${suffix ? `?${suffix}` : ""}`);
}

export async function getSupplier(id: string): Promise<Supplier> {
  return apiRequest(`/api/v1/admin/purchasing/suppliers/${id}`);
}

export async function createSupplier(input: SupplierCreateInput): Promise<Supplier> {
  return apiRequest("/api/v1/admin/purchasing/suppliers", { method: "POST", body: input });
}

export async function updateSupplier(id: string, input: SupplierUpdateInput): Promise<Supplier> {
  return apiRequest(`/api/v1/admin/purchasing/suppliers/${id}`, { method: "PUT", body: input });
}

export async function getSupplierFinancials(id: string): Promise<SupplierFinancials> {
  return apiRequest(`/api/v1/admin/purchasing/suppliers/${id}/financials`);
}

export async function updateSupplierFinancials(
  id: string,
  input: SupplierFinancialWriteInput,
): Promise<SupplierFinancials> {
  return apiRequest(`/api/v1/admin/purchasing/suppliers/${id}/financials`, {
    method: "PUT",
    body: input,
  });
}

export type SupplierProduct = {
  id: string;
  supplierProfileId: string;
  productVariantId: string;
  productId: string;
  productName: string;
  variantName: string;
  variantSku: string;
  supplierSku: string;
  notes: string | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
  rowVersion: number;
};

export type SupplierProductPage = {
  items: SupplierProduct[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type SupplierVariantOption = {
  id: string;
  productId: string;
  productName: string;
  variantName: string;
  variantSku: string;
};

export type SupplierProductCreateInput = {
  productVariantId: string;
  supplierSku: string;
  notes: string | null;
  isActive: boolean;
};

export type SupplierProductUpdateInput = {
  supplierSku: string;
  notes: string | null;
  isActive: boolean;
  rowVersion: number;
};

export async function getSupplierProducts(
  supplierId: string,
  params: { search?: string; isActive?: boolean; page?: number; pageSize?: number } = {},
): Promise<SupplierProductPage> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.isActive !== undefined) query.set("isActive", String(params.isActive));
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const suffix = query.toString();
  return apiRequest(`/api/v1/admin/purchasing/suppliers/${supplierId}/products${suffix ? `?${suffix}` : ""}`);
}

export async function searchSupplierVariants(
  supplierId: string,
  search: string,
): Promise<SupplierVariantOption[]> {
  const query = new URLSearchParams({ search });
  return apiRequest(`/api/v1/admin/purchasing/suppliers/${supplierId}/available-variants?${query.toString()}`);
}

export async function createSupplierProduct(
  supplierId: string,
  input: SupplierProductCreateInput,
): Promise<SupplierProduct> {
  return apiRequest(`/api/v1/admin/purchasing/suppliers/${supplierId}/products`, {
    method: "POST",
    body: input,
  });
}

export async function updateSupplierProduct(
  supplierId: string,
  id: string,
  input: SupplierProductUpdateInput,
): Promise<SupplierProduct> {
  return apiRequest(`/api/v1/admin/purchasing/suppliers/${supplierId}/products/${id}`, {
    method: "PUT",
    body: input,
  });
}

export type Warehouse = {
  id: string;
  code: string;
  name: string;
  address: string | null;
  contactPhone: string | null;
  managerName: string | null;
  notes: string | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
  rowVersion: number;
};

export type WarehousePage = {
  items: Warehouse[];
  page: number;
  pageSize: number;
  totalCount: number;
};

export type WarehouseCreateInput = {
  code: string;
  name: string;
  address: string | null;
  contactPhone: string | null;
  managerName: string | null;
  notes: string | null;
  isActive: boolean;
};

export type WarehouseUpdateInput = WarehouseCreateInput & {
  rowVersion: number;
};

export async function getWarehouses(params: {
  search?: string;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
} = {}): Promise<WarehousePage> {
  const query = new URLSearchParams();
  if (params.search) query.set("search", params.search);
  if (params.isActive !== undefined) query.set("isActive", String(params.isActive));
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const suffix = query.toString();
  return apiRequest(`/api/v1/admin/inventory/warehouses${suffix ? `?${suffix}` : ""}`);
}

export async function createWarehouse(input: WarehouseCreateInput): Promise<Warehouse> {
  return apiRequest("/api/v1/admin/inventory/warehouses", { method: "POST", body: input });
}

export async function updateWarehouse(id: string, input: WarehouseUpdateInput): Promise<Warehouse> {
  return apiRequest(`/api/v1/admin/inventory/warehouses/${id}`, { method: "PUT", body: input });
}

export type StoreSettings = {
  whatsAppNumber: string | null;
  whatsAppMessage: string | null;
  facebookUrl: string | null;
  telegramUrl: string | null;
  tikTokUrl: string | null;
  youTubeUrl: string | null;
  instagramUrl: string | null;
  pickupAddress: string | null;
  pickupHours: string | null;
  pickupInstructions: string | null;
  warrantySummary: string | null;
  serviceSummary: string | null;
  consultationSummary: string | null;
  rowVersion: number;
};

export type StoreSettingsUpdateInput = Omit<StoreSettings, "rowVersion"> & {
  rowVersion: number;
};

export async function getStorefrontSettings(): Promise<StoreSettings> {
  return apiRequest("/api/v1/storefront/settings");
}

export async function getAdminStoreSettings(): Promise<StoreSettings> {
  return apiRequest("/api/v1/admin/storefront/settings");
}

export async function updateAdminStoreSettings(input: StoreSettingsUpdateInput): Promise<StoreSettings> {
  return apiRequest("/api/v1/admin/storefront/settings", { method: "PUT", body: input });
}

export type WishlistItem = {
  productId: string;
  name: string;
  slug: string;
  sku: string;
  basePrice: number;
  primaryImageStorageKey: string | null;
  categorySlug: string | null;
  isAvailable: boolean;
  addedAtUtc: string;
};

export async function getWishlist(): Promise<WishlistItem[]> {
  return apiRequest("/api/v1/wishlist");
}

export async function addWishlistItem(productId: string): Promise<void> {
  await apiRequest(`/api/v1/wishlist/${productId}`, { method: "POST" });
}

export async function removeWishlistItem(productId: string): Promise<void> {
  await apiRequest(`/api/v1/wishlist/${productId}`, { method: "DELETE" });
}

export async function clearWishlist(): Promise<void> {
  await apiRequest("/api/v1/wishlist", { method: "DELETE" });
}

export async function mergeWishlist(productIds: string[]): Promise<WishlistItem[]> {
  return apiRequest("/api/v1/wishlist/merge", { method: "POST", body: { productIds } });
}

export type HomepageLayoutRecord = {
  layoutJson: string;
  rowVersion: number;
  publishedAtUtc: string | null;
};

export async function getPublishedHomepageLayout(): Promise<HomepageLayoutRecord> {
  return apiRequest("/api/v1/content/homepage");
}

export async function getHomepageDraft(): Promise<HomepageLayoutRecord> {
  return apiRequest("/api/v1/admin/content/homepage");
}

export async function saveHomepageDraft(layoutJson: string, rowVersion: number): Promise<HomepageLayoutRecord> {
  return apiRequest("/api/v1/admin/content/homepage", { method: "PUT", body: { layoutJson, rowVersion } });
}

export async function publishHomepage(rowVersion: number): Promise<HomepageLayoutRecord> {
  return apiRequest("/api/v1/admin/content/homepage/publish", { method: "POST", body: { rowVersion } });
}

export type ContentPage<T> = {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
};

export type ContentArticle = {
  id: string;
  slug: string;
  titleAr: string;
  titleEn: string;
  summaryAr: string;
  summaryEn: string;
  bodyAr: string;
  bodyEn: string;
  mainImageStorageKey: string | null;
  categorySlug: string | null;
  tags: string[];
  status: "Draft" | "Published" | string;
  seoTitleAr: string | null;
  seoTitleEn: string | null;
  seoDescriptionAr: string | null;
  seoDescriptionEn: string | null;
  canonicalUrl: string | null;
  sortOrder: number;
  publishedAtUtc: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  rowVersion: number;
};

export type ContentArticleInput = {
  slug: string;
  titleAr: string;
  titleEn: string;
  summaryAr: string;
  summaryEn: string;
  bodyAr: string;
  bodyEn: string;
  mainImageStorageKey: string | null;
  categorySlug: string | null;
  tags: string;
  seoTitleAr: string | null;
  seoTitleEn: string | null;
  seoDescriptionAr: string | null;
  seoDescriptionEn: string | null;
  canonicalUrl: string | null;
  sortOrder: number;
  rowVersion?: number;
};

export type ContentArticleRevision = {
  id: string;
  reason: string;
  createdAtUtc: string;
  actorUserAccountId: string | null;
};

export async function getPublicArticles(params: { category?: string; page?: number; pageSize?: number } = {}): Promise<ContentPage<ContentArticle>> {
  const query = new URLSearchParams();
  if (params.category) query.set("category", params.category);
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const suffix = query.toString();
  return apiRequest(`/api/v1/content/articles${suffix ? `?${suffix}` : ""}`);
}

export async function getPublicArticle(slug: string): Promise<ContentArticle> {
  return apiRequest(`/api/v1/content/articles/${encodeURIComponent(slug)}`);
}

export async function getAdminArticles(params: { status?: string; search?: string; page?: number; pageSize?: number } = {}): Promise<ContentPage<ContentArticle>> {
  const query = new URLSearchParams();
  if (params.status) query.set("status", params.status);
  if (params.search) query.set("search", params.search);
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const suffix = query.toString();
  return apiRequest(`/api/v1/admin/content/articles${suffix ? `?${suffix}` : ""}`);
}

export async function getAdminArticle(id: string): Promise<ContentArticle> {
  return apiRequest(`/api/v1/admin/content/articles/${id}`);
}

export async function getArticleRevisions(id: string): Promise<ContentArticleRevision[]> {
  return apiRequest(`/api/v1/admin/content/articles/${id}/revisions`);
}

export async function createArticle(input: ContentArticleInput): Promise<ContentArticle> {
  return apiRequest("/api/v1/admin/content/articles", { method: "POST", body: input });
}

export async function updateArticle(id: string, input: ContentArticleInput): Promise<ContentArticle> {
  return apiRequest(`/api/v1/admin/content/articles/${id}`, { method: "PUT", body: input });
}

export async function publishArticle(id: string, rowVersion: number): Promise<ContentArticle> {
  return apiRequest(`/api/v1/admin/content/articles/${id}/publish`, { method: "POST", body: { rowVersion } });
}

export async function unpublishArticle(id: string, rowVersion: number): Promise<ContentArticle> {
  return apiRequest(`/api/v1/admin/content/articles/${id}/unpublish`, { method: "POST", body: { rowVersion } });
}

export type ContentFaq = {
  id: string;
  questionAr: string;
  questionEn: string;
  answerAr: string;
  answerEn: string;
  category: string | null;
  sortOrder: number;
  status: string;
  publishedAtUtc: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  rowVersion: number;
};

export type ContentFaqInput = {
  questionAr: string;
  questionEn: string;
  answerAr: string;
  answerEn: string;
  category: string | null;
  sortOrder: number;
};

export async function getPublicFaqs(): Promise<ContentFaq[]> {
  return apiRequest("/api/v1/content/faqs");
}

export async function getAdminFaqs(): Promise<ContentFaq[]> {
  return apiRequest("/api/v1/admin/content/faqs");
}

export async function createFaq(input: ContentFaqInput): Promise<ContentFaq> {
  return apiRequest("/api/v1/admin/content/faqs", { method: "POST", body: input });
}

export async function updateFaq(id: string, input: ContentFaqInput, rowVersion: number): Promise<ContentFaq> {
  return apiRequest(`/api/v1/admin/content/faqs/${id}?rowVersion=${rowVersion}`, { method: "PUT", body: input });
}

export async function setFaqPublished(id: string, published: boolean, rowVersion: number): Promise<ContentFaq> {
  return apiRequest(`/api/v1/admin/content/faqs/${id}/${published ? "publish" : "unpublish"}`, {
    method: "POST",
    body: { rowVersion },
  });
}

export type ContentPolicy = {
  id: string;
  kind: string;
  titleAr: string;
  titleEn: string;
  bodyAr: string;
  bodyEn: string;
  status: string;
  versionNumber: number;
  publishedAtUtc: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  rowVersion: number;
};

export async function getPublicPolicy(kind: string): Promise<ContentPolicy> {
  return apiRequest(`/api/v1/content/policies/${encodeURIComponent(kind)}`);
}

export async function getAdminPolicies(kind?: string): Promise<ContentPolicy[]> {
  return apiRequest(`/api/v1/admin/content/policies${kind ? `?kind=${encodeURIComponent(kind)}` : ""}`);
}

export async function createPolicyVersion(kind: string): Promise<ContentPolicy> {
  return apiRequest(`/api/v1/admin/content/policies/${kind}/versions`, { method: "POST" });
}

export async function updatePolicy(id: string, input: { titleAr: string; titleEn: string; bodyAr: string; bodyEn: string; rowVersion: number }): Promise<ContentPolicy> {
  return apiRequest(`/api/v1/admin/content/policies/${id}`, { method: "PUT", body: input });
}

export async function reviewPolicy(id: string, rowVersion: number): Promise<ContentPolicy> {
  return apiRequest(`/api/v1/admin/content/policies/${id}/review`, { method: "POST", body: { rowVersion } });
}

export async function publishPolicy(id: string, rowVersion: number): Promise<ContentPolicy> {
  return apiRequest(`/api/v1/admin/content/policies/${id}/publish`, { method: "POST", body: { rowVersion } });
}

export type ContentMediaAsset = {
  id: string;
  titleAr: string;
  titleEn: string;
  altAr: string;
  altEn: string;
  captionAr: string | null;
  captionEn: string | null;
  tags: string[];
  storageKey: string;
  thumbnailStorageKey: string;
  fileSizeBytes: number;
  width: number;
  height: number;
  thumbnailWidth: number;
  thumbnailHeight: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  rowVersion: number;
};

export type ContentMediaUsage = {
  referenceType: string;
  referenceId: string;
  referenceTitle: string;
  isPublished: boolean;
  updatedAtUtc: string;
};

export async function getContentMedia(search?: string): Promise<ContentPage<ContentMediaAsset>> {
  return apiRequest(`/api/v1/admin/media/assets${search ? `?search=${encodeURIComponent(search)}` : ""}`);
}

async function sendForm<T>(path: string, form: FormData): Promise<T> {
  const headers = new Headers({ Accept: "application/json", "X-CSRF-TOKEN": await getCsrfToken() });
  const response = await fetch(path, {
    method: "POST",
    credentials: "include",
    cache: "no-store",
    headers,
    body: form,
  });
  const payload = await readJson(response);
  if (!response.ok) throw new ApiError(response.status, payload as ProblemPayload | null);
  return payload as T;
}

export async function uploadContentMedia(form: FormData): Promise<ContentMediaAsset> {
  return sendForm("/api/v1/admin/media/assets", form);
}

export async function replaceContentMedia(id: string, form: FormData, rowVersion: number): Promise<ContentMediaAsset> {
  return sendForm(`/api/v1/admin/media/assets/${id}/replace?rowVersion=${rowVersion}`, form);
}

export async function updateContentMediaMetadata(id: string, input: {
  titleAr: string;
  titleEn: string;
  altAr: string;
  altEn: string;
  captionAr: string | null;
  captionEn: string | null;
  tags: string;
  rowVersion: number;
}): Promise<ContentMediaAsset> {
  return apiRequest(`/api/v1/admin/media/assets/${id}`, { method: "PUT", body: input });
}

export async function deleteContentMedia(id: string, rowVersion: number): Promise<void> {
  await apiRequest(`/api/v1/admin/media/assets/${id}?rowVersion=${rowVersion}`, { method: "DELETE" });
}

export async function getContentMediaUsages(id: string): Promise<ContentMediaUsage[]> {
  return apiRequest(`/api/v1/admin/media/assets/${id}/usages`);
}

export type SecurityAuditItem = {
  id: string;
  occurredAtUtc: string;
  eventType: string;
  outcome: string;
  actorKind: string;
  actorUserAccountId: string | null;
  subjectUserAccountId: string | null;
  correlationId: string | null;
  metadata: Record<string, string>;
};

export type SecurityAuditPage = {
  items: SecurityAuditItem[];
  page: number;
  pageSize: number;
  total: number;
};

export async function getSecurityAuditEvents(params: {
  fromUtc?: string;
  toUtc?: string;
  eventType?: string;
  outcome?: string;
  actorUserAccountId?: string;
  subjectUserAccountId?: string;
  correlationId?: string;
  search?: string;
  page?: number;
  pageSize?: number;
} = {}): Promise<SecurityAuditPage> {
  const query = new URLSearchParams();
  if (params.fromUtc) query.set("fromUtc", params.fromUtc);
  if (params.toUtc) query.set("toUtc", params.toUtc);
  if (params.eventType) query.set("eventType", params.eventType);
  if (params.outcome) query.set("outcome", params.outcome);
  if (params.actorUserAccountId) query.set("actorUserAccountId", params.actorUserAccountId);
  if (params.subjectUserAccountId) query.set("subjectUserAccountId", params.subjectUserAccountId);
  if (params.correlationId) query.set("correlationId", params.correlationId);
  if (params.search) query.set("search", params.search);
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const suffix = query.toString();
  return apiRequest(`/api/v1/admin/audit/events${suffix ? `?${suffix}` : ""}`);
}

export type RatingDistribution = { stars: number; count: number };
export type ReviewAggregate = {
  averageRating: number;
  publishedCount: number;
  distribution: RatingDistribution[];
};

export type PublicProductReview = {
  id: string;
  productId: string;
  rating: number;
  title: string | null;
  body: string;
  authorName: string;
  authorAvatarStorageKey: string | null;
  createdAtUtc: string;
};

export type PublicStoreReview = {
  id: string;
  rating: number;
  body: string;
  authorName: string;
  authorAvatarStorageKey: string | null;
  createdAtUtc: string;
};

export type ReviewInput = {
  rating: number;
  title: string | null;
  body: string;
  showAvatar: boolean;
};

export type MyProductReview = {
  id: string;
  productId: string;
  rating: number;
  title: string | null;
  body: string;
  showAvatar: boolean;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  rowVersion: number;
};

export type MyStoreReview = {
  id: string;
  userAccountId: string;
  rating: number;
  body: string;
  showAvatar: boolean;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  rowVersion: number;
};

export type MyReviews = {
  productReviews: MyProductReview[];
  storeReview: MyStoreReview[];
};

export type AdminReview = {
  id: string;
  type: "Product" | "Store";
  userAccountId: string;
  productId: string | null;
  rating: number;
  title: string | null;
  body: string;
  status: string;
  moderationReason: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  rowVersion: number;
};

export async function getProductReviews(productId: string): Promise<{ aggregate: ReviewAggregate; items: PublicProductReview[] }> {
  return apiRequest(`/api/v1/catalog/products/${productId}/reviews`);
}

export async function getStoreReviews(): Promise<{ aggregate: ReviewAggregate; items: PublicStoreReview[] }> {
  return apiRequest("/api/v1/content/store-reviews");
}

export async function getMyReviews(): Promise<MyReviews> {
  return apiRequest("/api/v1/reviews/mine");
}

export async function upsertProductReview(productId: string, input: ReviewInput): Promise<MyProductReview> {
  return apiRequest(`/api/v1/reviews/products/${productId}`, { method: "PUT", body: input });
}

export async function deleteProductReview(productId: string): Promise<void> {
  await apiRequest(`/api/v1/reviews/products/${productId}`, { method: "DELETE" });
}

export async function upsertStoreReview(input: ReviewInput): Promise<MyStoreReview> {
  return apiRequest("/api/v1/reviews/store", { method: "PUT", body: input });
}

export async function deleteStoreReview(): Promise<void> {
  await apiRequest("/api/v1/reviews/store", { method: "DELETE" });
}

export async function getAdminReviews(params: { type?: string; status?: string; page?: number; pageSize?: number } = {}): Promise<ContentPage<AdminReview>> {
  const query = new URLSearchParams();
  if (params.type) query.set("type", params.type);
  if (params.status) query.set("status", params.status);
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const suffix = query.toString();
  return apiRequest(`/api/v1/admin/reviews${suffix ? `?${suffix}` : ""}`);
}

export async function moderateReview(type: string, id: string, action: string, rowVersion: number, reason?: string): Promise<AdminReview> {
  return apiRequest(`/api/v1/admin/reviews/${type}/${id}/moderate`, {
    method: "POST",
    body: { action, reason: reason ?? null, rowVersion },
  });
}

export type OfferItem = {
  productId: string;
  productName: string;
  productSlug: string;
  productSku: string;
  basePrice: number;
  offerPrice: number;
  thumbnailStorageKey: string | null;
  categorySlug: string | null;
  sortOrder: number;
};

export type Offer = {
  id: string;
  titleAr: string;
  titleEn: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  startsAtUtc: string;
  endsAtUtc: string | null;
  status: string;
  priority: number;
  createdAtUtc: string;
  updatedAtUtc: string;
  rowVersion: number;
  items: OfferItem[];
};

export type PublicOffer = Omit<Offer, "status" | "priority" | "createdAtUtc" | "updatedAtUtc" | "rowVersion">;

export type OfferInput = {
  titleAr: string;
  titleEn: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  startsAtUtc: string;
  endsAtUtc: string | null;
  priority: number;
  items: Array<{ productId: string; offerPrice: number }>;
  rowVersion?: number;
};

export async function getPublicOffers(): Promise<PublicOffer[]> {
  return apiRequest("/api/v1/content/offers");
}

export async function getAdminOffers(): Promise<Offer[]> {
  return apiRequest("/api/v1/admin/content/offers");
}

export async function createOffer(input: OfferInput): Promise<Offer> {
  return apiRequest("/api/v1/admin/content/offers", { method: "POST", body: input });
}

export async function updateOffer(id: string, input: OfferInput): Promise<Offer> {
  return apiRequest(`/api/v1/admin/content/offers/${id}`, { method: "PUT", body: input });
}

export async function setOfferPublished(id: string, published: boolean, rowVersion: number): Promise<Offer> {
  return apiRequest(`/api/v1/admin/content/offers/${id}/${published ? "publish" : "unpublish"}`, {
    method: "POST",
    body: { rowVersion },
  });
}

export async function cancelOffer(id: string, rowVersion: number): Promise<Offer> {
  return apiRequest(`/api/v1/admin/content/offers/${id}/cancel`, { method: "POST", body: { rowVersion } });
}

export type PurchaseSourceType = "RegisteredSupplier" | "Marketplace" | "IndividualSeller" | "Other";

export type PurchaseDraftItem = {
  id: string;
  productId: string;
  variantId: string | null;
  itemName: string;
  sku: string;
  itemKind: string;
  brand: string | null;
  model: string | null;
  category: string | null;
  condition: string | null;
  description: string | null;
  externalCode: string | null;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
};

export type PurchaseDraft = {
  id: string;
  sourceType: PurchaseSourceType;
  purchaseKind: string;
  supplierId: string | null;
  supplierCompanyName: string | null;
  marketplaceName: string | null;
  externalSellerName: string | null;
  externalSellerContact: string | null;
  listingUrl: string | null;
  purchaseDate: string | null;
  notes: string | null;
  status: string;
  totalAmount: number;
  createdByUserAccountId: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  rowVersion: number;
  items: PurchaseDraftItem[];
};

export type PurchaseDraftPage = { items: PurchaseDraft[]; page: number; pageSize: number; total: number };

export type PurchaseDraftInput = {
  sourceType: PurchaseSourceType;
  supplierId: string | null;
  marketplaceName: string | null;
  externalSellerName: string | null;
  externalSellerContact: string | null;
  listingUrl: string | null;
  purchaseDate: string | null;
  notes: string | null;
  items: Array<{ productId: string; variantId: string | null; quantity: number; unitPrice: number; itemName?: string | null; externalCode?: string | null; itemKind?: string | null }>;
  rowVersion?: number;
};

export async function getPurchaseDrafts(params: { sourceType?: string; status?: string; search?: string; page?: number; pageSize?: number } = {}): Promise<PurchaseDraftPage> {
  const query = new URLSearchParams();
  if (params.sourceType) query.set("sourceType", params.sourceType);
  if (params.status) query.set("status", params.status);
  if (params.search) query.set("search", params.search);
  if (params.page) query.set("page", String(params.page));
  if (params.pageSize) query.set("pageSize", String(params.pageSize));
  const suffix = query.toString();
  return apiRequest(`/api/v1/admin/purchasing/drafts${suffix ? `?${suffix}` : ""}`);
}

export async function createPurchaseDraft(input: PurchaseDraftInput): Promise<PurchaseDraft> {
  return apiRequest("/api/v1/admin/purchasing/drafts", { method: "POST", body: input });
}

export async function updatePurchaseDraft(id: string, input: PurchaseDraftInput): Promise<PurchaseDraft> {
  return apiRequest(`/api/v1/admin/purchasing/drafts/${id}`, { method: "PUT", body: input });
}

export async function cancelPurchaseDraft(id: string, rowVersion: number): Promise<PurchaseDraft> {
  return apiRequest(`/api/v1/admin/purchasing/drafts/${id}/cancel`, { method: "POST", body: { rowVersion } });
}

export type AdminDashboardSnapshot = {
  generatedAtUtc: string;
  catalog: Array<{ key: string; value: number }>;
  purchasing: Array<{ key: string; value: number }>;
  customers: Array<{ key: string; value: number }>;
  content: Array<{ key: string; value: number }>;
  reviews: Array<{ key: string; value: number }>;
  media: Array<{ key: string; value: number }>;
  productConditions: Array<{ label: string; value: number }>;
  draftPurchaseSources: Array<{ label: string; value: number }>;
  ratingDistribution: Array<{ label: string; value: number }>;
  accountsLast14Days: Array<{ date: string; value: number }>;
  reviewsLast14Days: Array<{ date: string; value: number }>;
  recentAuditEvents: Array<{ id: string; occurredAtUtc: string; eventType: string; outcome: string; actorKind: string }>;
};

export async function getAdminDashboard(): Promise<AdminDashboardSnapshot> {
  return apiRequest("/api/v1/admin/analytics/dashboard");
}

export async function uploadProfileAvatar(file: File): Promise<{ avatarStorageKey: string }> {
  const form = new FormData();
  form.set("image", file);
  return sendForm("/api/v1/identity/profile/avatar", form);
}

export async function deleteProfileAvatar(): Promise<void> {
  await apiRequest("/api/v1/identity/profile/avatar", { method: "DELETE" });
}

export type NotificationItem = {
  id: string;
  kind: string;
  titleKey: string;
  bodyKey: string | null;
  linkUrl: string | null;
  isRead: boolean;
  createdAtUtc: string;
};

export async function getNotifications(): Promise<{ items: NotificationItem[]; unreadCount: number }> {
  return apiRequest("/api/v1/notifications");
}

export async function markNotificationRead(id: string): Promise<void> {
  await apiRequest(`/api/v1/notifications/${id}/read`, { method: "POST" });
}

export async function markAllNotificationsRead(): Promise<void> {
  await apiRequest("/api/v1/notifications/read-all", { method: "POST" });
}

export async function requestEmailChange(input: { currentPassword: string; newEmail: string }): Promise<{ status: string; expiresAtUtc: string }> {
  return apiRequest("/api/v1/identity/email-change/request", { method: "POST", body: input });
}

export async function confirmEmailChange(token: string): Promise<{ status: string }> {
  return apiRequest("/api/v1/identity/email-change/confirm", { method: "POST", body: { token } });
}

export type PcSlotOption = {
  id: string;
  productId: string;
  productName: string;
  productSlug: string;
  sku: string;
  price: number;
  categorySlug: string | null;
  primaryImageKey: string | null;
  specifications: Array<{ key: string; value: string }>;
  isDefault: boolean;
};

export type PcSlot = {
  id: string;
  slotKey: string;
  labelAr: string;
  labelEn: string;
  isRequired: boolean;
  sortOrder: number;
  options: PcSlotOption[];
};

export type PcBuild = {
  id: string;
  titleAr: string;
  titleEn: string;
  summaryAr: string | null;
  summaryEn: string | null;
  status: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  rowVersion: number;
  slots: PcSlot[];
};

export async function getPcBuilds(): Promise<PcBuild[]> {
  return apiRequest("/api/v1/pc-builder/templates");
}

export async function getPcBuild(id: string): Promise<PcBuild> {
  return apiRequest(`/api/v1/pc-builder/templates/${id}`);
}

export async function evaluatePcBuild(id: string, selections: Record<string, string>): Promise<{
  status: string;
  totalPrice: number;
  messages: Array<{ level: string; code: string; slotKey: string; otherSlotKey: string | null }>;
}> {
  return apiRequest(`/api/v1/pc-builder/templates/${id}/evaluate`, { method: "POST", body: { selections } });
}

export async function getAdminPcBuilds(): Promise<PcBuild[]> {
  return apiRequest("/api/v1/admin/pc-builder/templates");
}

export async function createPcBuild(input: {
  titleAr: string;
  titleEn: string;
  summaryAr: string | null;
  summaryEn: string | null;
  slots: Array<{ slotKey: string; labelAr: string; labelEn: string; isRequired: boolean; options: Array<{ productId: string; isDefault: boolean }> }>;
}): Promise<PcBuild> {
  return apiRequest("/api/v1/admin/pc-builder/templates", { method: "POST", body: input });
}

export async function setPcBuildPublished(id: string, published: boolean, rowVersion: number): Promise<PcBuild> {
  return apiRequest(`/api/v1/admin/pc-builder/templates/${id}/${published ? "publish" : "unpublish"}`, {
    method: "POST",
    body: { rowVersion },
  });
}

export async function updatePcBuild(id: string, input: {
  titleAr: string;
  titleEn: string;
  summaryAr: string | null;
  summaryEn: string | null;
  slots: Array<{ slotKey: string; labelAr: string; labelEn: string; isRequired: boolean; options: Array<{ productId: string; isDefault: boolean }> }>;
  rowVersion: number;
}): Promise<PcBuild> {
  return apiRequest(`/api/v1/admin/pc-builder/templates/${id}`, { method: "PUT", body: input });
}

export type OrderComponent = {
  id: string;
  unitId: string;
  allocationPercent: number;
  revenueAmount: number;
  discountAmount: number;
  directExpenseAmount: number;
  costAmount: number;
  netProfit: number;
};
export type StoreOrderItem = {
  id: string;
  name: string;
  sku: string | null;
  variantId?: string | null;
  variantName?: string | null;
  thumbnailStorageKeySnapshot?: string | null;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
  discountAmount: number;
  directExpenseAmount: number;
  directExpenseNote: string | null;
  netProfit: number;
  components: OrderComponent[];
};
export type StoreOrder = {
  id: string;
  reference: string;
  status: string;
  paymentStatus: string;
  paymentRequired: boolean;
  paymentMethod?: string;
  addressText?: string | null;
  paymentWaivedAtUtc: string | null;
  paymentWaivedByUserAccountId: string | null;
  paymentWaiverReason: string | null;
  total: number;
  subtotal: number;
  deliveryFee: number;
  refundedAmount: number;
  customerName: string;
  phoneNumber: string;
  fulfillmentMethod: string;
  createdAtUtc: string;
  rowVersion: number;
  items: StoreOrderItem[];
  events: Array<{ eventType: string; notes: string | null; createdAtUtc: string }>;
};

export async function createStoreOrder(input: {
  customerName: string;
  phoneNumber: string;
  addressText: string | null;
  fulfillmentMethod: string;
  paymentMethod: string;
  notes: string | null;
  termsAccepted: boolean;
  items: Array<{ productId?: string | null; assemblyId?: string | null; variantId?: string | null; name: string; sku?: string | null; quantity: number; unitPrice: number }>;
}): Promise<StoreOrder> {
  return apiRequest("/api/v1/orders", { method: "POST", body: { ...input, leadSource: "Website" } });
}

export async function getMyOrders(): Promise<StoreOrder[]> {
  return apiRequest("/api/v1/orders/mine");
}
export async function getMyOrder(id: string): Promise<StoreOrder> {
  return apiRequest(`/api/v1/orders/mine/${id}`);
}

export type PublishedBuild = {
  id: string;
  titleAr: string | null;
  titleEn: string | null;
  salePrice: number;
  units: Array<{ productId: string | null; name: string; sku: string | null; condition: string }>;
};
export async function getPublishedBuilds(): Promise<PublishedBuild[]> {
  return apiRequest("/api/v1/store/assemblies");
}

export type RetailUnit = {
  id: string;
  productId: string | null;
  name: string;
  sku: string | null;
  brand: string | null;
  model: string | null;
  category: string | null;
  condition: string;
  inspectionCondition: string | null;
  serialNumber: string | null;
  sourceName: string | null;
  sourceType: string | null;
  purchaseKind: string | null;
  purchaseDate: string | null;
  description: string | null;
  externalCode: string | null;
  specificationsJson: string | null;
  acquisitionCost: number | null;
  standalonePrice: number | null;
  partnerPercentOverride: number | null;
  status: string;
  assemblyId: string | null;
  receivedAtUtc: string;
};
export type RetailAssemblyUnit = {
  unitId: string;
  name: string;
  sku: string | null;
  condition: string;
  acquisitionCost: number | null;
  standalonePrice: number | null;
  allocationPercent: number;
  allocatedAmount: number | null;
};
export type RetailAssembly = {
  id: string;
  name: string;
  publicTitleAr: string | null;
  publicTitleEn: string | null;
  salePrice: number;
  status: string;
  units: RetailAssemblyUnit[];
  componentCost: number | null;
  allocatedTotal: number | null;
  rowVersion: number;
};

export async function getRetailUnits(status?: string): Promise<RetailUnit[]> {
  return apiRequest(`/api/v1/admin/retail/units${status ? `?status=${encodeURIComponent(status)}` : ""}`);
}
export async function receivePurchaseDraft(draftId: string, notes: string | null): Promise<RetailUnit[]> {
  return apiRequest(`/api/v1/admin/retail/receipts/from-draft/${draftId}`, { method: "POST", body: { notes } });
}
export async function getRetailAssemblies(): Promise<RetailAssembly[]> {
  return apiRequest("/api/v1/admin/retail/assemblies");
}
export async function createRetailAssembly(name: string, unitIds: string[]): Promise<RetailAssembly> {
  return apiRequest("/api/v1/admin/retail/assemblies", { method: "POST", body: { name, unitIds } });
}
export async function updateRetailAssembly(id: string, input: { name: string; publicTitleAr: string | null; publicTitleEn: string | null; salePrice: number; rowVersion: number }): Promise<RetailAssembly> {
  return apiRequest(`/api/v1/admin/retail/assemblies/${id}`, { method: "PUT", body: input });
}
export async function setRetailAssemblyAllocations(id: string, allocations: Array<{ unitId: string; percent: number }>, rowVersion: number): Promise<RetailAssembly> {
  return apiRequest(`/api/v1/admin/retail/assemblies/${id}/allocations`, { method: "PUT", body: { allocations, rowVersion } });
}
export async function setRetailAssemblyPublished(id: string, published: boolean, rowVersion: number): Promise<RetailAssembly> {
  return apiRequest(`/api/v1/admin/retail/assemblies/${id}/${published ? "publish" : "unpublish"}`, { method: "POST", body: { rowVersion } });
}
export async function disassembleRetailAssembly(id: string, rowVersion: number): Promise<RetailAssembly> {
  return apiRequest(`/api/v1/admin/retail/assemblies/${id}/disassemble`, { method: "POST", body: { rowVersion } });
}
export async function getAdminOrders(status?: string): Promise<StoreOrder[]> {
  return apiRequest(`/api/v1/admin/retail/orders${status ? `?status=${encodeURIComponent(status)}` : ""}`);
}
export type AdminOrderUnit = { unitId: string; productId: string | null; name: string; sku: string | null; condition: string; inspectionCondition: string | null; serialNumber: string | null; status: string };
export async function getAdminOrder(id: string): Promise<{ order: StoreOrder; units: AdminOrderUnit[] }> {
  return apiRequest(`/api/v1/admin/retail/orders/${id}`);
}
export type BusinessReport = {
  from: string; to: string; grossSales: number; discounts: number; refunds: number; costOfGoods: number;
  itemDirectExpenses: number; additionalDirectExpenses: number; netItemProfit: number; generalOverhead: number;
  storeNetResult: number; partnerAccrued: number; partnerPaid: number; partnerRecoverable: number;
  completedOrders: number; returns: number;
};
export async function getBusinessReport(from: string, to: string): Promise<BusinessReport> {
  return apiRequest(`/api/v1/admin/retail/reports/business?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`);
}
export type ExpenseRecord = { id: string; expenseDate: string; amount: number; category: string; classification: string; note: string | null; orderId: string | null; unitId: string | null; attachmentReference: string | null; createdAtUtc: string; rowVersion: number };
export async function getExpenses(from?: string, to?: string): Promise<ExpenseRecord[]> {
  const query = from && to ? `?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}` : "";
  return apiRequest(`/api/v1/admin/retail/expenses${query}`);
}
export async function createExpense(input: { expenseDate: string; amount: number; category: string; classification: string; note: string | null; orderId: string | null; unitId: string | null; attachmentReference: string | null }): Promise<ExpenseRecord> {
  return apiRequest("/api/v1/admin/retail/expenses", { method: "POST", body: input });
}
export async function createManualOrder(input: {
  customerName: string;
  phoneNumber: string;
  addressText: string | null;
  fulfillmentMethod: string;
  paymentMethod: string;
  notes: string | null;
  leadSource: string;
  paymentRequired: boolean;
  items: Array<{ productId?: string | null; assemblyId?: string | null; name: string; quantity: number; unitPrice: number }>;
}): Promise<StoreOrder> {
  return apiRequest("/api/v1/admin/retail/orders", { method: "POST", body: input });
}
export async function updateOrderStatus(id: string, status: string, rowVersion: number, notes: string | null = null): Promise<StoreOrder> {
  return apiRequest(`/api/v1/admin/retail/orders/${id}/status`, { method: "POST", body: { status, notes, rowVersion } });
}
export async function completeOrder(id: string, rowVersion: number, notes: string | null = null): Promise<StoreOrder> {
  return apiRequest(`/api/v1/admin/retail/orders/${id}/complete`, { method: "POST", body: { notes, rowVersion } });
}
export async function updateOrderPayment(id: string, paymentStatus: string, rowVersion: number, notes: string | null = null): Promise<StoreOrder> {
  return apiRequest(`/api/v1/admin/retail/orders/${id}/payment`, { method: "POST", body: { paymentStatus, notes, rowVersion } });
}
export async function waiveOrderPayment(id: string, reason: string, rowVersion: number): Promise<StoreOrder> {
  return apiRequest(`/api/v1/admin/retail/orders/${id}/payment/waive`, { method: "POST", body: { reason, rowVersion } });
}
export type ReturnRecord = {
  id: string;
  orderId: string;
  orderItemId: string;
  unitId: string;
  refundAmount: number;
  reason: string;
  status: string;
  inspectionCondition: string | null;
  inspectionNotes: string | null;
  inspectedAtUtc: string | null;
  createdAtUtc: string;
  rowVersion: number;
};
export async function getOrderReturns(status?: string): Promise<ReturnRecord[]> {
  return apiRequest(`/api/v1/admin/retail/returns${status ? `?status=${encodeURIComponent(status)}` : ""}`);
}
export async function createOrderReturn(input: { orderId: string; orderItemId: string; unitId: string; refundAmount: number; reason: string }): Promise<ReturnRecord> {
  return apiRequest("/api/v1/admin/retail/returns", { method: "POST", body: input });
}
export async function inspectOrderReturn(id: string, condition: string, notes: string | null, rowVersion: number): Promise<ReturnRecord> {
  return apiRequest(`/api/v1/admin/retail/returns/${id}/inspect`, { method: "POST", body: { condition, notes, rowVersion } });
}
export async function rejectOrderReturn(id: string, notes: string | null, rowVersion: number): Promise<ReturnRecord> {
  return apiRequest(`/api/v1/admin/retail/returns/${id}/reject`, { method: "POST", body: { notes, rowVersion } });
}
export type PartnerEarning = {
  id: string;
  orderId: string;
  orderItemId: string;
  unitId: string | null;
  partnerName: string;
  percent: number;
  revenueAmount: number;
  discountAmount: number;
  directExpenseAmount: number;
  costAmount: number;
  grossProfit: number;
  amount: number;
  paidAmount: number;
  offsetAmount: number;
  status: string;
  reversedAtUtc: string | null;
  createdAtUtc: string;
};
export type ContactMessageRecord = {
  id: string; fullName: string; email: string; phone: string | null; subject: string;
  message: string; status: string; createdAtUtc: string; updatedAtUtc: string; rowVersion: number;
};
export async function submitContactMessage(input: { fullName: string; email: string; phone: string | null; subject: string; message: string; website: string }): Promise<void> {
  await apiRequest("/api/v1/contact", { method: "POST", body: input });
}

export async function getContactMessages(status?: string): Promise<{ items: ContactMessageRecord[]; page: number; pageSize: number; total: number }> {  return apiRequest(`/api/v1/admin/contact-messages${status ? `?status=${encodeURIComponent(status)}` : ""}`);
}
export async function updateContactStatus(id: string, status: string, rowVersion: number): Promise<ContactMessageRecord> {
  return apiRequest(`/api/v1/admin/contact-messages/${id}/status`, { method: "POST", body: { status, rowVersion } });
}

export async function getPartnerEarnings(partner?: string): Promise<PartnerEarning[]> {
  return apiRequest(`/api/v1/admin/retail/partner/earnings${partner ? `?partner=${encodeURIComponent(partner)}` : ""}`);
}
export type PartnerAdjustment = { id: string; partnerName: string; amount: number; appliedAmount: number; remainingAmount: number; status: string; reason: string; orderReturnId: string | null; createdAtUtc: string };
export type PartnerLedger = { partnerName: string; outstanding: number; cashDue: number; recoverable: number };
export async function getPartnerAdjustments(partner?: string): Promise<PartnerAdjustment[]> {
  return apiRequest(`/api/v1/admin/retail/partner/adjustments${partner ? `?partner=${encodeURIComponent(partner)}` : ""}`);
}
export async function getPartnerLedger(): Promise<PartnerLedger[]> {
  return apiRequest("/api/v1/admin/retail/partner/ledger");
}
export async function getPartnerAgreements(): Promise<Array<{ id: string; partnerName: string; defaultPercent: number; isActive: boolean; lossSharingEnabled: boolean; effectiveFrom: string; effectiveTo: string | null }>> {
  return apiRequest("/api/v1/admin/retail/partner/agreements");
}
export async function upsertPartnerAgreement(input: { partnerName: string; defaultPercent: number; isActive: boolean; lossSharingEnabled: boolean; effectiveFrom: string; effectiveTo: string | null }): Promise<unknown> {
  return apiRequest("/api/v1/admin/retail/partner/agreements", { method: "POST", body: input });
}
export async function settlePartner(input: { partnerName: string; amount: number; method: string; reference: string | null; note: string | null }): Promise<unknown> {
  return apiRequest("/api/v1/admin/retail/partner/settlements", { method: "POST", body: input });
}

export type ContentVideo = {
  id: string;
  titleAr: string;
  titleEn: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  mimeType: string;
  fileSizeBytes: number;
  posterStorageKey: string | null;
  status: string;
  provider: string;
  youtubeVideoId: string | null;
  embedUrl: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  rowVersion: number;
};

export async function getPublicVideos(): Promise<ContentVideo[]> {
  return apiRequest("/api/v1/content/videos");
}

export async function getPublicVideo(id: string): Promise<ContentVideo> {
  return apiRequest(`/api/v1/content/videos/${id}`);
}

export async function getAdminVideos(): Promise<ContentVideo[]> {
  return apiRequest("/api/v1/admin/media/videos");
}

export async function uploadContentVideo(form: FormData): Promise<ContentVideo> {
  return sendForm("/api/v1/admin/media/videos", form);
}

export async function createYouTubeVideo(input: {
  titleAr: string;
  titleEn: string;
  descriptionAr: string | null;
  descriptionEn: string | null;
  url: string;
}): Promise<ContentVideo> {
  return apiRequest("/api/v1/admin/media/videos/youtube", { method: "POST", body: input });
}

export async function setContentVideoPublished(id: string, published: boolean, rowVersion: number): Promise<ContentVideo> {
  return apiRequest(`/api/v1/admin/media/videos/${id}/${published ? "publish" : "unpublish"}`, {
    method: "POST",
    body: { rowVersion },
  });
}





