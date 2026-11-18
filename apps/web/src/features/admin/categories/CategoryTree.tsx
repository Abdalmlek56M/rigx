"use client";

import { useTranslations } from "next-intl";
import { flattenTree } from "./category-rules";
import type { Category } from "./categories-api";

/** The tree card: expand/collapse nodes with children and select a category for the edit panel. */
export function CategoryTree({ nodes, selectedId, collapsed, onSelect, onToggle }: {
  nodes: readonly Category[];
  selectedId: string | null;
  collapsed: ReadonlySet<string>;
  onSelect: (id: string) => void;
  onToggle: (id: string) => void;
}) {
  const t = useTranslations("admin.categories");
  const rows = flattenTree(nodes, collapsed);
  if (nodes.length === 0) {
    return <p className="m-0 rounded-[10px] border border-line bg-surface p-5 text-sm text-ink-muted">{t("emptyTree")}</p>;
  }
  return (
    <div aria-label={t("treeLabel")} role="group" className="rounded-[10px] border border-line bg-surface">
      <ul className="m-0 flex list-none flex-col p-0">
        {rows.map(({ node, depth }) => {
          const hasChildren = node.children.length > 0;
          const open = hasChildren && !collapsed.has(node.id);
          const selected = node.id === selectedId;
          return (
            <li
              key={node.id}
              className={`flex min-h-11 items-center gap-1.5 border-b border-line ps-2 pe-2 last:border-b-0 ${selected ? "border-s-[3px] border-s-orange bg-orange/10" : ""}`}
              style={{ paddingInlineStart: 8 + depth * 22 }}
            >
              {hasChildren ? (
                <button
                  type="button" aria-expanded={open} aria-label={t("toggle", { name: node.name })} onClick={() => onToggle(node.id)}
                  className="flex h-8 w-6 shrink-0 items-center justify-center text-xs text-ink-muted"
                >
                  <span aria-hidden="true" className={open ? undefined : "rtl:-scale-x-100"}>{open ? "▾" : "▸"}</span>
                </button>
              ) : (
                <span className="w-6 shrink-0" aria-hidden="true" />
              )}
              <button type="button" aria-current={selected ? "true" : undefined} onClick={() => onSelect(node.id)} className={`flex min-w-0 flex-1 items-center gap-2 py-2 text-start text-sm ${depth === 0 ? "font-bold" : ""}`}>
                <span dir="auto" className="truncate [unicode-bidi:isolate]">{node.name}</span>
                {node.code ? <span className="shrink-0 rounded-[4px] border border-input-line px-1.5 py-0.5 font-mono text-[11px] font-bold">{node.code}</span> : null}
                {!node.isActive || !node.isEffectivelyActive ? <span className="shrink-0 rounded-[4px] bg-line px-1.5 py-0.5 text-[11px] font-bold text-ink-muted">{t("hidden")}</span> : null}
              </button>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
