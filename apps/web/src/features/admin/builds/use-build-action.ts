"use client";

import { isConflict } from "@/lib/api/conflict";
import { useSubmit } from "../kit/use-submit";
import { pickBuild } from "./build-rules";
import type { Build } from "./builds-api";

/** What every build action component receives: the build, how to take its new state, and what to do when it changed meanwhile. */
export interface BuildActionProps {
  build: Build;
  onChanged: (build: Build) => void;
  onStale: () => void;
}

/**
 * A save that returns the updated build: `apply` puts it on screen (a 409 reads the build again first and rethrows);
 * `applyOwn` is the same for a call that returns every build it changed (remove, replace); `run` adds the form's busy/error handling.
 */
export function useBuildAction({ build, onChanged, onStale }: BuildActionProps) {
  const submit = useSubmit();

  const apply = async (action: () => Promise<Build>) => {
    try {
      onChanged(await action());
    } catch (error) {
      if (isConflict(error)) onStale();
      throw error;
    }
  };

  const applyOwn = (action: () => Promise<Build[]>) =>
    apply(async () => {
      const own = pickBuild(await action(), build.id);
      if (!own) throw new Error("The build is missing from the response.");
      return own;
    });

  const run = (action: () => Promise<Build>, success?: string) => submit.run(() => apply(action), success);
  const runOwn = (action: () => Promise<Build[]>, success?: string) => submit.run(() => applyOwn(action), success);

  return { submit, run, runOwn };
}
