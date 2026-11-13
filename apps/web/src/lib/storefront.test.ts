import { afterEach, describe, expect, it, vi } from "vitest";
import {
  configuredWhatsAppNumber,
  currencyLabel,
  productCanonicalUrl,
  whatsappProductInquiryUrl,
} from "@/lib/storefront";

afterEach(() => {
  vi.unstubAllEnvs();
});

describe("storefront product actions", () => {
  it("uses the approved currency labels and Latin price digits", () => {
    expect(currencyLabel("ar")).toBe("ج.م");
    expect(currencyLabel("en")).toBe("EGP");
  });

  it("builds canonical product URLs without duplicate slashes", () => {
    expect(productCanonicalUrl("gpu-1", "https://shop.example/")).toBe("https://shop.example/products/gpu-1");
  });

  it("hides WhatsApp until an owner-configured number exists", () => {
    vi.stubEnv("NEXT_PUBLIC_STORE_WHATSAPP_NUMBER", "");
    expect(configuredWhatsAppNumber()).toBeNull();
    vi.stubEnv("NEXT_PUBLIC_STORE_WHATSAPP_NUMBER", "+20 100 000 0000");
    expect(configuredWhatsAppNumber()).toBe("201000000000");
  });

  it("includes product context in a WhatsApp inquiry", () => {
    const url = whatsappProductInquiryUrl({
      phone: "201000000000",
      productName: "AMD Radeon RX 9070 XT",
      sku: "GPU-9070XT",
      canonicalUrl: "https://shop.example/products/gpu",
      locale: "en",
    });
    expect(url).toContain("https://wa.me/201000000000?text=");
    expect(decodeURIComponent(url)).toContain("AMD Radeon RX 9070 XT");
    expect(decodeURIComponent(url)).toContain("SKU: GPU-9070XT");
    expect(decodeURIComponent(url)).toContain("https://shop.example/products/gpu");
  });
});
