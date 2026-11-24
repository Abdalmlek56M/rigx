"use client";

import { useEffect, useRef, useState, type ReactNode, type RefObject } from "react";

interface ScaledPaperProps {
  /** The paper's design width in pixels: it is laid out at this width and scaled down to fit a narrower container. */
  width: number;
  /** The paper element itself (what a picture of the document is made from). */
  paperRef?: RefObject<HTMLDivElement | null>;
  children: ReactNode;
}

/** A fixed-width paper scaled to the width of its container (never enlarged), centred when there is room. The container takes the scaled height. */
export function ScaledPaper({ width, paperRef, children }: ScaledPaperProps) {
  const frame = useRef<HTMLDivElement>(null);
  const own = useRef<HTMLDivElement>(null);
  const paper = paperRef ?? own;
  const [fit, setFit] = useState({ scale: 1, offset: 0, height: 0 });

  useEffect(() => {
    const outer = frame.current;
    const inner = paper.current;
    if (!outer || !inner) return;
    const measure = () => {
      const scale = Math.min(1, outer.clientWidth / width);
      setFit({ scale, offset: Math.max(0, (outer.clientWidth - width * scale) / 2), height: inner.offsetHeight * scale });
    };
    const observer = new ResizeObserver(measure);
    observer.observe(outer);
    observer.observe(inner);
    return () => observer.disconnect();
  }, [width, paper]);

  return (
    <div ref={frame} data-document-frame className="relative w-full" style={{ height: fit.height || undefined }}>
      <div
        ref={paper}
        data-document-paper
        className="absolute top-0 origin-top-left"
        style={{ left: fit.offset, width, transform: `scale(${fit.scale})` }}
      >
        {children}
      </div>
    </div>
  );
}
