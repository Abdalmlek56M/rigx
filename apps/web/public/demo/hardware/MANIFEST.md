# RIGX Demo Hardware Image Manifest

These files are local, review-only storefront assets. They are **not** uploaded
to the catalog database and are not served through the catalog media API.
Catalog names, prices, SKUs, and specifications continue to come from the
published Catalog API.

The images are used only to preview how the RIGX catalog looks with real
hardware photography. Manufacturer names and product images remain trademarks
of their respective owners. Before a public production launch, the owner must
reconfirm permission for each manufacturer asset or replace it with an owned
product photo.

## Verified model images

| Local file | Catalog model | Original source | Reuse note |
| --- | --- | --- | --- |
| `products/amd-radeon-rx-9070-xt-16gb.jpg` | AMD Radeon RX 9070 XT 16GB (`GPU-9070XT`) | https://www.amd.com/content/dam/amd/en/images/products/graphics/radeon-rx-9070/2922918-radeon-rx-9070xt.jpg | AMD product-page render downloaded for local demo preview. |
| `products/amd-ryzen-7-9800x3d.jpg` | AMD Ryzen 7 9800X3D (`CPU-9800X3D`) | https://www.amd.com/content/dam/amd/en/images/products/processors/ryzen/2900400-ryzen-7-9800x3d.jpg | AMD product-page render downloaded for local demo preview. |
| `products/asus-rog-strix-b650e-f.jpg` | ASUS ROG Strix B650E-F (`MB-B650E-F`) | https://dlcdnwebimgs.asus.com/gain/566A4DF8-85E5-4958-9CEE-21F4629E53AB | ASUS product image CDN asset; file content verified as PNG. |
| `products/corsair-rm850e-gold.png` | Corsair RM850e Gold (`PSU-RM850E`) | Corsair product image CDN (downloaded during review) | Corsair PSU product render for local demo preview. |
| `products/corsair-vengeance-32gb-ddr5-6000.png` | Corsair Vengeance 32GB DDR5-6000 (`RAM-VEN32`) | Corsair product image CDN (downloaded during review) | Corsair memory product render for local demo preview. |
| `products/kingston-nv3-1tb.jpg` | Kingston NV3 1TB NVMe (`SSD-NV3-1`) | https://media.kingston.com/kingston/product/SNV3S_500GB-lg.jpg | Kingston NV3 series image; capacity badge is not treated as the exact 1TB SKU photo. |
| `products/lian-li-o11-vision.jpg` | Lian Li O11 Vision (`CASE-O11V`) | https://lian-li.com/wp-content/uploads/2023/11/O11_V_012.jpg | Lian Li product-page render for local demo preview. |
| `products/logitech-g-pro-x-superlight-2.png` | Logitech G Pro X Superlight 2 (`MOU-GPX2`) | Logitech product image CDN (downloaded during review) | Logitech mouse product render for local demo preview. |
| `products/samsung-990-pro-2tb.jpg` | Samsung 990 PRO 2TB NVMe (`SSD-990P2`) | https://img.us.news.samsung.com/us/wp-content/uploads/2022/08/22153822/990PRO-front-3-scaled-e1709224927750.jpg | Samsung 990 PRO press image. The exact 2TB retail pack was not verified; used as a series-level image. |
| `products/samsung-990-pro-2tb-alt.jpg` | Samsung 990 PRO 2TB NVMe (`SSD-990P2`) | https://img.us.news.samsung.com/us/wp-content/uploads/2023/09/06093904/samsung-990-pro-4tb-f.jpg | Samsung 990 PRO press image (4TB heatsink view). Secondary gallery image only; not presented as the exact 2TB SKU. |

## Products using a neutral category fallback

The following published models intentionally do **not** use a downloaded image
because an exact, reliable, locally reusable asset was not obtained:

- **NVIDIA GeForce RTX 5080 16GB (`GPU-5080`)** — the available press render has
  a green marketing background and reads as an advertisement, not a neutral
  product cut-out.
- **Intel Core Ultra 7 265K (`CPU-265K`)** — Intel's product page blocked the
  download and no verified local asset was obtained.
- **NZXT Kraken 360 RGB (`COOL-K360`)** — the NZXT product page returned 404
  during review and no verified local asset was obtained.

`ProductImage` renders a neutral, theme-aware category mark for these products.
It does not show a generated photo, an unrelated model, or a decorative
"illustrative image" badge on every card.

## Brand logos

The demo database also contains generated, colored placeholder brand logos.
The storefront brand rail therefore renders the real catalog brand name as a
typographic tile and deliberately ignores those placeholder logo files. Product
imagery, SKUs, prices, and brand names still come from the Catalog API. Real
brand logos can be wired back in after the owner supplies approved assets.

