"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { ApiError, getAdminProduct, type CatalogProduct } from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { useSession } from "@/components/session-provider";
import { CatalogProductView } from "@/components/catalog-product-view";
import { LoadingCard, Message } from "@/components/ui";

export function CatalogPreview({ productId }: { productId: string }) {
  const { messages } = useLocale();
  const { status, user } = useSession();
  const [product, setProduct] = useState<CatalogProduct | null>(null);
  const [error, setError] = useState<string | null>(null);
  const mayManage = user?.permissions.some((permission) => permission.startsWith("Catalog.")) ?? false;

  useEffect(() => {
    if (status !== "authenticated" || !mayManage) return;
    void getAdminProduct(productId)
      .then(setProduct)
      .catch((requestError) => setError(
        requestError instanceof ApiError && requestError.status === 403
          ? messages.adminCatalogForbidden
          : apiErrorMessage(requestError, messages)));
  }, [mayManage, messages, productId, status]);

  if (status === "loading") return <LoadingCard />;
  if (status === "anonymous") {
    return (
      <main className="mx-auto max-w-3xl px-5 py-16">
        <Link href={`/login?returnTo=${encodeURIComponent(`/admin/catalog/products/${productId}/preview`)}`} className="font-semibold text-[var(--ds-accent)]">
          {messages.navLogin}
        </Link>
      </main>
    );
  }
  if (!mayManage) {
    return <main className="mx-auto max-w-3xl px-5 py-16"><Message tone="error">{messages.adminCatalogForbidden}</Message></main>;
  }
  if (!product && !error) return <LoadingCard />;
  if (error) return <main className="mx-auto max-w-3xl px-5 py-16"><Message tone="error">{error}</Message></main>;
  if (!product) return null;

  return (
    <main className="mx-auto w-full max-w-6xl px-5 py-10 sm:px-8 sm:py-16">
      <CatalogProductView product={product} preview />
    </main>
  );
}
