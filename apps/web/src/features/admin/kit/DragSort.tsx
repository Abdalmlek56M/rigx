"use client";

import type { CSSProperties, ElementType, KeyboardEvent, ReactNode } from "react";
import { DndContext, MouseSensor, TouchSensor, useDraggable, useDroppable, useSensor, useSensors, type DragEndEvent } from "@dnd-kit/core";
import { GripIcon } from "@/components/ui/icons";

/**
 * Hand ordering by dragging, for the admin only (the one place `@dnd-kit/core` is used, so the library stays in the admin chunks).
 * A mouse drags after a few pixels; a finger after a short press, so a swipe still scrolls the page. The keyboard way is either the arrow
 * buttons a screen keeps next to each item, or (with `onStep` on the item) the grip itself: focus it and press the up or down arrow.
 */
export function DragSort({ onMove, children }: {
  /** Called when `active` is dropped on `over` (never the same id). */
  onMove: (active: string, over: string) => void;
  children: ReactNode;
}) {
  const sensors = useSensors(
    useSensor(MouseSensor, { activationConstraint: { distance: 6 } }),
    useSensor(TouchSensor, { activationConstraint: { delay: 250, tolerance: 8 } })
  );
  const end = ({ active, over }: DragEndEvent) => {
    if (over && over.id !== active.id) onMove(String(active.id), String(over.id));
  };
  return <DndContext sensors={sensors} onDragEnd={end}>{children}</DndContext>;
}

/** The attributes that make a grip a keyboard control: focusable, named, and the vertical arrows step the item. */
function gripKeyboard(label: string | undefined, onStep: (direction: -1 | 1) => void) {
  return {
    role: "button",
    tabIndex: 0,
    "aria-label": label,
    "aria-keyshortcuts": "ArrowUp ArrowDown",
    onKeyDown: (event: KeyboardEvent<HTMLElement>) => {
      if (event.key !== "ArrowUp" && event.key !== "ArrowDown") return;
      event.preventDefault();
      onStep(event.key === "ArrowUp" ? -1 : 1);
    }
  };
}

/**
 * One item that can be dragged and dropped on. With `handle` only the grip starts a drag (a row that has its own buttons and a name
 * to click); without it the whole item does (a photo tile). The item that is over gets `data-over`, the one in hand `data-dragging`.
 */
export function DragItem({ id, as: Tag = "li", handle = false, disabled = false, label, onStep, className = "", children }: {
  id: string;
  as?: ElementType;
  /** Not draggable and not a drop target (the list is filtered, or a save is running). */
  disabled?: boolean;
  handle?: boolean;
  /** What the grip is called when hovered and read out. */
  label?: string;
  /** Makes the grip a keyboard control: up (-1) and down (1) arrow keys move the item one place. Without it the grip is for the pointer only. */
  onStep?: (direction: -1 | 1) => void;
  className?: string;
  children: ReactNode;
}) {
  const { setNodeRef: setDragRef, setActivatorNodeRef, listeners, attributes, transform, isDragging } = useDraggable({ id, disabled });
  const { setNodeRef: setDropRef, isOver } = useDroppable({ id, disabled });
  const style: CSSProperties = {
    transform: transform ? `translate3d(${transform.x}px, ${transform.y}px, 0)` : undefined,
    zIndex: isDragging ? 30 : undefined,
    touchAction: isDragging ? "none" : undefined
  };
  const whole = handle ? {} : { ...listeners, ...attributes };
  return (
    <Tag
      ref={(node: HTMLElement | null) => {
        setDragRef(node);
        setDropRef(node);
      }}
      style={style}
      data-dragging={isDragging || undefined}
      data-over={isOver && !isDragging ? true : undefined}
      className={`${className} data-[dragging]:cursor-grabbing data-[dragging]:opacity-90 data-[dragging]:shadow-[0_8px_24px_rgba(21,23,27,.25)] data-[over]:outline-2 data-[over]:outline-dashed data-[over]:outline-orange ${handle || disabled ? "" : "cursor-grab"}`}
      {...whole}
    >
      {handle && !disabled ? (
        <span
          ref={setActivatorNodeRef} title={label} {...listeners} {...(onStep ? gripKeyboard(label, onStep) : { "aria-hidden": true })}
          className="flex h-8 w-6 shrink-0 cursor-grab touch-none items-center justify-center rounded-control text-ink-muted focus-visible:outline-2 focus-visible:outline-orange"
        >
          <GripIcon size={16} />
        </span>
      ) : null}
      {children}
    </Tag>
  );
}
