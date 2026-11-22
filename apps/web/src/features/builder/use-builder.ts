"use client";

import { useCallback, useEffect, useRef, useState } from "react";
import { unwrap, type Schemas } from "@rigx/api-client";
import { browserApi } from "@/lib/api/browser";
import { required } from "@/lib/api/required";
import { EMPTY_EVALUATION, type Evaluation } from "./builder-summary";
import { builderPath, cleanShareName, parsePartsParam, partsOf, partsParam, removeCode, replaceInSlot, type BuilderParams, type PartInput } from "./builder-state";
import { createShareLink, type ShareLink } from "./share-link";

export type Unmatched = Schemas["UnmatchedPartView"];

/** The builder's whole state: chosen parts (mirrored to the URL), their evaluation, and the link actions. */
export function useBuilder(start: BuilderParams) {
  const opensExisting = start.saved !== null || start.from !== null;
  const [parts, setParts] = useState<PartInput[]>(start.parts);
  const [ready, setReady] = useState(!opensExisting);
  const [openFailed, setOpenFailed] = useState(false);
  const [unmatched, setUnmatched] = useState<Unmatched[]>([]);
  const [evaluated, setEvaluated] = useState<{ key: string; evaluation: Evaluation } | null>(null);
  const [failedKey, setFailedKey] = useState<string | null>(null);
  const [name, setName] = useState("");
  const [share, setShare] = useState<(ShareLink & { key: string }) | null>(null);
  const knownSlots = useRef(new Map<string, string>());
  const key = partsParam(parts);
  // A copied link is only the current one for the same parts and the same name.
  const shareKey = `${key}|${cleanShareName(name) ?? ""}`;

  useEffect(() => {
    if (!opensExisting) return;
    let cancelled = false;
    const api = browserApi();
    const opening = start.saved
      ? required(unwrap(api.GET("/api/v1/builder/saved/{token}", { params: { path: { token: start.saved } } }))).then((view) => ({ evaluation: view.evaluation, unmatched: [] as Unmatched[], name: view.name }))
      : required(unwrap(api.GET("/api/v1/builder/from-build/{code}", { params: { path: { code: start.from ?? "" } } }))).then((view) => ({ evaluation: view.evaluation, unmatched: view.unmatched, name: null }));
    opening
      .then((opened) => {
        if (cancelled) return;
        if (opened.name) setName(opened.name);
        for (const part of opened.evaluation.parts) knownSlots.current.set(part.item.code, part.slot);
        const openedParts = partsOf(opened.evaluation);
        setEvaluated({ key: partsParam(openedParts), evaluation: opened.evaluation });
        setParts(openedParts);
        setUnmatched(opened.unmatched);
        setReady(true);
      })
      .catch(() => {
        if (cancelled) return;
        setOpenFailed(true);
        setReady(true);
      });
    return () => {
      cancelled = true;
    };
  }, [opensExisting, start.saved, start.from]);

  useEffect(() => {
    if (!key) return;
    let cancelled = false;
    required(unwrap(browserApi().POST("/api/v1/builder/check", { body: { parts: parsePartsParam(key) } })))
      .then((evaluation) => {
        if (cancelled) return;
        for (const part of evaluation.parts) knownSlots.current.set(part.item.code, part.slot);
        setEvaluated({ key, evaluation });
        setFailedKey(null);
      })
      .catch(() => {
        if (!cancelled) setFailedKey(key);
      });
    return () => {
      cancelled = true;
    };
  }, [key]);

  useEffect(() => {
    if (!ready) return;
    window.history.replaceState(window.history.state, "", `${window.location.pathname}${key ? `?parts=${encodeURIComponent(key)}` : ""}`);
  }, [ready, key]);

  const pick = useCallback((slot: string, code: string) => {
    knownSlots.current.set(code, slot);
    setParts((current) => replaceInSlot(current, knownSlots.current, slot, code));
  }, []);

  const clear = useCallback((slot: string) => setParts((current) => replaceInSlot(current, knownSlots.current, slot, null)), []);

  const remove = useCallback((code: string) => setParts((current) => removeCode(current, code)), []);

  const copyLink = useCallback(async () => {
    const link = await createShareLink(parts, name);
    setShare({ ...link, key: shareKey });
    await navigator.clipboard.writeText(link.url).catch(() => undefined);
  }, [parts, name, shareKey]);

  return {
    parts,
    ready,
    openFailed,
    unmatched,
    evaluation: key === "" ? EMPTY_EVALUATION : (evaluated?.evaluation ?? EMPTY_EVALUATION),
    checking: key !== "" && evaluated?.key !== key && failedKey !== key,
    checkFailed: failedKey === key,
    name,
    setName,
    share: share?.key === shareKey ? share : null,
    pick,
    clear,
    remove,
    copyLink,
    path: builderPath(parts)
  };
}
