"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { type FormEvent, useEffect, useState } from "react";
import {
  ApiError,
  catalogMediaUrl,
  deleteCatalogImage,
  getAdminBrands,
  getAdminCategories,
  getAdminProduct,
  saveProduct,
  updateCatalogImage,
  uploadCatalogImage,
  type CatalogBrand,
  type CatalogCategory,
  type CatalogProduct,
  type CatalogProductCondition,
  type CatalogProductStatus,
} from "@/lib/api-client";
import { apiErrorMessage, fieldError } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { useSession } from "@/components/session-provider";
import {
  FormStack,
  LoadingCard,
  Message,
  PageIntro,
  PrimaryButton,
  SecondaryButton,
  TextField,
} from "@/components/ui";

type VariantDraft = {
  id?: string;
  name: string;
  sku: string;
  barcode: string;
  price: string;
  isActive: boolean;
};

type SpecificationDraft = {
  id?: string;
  variantId?: string;
  variantSku?: string;
  groupName: string;
  name: string;
  value: string;
  sortOrder: number;
};

function flatten(categories: CatalogCategory[]): CatalogCategory[] {
  return categories.flatMap((category) => [category, ...flatten(category.children)]);
}

export function CatalogProductEditor({ productId }: { productId?: string }) {
  const { messages } = useLocale();
  const router = useRouter();
  const { status, user } = useSession();
  const [categories, setCategories] = useState<CatalogCategory[]>([]);
  const [brands, setBrands] = useState<CatalogBrand[]>([]);
  const [product, setProduct] = useState<CatalogProduct | null>(null);
  const [loading, setLoading] = useState(Boolean(productId));
  const [pending, setPending] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [name, setName] = useState("");
  const [slug, setSlug] = useState("");
  const [sku, setSku] = useState("");
  const [barcode, setBarcode] = useState("");
  const [categoryId, setCategoryId] = useState("");
  const [brandId, setBrandId] = useState("");
  const [shortDescription, setShortDescription] = useState("");
  const [description, setDescription] = useState("");
  const [basePrice, setBasePrice] = useState("0");
  const [productStatus, setProductStatus] = useState<CatalogProductStatus>("Draft");
  const [productCondition, setProductCondition] = useState<CatalogProductCondition>("New");
  const [variants, setVariants] = useState<VariantDraft[]>([]);
  const [specifications, setSpecifications] = useState<SpecificationDraft[]>([]);
  const [imageFile, setImageFile] = useState<File | null>(null);
  const [imageAlt, setImageAlt] = useState("");
  const [isPrimary, setIsPrimary] = useState(false);
  const [editorSection, setEditorSection] = useState<"identity" | "commercial" | "variants" | "specifications">("identity");

  const mayManage = user?.permissions.some((permission) => permission.startsWith("Catalog.")) ?? false;

  useEffect(() => {
    if (status === "anonymous") {
      router.replace(`/login?returnTo=${encodeURIComponent(`/admin/catalog/products/${productId ?? "new"}`)}`);
      return;
    }
    if (status !== "authenticated" || !mayManage) {
      setLoading(false);
      return;
    }
    void Promise.all([
      getAdminCategories(),
      getAdminBrands(),
      productId ? getAdminProduct(productId) : Promise.resolve(null),
    ]).then(([categoryData, brandData, productData]) => {
      setCategories(flatten(categoryData));
      setBrands(brandData);
      if (productData) applyProduct(productData);
      setLoading(false);
    }).catch((requestError) => {
      setError(requestError instanceof ApiError && requestError.status === 403
        ? messages.adminCatalogForbidden
        : apiErrorMessage(requestError, messages));
      setLoading(false);
    });
  }, [mayManage, messages, productId, router, status]);

  function applyProduct(value: CatalogProduct) {
    setProduct(value);
    setName(value.name);
    setSlug(value.slug);
    setSku(value.sku);
    setBarcode(value.barcode ?? "");
    setCategoryId(value.categoryId);
    setBrandId(value.brandId ?? "");
    setShortDescription(value.shortDescription ?? "");
    setDescription(value.description ?? "");
    setBasePrice(String(value.basePrice));
    setProductStatus(value.status);
    setProductCondition(value.condition);
    setVariants(value.variants.map((variant) => ({
      id: variant.id,
      name: variant.name,
      sku: variant.sku,
      barcode: variant.barcode ?? "",
      price: variant.price === null ? "" : String(variant.price),
      isActive: variant.isActive,
    })));
    setSpecifications(value.specifications.map((item) => ({
      id: item.id,
      variantId: item.variantId ?? undefined,
      variantSku: value.variants.find((variant) => variant.id === item.variantId)?.sku,
      groupName: item.groupName,
      name: item.name,
      value: item.value,
      sortOrder: item.sortOrder,
    })));
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setPending("save");
    setError(null);
    setNotice(null);
    try {
      const result = await saveProduct(productId ?? null, {
        name,
        slug,
        sku,
        barcode: barcode.trim() || undefined,
        categoryId,
        brandId: brandId || null,
        shortDescription: shortDescription.trim() || undefined,
        description: description.trim() || undefined,
        basePrice: Number(basePrice),
        status: productStatus,
        condition: productCondition,
        variants: variants.map((variant) => ({
          id: variant.id,
          name: variant.name,
          sku: variant.sku,
          barcode: variant.barcode.trim() || null,
          price: variant.price.trim() ? Number(variant.price) : null,
          isActive: variant.isActive,
        })),
        specifications: specifications.map((item, index) => ({
          id: item.id,
          variantId: item.variantId ?? null,
          variantSku: item.variantId ? null : item.variantSku ?? null,
          groupName: item.groupName,
          name: item.name,
          value: item.value,
          sortOrder: index,
        })),
        rowVersion: product?.rowVersion,
      });
      applyProduct(result);
      setNotice(messages.adminProductSaved);
      if (!productId) router.replace(`/admin/catalog/products/${result.id}`);
    } catch (requestError) {
      const nameError = fieldError(requestError, "name");
      if (nameError) setEditorSection("identity");
      setError(nameError ?? apiErrorMessage(requestError, messages));
    } finally {
      setPending(null);
    }
  }

  async function upload(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!product || !imageFile) return;
    setPending("image");
    setError(null);
    try {
      await uploadCatalogImage(product.id, imageFile, imageAlt, isPrimary);
      applyProduct(await getAdminProduct(product.id));
      setImageFile(null);
      setImageAlt("");
      setIsPrimary(false);
      setNotice(messages.adminProductSaved);
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setPending(null);
    }
  }

  async function removeImage(imageId: string) {
    if (!product) return;
    setPending(imageId);
    try {
      await deleteCatalogImage(product.id, imageId);
      applyProduct(await getAdminProduct(product.id));
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setPending(null);
    }
  }

  async function makePrimary(imageId: string) {
    if (!product) return;
    setPending(imageId);
    try {
      const image = product.images.find((item) => item.id === imageId);
      if (!image) return;
      await updateCatalogImage(product.id, imageId, {
        altText: image.altText,
        sortOrder: image.sortOrder,
        isPrimary: true,
      });
      setProduct({
        ...product,
        images: product.images.map((item) => ({ ...item, isPrimary: item.id === imageId })),
      });
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setPending(null);
    }
  }

  if (status === "loading" || loading) return <LoadingCard />;
  if (status === "authenticated" && !mayManage) {
    return <main className="mx-auto max-w-3xl px-5 py-16"><Message tone="error">{messages.adminCatalogForbidden}</Message></main>;
  }

  return (
    <main className="mx-auto w-full max-w-6xl px-5 py-10 sm:px-8 sm:py-16">
      <div className="mb-6">
        <Link href="/admin/catalog" className="text-sm font-semibold text-[var(--ds-accent)]">← {messages.adminCatalogTitle}</Link>
        {product ? (
          <Link
            href={`/admin/catalog/products/${product.id}/preview`}
            target="_blank"
            className="ms-4 text-sm font-semibold text-[var(--ds-accent)]"
          >
            {messages.catalogProductDetails}
          </Link>
        ) : null}
      </div>
      <PageIntro
        eyebrow={messages.adminProductEyebrow}
        title={productId ? messages.adminEdit : messages.adminNewProduct}
        lead={messages.adminCatalogLead}
      />
      {notice ? <div className="mb-5"><Message tone="success">{notice}</Message></div> : null}
      {error ? <div className="mb-5"><Message tone="error">{error}</Message></div> : null}

      <form onSubmit={submit} className="rounded-2xl border border-[var(--ds-border)] bg-[var(--ds-surface)] p-6">
        <FormStack>
          <nav className="ds-nav-scroll flex gap-1 overflow-x-auto rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface-muted)] p-1" role="tablist" aria-label={messages.adminCatalogTitle}>
            {([
              ["identity", messages.adminIdentitySection],
              ["commercial", messages.adminCommercialSection],
              ["variants", `${messages.catalogVariants} (${variants.length})`],
              ["specifications", `${messages.catalogSpecifications} (${specifications.length})`],
            ] as const).map(([key, label]) => (
              <button
                key={key}
                type="button"
                role="tab"
                aria-selected={editorSection === key}
                onClick={() => setEditorSection(key)}
                className={`min-h-10 shrink-0 rounded-lg px-4 text-sm font-bold transition ${editorSection === key ? "bg-[var(--ds-brand)] text-white" : "text-[var(--ds-text-muted)] hover:bg-[var(--ds-surface)]"}`}
              >
                {label}
              </button>
            ))}
          </nav>

          {editorSection === "identity" ? (
            <section className="grid gap-5" aria-label={messages.adminIdentitySection}>
              <div className="grid gap-4 md:grid-cols-2">
                <TextField label={messages.adminName} value={name} onChange={(event) => setName(event.target.value)} required />
                <TextField label={messages.adminSlug} value={slug} onChange={(event) => setSlug(event.target.value)} dir="ltr" required />
                <TextField label={messages.adminSku} value={sku} onChange={(event) => setSku(event.target.value)} dir="ltr" required />
                <TextField label={messages.adminBarcode} value={barcode} onChange={(event) => setBarcode(event.target.value)} dir="ltr" />
              </div>
              <div className="grid gap-4 md:grid-cols-2">
                <label className="text-sm font-semibold">
                  {messages.adminCategory}
                  <select value={categoryId} onChange={(event) => setCategoryId(event.target.value)} className="mt-2 min-h-11 w-full rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3" required>
                    <option value="">—</option>
                    {categories.map((category) => <option key={category.id} value={category.id}>{category.name}</option>)}
                  </select>
                </label>
                <label className="text-sm font-semibold">
                  {messages.adminBrand}
                  <select value={brandId} onChange={(event) => setBrandId(event.target.value)} className="mt-2 min-h-11 w-full rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3">
                    <option value="">—</option>
                    {brands.filter((brand) => brand.isActive).map((brand) => <option key={brand.id} value={brand.id}>{brand.name}</option>)}
                  </select>
                </label>
              </div>
              <label className="text-sm font-semibold">
                {messages.adminShortDescription}
                <textarea value={shortDescription} onChange={(event) => setShortDescription(event.target.value)} maxLength={600} className="mt-2 min-h-24 w-full rounded-xl border border-[var(--ds-border)] p-3" />
              </label>
              <label className="text-sm font-semibold">
                {messages.adminDescription}
                <textarea value={description} onChange={(event) => setDescription(event.target.value)} maxLength={8000} className="mt-2 min-h-36 w-full rounded-xl border border-[var(--ds-border)] p-3" />
              </label>
            </section>
          ) : null}

          {editorSection === "commercial" ? (
            <section className="grid gap-4 rounded-xl bg-[var(--ds-surface-muted)] p-5 md:grid-cols-2" aria-label={messages.adminCommercialSection}>
              <TextField label={messages.adminBasePrice} type="number" min="0" step="0.0001" value={basePrice} onChange={(event) => setBasePrice(event.target.value)} dir="ltr" required />
              <label className="text-sm font-semibold">
                {messages.catalogPublished}
                <select value={productStatus} onChange={(event) => setProductStatus(event.target.value as CatalogProductStatus)} className="mt-2 min-h-11 w-full rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3">
                  <option value="Draft">{messages.catalogDraft}</option>
                  <option value="Published">{messages.catalogPublished}</option>
                </select>
              </label>
              <label className="text-sm font-semibold">
                {messages.catalogCondition}
                <select value={productCondition} onChange={(event) => setProductCondition(event.target.value as CatalogProductCondition)} className="mt-2 min-h-11 w-full rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3">
                  <option value="New">{messages.catalogConditionNew}</option>
                  <option value="Used">{messages.catalogConditionUsed}</option>
                </select>
              </label>
            </section>
          ) : null}

          {editorSection === "variants" ? <section>
            <div className="flex items-center justify-between">
              <h2 className="text-xl font-bold">{messages.catalogVariants}</h2>
              <SecondaryButton type="button" onClick={() => setVariants((items) => [...items, { name: "", sku: "", barcode: "", price: "", isActive: true }])}>
                {messages.adminAddVariant}
              </SecondaryButton>
            </div>
            <div className="mt-3 grid gap-3">
              {variants.map((variant, index) => (
                <div key={variant.id ?? index} className="grid gap-3 rounded-xl bg-[var(--ds-surface-muted)] p-4 md:grid-cols-[1fr_1fr_1fr_10rem_auto_auto]">
                  <TextField label={messages.adminVariantName} value={variant.name} onChange={(event) => updateVariant(index, { name: event.target.value })} required />
                  <TextField label={messages.adminSku} value={variant.sku} onChange={(event) => updateVariant(index, { sku: event.target.value })} dir="ltr" required />
                  <TextField label={messages.adminBarcode} value={variant.barcode} onChange={(event) => updateVariant(index, { barcode: event.target.value })} dir="ltr" />
                  <TextField label={messages.adminVariantPrice} type="number" min="0" step="0.0001" value={variant.price} onChange={(event) => updateVariant(index, { price: event.target.value })} dir="ltr" />
                  <label className="flex min-h-11 items-center gap-2 self-end text-sm font-semibold">
                    <input type="checkbox" checked={variant.isActive} onChange={(event) => updateVariant(index, { isActive: event.target.checked })} />
                    {messages.adminActive}
                  </label>
                  <SecondaryButton type="button" onClick={() => removeVariant(index)}>
                    {messages.adminRemove}
                  </SecondaryButton>
                </div>
              ))}
            </div>
          </section> : null}

          {editorSection === "specifications" ? <section>
            <div className="flex items-center justify-between">
              <h2 className="text-xl font-bold">{messages.catalogSpecifications}</h2>
              <SecondaryButton type="button" onClick={() => setSpecifications((items) => [...items, { groupName: "General", name: "", value: "", sortOrder: items.length }])}>
                {messages.adminAddSpecification}
              </SecondaryButton>
            </div>
            <div className="mt-3 grid gap-3">
              {specifications.map((specification, index) => (
                <div key={specification.id ?? index} className="grid gap-3 rounded-xl bg-[var(--ds-surface-muted)] p-4 md:grid-cols-[1fr_1fr_1fr_1fr_auto]">
                  <label className="text-sm font-semibold">
                    {messages.catalogVariants}
                    <select
                      value={specification.variantId ?? specification.variantSku ?? ""}
                      onChange={(event) => {
                        const value = event.target.value;
                        const variant = variants.find((item) => item.id === value || item.sku === value);
                        updateSpecification(index, {
                          variantId: variant?.id,
                          variantSku: variant?.sku,
                        });
                      }}
                      className="mt-2 min-h-11 w-full rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3"
                    >
                      <option value="">{messages.catalogProductDetails}</option>
                      {variants.map((variant, variantIndex) => (
                        <option key={variant.id ?? variantIndex} value={variant.id ?? variant.sku}>
                          {variant.name || variant.sku || `#${variantIndex + 1}`}
                        </option>
                      ))}
                    </select>
                  </label>
                  <TextField label={messages.adminGroup} value={specification.groupName} onChange={(event) => updateSpecification(index, { groupName: event.target.value })} required />
                  <TextField label={messages.adminName} value={specification.name} onChange={(event) => updateSpecification(index, { name: event.target.value })} required />
                  <TextField label={messages.adminValue} value={specification.value} onChange={(event) => updateSpecification(index, { value: event.target.value })} required />
                  <SecondaryButton type="button" onClick={() => setSpecifications((items) => items.filter((_, itemIndex) => itemIndex !== index))}>
                    {messages.adminRemove}
                  </SecondaryButton>
                </div>
              ))}
            </div>
          </section> : null}

          <PrimaryButton type="submit" pending={pending === "save"}>{messages.adminSave}</PrimaryButton>
        </FormStack>
      </form>

      {product ? (
        <section className="mt-6 rounded-2xl border border-[var(--ds-border)] bg-[var(--ds-surface)] p-6">
          <h2 className="text-xl font-bold">{messages.adminImageUpload}</h2>
          <div className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            {product.images.map((image) => (
              <article key={image.id} className="overflow-hidden rounded-xl border border-[var(--ds-border)]">
                <img src={catalogMediaUrl(image.thumbnailStorageKey)} alt={image.altText} className="aspect-square w-full object-contain bg-[var(--ds-surface-muted)]" />
                <div className="grid gap-2 p-3">
                  <p className="truncate text-xs text-[var(--ds-text-muted)]">{image.altText}</p>
                  {!image.isPrimary ? (
                    <SecondaryButton type="button" onClick={() => void makePrimary(image.id)} disabled={pending === image.id}>
                      {messages.adminPrimaryImage}
                    </SecondaryButton>
                  ) : <span className="text-xs font-bold text-emerald-700">{messages.adminPrimaryImage}</span>}
                  <SecondaryButton type="button" onClick={() => void removeImage(image.id)} disabled={pending === image.id} className="border-red-200 text-red-700">
                    {messages.adminRemove}
                  </SecondaryButton>
                </div>
              </article>
            ))}
          </div>
          <form onSubmit={upload} className="mt-6 grid gap-4 md:grid-cols-[1fr_1fr_auto_auto] md:items-end">
            <label className="text-sm font-semibold">
              {messages.adminImageUpload}
              <input type="file" accept="image/*" onChange={(event) => setImageFile(event.target.files?.[0] ?? null)} className="mt-2 block min-h-11 w-full rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] p-2" required />
            </label>
            <TextField label={messages.adminImageAlt} value={imageAlt} onChange={(event) => setImageAlt(event.target.value)} required />
            <label className="flex min-h-11 items-center gap-2 text-sm font-semibold">
              <input type="checkbox" checked={isPrimary} onChange={(event) => setIsPrimary(event.target.checked)} />
              {messages.adminPrimaryImage}
            </label>
            <PrimaryButton type="submit" pending={pending === "image"} disabled={!imageFile}>{messages.adminUploadImage}</PrimaryButton>
          </form>
        </section>
      ) : (
        <Message tone="info">{messages.adminSave}: {messages.adminImageUpload}</Message>
      )}
    </main>
  );

  function updateVariant(index: number, patch: Partial<VariantDraft>) {
    const previousSku = variants[index]?.sku;
    setVariants((items) => items.map((item, itemIndex) => itemIndex === index ? { ...item, ...patch } : item));
    if (patch.sku !== undefined && previousSku) {
      setSpecifications((items) => items.map((item) =>
        !item.variantId && item.variantSku === previousSku
          ? { ...item, variantSku: patch.sku }
          : item));
    }
  }

  function removeVariant(index: number) {
    const removed = variants[index];
    setVariants((items) => items.filter((_, itemIndex) => itemIndex !== index));
    if (!removed) return;
    setSpecifications((items) => items.map((item) =>
      (removed.id && item.variantId === removed.id) ||
      (!removed.id && item.variantSku === removed.sku)
        ? { ...item, variantId: undefined, variantSku: undefined }
        : item));
  }

  function updateSpecification(index: number, patch: Partial<SpecificationDraft>) {
    setSpecifications((items) => items.map((item, itemIndex) => itemIndex === index ? { ...item, ...patch } : item));
  }
}
