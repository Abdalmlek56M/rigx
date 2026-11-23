import assert from "node:assert/strict";
import { readdirSync, readFileSync } from "node:fs";
import { join, relative } from "node:path";
import { describe, it } from "node:test";

const SRC = join(import.meta.dirname, "..", "..");

function sources(dir: string): string[] {
  return readdirSync(dir, { withFileTypes: true }).flatMap((entry) => {
    const path = join(dir, entry.name);
    if (entry.isDirectory()) return sources(path);
    return /\.tsx$/.test(entry.name) ? [path] : [];
  });
}

// Owner decision (23 Nov 2026): a brand's logo shows on the product page and in the catalog's brand filter, never on the small product cards.
describe("brand logo placement", () => {
  const users = sources(SRC)
    .filter((path) => readFileSync(path, "utf8").includes("<BrandMark"))
    .map((path) => relative(SRC, path).replaceAll("\\", "/"))
    .sort();

  it("is drawn only on the product page and in the filter", () => {
    assert.deepEqual(users, ["app/[locale]/(store)/p/[slug]/page.tsx", "features/catalog/FilterPanel.tsx"]);
  });

  it("never reaches a product card", () => {
    for (const card of ["features/catalog/ItemCard.tsx", "features/builds/BuildCard.tsx"]) {
      assert.ok(!sources(SRC).some((path) => relative(SRC, path).replaceAll("\\", "/") === card && readFileSync(path, "utf8").includes("BrandMark")), card);
    }
  });
});
