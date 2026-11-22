"use client";

import { useEffect, useRef, useState, type ReactNode } from "react";
import { CopyImageButton } from "./CopyImageButton";

interface DocumentFrameProps {
  /** The paper's design width in pixels: it is laid out at this width and scaled down to fit a narrower screen, so it looks the same everywhere. */
  width: number;
  fileKind: string;
  fileLabel: string;
  /** A button at the start of the toolbar (a way back). */
  leading?: ReactNode;
  /** Buttons next to "copy as image" (print / save PDF). */
  actions?: ReactNode;
  children: ReactNode;
}

/**
 * A printed-style document on screen: the paper keeps one fixed layout (it is also what the PNG is made from), is scaled to the screen's
 * width, centred when there is room, and the toolbar above it offers the picture and the PDF. In print the scale is dropped (see `data-document-*` in the CSS).
 */
export function DocumentFrame({ width, fileKind, fileLabel, leading, actions, children }: DocumentFrameProps) {
  const frame = useRef<HTMLDivElement>(null);
  const paper = useRef<HTMLDivElement>(null);
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
  }, [width]);

  return (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center justify-end gap-3 print:hidden">
        {leading ? <div className="me-auto">{leading}</div> : null}
        <CopyImageButton target={paper} width={width} fileKind={fileKind} fileLabel={fileLabel} />
        {actions}
      </div>
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
    </div>
  );
}
