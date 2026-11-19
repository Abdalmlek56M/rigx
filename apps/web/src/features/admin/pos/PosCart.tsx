"use client";

import { Fragment } from "react";
import { useLocale, useTranslations } from "next-intl";
import { ConditionBadge } from "@/components/ui/ConditionBadge";
import { formatEgp } from "@/lib/format";
import { FormSection } from "@/features/checkout/form-parts";
import { MoneyInput, usePosError } from "./pos-parts";
import {
  moneyValid, PRICE_OPTIONS, shownPrice, type BuildLine, type CartLine, type ItemLine, type PosField, type PriceOption
} from "./pos-rules";
import { CloseIcon } from "@/components/ui/icons";
import { IconButton } from "../kit/Button";

const condition = (value: string) => (value === "New" ? "New" : "Used");
const CELL = "px-2 py-2 align-middle";
const COLUMNS = ["item", "condition", "warranty", "option", "quantity", "shown", "sale"] as const;

interface LineProps<L extends CartLine> {
  line: L;
  below: ReadonlySet<string>;
  onChange: (line: L) => void;
  onRemove: () => void;
}

function RemoveButton({ name, onRemove }: { name: string; onRemove: () => void }) {
  const t = useTranslations("admin.pos.cart");
  return <IconButton icon={CloseIcon} label={t("remove", { name })} variant="secondary" onClick={onRemove} />;
}

/** The sale price input of a line, with the below-cost mark under it. */
function SalePrice({ label, value, below, onChange }: { label: string; value: string; below: boolean; onChange: (value: string) => void }) {
  const t = useTranslations("admin.pos.cart");
  return (
    <div className="flex flex-col gap-0.5">
      <MoneyInput label={label} value={value} onChange={onChange} invalid={!moneyValid(value)} />
      {below ? <span className="text-xs font-bold text-error">{t("belowCost")}</span> : null}
    </div>
  );
}

function NameCell({ name, code }: { name: string; code: string }) {
  return (
    <td className={CELL}>
      <span dir="auto" className="block text-sm font-bold [unicode-bidi:isolate]">{name}</span>
      <span dir="ltr" className="block text-start font-mono text-xs text-ink-muted">{code}</span>
    </td>
  );
}

function ItemRow({ line, below, onChange, onRemove }: LineProps<ItemLine>) {
  const t = useTranslations("admin.pos");
  const locale = useLocale();
  const { pick } = line;
  const isBelow = below.has(pick.code);
  const setOption = (priceOption: PriceOption) => onChange({ ...line, priceOption, salePrice: String(shownPrice(pick, priceOption)) });
  const setQuantity = (value: string) => onChange({ ...line, quantity: Math.min(pick.available, Math.max(1, Math.trunc(Number(value)) || 1)) });
  return (
    <tr className={isBelow ? "bg-error/10" : undefined}>
      <NameCell name={pick.name} code={pick.code} />
      <td className={CELL}><ConditionBadge condition={condition(pick.condition)} /></td>
      <td className={`${CELL} text-sm`}>{pick.warrantyLabel ?? t("cart.noWarranty")}</td>
      <td className={CELL}>
        <select
          aria-label={t("cart.columns.option")} value={line.priceOption} onChange={(event) => setOption(event.target.value as PriceOption)}
          className="h-11 rounded-control border border-input-line bg-surface px-2 text-sm"
        >
          {PRICE_OPTIONS.map((option) => <option key={option} value={option}>{t(`picker.prices.${option}`)}</option>)}
        </select>
      </td>
      <td className={CELL}>
        <input
          type="number" min={1} max={pick.available} dir="ltr" aria-label={t("cart.columns.quantity")} value={line.quantity}
          onChange={(event) => setQuantity(event.target.value)}
          className="h-11 w-16 rounded-control border border-input-line bg-surface px-2 text-start text-sm"
        />
      </td>
      <td dir="ltr" className={`${CELL} whitespace-nowrap text-start text-sm`}>{formatEgp(shownPrice(pick, line.priceOption), locale)}</td>
      <td className={CELL}>
        <SalePrice label={t("cart.saleOf", { name: pick.code })} value={line.salePrice} below={isBelow} onChange={(salePrice) => onChange({ ...line, salePrice })} />
      </td>
      <td className={CELL}><RemoveButton name={pick.name} onRemove={onRemove} /></td>
    </tr>
  );
}

function ModeToggle({ line, onChange }: { line: BuildLine; onChange: (line: BuildLine) => void }) {
  const t = useTranslations("admin.pos.cart");
  return (
    <div role="radiogroup" aria-label={t("mode.label")} className="flex gap-1.5">
      {(["whole", "split"] as const).map((mode) => (
        <button
          key={mode} type="button" role="radio" aria-checked={line.mode === mode} onClick={() => onChange({ ...line, mode })}
          className={`h-11 whitespace-nowrap rounded-control border px-3 text-sm ${line.mode === mode ? "border-carbon bg-carbon font-bold text-on-carbon" : "border-input-line bg-surface"}`}
        >
          {t(`mode.${mode}`)}
        </button>
      ))}
    </div>
  );
}

function BuildRows({ line, below, onChange, onRemove }: LineProps<BuildLine>) {
  const t = useTranslations("admin.pos.cart");
  const locale = useLocale();
  const { pick } = line;
  const split = line.mode === "split";
  const isBelow = below.has(pick.code);
  return (
    <Fragment>
      <tr className={isBelow ? "bg-error/10" : undefined}>
        <NameCell name={pick.name} code={pick.code} />
        <td className={`${CELL} text-xs font-bold`}>{t("build")}</td>
        <td className={CELL} />
        <td className={CELL} colSpan={2}>
          {pick.splittable ? <ModeToggle line={line} onChange={onChange} /> : <span className="text-xs text-ink-muted">{t("wholeHint")}</span>}
        </td>
        <td dir="ltr" className={`${CELL} whitespace-nowrap text-start text-sm`}>{formatEgp(pick.salePrice, locale)}</td>
        <td className={CELL}>
          {split ? null : <SalePrice label={t("saleOf", { name: pick.code })} value={line.salePrice} below={isBelow} onChange={(salePrice) => onChange({ ...line, salePrice })} />}
        </td>
        <td className={CELL}><RemoveButton name={pick.name} onRemove={onRemove} /></td>
      </tr>
      {split ? (
        <>
          <tr><td colSpan={8} className="px-2 pb-1 text-xs text-ink-muted">{t("splitHint")}</td></tr>
          {(pick.parts ?? []).map((part) => {
            const state = line.parts[part.unitId]!;
            const setPart = (change: Partial<typeof state>) => onChange({ ...line, parts: { ...line.parts, [part.unitId]: { ...state, ...change } } });
            const partBelow = below.has(part.code);
            return (
              <tr key={part.unitId} className={partBelow ? "bg-error/10" : "bg-paper"}>
                <td className={CELL}>
                  <label className="flex min-h-11 items-center gap-2.5">
                    <input
                      type="checkbox" aria-label={t("selectPart", { name: part.name })} checked={state.selected}
                      onChange={(event) => setPart({ selected: event.target.checked })} className="h-[18px] w-[18px] accent-carbon"
                    />
                    <span>
                      <span dir="auto" className="block text-sm [unicode-bidi:isolate]">{part.name}</span>
                      <span dir="ltr" className="block text-start font-mono text-xs text-ink-muted">{part.code}</span>
                    </span>
                  </label>
                </td>
                <td className={CELL}><ConditionBadge condition={condition(part.condition)} /></td>
                <td className={`${CELL} text-sm`}>{part.warrantyLabel ?? t("noWarranty")}</td>
                <td className={CELL} colSpan={2} />
                <td dir="ltr" className={`${CELL} whitespace-nowrap text-start text-sm`}>{formatEgp(part.partPrice, locale)}</td>
                <td className={CELL}>
                  {state.selected ? <SalePrice label={t("saleOf", { name: part.code })} value={state.salePrice} below={partBelow} onChange={(salePrice) => setPart({ salePrice })} /> : null}
                </td>
                <td className={CELL} />
              </tr>
            );
          })}
        </>
      ) : null}
    </Fragment>
  );
}

interface PosCartProps {
  cart: readonly CartLine[];
  /** Codes of the lines the server priced below cost. */
  below: ReadonlySet<string>;
  errors: readonly PosField[];
  onChange: (cart: CartLine[]) => void;
}

/** The lines being sold: price type and quantity for items, whole or part-by-part for builds, and the typed sale price. */
export function PosCart({ cart, below, errors, onChange }: PosCartProps) {
  const t = useTranslations("admin.pos.cart");
  const error = usePosError(errors);
  const patch = (line: CartLine) => onChange(cart.map((entry) => (entry.key === line.key ? line : entry)));
  const remove = (key: string) => onChange(cart.filter((entry) => entry.key !== key));
  const problems = (["lines", "salePrice", "parts"] as const).map(error).filter((message) => message !== null);

  return (
    <FormSection title={t("title")}>
      {cart.length === 0 ? <p className="m-0 text-sm text-ink-muted">{t("empty")}</p> : (
        <div className="overflow-x-auto">
          <table className="w-full min-w-[820px] border-collapse text-start">
            <thead>
              <tr className="text-xs text-ink-muted">
                {COLUMNS.map((column) => <th key={column} scope="col" className="px-2 py-2 text-start font-bold">{t(`columns.${column}`)}</th>)}
                <td />
              </tr>
            </thead>
            <tbody className="[&>tr]:border-t [&>tr]:border-line">
              {cart.map((line) => line.kind === "item"
                ? <ItemRow key={line.key} line={line} below={below} onChange={patch} onRemove={() => remove(line.key)} />
                : <BuildRows key={line.key} line={line} below={below} onChange={patch} onRemove={() => remove(line.key)} />)}
            </tbody>
          </table>
        </div>
      )}
      <p className="m-0 text-[13px] text-ink-muted">{t("note")}</p>
      {problems.map((message) => <p key={message} role="alert" className="m-0 text-xs text-error">{message}</p>)}
    </FormSection>
  );
}
