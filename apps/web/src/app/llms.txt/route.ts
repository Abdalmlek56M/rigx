import { absoluteUrl } from "@/lib/seo/urls";

export const dynamic = "force-static";

const LINKS: [string, string][] = [
  ["القطع (الكتالوج) / Parts catalog", "/products"],
  ["التجميعات الجاهزة / Ready-made builds", "/builds"],
  ["تجميعات العملاء / Customer builds", "/customer-builds"],
  ["جمّع جهازك / PC builder", "/builder"],
  ["الخدمات / Services", "/services"],
  ["الأسئلة الشائعة / FAQ", "/faq"],
  ["الشحن / Shipping policy", "/policies/shipping"],
  ["الضمان / Warranty policy", "/policies/warranty"],
  ["استعلام الضمان / Warranty lookup", "/warranty"]
];

/** A short map of the store for AI assistants (llmstxt.org); Google Search does not use it. */
export function GET() {
  const body = [
    "# RIGX Hardware",
    "",
    "> متجر قطع كمبيوتر جديد ومستعمل وتجميعات جاهزة، بيع أونلاين والشحن لكل المحافظات في مصر. الأسعار بالجنيه المصري.",
    "> New and used PC hardware and ready-made builds, sold online with shipping to every governorate in Egypt. Prices in EGP.",
    "",
    "كل قطعة ليها صفحة على /p/<اسم-الموديل> فيها السعر والحالة (جديد أو مستعمل) والتوفر. / Every part has a page at /p/<model-name> with its price, condition (new or used) and availability.",
    "",
    "## Pages",
    ...LINKS.map(([label, path]) => `- [${label}](${absoluteUrl(path)})`),
    "",
    `Sitemap: ${absoluteUrl("/sitemap.xml")}`,
    ""
  ].join("\n");
  return new Response(body, { headers: { "Content-Type": "text/plain; charset=utf-8" } });
}
