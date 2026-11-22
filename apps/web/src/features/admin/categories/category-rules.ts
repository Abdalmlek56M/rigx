/** Tree walking and form mapping for the categories screen. Pure, so it is tested. Messages live under `admin.categories`. */
import type { Category, CategoryInput, SpecDefinition } from "./categories-api";

export interface FlatCategory {
  node: Category;
  depth: number;
}

/**
 * The admin endpoint returns every category flat (`parentId`, empty `children`), already in sort order.
 * Nest them by `parentId`, keeping that order; a category whose parent is missing becomes a root.
 */
export function buildTree(list: readonly Category[]): Category[] {
  const ids = new Set(list.map((category) => category.id));
  const childrenOf = new Map<string | null, Category[]>();
  for (const category of list) {
    const parent = category.parentId && ids.has(category.parentId) ? category.parentId : null;
    childrenOf.set(parent, [...(childrenOf.get(parent) ?? []), category]);
  }
  const nest = (parent: string | null): Category[] =>
    (childrenOf.get(parent) ?? []).map((category) => ({ ...category, children: nest(category.id) }));
  return nest(null);
}

/** The visible rows of the tree: a collapsed node hides its children. */
export function flattenTree(nodes: readonly Category[], collapsed: ReadonlySet<string>): FlatCategory[] {
  const rows: FlatCategory[] = [];
  const walk = (list: readonly Category[], depth: number) => {
    for (const node of list) {
      rows.push({ node, depth });
      if (node.children.length > 0 && !collapsed.has(node.id)) walk(node.children, depth + 1);
    }
  };
  walk(nodes, 0);
  return rows;
}

/** The chain from the root to the category (empty when the id is not in the tree). */
export function findPath(nodes: readonly Category[], id: string): Category[] {
  for (const node of nodes) {
    if (node.id === id) return [node];
    const inside = findPath(node.children, id);
    if (inside.length > 0) return [node, ...inside];
  }
  return [];
}

/** Which of the three sale prices the category offers (all of them while no category is chosen or it is unknown). */
export function offeredPrices(nodes: readonly Category[], categoryId: string | null): { alone: boolean; withBoard: boolean; inBuild: boolean } {
  const options = categoryId ? findPath(nodes, categoryId).at(-1)?.priceOptions : undefined;
  return { alone: options?.alone ?? true, withBoard: options?.withBoard ?? true, inBuild: options?.inBuild ?? true };
}

/** Every category that may become the parent of `excludeId`: the node itself and its whole subtree are left out (no cycles). */
export function parentOptions(nodes: readonly Category[], excludeId: string | null): { id: string; name: string; depth: number }[] {
  const options: { id: string; name: string; depth: number }[] = [];
  const walk = (list: readonly Category[], depth: number) => {
    for (const node of list) {
      if (node.id === excludeId) continue;
      options.push({ id: node.id, name: node.name, depth });
      walk(node.children, depth + 1);
    }
  };
  walk(nodes, 0);
  return options;
}

export interface EffectiveSpec {
  spec: SpecDefinition;
  /** The ancestor the spec is inherited from, or `null` when this category defines it. */
  inheritedFrom: Category | null;
}

/** The specs a category really has: root → leaf, a child's spec with the same key replacing the parent's in place. */
export function effectiveSpecs(nodes: readonly Category[], id: string): EffectiveSpec[] {
  const path = findPath(nodes, id);
  const merged = new Map<string, EffectiveSpec>();
  path.forEach((node, index) => {
    for (const spec of node.specs) merged.set(spec.key, { spec, inheritedFrom: index === path.length - 1 ? null : node });
  });
  return [...merged.values()];
}

export interface CategoryForm {
  name: string;
  nameEn: string;
  slug: string;
  parentId: string;
  code: string;
  description: string;
  sortOrder: string;
  isActive: boolean;
  showInMenu: boolean;
  showInBuilder: boolean;
  /** The sale prices the category offers (at least one stays on). */
  priceAlone: boolean;
  priceWithBoard: boolean;
  priceInBuild: boolean;
}

export function emptyCategoryForm(parentId: string | null): CategoryForm {
  return {
    name: "", nameEn: "", slug: "", parentId: parentId ?? "", code: "", description: "", sortOrder: "0",
    isActive: true, showInMenu: true, showInBuilder: false, priceAlone: true, priceWithBoard: true, priceInBuild: true
  };
}

export function categoryForm(category: Category): CategoryForm {
  return {
    name: category.name,
    nameEn: category.nameEn ?? "",
    slug: category.slug,
    parentId: category.parentId ?? "",
    code: category.code ?? "",
    description: category.description ?? "",
    sortOrder: String(category.sortOrder),
    isActive: category.isActive,
    showInMenu: category.showInMenu,
    showInBuilder: category.showInBuilder,
    priceAlone: category.priceOptions?.alone ?? true,
    priceWithBoard: category.priceOptions?.withBoard ?? true,
    priceInBuild: category.priceOptions?.inBuild ?? true
  };
}

export function categoryFormValid(form: CategoryForm): boolean {
  return form.name.trim() !== "" && form.slug.trim() !== "" && (form.priceAlone || form.priceWithBoard || form.priceInBuild);
}

/** The API body; the image key is carried over so saving the text never removes the picture. */
export function toCategoryInput(form: CategoryForm, category: Category | null): CategoryInput {
  const sortOrder = Number.parseInt(form.sortOrder, 10);
  return {
    name: form.name.trim(),
    nameEn: form.nameEn.trim() === "" ? null : form.nameEn.trim(),
    slug: form.slug.trim(),
    parentId: form.parentId === "" ? null : form.parentId,
    description: form.description.trim() === "" ? null : form.description.trim(),
    iconStorageKey: category?.iconStorageKey ?? null,
    sortOrder: Number.isFinite(sortOrder) ? sortOrder : 0,
    isActive: form.isActive,
    rowVersion: category?.rowVersion ?? null,
    code: form.code.trim() === "" ? null : form.code.trim(),
    showInMenu: form.showInMenu,
    showInBuilder: form.showInBuilder,
    priceOptions: { alone: form.priceAlone, withBoard: form.priceWithBoard, inBuild: form.priceInBuild }
  };
}

export interface SpecRow {
  /** A stable local identity for the table row (never sent to the API). */
  id: string;
  key: string;
  name: string;
  type: string;
  options: string;
  unit: string;
  isFilter: boolean;
  isCompatibility: boolean;
}

export function specRows(specs: readonly SpecDefinition[]): SpecRow[] {
  return specs.map((spec) => ({
    id: spec.key,
    key: spec.key,
    name: spec.name,
    type: spec.type,
    options: (spec.options ?? []).join(", "),
    unit: spec.unit ?? "",
    isFilter: spec.isFilter,
    isCompatibility: spec.isCompatibility
  }));
}

export function emptySpecRow(): SpecRow {
  return { id: crypto.randomUUID(), key: "", name: "", type: "Text", options: "", unit: "", isFilter: false, isCompatibility: false };
}

const SPEC_KEY = /^[a-z][a-z0-9_]{0,39}$/;

/** The row as the API expects it: lower-case key, trimmed text and choice options split on commas (none for other types). */
export function toSpecDefinitions(rows: readonly SpecRow[]): SpecDefinition[] {
  return rows.map((row) => ({
    key: row.key.trim().toLowerCase(),
    name: row.name.trim(),
    type: row.type,
    options: row.type === "Choice" ? row.options.split(",").map((option) => option.trim()).filter(Boolean) : null,
    unit: row.unit.trim() === "" ? null : row.unit.trim(),
    isFilter: row.isFilter,
    isCompatibility: row.isCompatibility
  }));
}

/** One row is ready to save: a valid key, a name, and at least one option for a choice spec. */
export function specRowValid(row: SpecRow): boolean {
  return SPEC_KEY.test(row.key.trim().toLowerCase()) && row.name.trim() !== "" &&
    (row.type !== "Choice" || row.options.split(",").some((option) => option.trim() !== ""));
}
