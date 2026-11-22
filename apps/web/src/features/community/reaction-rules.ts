import type { Schemas } from "@rigx/api-client";

export type Reactions = Schemas["BuildReactionSummary"];
export type ReactionKind = "Heart" | "Like";

export function isOn(reactions: Reactions, kind: ReactionKind): boolean {
  return kind === "Heart" ? reactions.hearted : reactions.liked;
}

/** The reactions as they will be once the visitor presses `kind`: the count moves by one and the mark flips (shown at once; the server's answer replaces it). */
export function toggled(reactions: Reactions, kind: ReactionKind): Reactions {
  const delta = isOn(reactions, kind) ? -1 : 1;
  return kind === "Heart"
    ? { ...reactions, hearts: Math.max(0, reactions.hearts + delta), hearted: !reactions.hearted }
    : { ...reactions, likes: Math.max(0, reactions.likes + delta), liked: !reactions.liked };
}
