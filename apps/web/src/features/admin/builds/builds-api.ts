import { multipart, type Schemas, unwrap } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";

export type Build = Schemas["AssemblyView"];
export type BuildPart = Schemas["AssemblyUnitView"];
export type BuildEditRequest = Schemas["AssemblyEditRequest"];
export type BuildPartBody = Schemas["AssemblyPartBody"];
export type BuildNewPartBody = Schemas["AssemblyNewPartBody"];
export type BuildAllocationInput = Schemas["AssemblyAllocationInput"];
export type PlannedPart = Schemas["PlannedPart"];
export type PlannedPartInput = Schemas["PlannedPartInput"];

const api = () => browserApi();
const path = (id: string) => ({ params: { path: { id } } });
const partPath = (id: string, unitId: string) => ({ params: { path: { id, unitId } } });

export const fetchBuilds = () => required(unwrap(api().GET("/api/v1/admin/retail/assemblies")));

export const fetchBuild = (id: string) => required(unwrap(api().GET("/api/v1/admin/retail/assemblies/{id}", path(id))));

/** A new draft build, with its first part from stock or empty (its parts can then be new pieces that are not in stock). */
export const createBuild = (name: string, firstUnitId: string | null) =>
  required(unwrap(api().POST("/api/v1/admin/retail/assemblies", { body: { name, unitIds: firstUnitId ? [firstUnitId] : [] } })));

export const editBuild = (id: string, body: BuildEditRequest) =>
  required(unwrap(api().PUT("/api/v1/admin/retail/assemblies/{id}", { ...path(id), body })));

export const saveAllocations = (id: string, allocations: BuildAllocationInput[], rowVersion: number) =>
  required(unwrap(api().PUT("/api/v1/admin/retail/assemblies/{id}/allocations", { ...path(id), body: { allocations, rowVersion } })));

export type BuildTransition = "publish" | "unpublish" | "disassemble";

export const transitionBuild = (id: string, action: BuildTransition, rowVersion: number) =>
  required(unwrap(api().POST(`/api/v1/admin/retail/assemblies/{id}/${action}`, { ...path(id), body: { rowVersion } })));

export const addStockPart = (id: string, unitId: string, rowVersion: number) =>
  required(unwrap(api().POST("/api/v1/admin/retail/assemblies/{id}/parts", { ...path(id), body: { unitId, rowVersion } })));

export const addNewPart = (id: string, body: BuildNewPartBody) =>
  required(unwrap(api().POST("/api/v1/admin/retail/assemblies/{id}/parts/new", { ...path(id), body })));

export const editPart = (id: string, unitId: string, body: BuildPartBody) =>
  required(unwrap(api().PUT("/api/v1/admin/retail/assemblies/{id}/parts/{unitId}", { ...partPath(id, unitId), body })));

/** Back to stock, or (with both target fields) into another build. Returns every build the call changed. */
export const removePart = (id: string, unitId: string, rowVersion: number, target?: { assemblyId: string; rowVersion: number }) =>
  required(unwrap(api().POST("/api/v1/admin/retail/assemblies/{id}/parts/{unitId}/remove", {
    ...partPath(id, unitId),
    body: { rowVersion, targetAssemblyId: target?.assemblyId ?? null, targetRowVersion: target?.rowVersion ?? null }
  })));

/** The replacement comes from free stock, or from another build (`otherRowVersion`). Returns every build the call changed. */
export const replacePart = (id: string, unitId: string, replacementUnitId: string, rowVersion: number, otherRowVersion?: number) =>
  required(unwrap(api().POST("/api/v1/admin/retail/assemblies/{id}/parts/{unitId}/replace", {
    ...partPath(id, unitId),
    body: { replacementUnitId, rowVersion, otherRowVersion: otherRowVersion ?? null }
  })));

export const uploadBuildPhoto = (id: string, file: File) => {
  const form = new FormData();
  form.set("image", file);
  return required(unwrap(api().POST("/api/v1/admin/retail/assemblies/{id}/photos", { ...path(id), ...multipart(form) })));
};

export const deleteBuildPhoto = (id: string, storageKey: string) =>
  unwrap(api().DELETE("/api/v1/admin/retail/assemblies/{id}/photos", { params: { path: { id }, query: { storageKey } } }));

/** Saves the build's whole list of typed parts (parts that are not in stock; nothing is reserved for them). */
export const savePlannedParts = (id: string, parts: PlannedPartInput[], rowVersion: number) =>
  required(unwrap(api().PUT("/api/v1/admin/retail/assemblies/{id}/planned-parts", { ...path(id), body: { parts, rowVersion } })));
