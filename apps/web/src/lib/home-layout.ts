"use client";

export type HomeSectionId = "hero" | "categories" | "brands" | "offers" | "discovery" | "guide" | "latest" | "closing";
export type HomeRailId = "brands" | "categories";
export type HomeRailContentType = "brands" | "categories" | "products";

export type HomeRailConfig = {
  title: string;
  lead: string;
  contentType: HomeRailContentType;
  limit: number;
  itemOrder: string[];
  hiddenItemIds: string[];
};

export type HomeSectionConfig = {
  id: HomeSectionId;
  visible: boolean;
};

export type HomeLayout = {
  version: 1;
  sections: HomeSectionConfig[];
  rails: Record<HomeRailId, HomeRailConfig>;
};

export const HOME_LAYOUT_STORAGE_KEY = "rigx-home-layout-v1";

export const HOME_SECTION_IDS: HomeSectionId[] = [
  "hero",
  "categories",
  "brands",
  "offers",
  "discovery",
  "guide",
  "latest",
  "closing",
];

const defaultRail: HomeRailConfig = {
  title: "",
  lead: "",
  contentType: "brands",
  limit: 10,
  itemOrder: [],
  hiddenItemIds: [],
};

export function createDefaultHomeLayout(): HomeLayout {
  return {
    version: 1,
    sections: HOME_SECTION_IDS.map((id) => ({ id, visible: true })),
    rails: {
      brands: { ...defaultRail, contentType: "brands" },
      categories: { ...defaultRail, contentType: "categories" },
    },
  };
}

function isSectionId(value: unknown): value is HomeSectionId {
  return typeof value === "string" && (HOME_SECTION_IDS as string[]).includes(value);
}

function normalizeRail(value: unknown, fallback: HomeRailConfig): HomeRailConfig {
  if (typeof value !== "object" || value === null) return { ...fallback };
  const candidate = value as Partial<HomeRailConfig>;
  const contentType = candidate.contentType;
  return {
    title: typeof candidate.title === "string" ? candidate.title : fallback.title,
    lead: typeof candidate.lead === "string" ? candidate.lead : fallback.lead,
    contentType: contentType === "brands" || contentType === "categories" || contentType === "products"
      ? contentType
      : fallback.contentType,
    limit: typeof candidate.limit === "number" && Number.isFinite(candidate.limit)
      ? Math.min(24, Math.max(1, Math.round(candidate.limit)))
      : fallback.limit,
    itemOrder: Array.isArray(candidate.itemOrder)
      ? candidate.itemOrder.filter((id): id is string => typeof id === "string")
      : fallback.itemOrder,
    hiddenItemIds: Array.isArray(candidate.hiddenItemIds)
      ? candidate.hiddenItemIds.filter((id): id is string => typeof id === "string")
      : fallback.hiddenItemIds,
  };
}

export function parseHomeLayout(raw: string | null): HomeLayout {
  const fallback = createDefaultHomeLayout();
  if (!raw) return fallback;
  try {
    const parsed = JSON.parse(raw) as unknown;
    if (typeof parsed !== "object" || parsed === null) return fallback;
    const candidate = parsed as Partial<HomeLayout>;
    const sections = Array.isArray(candidate.sections)
      ? candidate.sections
          .filter((section): section is HomeSectionConfig =>
            typeof section === "object" &&
            section !== null &&
            isSectionId((section as HomeSectionConfig).id) &&
            typeof (section as HomeSectionConfig).visible === "boolean")
      : [];

    // Keep exactly one entry per known section, preserving stored order first.
    const seen = new Set<HomeSectionId>();
    const normalizedSections: HomeSectionConfig[] = [];
    for (const section of sections) {
      if (seen.has(section.id)) continue;
      seen.add(section.id);
      normalizedSections.push({ id: section.id, visible: section.visible });
    }
    for (const id of HOME_SECTION_IDS) {
      if (!seen.has(id)) normalizedSections.push({ id, visible: true });
    }

    // Keep the commercial discovery order stable for layouts published before
    // the offers section existed: category discovery, then brands, then offers.
    const offersIndex = normalizedSections.findIndex((section) => section.id === "offers");
    const categoriesIndex = normalizedSections.findIndex((section) => section.id === "categories");
    const brandsIndex = normalizedSections.findIndex((section) => section.id === "brands");
    if (offersIndex > brandsIndex && brandsIndex >= 0 && categoriesIndex >= 0) {
      const [offers] = normalizedSections.splice(offersIndex, 1);
      normalizedSections.splice(brandsIndex + 1, 0, offers);
    }

    return {
      version: 1,
      sections: normalizedSections,
      rails: {
        brands: normalizeRail(candidate.rails?.brands, fallback.rails.brands),
        categories: normalizeRail(candidate.rails?.categories, fallback.rails.categories),
      },
    };
  } catch {
    return fallback;
  }
}

/** Applies the admin-curated order and visibility to a rail's real data. */
export function arrangeRailItems<T extends { id: string }>(
  items: T[],
  config: HomeRailConfig,
): T[] {
  const hidden = new Set(config.hiddenItemIds);
  const order = new Map(config.itemOrder.map((id, index) => [id, index]));
  return items
    .filter((item) => !hidden.has(item.id))
    .sort((left, right) => (order.get(left.id) ?? Number.MAX_SAFE_INTEGER) - (order.get(right.id) ?? Number.MAX_SAFE_INTEGER))
    .slice(0, config.limit);
}
