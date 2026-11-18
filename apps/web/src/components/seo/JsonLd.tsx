import { serializeJsonLd, type JsonLd as JsonLdData } from "@/lib/seo/structured-data";

/** Structured data for search engines (rendered in the server HTML). */
export function JsonLd({ data }: { data: JsonLdData | JsonLdData[] }) {
  return <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: serializeJsonLd(data) }} />;
}
