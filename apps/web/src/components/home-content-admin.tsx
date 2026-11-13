"use client";

import Link from "next/link";
import { useEffect, useMemo, useState } from "react";
import {
  getCatalogBrands,
  getCatalogCategories,
  getCatalogProducts,
  getHomepageDraft,
  publishHomepage,
  saveHomepageDraft,
  type CatalogBrand,
  type CatalogCategory,
  type CatalogProductSummary,
} from "@/lib/api-client";
import { apiErrorMessage } from "@/lib/form-errors";
import { useLocale } from "@/lib/i18n";
import { localizedCategoryName } from "@/lib/category-labels";
import { Icon } from "@/components/icons";
import { useHomeLayout } from "@/components/home-layout-provider";
import {
  arrangeRailItems,
  parseHomeLayout,
  type HomeRailConfig,
  type HomeRailContentType,
  type HomeRailId,
  type HomeSectionId,
} from "@/lib/home-layout";
import { LoadingCard, Message, PrimaryButton, SecondaryButton } from "@/components/ui";

type ContentItem = { id: string; name: string };

export function HomeContentAdmin() {
  const { messages, locale } = useLocale();
  const { layout, ready, updateLayout, resetLayout, loadLayout } = useHomeLayout();
  const [categories, setCategories] = useState<CatalogCategory[]>([]);
  const [brands, setBrands] = useState<CatalogBrand[]>([]);
  const [products, setProducts] = useState<CatalogProductSummary[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [rowVersion, setRowVersion] = useState(0);
  const [saving, setSaving] = useState(false);
  const [publishing, setPublishing] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    setLoading(true);
    void Promise.all([
      getCatalogCategories(),
      getCatalogBrands(),
      getCatalogProducts({ pageSize: 100, sort: "newest" }),
      getHomepageDraft(),
    ])
      .then(([nextCategories, nextBrands, nextProducts, draft]) => {
        if (!active) return;
        setCategories(nextCategories);
        setBrands(nextBrands);
        setProducts(nextProducts.items);
        loadLayout(parseHomeLayout(draft.layoutJson));
        setRowVersion(draft.rowVersion);
        setError(null);
      })
      .catch((requestError) => {
        if (active) setError(apiErrorMessage(requestError, messages));
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [loadLayout, messages]);

  async function persistDraft(): Promise<number | null> {
    setSaving(true);
    setError(null);
    setNotice(null);
    try {
      const saved = await saveHomepageDraft(JSON.stringify(layout), rowVersion);
      setRowVersion(saved.rowVersion);
      setNotice(messages.homeContentDraftSaved);
      return saved.rowVersion;
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
      return null;
    } finally {
      setSaving(false);
    }
  }

  async function publish() {
    setPublishing(true);
    setError(null);
    setNotice(null);
    try {
      const saved = await saveHomepageDraft(JSON.stringify(layout), rowVersion);
      setRowVersion(saved.rowVersion);
      const published = await publishHomepage(saved.rowVersion);
      setRowVersion(published.rowVersion);
      setNotice(messages.homeContentPublished);
    } catch (requestError) {
      setError(apiErrorMessage(requestError, messages));
    } finally {
      setPublishing(false);
    }
  }

  const rootCategories = useMemo(
    () => categories.filter((category) => category.parentId === null && category.isActive && category.isEffectivelyActive),
    [categories],
  );
  const activeBrands = useMemo(() => brands.filter((brand) => brand.isActive), [brands]);

  const sectionLabels: Record<HomeSectionId, string> = {
    hero: messages.homeContentSectionHero,
    brands: messages.homeContentSectionBrands,
    categories: messages.homeContentSectionCategories,
    offers: messages.homeContentSectionOffers,
    discovery: messages.homeContentSectionDiscovery,
    guide: messages.homeContentSectionGuide,
    latest: messages.homeContentSectionLatest,
    closing: messages.homeContentSectionClosing,
  };

  function updateRail(id: HomeRailId, patch: Partial<HomeRailConfig>) {
    updateLayout((current) => ({
      ...current,
      rails: { ...current.rails, [id]: { ...current.rails[id], ...patch } },
    }));
  }

  function moveSection(index: number, direction: -1 | 1) {
    updateLayout((current) => {
      const target = index + direction;
      if (target < 0 || target >= current.sections.length) return current;
      const sections = [...current.sections];
      [sections[index], sections[target]] = [sections[target], sections[index]];
      return { ...current, sections };
    });
  }

  function toggleSection(index: number) {
    updateLayout((current) => ({
      ...current,
      sections: current.sections.map((section, sectionIndex) =>
        sectionIndex === index ? { ...section, visible: !section.visible } : section),
    }));
  }

  function sourceItems(contentType: HomeRailContentType): ContentItem[] {
    if (contentType === "brands") return activeBrands.map((brand) => ({ id: brand.id, name: brand.name }));
    if (contentType === "categories") {
      return rootCategories.map((category) => ({
        id: category.id,
        name: localizedCategoryName(category.slug, category.name, locale),
      }));
    }
    return products.map((product) => ({ id: product.id, name: product.name }));
  }

  function orderedSourceItems(config: HomeRailConfig): ContentItem[] {
    const items = sourceItems(config.contentType);
    const order = new Map(config.itemOrder.map((id, index) => [id, index]));
    return [...items].sort((left, right) =>
      (order.get(left.id) ?? Number.MAX_SAFE_INTEGER) - (order.get(right.id) ?? Number.MAX_SAFE_INTEGER));
  }

  function moveItem(railId: HomeRailId, config: HomeRailConfig, index: number, direction: -1 | 1) {
    const ordered = orderedSourceItems(config);
    const target = index + direction;
    if (target < 0 || target >= ordered.length) return;
    const next = [...ordered];
    [next[index], next[target]] = [next[target], next[index]];
    updateRail(railId, { itemOrder: next.map((item) => item.id) });
  }

  function toggleItemHidden(railId: HomeRailId, config: HomeRailConfig, id: string) {
    const hidden = new Set(config.hiddenItemIds);
    if (hidden.has(id)) hidden.delete(id);
    else hidden.add(id);
    updateRail(railId, { hiddenItemIds: Array.from(hidden) });
  }

  if (!ready || loading) return <LoadingCard />;
  if (error) {
    return (
      <main className="mx-auto w-full max-w-5xl px-5 py-10">
        <Message tone="error">{error}</Message>
      </main>
    );
  }

  const railEditors: Array<{ id: HomeRailId; label: string }> = [
    { id: "brands", label: sectionLabels.brands },
    { id: "categories", label: sectionLabels.categories },
  ];

  return (
    <main className="rigx-home-admin mx-auto w-full max-w-6xl px-4 py-8 sm:px-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <p className="text-xs font-bold uppercase tracking-[0.16em] text-[var(--ds-accent)]">{messages.homeContentEyebrow}</p>
          <h1 className="mt-1 text-2xl font-black text-[var(--ds-text)] sm:text-3xl">{messages.homeContentTitle}</h1>
          <p className="mt-2 max-w-2xl text-sm leading-6 text-[var(--ds-text-muted)]">{messages.homeContentLead}</p>
        </div>
        <div className="flex flex-wrap gap-2">
          <Link href="/" target="_blank" className="rigx-button rigx-button-secondary">
            <Icon name="store" className="h-4 w-4" />
            {messages.homeContentOpenStorefront}
          </Link>
          <SecondaryButton type="button" onClick={resetLayout}>
            {messages.homeContentReset}
          </SecondaryButton>
          <SecondaryButton type="button" onClick={() => void persistDraft()} disabled={saving || publishing}>
            {messages.homeContentSaveDraft}
          </SecondaryButton>
          <PrimaryButton type="button" onClick={() => void publish()} pending={publishing} disabled={saving}>
            {messages.homeContentPublish}
          </PrimaryButton>
        </div>
      </div>

      <div className="mt-5 grid gap-3">
        {notice ? <Message tone="success">{notice}</Message> : null}
        <Message tone="info">
          <strong>{messages.homeContentDemoBadge}</strong> · {messages.homeContentBackendNow}
        </Message>
      </div>

      <div className="mt-6 grid gap-5 lg:grid-cols-[minmax(0,1.15fr)_minmax(18rem,0.85fr)]">
        <section className="rigx-admin-card">
          <div className="rigx-admin-card-head">
            <div>
              <h2>{messages.homeContentSections}</h2>
              <p>{messages.homeContentOrderHint}</p>
            </div>
          </div>
          <ol className="rigx-home-section-list">
            {layout.sections.map((section, index) => (
              <li key={section.id} data-hidden={section.visible ? "false" : "true"}>
                <span className="rigx-home-section-index" dir="ltr">{String(index + 1).padStart(2, "0")}</span>
                <span className="rigx-home-section-name">
                  <strong>{sectionLabels[section.id]}</strong>
                  <small>{section.visible ? messages.homeContentShow : messages.homeContentHide}</small>
                </span>
                <span className="rigx-home-section-actions">
                  <button type="button" onClick={() => moveSection(index, -1)} disabled={index === 0} aria-label={`${messages.homeContentMoveUp}: ${sectionLabels[section.id]}`} title={messages.homeContentMoveUp}>
                    <Icon name="chevron" className="h-4 w-4 -rotate-90" />
                  </button>
                  <button type="button" onClick={() => moveSection(index, 1)} disabled={index === layout.sections.length - 1} aria-label={`${messages.homeContentMoveDown}: ${sectionLabels[section.id]}`} title={messages.homeContentMoveDown}>
                    <Icon name="chevron" className="h-4 w-4 rotate-90" />
                  </button>
                  <button type="button" onClick={() => toggleSection(index)} aria-pressed={section.visible} className="rigx-home-visibility">
                    <Icon name={section.visible ? "eye" : "eyeOff"} className="h-4 w-4" />
                    {section.visible ? messages.homeContentHide : messages.homeContentShow}
                  </button>
                </span>
              </li>
            ))}
          </ol>
        </section>

        <section className="rigx-admin-card">
          <div className="rigx-admin-card-head">
            <div>
              <h2>{messages.homeContentPreview}</h2>
              <p>{messages.homeContentSaved}</p>
            </div>
          </div>
          <ol className="rigx-home-preview-list">
            {layout.sections.filter((section) => section.visible).map((section) => (
              <li key={section.id}>{sectionLabels[section.id]}</li>
            ))}
          </ol>
          <p className="mt-4 text-xs leading-5 text-[var(--ds-text-muted)]">{messages.homeContentBackendNote}</p>
        </section>
      </div>

      <div className="mt-6 grid gap-5">
        {railEditors.map(({ id, label }) => {
          const config = layout.rails[id];
          const ordered = orderedSourceItems(config);
          const visibleCount = arrangeRailItems(sourceItems(config.contentType).map((item) => ({ ...item })), config).length;
          return (
            <section key={id} className="rigx-admin-card">
              <div className="rigx-admin-card-head">
                <div>
                  <p className="rigx-admin-card-kicker">{messages.homeContentRailSettings}</p>
                  <h2>{label}</h2>
                </div>
                <span className="rigx-admin-count">{visibleCount} / {config.limit}</span>
              </div>

              <div className="grid gap-4 md:grid-cols-2">
                <label className="grid gap-1.5 text-sm font-semibold text-[var(--ds-text)]">
                  {messages.homeContentRailTitle}
                  <input
                    value={config.title}
                    onChange={(event) => updateRail(id, { title: event.target.value })}
                    className="rigx-admin-input"
                    placeholder={id === "brands" ? messages.featuredBrands : messages.shopByCategory}
                  />
                </label>
                <label className="grid gap-1.5 text-sm font-semibold text-[var(--ds-text)]">
                  {messages.homeContentContentType}
                  <select
                    value={config.contentType}
                    onChange={(event) => updateRail(id, { contentType: event.target.value as HomeRailContentType, itemOrder: [], hiddenItemIds: [] })}
                    className="rigx-admin-input"
                  >
                    <option value="brands">{messages.homeContentTypeBrands}</option>
                    <option value="categories">{messages.homeContentTypeCategories}</option>
                    <option value="products">{messages.homeContentTypeProducts}</option>
                  </select>
                </label>
                <label className="grid gap-1.5 text-sm font-semibold text-[var(--ds-text)] md:col-span-2">
                  {messages.homeContentRailLead}
                  <textarea
                    value={config.lead}
                    onChange={(event) => updateRail(id, { lead: event.target.value })}
                    rows={2}
                    className="rigx-admin-input"
                    placeholder={id === "brands" ? messages.footerDepartments : messages.shopByCategory}
                  />
                </label>
                <label className="grid gap-1.5 text-sm font-semibold text-[var(--ds-text)]">
                  {messages.homeContentLimit}
                  <input
                    type="number"
                    min={1}
                    max={24}
                    value={config.limit}
                    onChange={(event) => updateRail(id, { limit: Math.min(24, Math.max(1, Number(event.target.value) || 1)) })}
                    className="rigx-admin-input"
                  />
                </label>
              </div>

              <div className="mt-5">
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <h3 className="text-sm font-bold text-[var(--ds-text)]">{messages.homeContentItems}</h3>
                  <p className="text-xs text-[var(--ds-text-muted)]">{messages.homeContentItemsHint}</p>
                </div>
                {ordered.length === 0 ? (
                  <p className="mt-3 text-sm text-[var(--ds-text-muted)]">{messages.homeContentNoItems}</p>
                ) : (
                  <ol className="rigx-home-item-list">
                    {ordered.map((item, index) => {
                      const hidden = config.hiddenItemIds.includes(item.id);
                      return (
                        <li key={item.id} data-hidden={hidden ? "true" : "false"}>
                          <span className="rigx-home-item-index" dir="ltr">{String(index + 1).padStart(2, "0")}</span>
                          <span className="min-w-0 flex-1 truncate text-sm font-semibold text-[var(--ds-text)]">{item.name}</span>
                          <button type="button" onClick={() => moveItem(id, config, index, -1)} disabled={index === 0} aria-label={`${messages.homeContentMoveUp}: ${item.name}`} title={messages.homeContentMoveUp}>
                            <Icon name="chevron" className="h-4 w-4 -rotate-90" />
                          </button>
                          <button type="button" onClick={() => moveItem(id, config, index, 1)} disabled={index === ordered.length - 1} aria-label={`${messages.homeContentMoveDown}: ${item.name}`} title={messages.homeContentMoveDown}>
                            <Icon name="chevron" className="h-4 w-4 rotate-90" />
                          </button>
                          <button type="button" onClick={() => toggleItemHidden(id, config, item.id)} aria-pressed={!hidden} aria-label={`${hidden ? messages.homeContentShow : messages.homeContentHide}: ${item.name}`} title={hidden ? messages.homeContentShow : messages.homeContentHide}>
                            <Icon name={hidden ? "eyeOff" : "eye"} className="h-4 w-4" />
                          </button>
                        </li>
                      );
                    })}
                  </ol>
                )}
              </div>
            </section>
          );
        })}
      </div>
    </main>
  );
}
