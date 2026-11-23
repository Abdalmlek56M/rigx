"use client";

import { useTranslations } from "next-intl";
import { ArrowDownIcon, ArrowUpIcon } from "@/components/ui/icons";
import { StatusBadge } from "@/components/ui/StatusBadge";
import { IconButton } from "../kit/Button";
import { DragItem, DragSort } from "../kit/DragSort";
import { EmptyPanel } from "../kit/LoadState";
import { dropOn, moveKey } from "../kit/reorder-rules";
import { flattenTree, parentIdOf, siblingIds } from "./category-rules";
import type { Category } from "./categories-api";

/**
 * The tree card: expand/collapse nodes with children and select a category for the edit panel. With `onReorder` a category can be dragged
 * by its grip to another place among its own siblings (a category keeps its parent; to change that, edit it), and the selected one has the
 * two arrow buttons for the keyboard.
 */
export function CategoryTree({ nodes, selectedId, collapsed, onSelect, onToggle, onReorder }: {
  nodes: readonly Category[];
  selectedId: string | null;
  collapsed: ReadonlySet<string>;
  onSelect: (id: string) => void;
  onToggle: (id: string) => void;
  /** Saves the siblings of `parentId` (null = the top level) in this order. */
  onReorder?: (parentId: string | null, ids: string[]) => void;
}) {
  const t = useTranslations("admin.categories");
  const rows = flattenTree(nodes, collapsed);
  if (nodes.length === 0) {
    return <EmptyPanel message={t("emptyTree")} />;
  }

  const drop = (active: string, over: string) => {
    const parent = parentIdOf(nodes, active);
    if (parent === undefined || parent !== parentIdOf(nodes, over)) return;
    onReorder?.(parent, dropOn(siblingIds(nodes, parent), active, over));
  };
  const step = (id: string, by: -1 | 1) => {
    const parent = parentIdOf(nodes, id);
    if (parent === undefined) return;
    const ids = siblingIds(nodes, parent);
    onReorder?.(parent, moveKey(ids, ids.indexOf(id), ids.indexOf(id) + by));
  };

  return (
    <div aria-label={t("treeLabel")} role="group" className="rounded-card border border-line bg-surface">
      <DragSort onMove={drop}>
        <ul className="m-0 flex list-none flex-col p-0">
          {rows.map(({ node, depth }) => {
            const hasChildren = node.children.length > 0;
            const open = hasChildren && !collapsed.has(node.id);
            const selected = node.id === selectedId;
            const siblings = onReorder && selected ? siblingIds(nodes, parentIdOf(nodes, node.id) ?? null) : [];
            const index = siblings.indexOf(node.id);
            return (
              <DragItem
                key={node.id} id={node.id} handle disabled={!onReorder} label={t("drag")}
                className={`flex min-h-11 items-center gap-1.5 border-b border-line bg-surface pe-2 last:border-b-0 ${selected ? "border-s-[3px] border-s-orange bg-orange/10" : ""}`}
              >
                <span className="flex min-w-0 flex-1 items-center gap-1.5" style={{ paddingInlineStart: depth * 22 }}>
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
                    {node.code ? <span className="shrink-0 rounded-control border border-input-line px-1.5 py-0.5 font-mono text-[11px] font-bold">{node.code}</span> : null}
                    {!node.isActive || !node.isEffectivelyActive ? <StatusBadge tone="neutral">{t("hidden")}</StatusBadge> : null}
                  </button>
                </span>
                {siblings.length > 1 ? (
                  <span className="flex shrink-0">
                    <IconButton icon={ArrowUpIcon} label={t("moveUp")} size="sm" disabled={index === 0} onClick={() => step(node.id, -1)} />
                    <IconButton icon={ArrowDownIcon} label={t("moveDown")} size="sm" disabled={index === siblings.length - 1} onClick={() => step(node.id, 1)} />
                  </span>
                ) : null}
              </DragItem>
            );
          })}
        </ul>
      </DragSort>
    </div>
  );
}
