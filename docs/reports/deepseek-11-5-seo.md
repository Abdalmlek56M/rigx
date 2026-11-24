# Report 5: SEO and launch-readiness audit

Checked by reading: `apps/web/src/app` (layout, store pages, `sitemap.ts`, `robots.ts`, `llms.txt`, `proxy.ts`), `lib/seo/*`, the generated API types, and roadmap step 4.7. No external request, no browser.

## Summary
- The technical base is solid: canonical and hreflang on every public page, `noindex` on cart, checkout, login, account, tracking, invoices and admin (and also blocked in `robots.txt`), sitemap with all indexable pages, Product, BreadcrumbList, Organization and FAQ markup, 301 for old `RX-` links and for renamed product slugs.
- Main gap: **only the home page has its own meta description.** Products, builds, categories, policies and FAQ all repeat the site-wide sentence.
- Product markup is valid but thin for Google's free product listings (no description, return policy or shipping details).

## Findings
| Severity | Location | Problem | Fix |
|---|---|---|---|
| Medium | `app/[locale]/layout.tsx:55` is the only fallback; `description:` is set only in `(store)/page.tsx` and the layout (`grep description: app`) | Every product, build, category and policy page gets the same `<meta name="description">`. Google rewrites weak snippets, but duplicates are reported in audits and give the search result no price, condition or warranty. | Add a small `describe*` helper in `lib/seo/` and use it in the product, build, catalog and policy `generateMetadata`. Product example: "RTX 3060 مستعمل، ضمان 3 أشهر، السعر 8,500 ج.م. شحن لكل المحافظات." (built from fields already on the page; formal Arabic). Cap at about 155 characters. |
| Medium | `lib/seo/structured-data.ts:22-39` | `Product` has no `description`, no `priceValidUntil`, no `hasMerchantReturnPolicy`, no `shippingDetails`. Search Console's merchant-listing report shows these as "recommended" warnings, and free listings in Google Shopping need them for a clean status. | Add `description` (same text as the meta description). Add `hasMerchantReturnPolicy` and `shippingDetails` once the policies are published, with values taken from the shipping and returns policy settings (needs a business decision, see Questions). Skip `priceValidUntil` unless offers have an end date. |
| Medium | `app/sitemap.ts:63-76` | No `lastModified` on any URL. The items API does not expose an update date (`StoreItemDetail`, `schema.d.ts:18938`), so Google cannot tell which product pages changed. Policies do have `publishedAtUtc`. | Add `updatedAtUtc` to the store items API (needs a backend change), then use it. Quick win now: `lastModified` for policy pages from `publishedAtUtc`. |
| Low | `lib/seo/structured-data.ts`, home `page.tsx:34-57` | No `WebSite` node, so Google has no declared site name. The Arabic brand name in search results may appear as the domain. | Add `WebSite` JSON-LD with `name: "RIGX Hardware"` and an `alternateName` (see Questions). |
| Low | `app/llms.txt/route.ts:24` | Colloquial text: "كل قطعة ليها صفحة على…". | "لكل قطعة صفحة على /p/<اسم-الموديل> تتضمن السعر والحالة (جديد أو مستعمل) والتوفر." Same for line 22 ("الشحن لكل المحافظات" is fine). |
| Low | `app/[locale]/(store)/builds/loading.tsx`, `products/loading.tsx` | A route with `loading.tsx` streams its shell before the page code runs. For `/builds/<unknown-code>` the status code may be 200 with a not-found view (a "soft 404"). Next adds a `noindex` tag in that case, so indexing is safe. **Unsure**: confirm with `curl -I` on the live site. | If 200, move the existence check into `generateMetadata`/layout, or accept it because `noindex` is present. |
| Low | `(store)/feedback/page.tsx`, sitemap `STATIC_PATHS` | `/feedback` is a form page with no content for search; it is in the sitemap. | Remove it from `STATIC_PATHS` (keep it indexable if the owner wants). |
| Low | `structured-data.ts:86-95` | `FAQPage` markup is correct, but Google now shows FAQ rich results only for a few authority sites. No harm and it helps AI assistants. | Keep; no change. |

## Checked and fine
- Canonical, hreflang and `x-default` on home, catalog, product, build, FAQ, policies, services, warranty (`lib/seo/page-seo.ts`). English pages go `noindex` while English is switched off (`page-seo.ts:20`).
- `noindex` on search results (`products/page.tsx:52`), warranty lookups with a query (`warranty/page.tsx:33`), shared build links `/b/<token>`, cart, checkout and success, login, account, `/t/` and `/i/` tokens, and the whole admin.
- `robots.txt`: blocks `/api/` and private areas in both locales, allows `/api/v1/catalog/media/` for image indexing, and points to the sitemap.
- Product pages: unique title "model — جديد/مستعمل", Open Graph with the first photo (`p/[slug]/page.tsx:54-58`), image alt = model name, breadcrumb markup with the category path, 301 to the current slug if an old slug or upper-case code is used.
- Sitemap: every locale and alternates, hourly regeneration, failing sources only drop their own URLs, far under the 50,000-URL limit.

## Questions for the owner
1. Product return and shipping markup (`hasMerchantReturnPolicy`, `shippingDetails`) needs facts from the policies: return window in days, who pays return shipping, shipping fee and delivery days. Take them from `docs/12-policies.md` once you approve the final text?
2. How should the brand be written in Arabic in Google results ("ريجكس"، "آر آي جي إكس"، or Latin only)?
3. Do you want to register the store in **Google Merchant Center** (free listings in Google Shopping)? It needs the markup above and published policies.

## What I did not check
- The live site (status codes, rendered HTML, Search Console reports).
- Core Web Vitals and image sizes.
- Meta text of articles and of the English pages.
- Whether `loadItem` in the product page resolves old slugs on the API side (the redirect code exists; the API behaviour is unconfirmed).
