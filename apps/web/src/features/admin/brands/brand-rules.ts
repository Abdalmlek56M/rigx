/** Form mapping and search for the brands screen. Pure, so it is tested. Messages live under `admin.brands`. */
import type { Brand, BrandInput } from "./brands-api";

const SLUG = /^[a-z0-9]+(-[a-z0-9]+)*$/;

export interface BrandForm {
  name: string;
  slug: string;
  isActive: boolean;
  /** The categories the brand is suggested for in the pickers (never a limit on what can be chosen). */
  categoryIds: string[];
}

export const emptyBrandForm: BrandForm = { name: "", slug: "", isActive: true, categoryIds: [] };

export const brandForm = (brand: Brand): BrandForm => ({ name: brand.name, slug: brand.slug, isActive: brand.isActive, categoryIds: [...brand.categoryIds] });

/** The ids with `id` added, or taken out when it was in. */
export const toggleId = (ids: readonly string[], id: string): string[] => (ids.includes(id) ? ids.filter((item) => item !== id) : [...ids, id]);

/** "be quiet!" → "be-quiet", "G.Skill" → "g-skill": the link part a new brand gets from its name until the owner edits it. */
export function slugFromName(name: string): string {
  return name.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-+|-+$/g, "");
}

export function brandFormValid(form: BrandForm): boolean {
  return form.name.trim() !== "" && SLUG.test(form.slug);
}

/** The request body; what this screen does not edit (description, logo, support link) is kept as it is. */
export function toBrandInput(form: BrandForm, brand: Brand | null): BrandInput {
  return {
    name: form.name.trim(),
    slug: form.slug,
    description: brand?.description ?? null,
    logoStorageKey: brand?.logoStorageKey ?? null,
    supportUrl: brand?.supportUrl ?? null,
    isActive: form.isActive,
    rowVersion: brand?.rowVersion ?? null,
    categoryIds: form.categoryIds
  };
}

/**
 * The brands for a picker once a category is chosen: the ones suggested for it (or for a category above it) first, each marked, then the
 * rest, both parts keeping the owner's order. With no category chosen nothing is marked. A suggestion never hides a brand.
 */
export function suggestBrandsFirst<T extends { categoryIds: readonly string[] }>(brands: readonly T[], categoryChain: readonly string[]): { brand: T; suggested: boolean }[] {
  const marked = brands.map((brand) => ({ brand, suggested: brand.categoryIds.some((id) => categoryChain.includes(id)) }));
  return [...marked.filter((entry) => entry.suggested), ...marked.filter((entry) => !entry.suggested)];
}

/** The brands whose name has the typed text, in any case. */
export function filterBrands(brands: readonly Brand[], query: string): Brand[] {
  const text = query.trim().toLowerCase();
  return text ? brands.filter((brand) => brand.name.toLowerCase().includes(text)) : [...brands];
}
