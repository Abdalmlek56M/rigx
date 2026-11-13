import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { RowActionMenu } from "@/components/row-action-menu";

vi.mock("next/navigation", () => ({
  usePathname: () => "/admin/catalog",
}));

describe("RowActionMenu", () => {
  it("opens a portaled menu and restores focus on Escape", async () => {
    render(
      <RowActionMenu
        label="إجراءات السجل"
        actions={[
          { key: "edit", label: "تعديل", onSelect: vi.fn() },
          { key: "hide", label: "تعطيل", onSelect: vi.fn() },
        ]}
      />,
    );

    const trigger = screen.getByRole("button", { name: "إجراءات السجل" });
    await userEvent.click(trigger);
    expect(screen.getByRole("menu", { name: "إجراءات السجل" })).toBeInTheDocument();
    expect(trigger).toHaveAttribute("aria-expanded", "true");

    await userEvent.keyboard("{Escape}");
    await waitFor(() => {
      expect(screen.queryByRole("menu", { name: "إجراءات السجل" })).not.toBeInTheDocument();
    });
    expect(trigger).toHaveFocus();
  });

  it("closes after selecting an action and runs it", async () => {
    const onSelect = vi.fn();
    render(
      <RowActionMenu
        label="إجراءات السجل"
        actions={[{ key: "edit", label: "تعديل", onSelect }]}
      />,
    );

    await userEvent.click(screen.getByRole("button", { name: "إجراءات السجل" }));
    await userEvent.click(screen.getByRole("menuitem", { name: "تعديل" }));

    expect(onSelect).toHaveBeenCalledTimes(1);
    expect(screen.queryByRole("menu", { name: "إجراءات السجل" })).not.toBeInTheDocument();
  });

  it("closes on an outside pointer press", async () => {
    render(
      <RowActionMenu
        label="إجراءات السجل"
        actions={[{ key: "edit", label: "تعديل", onSelect: vi.fn() }]}
      />,
    );

    await userEvent.click(screen.getByRole("button", { name: "إجراءات السجل" }));
    expect(screen.getByRole("menu", { name: "إجراءات السجل" })).toBeInTheDocument();

    await userEvent.click(document.body);
    await waitFor(() => {
      expect(screen.queryByRole("menu", { name: "إجراءات السجل" })).not.toBeInTheDocument();
    });
  });

  it("keeps only one row menu open at a time", async () => {
    render(
      <>
        <RowActionMenu label="الأول" actions={[{ key: "a", label: "أ", onSelect: vi.fn() }]} />
        <RowActionMenu label="الثاني" actions={[{ key: "b", label: "ب", onSelect: vi.fn() }]} />
      </>,
    );

    await userEvent.click(screen.getByRole("button", { name: "الأول" }));
    expect(screen.getByRole("menu", { name: "الأول" })).toBeInTheDocument();

    await userEvent.click(screen.getByRole("button", { name: "الثاني" }));
    await waitFor(() => {
      expect(screen.queryByRole("menu", { name: "الأول" })).not.toBeInTheDocument();
    });
    expect(screen.getByRole("menu", { name: "الثاني" })).toBeInTheDocument();
  });
});
