import type { Schemas } from "@rigx/api-client";
import type { CategoryNode } from "../../lib/categories.ts";

/** The issue codes the builder rules produce (`BuilderRules.cs`); the wording lives in the `builder.issues` messages. */
const MESSAGE_KEYS: Record<string, "specMismatch" | "overLimit"> = {
  SpecMismatch: "specMismatch",
  OverLimit: "overLimit"
};

const POWER_CODES: ReadonlySet<string> = new Set(["PowerOverCapacity", "PowerNearCapacity"]);

/** Power problems are told by the power meter (with the output to look for), so the problem list leaves them out. */
export function withoutPowerIssues<T extends { code: string }>(issues: readonly T[]): T[] {
  return issues.filter((issue) => !POWER_CODES.has(issue.code));
}

export type IssueMessageKey = (typeof MESSAGE_KEYS)[string] | "generic";

export interface IssueText {
  key: IssueMessageKey;
  values: { slot: string; other: string; spec: string };
  conflict: boolean;
}

/** Names the issue's slots and spec in the visitor's language (unknown ones fall back to their raw key). */
export function describeIssue(
  issue: Schemas["BuilderIssue"],
  slotNames: Readonly<Record<string, string>>,
  specNames: Readonly<Record<string, string>>
): IssueText {
  return {
    key: MESSAGE_KEYS[issue.code] ?? "generic",
    values: {
      slot: slotNames[issue.slot] ?? issue.slot,
      other: issue.otherSlot ? (slotNames[issue.otherSlot] ?? issue.otherSlot) : "",
      spec: issue.specKey ? (specNames[issue.specKey] ?? issue.specKey) : ""
    },
    conflict: issue.level === "Conflict"
  };
}

/** Spec key → display name across the whole category tree (the first definition of a key wins). */
export function specNameMap(tree: readonly CategoryNode[]): Record<string, string> {
  const names: Record<string, string> = {};
  for (const node of tree) {
    for (const spec of node.specs) names[spec.key] ??= spec.name;
    Object.entries(specNameMap(node.children)).forEach(([key, name]) => {
      names[key] ??= name;
    });
  }
  return names;
}
