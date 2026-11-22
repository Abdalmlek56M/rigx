/** The steps of "اقترح لي تجميعة": what it will be used for, the budget, then the message to send. */
export const RECOMMEND_STEPS = ["usage", "budget", "send"] as const;
export type RecommendStep = (typeof RECOMMEND_STEPS)[number];

export type SlideDirection = "forward" | "back";

/** The step `delta` places away (+1 next, -1 back), never past either end. */
export function stepBy(step: RecommendStep, delta: 1 | -1): RecommendStep {
  const index = RECOMMEND_STEPS.indexOf(step) + delta;
  return RECOMMEND_STEPS[Math.min(RECOMMEND_STEPS.length - 1, Math.max(0, index))] ?? step;
}

/** Which way the new step slides in from: forward when it is later than the one being left. */
export function slideDirection(from: RecommendStep, to: RecommendStep): SlideDirection {
  return RECOMMEND_STEPS.indexOf(to) >= RECOMMEND_STEPS.indexOf(from) ? "forward" : "back";
}
