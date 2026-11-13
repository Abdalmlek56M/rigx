/**
 * Local, review-only hardware imagery for the published demo catalog.
 *
 * These assets are intentionally kept outside the catalog media API: Phase A
 * must not modify the database or the backend. The source and reuse notes for
 * every file live in `public/demo/hardware/MANIFEST.md`.
 *
 * A model only gets a real image when the downloaded asset was verified as the
 * same product family/model. Anything else falls back to a neutral category
 * treatment in `ProductImage` instead of showing an unrelated or generated
 * photo as if it were the real product.
 */

type ProductIdentity = {
  slug?: string | null;
  sku?: string | null;
};

const imagesBySlug: Record<string, string[]> = {
  "amd-radeon-rx-9070-xt-16gb": ["/demo/hardware/products/amd-radeon-rx-9070-xt-16gb.jpg"],
  "amd-ryzen-7-9800x3d": ["/demo/hardware/products/amd-ryzen-7-9800x3d.jpg"],
  "asus-rog-strix-b650e-f": ["/demo/hardware/products/asus-rog-strix-b650e-f.jpg"],
  "corsair-rm850e-gold": ["/demo/hardware/products/corsair-rm850e-gold.png"],
  "corsair-vengeance-32gb-ddr5-6000": ["/demo/hardware/products/corsair-vengeance-32gb-ddr5-6000.png"],
  "kingston-nv3-1tb": ["/demo/hardware/products/kingston-nv3-1tb.jpg"],
  "lian-li-o11-vision": ["/demo/hardware/products/lian-li-o11-vision.jpg"],
  "logitech-g-pro-x-superlight-2": ["/demo/hardware/products/logitech-g-pro-x-superlight-2.png"],
  "samsung-990-pro-2tb": [
    "/demo/hardware/products/samsung-990-pro-2tb.jpg",
    "/demo/hardware/products/samsung-990-pro-2tb-alt.jpg",
  ],
};

const imagesBySku: Record<string, string[]> = {
  "GPU-9070XT": imagesBySlug["amd-radeon-rx-9070-xt-16gb"],
  "CPU-9800X3D": imagesBySlug["amd-ryzen-7-9800x3d"],
  "MB-B650E-F": imagesBySlug["asus-rog-strix-b650e-f"],
  "PSU-RM850E": imagesBySlug["corsair-rm850e-gold"],
  "RAM-VEN32": imagesBySlug["corsair-vengeance-32gb-ddr5-6000"],
  "SSD-NV3-1": imagesBySlug["kingston-nv3-1tb"],
  "CASE-O11V": imagesBySlug["lian-li-o11-vision"],
  "MOU-GPX2": imagesBySlug["logitech-g-pro-x-superlight-2"],
  "SSD-990P2": imagesBySlug["samsung-990-pro-2tb"],
};

/** Returns every verified local image for a catalog product, primary first. */
export function resolveDemoProductImages(product: ProductIdentity): string[] {
  const bySlug = product.slug ? imagesBySlug[product.slug] : undefined;
  if (bySlug?.length) return bySlug;
  const bySku = product.sku ? imagesBySku[product.sku] : undefined;
  return bySku ?? [];
}

/** Returns the primary verified local image, or null when a neutral fallback is required. */
export function resolveDemoProductImage(product: ProductIdentity): string | null {
  return resolveDemoProductImages(product)[0] ?? null;
}
