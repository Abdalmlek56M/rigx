import assert from "node:assert/strict";
import { describe, it } from "node:test";
import type { Schemas } from "@rigx/api-client";
import { pickRelatedBuilds, RELATED_BUILDS_MAX } from "./related-builds.ts";

type Build = Schemas["AssemblyPublicView"];

const build = (code: string, usage: string | null): Build => ({
  id: code,
  code,
  titleAr: code,
  titleEn: code,
  salePrice: 100,
  usage,
  sellMode: "FullOnly",
  copies: 1,
  warrantyLabel: null,
  suitableForAr: null,
  suitableForEn: null,
  photos: [],
  units: []
} as Build);

describe("pickRelatedBuilds", () => {
  it("leaves out the current build and puts the same usage first", () => {
    const builds = [build("BLD-1", "Gaming"), build("BLD-2", "StudyWork"), build("BLD-3", "Gaming"), build("BLD-4", null)];
    assert.deepEqual(pickRelatedBuilds("BLD-2", "Gaming", builds).map((item) => item.code), ["BLD-1", "BLD-3", "BLD-4"]);
  });

  it("keeps the input order when the current build has no usage", () => {
    const builds = [build("BLD-1", "Gaming"), build("BLD-2", null), build("BLD-3", "Gaming")];
    assert.deepEqual(pickRelatedBuilds("BLD-2", null, builds).map((item) => item.code), ["BLD-1", "BLD-3"]);
  });

  it("never returns more than the cap", () => {
    const builds = Array.from({ length: 20 }, (_, index) => build(`BLD-${index + 1}`, "Gaming"));
    assert.equal(pickRelatedBuilds("BLD-1", "Gaming", builds).length, RELATED_BUILDS_MAX);
  });
});
