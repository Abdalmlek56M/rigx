import assert from "node:assert/strict";
import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import { describe, it } from "node:test";

const repoRoot = new URL("../../../../../", import.meta.url);
const catalogSource = readFileSync(new URL("src/Modules/Identity/PermissionCatalog.cs", repoRoot), "utf8");
const webRoot = new URL("apps/web/src/", repoRoot);

/** The permission names the API knows: every "Area.Name" in the `All` list of `PermissionCatalog`. */
const catalog = new Set([...catalogSource.matchAll(/"([A-Z][A-Za-z]+\.[A-Z][A-Za-z]+)"/g)].map((match) => match[1] as string));
const areas = [...new Set([...catalog].map((name) => name.split(".")[0] as string))];
const permissionShape = new RegExp(`"((?:${areas.join("|")})\\.[A-Z][A-Za-z]+)"`, "g");

function sourceFiles(directory: URL): URL[] {
  return readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
    const child = new URL(`${entry.name}${entry.isDirectory() ? "/" : ""}`, directory);
    if (entry.isDirectory()) return sourceFiles(child);
    return /\.tsx?$/.test(entry.name) && !/\.test\.tsx?$/.test(entry.name) ? [child] : [];
  });
}

describe("permission names in the web code", () => {
  it("reads the catalogue the API owns", () => {
    assert.ok(catalog.size > 40, `Only ${catalog.size} permissions were read from PermissionCatalog.cs; the pattern no longer matches.`);
    assert.ok(catalog.has("Sales.CreatePOS"));
  });

  it("every permission written in the web code exists in the API's catalogue, so a typo cannot hide a button for everyone", () => {
    const unknown = new Map<string, string[]>();
    let found = 0;
    for (const file of sourceFiles(webRoot)) {
      for (const match of readFileSync(file, "utf8").matchAll(permissionShape)) {
        found++;
        const name = match[1] as string;
        if (!catalog.has(name)) unknown.set(name, [...(unknown.get(name) ?? []), join("src", file.pathname.slice(webRoot.pathname.length))]);
      }
    }
    assert.ok(found > 20, `The scan found only ${found} permission names; the pattern no longer matches the code.`);
    assert.deepEqual([...unknown.entries()], []);
  });
});
