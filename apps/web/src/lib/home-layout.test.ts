import { describe, expect, it } from "vitest";
import {
  arrangeRailItems,
  createDefaultHomeLayout,
  HOME_LAYOUT_STORAGE_KEY,
  parseHomeLayout,
} from "@/lib/home-layout";

const items = [
  { id: "a", name: "A" },
  { id: "b", name: "B" },
  { id: "c", name: "C" },
];

describe("home layout prototype", () => {
  it("starts with all eight sections visible in the commercial order", () => {
    const layout = createDefaultHomeLayout();
    expect(layout.version).toBe(1);
    expect(layout.sections.map((section) => section.id)).toEqual([
      "hero",
      "categories",
      "brands",
      "offers",
      "discovery",
      "guide",
      "latest",
      "closing",
    ]);
    expect(layout.sections.every((section) => section.visible)).toBe(true);
  });

  it("falls back safely when storage is empty or malformed", () => {
    expect(parseHomeLayout(null)).toEqual(createDefaultHomeLayout());
    expect(parseHomeLayout("{not json")).toEqual(createDefaultHomeLayout());
    expect(parseHomeLayout(JSON.stringify({ sections: "bad" }))).toEqual(createDefaultHomeLayout());
  });

  it("keeps stored section order and visibility while adding missing sections", () => {
    const stored = {
      version: 1,
      sections: [
        { id: "latest", visible: false },
        { id: "hero", visible: true },
      ],
      rails: {},
    };
    const layout = parseHomeLayout(JSON.stringify(stored));
    expect(layout.sections[0]).toEqual({ id: "latest", visible: false });
    expect(layout.sections[1]).toEqual({ id: "hero", visible: true });
    expect(new Set(layout.sections.map((section) => section.id)).size).toBe(8);
  });

  it("applies item order, hidden items, and the item limit", () => {
    const layout = createDefaultHomeLayout();
    const config = {
      ...layout.rails.brands,
      itemOrder: ["c", "a", "b"],
      hiddenItemIds: ["a"],
      limit: 2,
    };
    expect(arrangeRailItems(items, config).map((item) => item.id)).toEqual(["c", "b"]);
  });

  it("uses a stable v1 storage key", () => {
    expect(HOME_LAYOUT_STORAGE_KEY).toBe("rigx-home-layout-v1");
  });
});
