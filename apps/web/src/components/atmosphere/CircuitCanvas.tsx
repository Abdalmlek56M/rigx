"use client";

import { useEffect, useRef } from "react";
import { circuitMode, hexToRgb } from "./circuit-config";
import { buildTraces, pointAt, type Trace } from "./circuit-traces";

/** 30 frames a second is plenty for a pulse that moves about 100 px a second, and half the cost of 60. */
const FRAME_MS = 1000 / 30;
const MAX_PIXEL_RATIO = 1.5;
const TAIL_STEPS = 7;
const TAIL_GAP = 7;
/** Rings around the pointer, from the widest and faintest to the nearest: the lines light up where the mouse is. */
const POINTER_RINGS = [
  { radius: 170, alpha: 0.3 },
  { radius: 110, alpha: 0.45 },
  { radius: 56, alpha: 0.7 }
] as const;

interface Pulse {
  trace: number;
  /** Distance of the head along its trace; negative while it waits to start. */
  at: number;
  speed: number;
}

function paintLayer(width: number, height: number, ratio: number, traces: readonly Trace[], rgb: readonly [number, number, number], alpha: number): HTMLCanvasElement {
  const layer = document.createElement("canvas");
  layer.width = Math.round(width * ratio);
  layer.height = Math.round(height * ratio);
  const context = layer.getContext("2d");
  if (!context) return layer;
  context.scale(ratio, ratio);
  context.strokeStyle = `rgba(${rgb.join(",")},${alpha})`;
  context.lineWidth = 1;
  context.lineJoin = "round";
  for (const trace of traces) {
    context.beginPath();
    trace.points.forEach((point, at) => (at === 0 ? context.moveTo(point.x, point.y) : context.lineTo(point.x, point.y)));
    context.stroke();
    // A small ring (a via) at each end of the line.
    for (const end of [trace.points[0], trace.points[trace.points.length - 1]]) {
      if (!end) continue;
      context.beginPath();
      context.arc(end.x, end.y, 2.5, 0, Math.PI * 2);
      context.stroke();
    }
  }
  return layer;
}

function newPulse(traces: readonly Trace[]): Pulse {
  return { trace: Math.floor(Math.random() * traces.length), at: -Math.random() * 240, speed: 70 + Math.random() * 90 };
}

/**
 * The dark theme's circuit board: thin printed lines with a faint light travelling along some of them, a little brighter near
 * the mouse. Drawn once per size into two cached layers; a frame only copies them and moves a few dots. It runs only while it is
 * on screen and the tab is visible, as a still picture for reduced motion and low-end devices, and not at all with save-data.
 */
export default function CircuitCanvas() {
  const canvasRef = useRef<HTMLCanvasElement>(null);

  useEffect(() => {
    const canvas = canvasRef.current;
    const host = canvas?.parentElement;
    const context = canvas?.getContext("2d");
    if (!canvas || !host || !context) return;
    const ctx: CanvasRenderingContext2D = context;

    const connection = (navigator as Navigator & { connection?: { saveData?: boolean } }).connection;
    const mode = circuitMode({
      reducedMotion: window.matchMedia("(prefers-reduced-motion: reduce)").matches,
      saveData: connection?.saveData === true,
      cores: navigator.hardwareConcurrency ?? 0,
      narrow: window.matchMedia("(max-width: 767px)").matches
    });
    if (mode === "off") return;

    const tokens = getComputedStyle(document.documentElement);
    const lineColor = hexToRgb(tokens.getPropertyValue("--ink"));
    const pulseColor = hexToRgb(tokens.getPropertyValue("--orange"));

    let width = 0;
    let height = 0;
    let traces: Trace[] = [];
    let base: HTMLCanvasElement | null = null;
    let bright: HTMLCanvasElement | null = null;
    let pulses: Pulse[] = [];
    let frame = 0;
    let last = 0;
    let onScreen = true;
    const pointer = { x: 0, y: 0, inside: false };

    function draw() {
      if (!base) return;
      ctx.clearRect(0, 0, width, height);
      ctx.drawImage(base, 0, 0, width, height);
      if (pointer.inside && bright) {
        for (const ring of POINTER_RINGS) {
          ctx.save();
          ctx.beginPath();
          ctx.arc(pointer.x, pointer.y, ring.radius, 0, Math.PI * 2);
          ctx.clip();
          ctx.globalAlpha = ring.alpha;
          ctx.drawImage(bright, 0, 0, width, height);
          ctx.restore();
        }
      }
      for (const pulse of pulses) {
        const trace = traces[pulse.trace];
        if (!trace) continue;
        for (let step = 0; step < TAIL_STEPS; step += 1) {
          const distance = pulse.at - step * TAIL_GAP;
          if (distance < 0 || distance > trace.total) continue;
          const point = pointAt(trace, distance);
          const strength = 1 - step / TAIL_STEPS;
          ctx.fillStyle = `rgba(${pulseColor.join(",")},${(strength * 0.85).toFixed(2)})`;
          ctx.beginPath();
          ctx.arc(point.x, point.y, step === 0 ? 2.2 : 0.6 + strength * 1.2, 0, Math.PI * 2);
          ctx.fill();
          if (step === 0) {
            ctx.fillStyle = `rgba(${pulseColor.join(",")},0.16)`;
            ctx.beginPath();
            ctx.arc(point.x, point.y, 7, 0, Math.PI * 2);
            ctx.fill();
          }
        }
      }
    }

    function rebuild() {
      if (!canvas || !host) return;
      width = host.clientWidth;
      height = host.clientHeight;
      if (width === 0 || height === 0) return;
      const ratio = Math.min(window.devicePixelRatio || 1, MAX_PIXEL_RATIO);
      canvas.width = Math.round(width * ratio);
      canvas.height = Math.round(height * ratio);
      ctx.setTransform(ratio, 0, 0, ratio, 0, 0);
      traces = buildTraces(width, height);
      base = paintLayer(width, height, ratio, traces, lineColor, 0.07);
      bright = paintLayer(width, height, ratio, traces, lineColor, 0.34);
      pulses = traces.length === 0 ? [] : Array.from({ length: Math.min(10, Math.max(3, Math.round(traces.length / 5))) }, () => newPulse(traces));
      draw();
    }

    function tick(now: number) {
      frame = 0;
      if (!onScreen || document.hidden) return;
      if (now - last >= FRAME_MS) {
        const seconds = Math.min(0.1, (now - last) / 1000);
        last = now;
        for (const [index, pulse] of pulses.entries()) {
          pulse.at += pulse.speed * seconds;
          const trace = traces[pulse.trace];
          if (trace && pulse.at - TAIL_STEPS * TAIL_GAP > trace.total) pulses[index] = newPulse(traces);
        }
        draw();
      }
      frame = requestAnimationFrame(tick);
    }

    /** Starts or stops the loop: it runs only for the live mode, on screen, in a visible tab. */
    function sync() {
      const run = mode === "live" && onScreen && !document.hidden;
      if (run && frame === 0) {
        last = performance.now();
        frame = requestAnimationFrame(tick);
      } else if (!run && frame !== 0) {
        cancelAnimationFrame(frame);
        frame = 0;
      }
    }

    function follow(event: PointerEvent) {
      if (event.pointerType === "touch" || !canvas) return;
      const box = canvas.getBoundingClientRect();
      pointer.x = event.clientX - box.left;
      pointer.y = event.clientY - box.top;
      pointer.inside = pointer.x >= 0 && pointer.y >= 0 && pointer.x <= box.width && pointer.y <= box.height;
    }

    const resize = new ResizeObserver(rebuild);
    resize.observe(host);
    const visibility = new IntersectionObserver((entries) => {
      onScreen = entries.some((entry) => entry.isIntersecting);
      sync();
    });
    visibility.observe(host);
    document.addEventListener("visibilitychange", sync);
    if (mode === "live") window.addEventListener("pointermove", follow, { passive: true });
    rebuild();
    sync();

    return () => {
      resize.disconnect();
      visibility.disconnect();
      document.removeEventListener("visibilitychange", sync);
      window.removeEventListener("pointermove", follow);
      cancelAnimationFrame(frame);
    };
  }, []);

  return <canvas ref={canvasRef} aria-hidden="true" />;
}
