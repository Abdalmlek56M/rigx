/** Form mapping and search for the brands screen. Pure, so it is tested. Messages live under `admin.brands`. */
import type { Brand, BrandInput } from "./brands-api";

const SLUG = /^[a-z0-9]+(-[a-z0-9]+)*$/;

export interface BrandForm {
  name: string;
  slug: string;
  isActive: boolean;
}

export const emptyBrandForm: BrandForm = { name: "", slug: "", isActive: true };

export const brandForm = (brand: Brand): BrandForm => ({ name: brand.name, slug: brand.slug, isActive: brand.isActive });

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
    rowVersion: brand?.rowVersion ?? null
  };
}

/** The brands whose name has the typed text, in any case. */
export function filterBrands(brands: readonly Brand[], query: string): Brand[] {
  const text = query.trim().toLowerCase();
  return text ? brands.filter((brand) => brand.name.toLowerCase().includes(text)) : [...brands];
}
