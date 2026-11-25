export interface SpecDef {
  key: string;
  name: string;
  options: string[];
  unit: string | null;
  isFilter: boolean;
  /** "Choice", "Number", "YesNo" or "Text" (the Catalog's spec types). */
  type: string;
}

export interface CategoryNode {
  id: string;
  parentId: string | null;
  name: string;
  /** English display name from the API; absent until the owner fills it. */
  nameEn?: string | null;
  slug: string;
  specs: SpecDef[];
  children: CategoryNode[];
}

function* walk(tree: CategoryNode[]): Generator<CategoryNode> {
  for (const node of tree) {
    yield node;
    yield* walk(node.children);
  }
}

export function findCategory(tree: CategoryNode[], match: (node: CategoryNode) => boolean): CategoryNode | undefined {
  for (const node of walk(tree)) if (match(node)) return node;
  return undefined;
}

export const categoryBySlug = (tree: CategoryNode[], slug: string) => findCategory(tree, (node) => node.slug === slug);
export const categoryById = (tree: CategoryNode[], id: string) => findCategory(tree, (node) => node.id === id);

/** Root → category (inclusive); empty when the id is unknown. */
export function categoryPath(tree: CategoryNode[], id: string): CategoryNode[] {
  const path: CategoryNode[] = [];
  for (let node = categoryById(tree, id); node; node = node.parentId ? categoryById(tree, node.parentId) : undefined)
    path.unshift(node);
  return path;
}

/** Specs are inherited down the tree: ancestors first, then the category's own (an own spec replaces an inherited one with the same key). */
export function effectiveSpecs(tree: CategoryNode[], id: string): SpecDef[] {
  const merged = new Map<string, SpecDef>();
  for (const node of categoryPath(tree, id)) for (const spec of node.specs) merged.set(spec.key, spec);
  return [...merged.values()];
}
