"use client";

import type { CSSProperties, ElementType, ReactNode } from "react";
import { DndContext, MouseSensor, TouchSensor, useDraggable, useDroppable, useSensor, useSensors, type DragEndEvent } from "@dnd-kit/core";
import { GripIcon } from "@/components/ui/icons";

/**
 * Hand ordering by dragging, for the admin only (the one place `@dnd-kit/core` is used, so the library stays in the admin chunks).
 * A mouse drags after a few pixels; a finger after a short press, so a swipe still scrolls the page. The arrow buttons next to each
 * item stay the keyboard way, which is why nothing here takes the keyboard.
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

/**
 * One item that can be dragged and dropped on. With `handle` only the grip starts a drag (a row that has its own buttons and a name
 * to click); without it the whole item does (a photo tile). The item that is over gets `data-over`, the one in hand `data-dragging`.
 */
export function DragItem({ id, as: Tag = "li", handle = false, disabled = false, label, className = "", children }: {
  id: string;
  as?: ElementType;
  /** Not draggable and not a drop target (the list is filtered, or a save is running). */
  disabled?: boolean;
  handle?: boolean;
  /** What the grip is called when hovered (the way to order by keyboard is the arrow buttons). */
  label?: string;
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
          ref={setActivatorNodeRef} title={label} aria-hidden="true" {...listeners}
          className="flex h-8 w-6 shrink-0 cursor-grab touch-none items-center justify-center text-ink-muted"
        >
          <GripIcon size={16} />
        </span>
      ) : null}
      {children}
    </Tag>
  );
}
