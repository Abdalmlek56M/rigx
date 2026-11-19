/** Policy status of a kind from its versions, the editor form ⇄ API and its validation. Pure, so it is tested. */
import type { Policy, PolicyInput } from "./policies-api";

export const POLICY_STATUSES = ["Draft", "Review", "Published", "Archived"] as const;
export type PolicyStatusKey = (typeof POLICY_STATUSES)[number] | "Other";

export function policyStatusKey(status: string): PolicyStatusKey {
  return POLICY_STATUSES.find((known) => known === status) ?? "Other";
}

/** Only a draft or a version in review can be edited; a published version is immutable. */
export function isEditable(policy: Policy): boolean {
  return policy.status === "Draft" || policy.status === "Review";
}

export interface KindSummary {
  /** The live version, if any. */
  published: Policy | null;
  /** The version being worked on (draft or in review), if any. */
  draft: Policy | null;
  /** All versions of the kind, newest first. */
  versions: Policy[];
}

export function kindSummary(kind: string, policies: readonly Policy[]): KindSummary {
  const versions = policies.filter((policy) => policy.kind === kind).sort((a, b) => b.versionNumber - a.versionNumber);
  return {
    published: versions.find((policy) => policy.status === "Published") ?? null,
    draft: versions.find(isEditable) ?? null,
    versions
  };
}

export interface PolicyForm {
  titleAr: string;
  titleEn: string;
  bodyAr: string;
  bodyEn: string;
}

export const emptyPolicyForm = (): PolicyForm => ({ titleAr: "", titleEn: "", bodyAr: "", bodyEn: "" });

export const policyForm = (policy: Policy): PolicyForm => ({
  titleAr: policy.titleAr, titleEn: policy.titleEn, bodyAr: policy.bodyAr, bodyEn: policy.bodyEn
});

/** A draft can be saved with both titles (the API requires them); the bodies may still be unfinished. */
export function policySavable(form: PolicyForm): boolean {
  return form.titleAr.trim() !== "" && form.titleEn.trim() !== "";
}

/** Publishing needs both titles and both bodies. */
export function policyPublishable(form: PolicyForm): boolean {
  return policySavable(form) && form.bodyAr.trim() !== "" && form.bodyEn.trim() !== "";
}

export function toPolicyInput(form: PolicyForm, rowVersion: number): PolicyInput {
  return {
    titleAr: form.titleAr.trim(), titleEn: form.titleEn.trim(), bodyAr: form.bodyAr.trim(), bodyEn: form.bodyEn.trim(), rowVersion
  };
}

export function sameForm(a: PolicyForm, b: PolicyForm): boolean {
  return a.titleAr === b.titleAr && a.titleEn === b.titleEn && a.bodyAr === b.bodyAr && a.bodyEn === b.bodyEn;
}
