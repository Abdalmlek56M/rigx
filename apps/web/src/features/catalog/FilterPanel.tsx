import { getLocale, getTranslations } from "next-intl/server";
import { Link, getPathname } from "@/i18n/navigation";
import type { Locale } from "@/i18n/routing";
import { categoryPath, effectiveSpecs, type CategoryNode } from "@/lib/categories";
import { CONDITIONS } from "@/lib/condition";
import type { Brand } from "@/lib/storefront";
import { catalogHref, toSearchParams, type CatalogState } from "./catalog-query";
import { CheckIcon } from "@/components/ui/icons";

const linkRow = "flex min-h-10 items-center rounded-control px-3 text-[13px] transition-colors duration-150 hover:bg-paper motion-reduce:transition-none";
const legend = "mb-2 text-sm font-bold";
const fieldset = "m-0 flex flex-col gap-1 border-0 p-0";
const numberInput = "h-11 w-full rounded-control border border-input-line bg-surface px-2.5";

interface FilterPanelProps {
  state: CatalogState;
  tree: CategoryNode[];
  brands: Brand[];
  /** Id of the selected category (spec filters come from it). */
  categoryId: string | null;
  showSoldOut: boolean;
  /** Keeps the two copies (sidebar and mobile sheet) from sharing input ids. */
  idPrefix: string;
}

function CategoryList({ nodes, state, openIds }: { nodes: CategoryNode[]; state: CatalogState; openIds: ReadonlySet<string> }) {
  return (
    <ul className="m-0 flex list-none flex-col gap-0.5 p-0 text-sm">
      {nodes.map((node) => {
        const selected = state.category === node.slug;
        return (
          <li key={node.id}>
            <Link
              href={catalogHref(state, { category: selected ? null : node.slug })}
              aria-current={selected ? "true" : undefined}
              className={`flex min-h-10 items-center rounded-control px-2 hover:bg-paper ${openIds.has(node.id) ? "font-bold" : ""} ${selected ? "bg-paper" : ""}`}
            >
              {node.name}
            </Link>
            {openIds.has(node.id) && node.children.length > 0 ? (
              <div className="ms-2 border-s border-line ps-3">
                <CategoryList nodes={node.children} state={state} openIds={openIds} />
              </div>
            ) : null}
          </li>
        );
      })}
    </ul>
  );
}

function CheckBox({ checked }: { checked: boolean }) {
  return (
    <span
      aria-hidden="true"
      className={`flex h-[18px] w-[18px] shrink-0 items-center justify-center rounded-sm border text-xs transition-colors duration-150 ${checked ? "border-carbon bg-carbon text-on-carbon" : "border-input-line"} motion-reduce:transition-none`}
    >
      {checked ? <CheckIcon size={13} /> : null}
    </span>
  );
}

/** The filter sidebar: condition, category tree, brands, spec chips of the chosen category, price range and availability. Plain links and a GET form, so it works without scripts. */
export async function FilterPanel({ state, tree, brands, categoryId, showSoldOut, idPrefix }: FilterPanelProps) {
  const t = await getTranslations("catalog.filters");
  const tc = await getTranslations("common.condition");
  const locale = (await getLocale()) as Locale;
  const openIds = new Set(categoryId ? categoryPath(tree, categoryId).map((node) => node.id) : []);
  const specs = categoryId ? effectiveSpecs(tree, categoryId).filter((spec) => spec.isFilter && spec.options.length > 0) : [];
  const hidden = [...toSearchParams(state).entries()].filter(([name]) => name !== "min" && name !== "max" && name !== "page");
  const stocks = [
    { value: "all", label: t("allStock") },
    { value: "in", label: t("inStock") }
  ] as const;

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <span className="text-[17px] font-bold">{t("title")}</span>
        <Link href="/products" className="text-[13px] text-orange-text underline hover:text-orange-text-hover">{t("clearAll")}</Link>
      </div>

      <fieldset className={fieldset}>
        <legend className={legend}>{t("condition")}</legend>
        {CONDITIONS.map((condition) => {
          const selected = state.condition === condition;
          return (
            <Link
              key={condition}
              href={catalogHref(state, { condition: selected ? null : condition })}
              aria-current={selected ? "true" : undefined}
              className={`${linkRow} gap-2.5 ${selected ? "bg-paper font-bold" : ""}`}
            >
              <CheckBox checked={selected} />
              {tc(condition)}
            </Link>
          );
        })}
      </fieldset>

      {tree.length > 0 ? (
        <fieldset className={fieldset}>
          <legend className={legend}>{t("category")}</legend>
          <CategoryList nodes={tree} state={state} openIds={openIds} />
        </fieldset>
      ) : null}

      {brands.length > 0 ? (
        <fieldset className={fieldset}>
          <legend className={legend}>{t("brand")}</legend>
          <ul className="m-0 flex max-h-60 list-none flex-col gap-0.5 overflow-y-auto overscroll-contain p-0">
            {brands.map((brand) => {
              const selected = state.brand === brand.slug;
              return (
                <li key={brand.id}>
                  <Link
                    href={catalogHref(state, { brand: selected ? null : brand.slug })}
                    aria-current={selected ? "true" : undefined}
                    className={`${linkRow} gap-2.5 ${selected ? "bg-paper font-bold" : ""}`}
                  >
                    <CheckBox checked={selected} />
                    <span dir="ltr">{brand.name}</span>
                  </Link>
                </li>
              );
            })}
          </ul>
        </fieldset>
      ) : null}

      {specs.map((spec) => {
        const rest = Object.fromEntries(Object.entries(state.specs).filter(([key]) => key !== spec.key));
        return (
          <fieldset key={spec.key} className={fieldset}>
            <legend className={legend}>{spec.name}</legend>
            <div className="flex flex-wrap gap-1.5">
              {spec.options.map((option) => {
                const selected = state.specs[spec.key] === option;
                return (
                  <Link
                    key={option}
                    href={catalogHref(state, { specs: selected ? rest : { ...rest, [spec.key]: option } })}
                    aria-current={selected ? "true" : undefined}
                    dir="ltr"
                    className={`flex h-10 items-center rounded-control px-3 font-mono text-[13px] transition-colors duration-150 motion-reduce:transition-none ${selected ? "bg-carbon text-on-carbon" : "border border-input-line bg-surface hover:bg-paper"}`}
                  >
                    {option}
                  </Link>
                );
              })}
            </div>
          </fieldset>
        );
      })}

      <form action={getPathname({ href: "/products", locale })} method="get" className={fieldset}>
        {hidden.map(([name, value]) => (
          <input key={`${name}=${value}`} type="hidden" name={name} value={value} />
        ))}
        <fieldset className={fieldset}>
          <legend className={legend}>{t("price")}</legend>
          <div className="flex items-center gap-2">
            <label htmlFor={`${idPrefix}-min`} className="sr-only">{t("min")}</label>
            <input id={`${idPrefix}-min`} name="min" type="number" min={0} inputMode="numeric" placeholder={t("min")} defaultValue={state.min ?? ""} className={numberInput} />
            <span aria-hidden="true">–</span>
            <label htmlFor={`${idPrefix}-max`} className="sr-only">{t("max")}</label>
            <input id={`${idPrefix}-max`} name="max" type="number" min={0} inputMode="numeric" placeholder={t("max")} defaultValue={state.max ?? ""} className={numberInput} />
          </div>
          <button type="submit" className="mt-1 h-11 rounded-control border border-input-line bg-surface text-sm font-bold">{t("apply")}</button>
        </fieldset>
      </form>

      {showSoldOut ? (
        <fieldset className={fieldset}>
          <legend className={legend}>{t("availability")}</legend>
          <div className="flex gap-1 rounded-card bg-paper p-1">
            {stocks.map((stock) => {
              const selected = state.stock === stock.value;
              return (
                <Link
                  key={stock.value}
                  href={catalogHref(state, { stock: stock.value })}
                  aria-current={selected ? "true" : undefined}
                  className={`flex h-10 flex-1 items-center justify-center rounded-control text-[13px] transition-colors duration-150 motion-reduce:transition-none ${selected ? "bg-carbon font-bold text-on-carbon" : "hover:bg-surface"}`}
                >
                  {stock.label}
                </Link>
              );
            })}
          </div>
        </fieldset>
      ) : null}
    </div>
  );
}
