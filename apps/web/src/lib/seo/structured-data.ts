/** schema.org JSON-LD builders. Every value must be visible on the page it describes. */

export type JsonLd = Record<string, unknown>;

const CONTEXT = "https://schema.org";

export interface ProductData {
  name: string;
  url: string;
  images: string[];
  /** The price of the item bought alone, as the cards show it. */
  price: number;
  available: boolean;
  /** Omitted for builds (they can mix new and used parts). */
  condition?: "New" | "Used";
  sku?: string;
  category?: string;
  brand?: string;
  organizationId: string;
}

export function productJsonLd(data: ProductData): JsonLd {
  return {
    "@context": CONTEXT,
    "@type": "Product",
    name: data.name,
    url: data.url,
    ...(data.images.length > 0 ? { image: data.images } : {}),
    ...(data.sku ? { sku: data.sku } : {}),
    ...(data.category ? { category: data.category } : {}),
    ...(data.brand ? { brand: { "@type": "Brand", name: data.brand } } : {}),
    offers: {
      "@type": "Offer",
      url: data.url,
      price: data.price.toFixed(2),
      priceCurrency: "EGP",
      availability: `${CONTEXT}/${data.available ? "InStock" : "SoldOut"}`,
      ...(data.condition ? { itemCondition: `${CONTEXT}/${data.condition === "New" ? "NewCondition" : "UsedCondition"}` } : {}),
      seller: { "@id": data.organizationId }
    }
  };
}

/** The last crumb (the current page) may have no URL. */
export function breadcrumbJsonLd(crumbs: { name: string; url?: string }[]): JsonLd {
  return {
    "@context": CONTEXT,
    "@type": "BreadcrumbList",
    itemListElement: crumbs.map((crumb, index) => ({
      "@type": "ListItem",
      position: index + 1,
      name: crumb.name,
      ...(crumb.url ? { item: crumb.url } : {})
    }))
  };
}

export interface OrganizationData {
  id: string;
  name: string;
  url: string;
  logo: string;
  description: string;
  email: string | null;
  telephone: string | null;
  sameAs: string[];
}

/** Online store serving all of Egypt; no street address (the business runs from home, service area only). */
export function organizationJsonLd(data: OrganizationData): JsonLd {
  return {
    "@context": CONTEXT,
    "@type": "OnlineStore",
    "@id": data.id,
    name: data.name,
    url: data.url,
    logo: data.logo,
    description: data.description,
    areaServed: { "@type": "Country", name: "Egypt" },
    ...(data.email ? { email: data.email } : {}),
    ...(data.telephone ? { telephone: data.telephone } : {}),
    ...(data.sameAs.length > 0 ? { sameAs: data.sameAs } : {})
  };
}

export function faqJsonLd(entries: { question: string; answer: string }[]): JsonLd {
  return {
    "@context": CONTEXT,
    "@type": "FAQPage",
    mainEntity: entries.map((entry) => ({
      "@type": "Question",
      name: entry.question,
      acceptedAnswer: { "@type": "Answer", text: entry.answer }
    }))
  };
}

/** JSON for an inline `<script type="application/ld+json">`: `<` is escaped so text can never close the tag. */
export function serializeJsonLd(data: JsonLd | JsonLd[]): string {
  return JSON.stringify(data).replace(/</g, "\\u003c");
}
