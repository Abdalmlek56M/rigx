"use client";

import { useCallback, useMemo, useState } from "react";
import { useTranslations } from "next-intl";
import { Link } from "@/i18n/navigation";
import { useLoad, type Load } from "@/lib/hooks/use-load";
import { fetchBrands } from "../brands/brands-api";
import { fetchCategories } from "../categories/categories-api";
import { parentOptions } from "../categories/category-rules";
import { fetchPartners } from "../expenses/expenses-api";
import { FilterBar, FilterSelect, SearchBox } from "../kit/FilterBar";
import { DataTable } from "../kit/DataTable";
import { PageHeader } from "../kit/PageHeader";
import { Pager } from "../kit/Pager";
import { useToast } from "../kit/Toast";
import { pageNumber } from "../kit/url-filters";
import { useReveal } from "../kit/use-reveal";
import { useUrlFilters } from "../kit/use-url-filters";
import { usePermissions } from "../use-permissions";
import { InventoryDetail } from "./InventoryDetail";
import { fetchStockItems, setItemsHidden, type StockItem } from "./inventory-api";
import { groupByBuild, pickedIds, triState, type StockRow } from "./inventory-rules";
import { useInventoryColumns } from "./use-inventory-columns";
import { BanIcon, CheckIcon, EyeIcon, PlusIcon } from "@/components/ui/icons";
import { Button, buttonClass, buttonIconSize } from "../kit/Button";

const FILTERS = { q: "", category: "", condition: "", stock: "", hidden: "", item: "", page: "1" };

/** ADM-Inventory: the stock list with filters and bulk hide/show, and the picked item (edit, photos, units) below it. */
export function Inventory() {
  const t = useTranslations("admin.inventory");
  const toast = useToast();
  const { has } = usePermissions();
  const { values, set } = useUrlFilters(FILTERS);
  const page = pageNumber(values.page);
  const canSeePartners = has("Finance.ViewReports");
  const [selected, setSelected] = useState<ReadonlySet<string>>(new Set());
  const [busy, setBusy] = useState(false);
  const [collapsed, setCollapsed] = useState<ReadonlySet<string>>(new Set());
  const detailRef = useReveal<HTMLDivElement>(values.item || null);

  const lookups = useLoad(useCallback(async () => {
    const [tree, brands, partners] = await Promise.all([
      fetchCategories().catch(() => []),
      fetchBrands().catch(() => []),
      canSeePartners ? fetchPartners().catch(() => []) : Promise.resolve([])
    ]);
    return { tree, brands, partners };
  }, [canSeePartners]));
  const tree = useMemo(() => (lookups.state.status === "ready" ? lookups.state.data.tree : []), [lookups.state]);
  const partners = useMemo(() => (lookups.state.status === "ready" ? lookups.state.data.partners : []), [lookups.state]);
  const brands = useMemo(() => (lookups.state.status === "ready" ? lookups.state.data.brands : []), [lookups.state]);

  const { state, reload } = useLoad(useCallback(() => fetchStockItems({
    search: values.q || undefined,
    categoryId: values.category || undefined,
    condition: values.condition || undefined,
    inStock: triState(values.stock),
    hidden: triState(values.hidden),
    page
  }), [values.q, values.category, values.condition, values.stock, values.hidden, page]));

  const items = useMemo<readonly StockItem[]>(() => (state.status === "ready" ? state.data.items : []), [state]);
  const picked = pickedIds(selected, items);
  const rows = useMemo(() => groupByBuild(items, collapsed), [items, collapsed]);
  const tableState: Load<readonly StockRow[]> = state.status === "ready" ? { status: "ready", data: rows } : state;

  const columns = useInventoryColumns({
    tree, partners, selected, activeId: values.item,
    onToggle: (id) => setSelected((current) => {
      const next = new Set(current);
      if (!next.delete(id)) next.add(id);
      return next;
    }),
    onOpen: (id) => set({ item: id, page: String(page) }),
    onToggleBuild: (id) => setCollapsed((current) => {
      const next = new Set(current);
      if (!next.delete(id)) next.add(id);
      return next;
    })
  });

  async function setHidden(hidden: boolean) {
    setBusy(true);
    try {
      await setItemsHidden(picked, hidden);
      toast(t(hidden ? "bulk.hiddenDone" : "bulk.shownDone", { count: picked.length }));
      setSelected(new Set());
      reload();
    } catch {
      toast(t("bulk.failed"), "error");
    } finally {
      setBusy(false);
    }
  }

  const categoryOptions = parentOptions(tree, null).map((option) => ({ value: option.id, label: `${"— ".repeat(option.depth)}${option.name}` }));
  const triOptions = (key: "stock" | "hidden") => [
    { value: "", label: t(`filters.${key}.all`) },
    { value: "true", label: t(`filters.${key}.yes`) },
    { value: "false", label: t(`filters.${key}.no`) }
  ];

  return (
    <>
      <PageHeader
        title={t("title")}
        actions={
          <>
            {has("Purchasing.CreateOrder") ? <Link href="/admin/purchases/new" className={buttonClass()}><PlusIcon size={buttonIconSize()} />{t("newPurchase")}</Link> : null}
            {has("Catalog.ManageProducts") ? <Link href="/admin/builds?new=1" className={buttonClass()}><PlusIcon size={buttonIconSize()} />{t("newBuild")}</Link> : null}
          </>
        }
      />
      <FilterBar>
        <SearchBox label={t("search")} value={values.q} onChange={(q) => set({ q })} />
        <FilterSelect label={t("filters.category")} value={values.category} onChange={(category) => set({ category })} options={[{ value: "", label: t("filters.allCategories") }, ...categoryOptions]} />
        <FilterSelect
          label={t("filters.condition")} value={values.condition} onChange={(condition) => set({ condition })}
          options={[{ value: "", label: t("filters.allConditions") }, { value: "New", label: t("filters.new") }, { value: "Used", label: t("filters.used") }]}
        />
        <FilterSelect label={t("filters.stock.label")} value={values.stock} onChange={(stock) => set({ stock })} options={triOptions("stock")} />
        <FilterSelect label={t("filters.hidden.label")} value={values.hidden} onChange={(hidden) => set({ hidden })} options={triOptions("hidden")} />
      </FilterBar>
      {has("Inventory.StockAdjust") ? (
        <div className="flex flex-wrap items-center gap-2 text-sm">
          <Button icon={CheckIcon} disabled={items.length === 0} onClick={() => setSelected(new Set(items.map((item) => item.id)))}>{t("bulk.selectAll")}</Button>
          <span aria-live="polite" className="font-bold">{t("bulk.picked", { count: picked.length })}</span>
          <Button icon={BanIcon} loading={busy} disabled={picked.length === 0} onClick={() => void setHidden(true)}>{t("bulk.hide")}</Button>
          <Button icon={EyeIcon} loading={busy} disabled={picked.length === 0} onClick={() => void setHidden(false)}>{t("bulk.show")}</Button>
        </div>
      ) : null}
      <DataTable label={t("title")} columns={columns} state={tableState} onRetry={reload} rowKey={(row) => row.key} empty={t("empty")} />
      {state.status === "ready" ? (
        <Pager info={{ page: state.data.page, pageSize: state.data.pageSize, total: state.data.total }} onPage={(next) => set({ page: String(next) })} />
      ) : null}
      <p className="m-0 text-sm text-ink-muted">{t("hiddenNote")}</p>
      <div ref={detailRef} className="scroll-mt-20 empty:hidden">
        {values.item ? <InventoryDetail key={values.item} id={values.item} tree={tree} brands={brands} partners={partners} onClose={() => set({ item: "", page: String(page) })} onChanged={reload} /> : null}
      </div>
    </>
  );
}
