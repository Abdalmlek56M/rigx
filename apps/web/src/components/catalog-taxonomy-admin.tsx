"use client";

import { type FormEvent, useCallback, useMemo, useRef, useState } from "react";
import {
  catalogMediaUrl,
  deleteBrandLogo,
  deleteCategoryImage,
  saveBrand,
  saveCategory,
  uploadBrandLogo,
  uploadCategoryImage,
  type CatalogBrand,
  type CatalogCategory,
  type CatalogProductSummary,
} from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { ConfirmDialog, FormStack, PrimaryButton, SecondaryButton, SideDrawer, TextField } from "@/components/ui";
import { Icon } from "@/components/icons";
import { RowActionMenu } from "@/components/row-action-menu";

type FlatCategory = CatalogCategory & { depth: number };
type EditorKind = "category" | "brand";

export function CatalogTaxonomyAdmin({
  categories,
  brands,
  products,
  initialTab = "category",
  onChanged,
  onNotice,
  onError,
}: {
  categories: CatalogCategory[];
  brands: CatalogBrand[];
  products: CatalogProductSummary[];
  initialTab?: EditorKind;
  onChanged: () => Promise<void>;
  onNotice: (message: string) => void;
  onError: (message: string) => void;
}) {
  const { messages } = useLocale();
  const flatCategories = useMemo(() => flatten(categories), [categories]);
  const [tab, setTab] = useState<EditorKind>(initialTab);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<"all" | "active" | "inactive">("active");
  const [drawerKind, setDrawerKind] = useState<EditorKind | null>(null);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [pending, setPending] = useState<string | null>(null);
  const [name, setName] = useState("");
  const [slug, setSlug] = useState("");
  const [description, setDescription] = useState("");
  const [parentId, setParentId] = useState("");
  const [supportUrl, setSupportUrl] = useState("");
  const [active, setActive] = useState(true);
  const [imageToRemove, setImageToRemove] = useState<{ kind: EditorKind; id: string } | null>(null);

  const normalizedSearch = search.trim().toLowerCase();
  const visibleCategories = flatCategories.filter((category) => {
    const matchesSearch = !normalizedSearch ||
      category.name.toLowerCase().includes(normalizedSearch) ||
      category.slug.toLowerCase().includes(normalizedSearch);
    const matchesStatus = status === "all" || (status === "active" ? category.isActive : !category.isActive);
    return matchesSearch && matchesStatus;
  });
  const visibleBrands = brands.filter((brand) => {
    const matchesSearch = !normalizedSearch ||
      brand.name.toLowerCase().includes(normalizedSearch) ||
      brand.slug.toLowerCase().includes(normalizedSearch);
    const matchesStatus = status === "all" || (status === "active" ? brand.isActive : !brand.isActive);
    return matchesSearch && matchesStatus;
  });

  const closeDrawer = useCallback(() => {
    setDrawerKind(null);
    setEditingId(null);
  }, []);

  function openCreate(kind: EditorKind) {
    setDrawerKind(kind);
    setEditingId(null);
    setName("");
    setSlug("");
    setDescription("");
    setParentId("");
    setSupportUrl("");
    setActive(true);
  }

  function openCategory(category: CatalogCategory) {
    setTab("category");
    setDrawerKind("category");
    setEditingId(category.id);
    setName(category.name);
    setSlug(category.slug);
    setDescription(category.description ?? "");
    setParentId(category.parentId ?? "");
    setActive(category.isActive);
  }

  function openBrand(brand: CatalogBrand) {
    setTab("brand");
    setDrawerKind("brand");
    setEditingId(brand.id);
    setName(brand.name);
    setSlug(brand.slug);
    setDescription(brand.description ?? "");
    setSupportUrl(brand.supportUrl ?? "");
    setActive(brand.isActive);
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!drawerKind) return;
    setPending("save");
    try {
      if (drawerKind === "category") {
        const existing = flatCategories.find((category) => category.id === editingId);
        await saveCategory(editingId, {
          name,
          slug: slug.trim() || slugify(name),
          parentId: parentId || null,
          description,
          iconStorageKey: existing?.iconStorageKey ?? undefined,
          sortOrder: existing?.sortOrder ?? 0,
          isActive: active,
          rowVersion: existing?.rowVersion,
        });
      } else {
        const existing = brands.find((brand) => brand.id === editingId);
        await saveBrand(editingId, {
          name,
          slug: slug.trim() || slugify(name),
          description,
          logoStorageKey: existing?.logoStorageKey ?? undefined,
          supportUrl,
          isActive: active,
          rowVersion: existing?.rowVersion,
        });
      }
      onNotice(messages.adminProductSaved);
      closeDrawer();
      await onChanged();
    } catch (error) {
      onError(apiErrorMessage(error, messages));
    } finally {
      setPending(null);
    }
  }

  async function toggleCategory(category: CatalogCategory) {
    setPending(category.id);
    try {
      await saveCategory(category.id, {
        name: category.name,
        slug: category.slug,
        parentId: category.parentId,
        description: category.description ?? undefined,
        iconStorageKey: category.iconStorageKey ?? undefined,
        sortOrder: category.sortOrder,
        isActive: !category.isActive,
        rowVersion: category.rowVersion,
      });
      await onChanged();
    } catch (error) {
      onError(apiErrorMessage(error, messages));
    } finally {
      setPending(null);
    }
  }

  async function toggleBrand(brand: CatalogBrand) {
    setPending(brand.id);
    try {
      await saveBrand(brand.id, {
        name: brand.name,
        slug: brand.slug,
        description: brand.description ?? undefined,
        logoStorageKey: brand.logoStorageKey ?? undefined,
        supportUrl: brand.supportUrl ?? undefined,
        isActive: !brand.isActive,
        rowVersion: brand.rowVersion,
      });
      await onChanged();
    } catch (error) {
      onError(apiErrorMessage(error, messages));
    } finally {
      setPending(null);
    }
  }

  async function uploadImage(kind: EditorKind, id: string, file: File | null) {
    if (!file) return;
    setPending(id);
    try {
      if (kind === "category") await uploadCategoryImage(id, file);
      else await uploadBrandLogo(id, file);
      onNotice(messages.adminProductSaved);
      await onChanged();
    } catch (error) {
      onError(apiErrorMessage(error, messages));
    } finally {
      setPending(null);
    }
  }

  async function removeImage() {
    if (!imageToRemove) return;
    setPending(imageToRemove.id);
    try {
      if (imageToRemove.kind === "category") await deleteCategoryImage(imageToRemove.id);
      else await deleteBrandLogo(imageToRemove.id);
      setImageToRemove(null);
      await onChanged();
    } catch (error) {
      onError(apiErrorMessage(error, messages));
    } finally {
      setPending(null);
    }
  }

  const drawerTitle = drawerKind === "brand"
    ? editingId ? messages.adminEditBrand : messages.adminNewBrand
    : editingId ? messages.adminEditCategory : messages.adminNewCategory;

  return (
    <section id="taxonomy" className="mt-6 scroll-mt-24 rounded-2xl border border-[var(--ds-border)] bg-[var(--ds-surface)]">
      <div className="border-b border-[var(--ds-border)] p-5">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <div>
            <p className="text-xs font-bold uppercase tracking-[0.16em] text-[var(--ds-accent)]">RIGX ADMIN</p>
            <h2 className="mt-1 text-2xl font-black text-[var(--ds-text)]">{messages.adminGroupCatalog}</h2>
          </div>
          <PrimaryButton type="button" onClick={() => openCreate(tab)}>
            + {tab === "category" ? messages.adminNewCategory : messages.adminNewBrand}
          </PrimaryButton>
        </div>
        <div className="mt-5 grid gap-3 md:grid-cols-[auto_minmax(0,1fr)_12rem] md:items-end">
          <div className="inline-flex rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface-muted)] p-1" role="tablist" aria-label={messages.adminGroupCatalog}>
            {(["category", "brand"] as const).map((item) => (
              <button
                key={item}
                type="button"
                role="tab"
                aria-selected={tab === item}
                onClick={() => setTab(item)}
                className={`min-h-9 rounded-lg px-4 text-sm font-bold transition ${tab === item ? "bg-[var(--ds-brand)] text-white" : "text-[var(--ds-text-muted)] hover:bg-[var(--ds-surface)]"}`}
              >
                {item === "category" ? messages.adminCategories : messages.adminBrands}
              </button>
            ))}
          </div>
          <TextField
            label={messages.adminSearchTaxonomy}
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            type="search"
            placeholder={messages.adminSearchTaxonomy}
          />
          <label className="text-sm font-semibold text-[var(--ds-text)]">
            {messages.adminTaxonomyStatus}
            <select value={status} onChange={(event) => setStatus(event.target.value as typeof status)} className="mt-2 min-h-11 w-full rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3">
              <option value="all">{messages.adminAllRecords}</option>
              <option value="active">{messages.adminActive}</option>
              <option value="inactive">{messages.adminInactive}</option>
            </select>
          </label>
        </div>
        <div className="mt-3 flex flex-wrap items-center justify-between gap-2">
          <p className="text-sm text-[var(--ds-text-muted)]">
            {tab === "category" ? visibleCategories.length : visibleBrands.length} {messages.adminResultsCount}
          </p>
          <p className="text-xs text-[var(--ds-text-muted)]">{messages.adminLocalizationPending}</p>
        </div>
      </div>

      <div className="overflow-x-auto">
        {tab === "category" ? (
          <table className="w-full min-w-[52rem] text-start text-sm">
            <caption className="sr-only">{messages.adminCategories}</caption>
            <thead className="bg-[var(--ds-surface-muted)] text-[var(--ds-text-muted)]">
              <tr>
                <th className="p-4 text-start">{messages.adminImage}</th>
                <th className="p-4 text-start">{messages.adminName}</th>
                <th className="p-4 text-start">{messages.adminSlug}</th>
                <th className="p-4 text-start">{messages.adminParentCategory}</th>
                <th className="p-4 text-start">{messages.adminProductsCount}</th>
                <th className="p-4 text-start">{messages.adminTaxonomyStatus}</th>
                <th className="p-4 text-start">{messages.adminActions}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--ds-border)]">
              {visibleCategories.map((category) => (
                <tr key={category.id} className="hover:bg-[var(--ds-surface-muted)]">
                  <td className="p-4">
                    {category.iconStorageKey ? (
                      <img src={catalogMediaUrl(category.iconStorageKey)} alt="" className="h-10 w-10 rounded-lg bg-[var(--ds-surface-muted)] object-contain" />
                    ) : <span className="grid h-10 w-10 place-items-center rounded-lg bg-[var(--ds-surface-muted)] text-xs text-[var(--ds-text-muted)]">—</span>}
                  </td>
                  <td className="p-4 font-bold text-[var(--ds-text)]">
                    <span style={{ paddingInlineStart: `${category.depth * 1.25}rem` }}>
                      {category.depth > 0 ? "↳ " : ""}{category.name}
                    </span>
                  </td>
                  <td className="p-4 font-mono text-xs text-[var(--ds-text-muted)]" dir="ltr">{category.slug}</td>
                  <td className="p-4 text-[var(--ds-text-muted)]">{flatCategories.find((parent) => parent.id === category.parentId)?.name ?? "—"}</td>
                  <td className="p-4"><RecordCount products={products.filter((product) => product.categorySlug === category.slug)} /></td>
                  <td className="p-4"><StatusBadge active={category.isActive} label={category.isActive ? messages.adminActive : messages.adminInactive} /></td>
                  <td className="p-4">
                    <RowActions
                      label={messages.adminActions}
                      onEdit={() => openCategory(category)}
                      editLabel={messages.adminEdit}
                      active={category.isActive}
                      activeLabel={category.isActive ? messages.adminDisable : messages.adminEnable}
                      onToggle={() => void toggleCategory(category)}
                      imageLabel={messages.adminCategoryImage}
                      onImage={(file) => void uploadImage("category", category.id, file)}
                      deleteLabel={messages.adminDeleteCategoryImage}
                      onDeleteImage={category.iconStorageKey ? () => setImageToRemove({ kind: "category", id: category.id }) : undefined}
                      pending={pending === category.id}
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : (
          <table className="w-full min-w-[46rem] text-start text-sm">
            <caption className="sr-only">{messages.adminBrands}</caption>
            <thead className="bg-[var(--ds-surface-muted)] text-[var(--ds-text-muted)]">
              <tr>
                <th className="p-4 text-start">{messages.adminImage}</th>
                <th className="p-4 text-start">{messages.adminName}</th>
                <th className="p-4 text-start">{messages.adminSlug}</th>
                <th className="p-4 text-start">{messages.adminSupportUrl}</th>
                <th className="p-4 text-start">{messages.adminProductsCount}</th>
                <th className="p-4 text-start">{messages.adminTaxonomyStatus}</th>
                <th className="p-4 text-start">{messages.adminActions}</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[var(--ds-border)]">
              {visibleBrands.map((brand) => (
                <tr key={brand.id} className="hover:bg-[var(--ds-surface-muted)]">
                  <td className="p-4">
                    {brand.logoStorageKey ? (
                      <img src={catalogMediaUrl(brand.logoStorageKey)} alt="" className="h-10 w-10 rounded-lg bg-[var(--ds-surface-muted)] object-contain" />
                    ) : <span className="grid h-10 w-10 place-items-center rounded-lg bg-[var(--ds-surface-muted)] text-xs text-[var(--ds-text-muted)]">—</span>}
                  </td>
                  <td className="p-4 font-bold text-[var(--ds-text)]">{brand.name}</td>
                  <td className="p-4 font-mono text-xs text-[var(--ds-text-muted)]" dir="ltr">{brand.slug}</td>
                  <td className="max-w-56 truncate p-4 text-[var(--ds-text-muted)]" dir="ltr">{brand.supportUrl ?? "—"}</td>
                  <td className="p-4"><RecordCount products={products.filter((product) => product.brandName === brand.name)} /></td>
                  <td className="p-4"><StatusBadge active={brand.isActive} label={brand.isActive ? messages.adminActive : messages.adminInactive} /></td>
                  <td className="p-4">
                    <RowActions
                      label={messages.adminActions}
                      onEdit={() => openBrand(brand)}
                      editLabel={messages.adminEdit}
                      active={brand.isActive}
                      activeLabel={brand.isActive ? messages.adminDisable : messages.adminEnable}
                      onToggle={() => void toggleBrand(brand)}
                      imageLabel={messages.adminBrandLogo}
                      onImage={(file) => void uploadImage("brand", brand.id, file)}
                      deleteLabel={messages.adminDeleteBrandLogo}
                      onDeleteImage={brand.logoStorageKey ? () => setImageToRemove({ kind: "brand", id: brand.id }) : undefined}
                      pending={pending === brand.id}
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
        {(tab === "category" ? visibleCategories.length : visibleBrands.length) === 0 ? (
          <p className="p-8 text-center text-sm text-[var(--ds-text-muted)]">{messages.adminNoResults}</p>
        ) : null}
      </div>

      <SideDrawer open={drawerKind !== null} title={drawerTitle} onClose={closeDrawer}>
        <form onSubmit={submit}>
          <FormStack>
            <TextField label={messages.adminName} value={name} onChange={(event) => setName(event.target.value)} onBlur={() => { if (!slug) setSlug(slugify(name)); }} required />
            <TextField label={messages.adminSlug} value={slug} onChange={(event) => setSlug(event.target.value)} hint={messages.adminSlugHelp} dir="ltr" required />
            <TextField label={messages.adminDescription} value={description} onChange={(event) => setDescription(event.target.value)} />
            {drawerKind === "category" ? (
              <label className="text-sm font-semibold text-[var(--ds-text)]">
                {messages.adminParentCategory}
                <select value={parentId} onChange={(event) => setParentId(event.target.value)} className="mt-2 min-h-11 w-full rounded-xl border border-[var(--ds-border)] bg-[var(--ds-surface)] px-3">
                  <option value="">—</option>
                  {flatCategories.filter((category) => category.id !== editingId).map((category) => (
                    <option key={category.id} value={category.id}>{"—".repeat(category.depth)} {category.name}</option>
                  ))}
                </select>
                <span className="mt-1 block text-xs font-normal text-[var(--ds-text-muted)]">{messages.adminCategoryParentHelp}</span>
              </label>
            ) : (
              <TextField
                label={messages.adminSupportUrl}
                value={supportUrl}
                onChange={(event) => setSupportUrl(event.target.value)}
                hint={messages.adminSupportUrlHelp}
                dir="ltr"
                type="url"
              />
            )}
            <label className="flex min-h-11 items-center gap-2 text-sm font-semibold text-[var(--ds-text)]">
              <input type="checkbox" checked={active} onChange={(event) => setActive(event.target.checked)} className="h-4 w-4 rounded border-[var(--ds-border)]" />
              {messages.adminActive}
            </label>
            <div className="flex flex-col-reverse gap-2 pt-2 sm:flex-row sm:justify-end">
              <SecondaryButton type="button" onClick={closeDrawer}>{messages.adminCancel}</SecondaryButton>
              <PrimaryButton type="submit" pending={pending === "save"}>{messages.adminSaveChanges}</PrimaryButton>
            </div>
          </FormStack>
        </form>
      </SideDrawer>

      <ConfirmDialog
        open={imageToRemove !== null}
        title={messages.adminRemove}
        description={messages.adminConfirmTaxonomyImage}
        confirmLabel={messages.adminRemove}
        cancelLabel={messages.adminCancel}
        pending={imageToRemove !== null && pending === imageToRemove.id}
        onConfirm={() => void removeImage()}
        onCancel={() => setImageToRemove(null)}
      />
    </section>
  );
}

function flatten(categories: CatalogCategory[], depth = 0): FlatCategory[] {
  return categories.flatMap((category) => [
    { ...category, depth },
    ...flatten(category.children, depth + 1),
  ]);
}

function slugify(value: string): string {
  return value
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}

function RecordCount({ products }: { products: CatalogProductSummary[] }) {
  const published = products.filter((product) => product.status === "Published").length;
  return <span className="font-bold text-[var(--ds-text)]">{products.length} <span className="font-normal text-[var(--ds-text-muted)]">({published})</span></span>;
}

function StatusBadge({ active, label }: { active: boolean; label: string }) {
  return (
    <span className={`inline-flex rounded-full border px-2.5 py-1 text-xs font-bold ${
      active ? "border-emerald-200 bg-emerald-50 text-emerald-800" : "border-amber-200 bg-amber-50 text-amber-800"
    }`}>
      {label}
    </span>
  );
}

function RowActions({
  label,
  onEdit,
  editLabel,
  active,
  activeLabel,
  onToggle,
  imageLabel,
  onImage,
  deleteLabel,
  onDeleteImage,
  pending,
}: {
  label: string;
  onEdit: () => void;
  editLabel: string;
  active: boolean;
  activeLabel: string;
  onToggle: () => void;
  imageLabel: string;
  onImage: (file: File | null) => void;
  deleteLabel: string;
  onDeleteImage?: () => void;
  pending: boolean;
}) {
  const inputRef = useRef<HTMLInputElement | null>(null);

  return (
    <>
      <RowActionMenu
        label={label}
        actions={[
          { key: "edit", label: editLabel, icon: "settings", onSelect: onEdit },
          {
            key: "toggle",
            label: activeLabel,
            icon: active ? "eyeOff" : "check",
            onSelect: onToggle,
            disabled: pending,
          },
          {
            key: "image",
            label: imageLabel,
            icon: "image",
            onSelect: () => inputRef.current?.click(),
            disabled: pending,
          },
          ...(onDeleteImage
            ? [{
                key: "delete-image",
                label: deleteLabel,
                icon: "trash" as const,
                onSelect: onDeleteImage,
                disabled: pending,
                tone: "danger" as const,
              }]
            : []),
        ]}
      />
      <input
        ref={inputRef}
        type="file"
        accept="image/*"
        className="sr-only"
        tabIndex={-1}
        disabled={pending}
        onChange={(event) => {
          onImage(event.target.files?.[0] ?? null);
          event.target.value = "";
        }}
      />
    </>
  );
}
