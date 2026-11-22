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

const FILES = sources(SRC).map((path) => ({ name: relative(SRC, path).replaceAll("\\", "/"), lines: readFileSync(path, "utf8").split(/\r?\n/) }));
const KIT_BUTTONS = new Set(["components/ui/Button.tsx", "features/admin/kit/Button.tsx"]);

function violations(test: (line: string, name: string) => boolean, skip: (name: string) => boolean = () => false): string[] {
  return FILES.filter((file) => !skip(file.name)).flatMap((file) =>
    file.lines.flatMap((line, index) => (test(line, file.name) ? [`${file.name}:${index + 1}`] : []))
  );
}

// The design-kit rules of docs/design/storefront-kit.md that a grep can prove; a failure names the file and line.
describe("design kit rules", () => {
  it("builds every button from the kit, never from a hard-coded fill", () => {
    const hardCoded = /rounded-control.*(bg-orange text-on-orange|bg-carbon text-on-carbon|bg-error text-white)|(bg-orange text-on-orange|bg-carbon text-on-carbon|bg-error text-white).*rounded-control/;
    assert.deepEqual(violations((line) => hardCoded.test(line), (name) => KIT_BUTTONS.has(name) || name.startsWith("features/admin/")), []);
  });

  it("styles a disabled state only in the kit (no dimmed copies with disabled:opacity)", () => {
    assert.deepEqual(violations((line) => line.includes("disabled:opacity-")), []);
  });

  it("uses text-error-text for red text, so it stays readable in the dark theme", () => {
    assert.deepEqual(violations((line) => /(?<![-\w])text-error(?![-\w])/.test(line)), []);
  });

  it("sizes h2 and h3 headings with the type scale, not ad-hoc sizes", () => {
    const adHoc = /<h[23][^>]*className="[^"]*(text-\[\d|text-(base|lg|xl|2xl|3xl|4xl)\b)/;
    assert.deepEqual(violations((line) => adHoc.test(line), (name) => name.startsWith("features/admin/")), []);
  });
});
