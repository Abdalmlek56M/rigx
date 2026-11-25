import { circle, fan, path, rect, type Piece, type Shape, type SlotDrawing } from "./blueprint-geometry.ts";
import type { BoardSize, PcLayout } from "./pc-layout.ts";

/** Drawing order is back to front: the shell and the board first, the cooler and the fans last; the glass goes over everything. */
export const BLUEPRINT_ORDER = ["CASE", "BOARD", "PSU", "SSD", "RAM", "GPU", "CPU", "COOL", "FAN"] as const;

interface FanMount {
  cx: number;
  cy: number;
  r: number;
}

/**
 * The six places a case fan can sit, in the order the fans fill them: front middle, rear, front top, front bottom, top rear, top front.
 * One fan sits at the front; two are an intake and an exhaust; from three up the front fills before the top. The fans that come with the
 * case take the first places, and the fans chosen on their own the places after them.
 */
export const FAN_MOUNTS: readonly FanMount[] = [
  { cx: 56, cy: 114, r: 21 },
  { cx: 272, cy: 74, r: 16 },
  { cx: 56, cy: 62, r: 21 },
  { cx: 56, cy: 166, r: 21 },
  { cx: 196, cy: 60, r: 20 },
  { cx: 138, cy: 60, r: 20 }
];

/** The two top places, taken by the radiator when the cooler is liquid. */
const TOP_MOUNTS = new Set([4, 5]);

const SOCKET = { cx: 145, cy: 141 };

/** Memory slots, listed in the order sticks fill them (the second and the fourth first, as a board's manual asks for two sticks). */
const RAM_SLOTS: Readonly<Record<2 | 4, readonly number[]>> = { 4: [206, 226, 196, 216], 2: [206, 226] };

/** Where the card's fans sit along it, by how many it has. */
const GPU_FANS: Readonly<Record<1 | 2 | 3, { xs: readonly number[]; r: number }>> = {
  1: { xs: [175], r: 14 },
  2: { xs: [146, 204], r: 14 },
  3: { xs: [128, 175, 222], r: 12.5 }
};

/** The radiator's fans under the top of the case, by how many it has. */
const RADIATOR_FANS: Readonly<Record<1 | 2 | 3, readonly number[]>> = { 1: [170], 2: [147, 193], 3: [124, 170, 216] };

/** Three drive plates on the front, top to bottom. */
const DRIVE_PLATES = [216, 238, 260];

const stick = (x: number): Piece => ({
  parts: [rect(x, 112, 7, 62, 2)],
  // The heat spreader's chips, and the light strip along its top edge.
  details: [...[0, 1, 2, 3].map((index) => rect(x + 1.5, 118 + index * 14, 4, 9, 1, "chip")), rect(x, 112, 7, 3, 1.5, "led")]
});

const drive = (y: number): Piece => ({
  parts: [rect(34, y, 50, 16, 3)],
  details: [rect(79, y + 4, 4, 8, 1, "chip")],
  label: { x: 55, y: y + 12 }
});

const fanPiece = ({ cx, cy, r }: FanMount): Piece => ({ parts: [circle(cx, cy, r)], details: fan(cx, cy, r - 2, 9).slice(1) });

const screws = (points: readonly (readonly [number, number])[]) => points.map(([x, y]) => circle(x, y, 1.6, "screw"));

/** Horizontal lines from `x1` to `x2`, every `step` from `top` to `bottom` (cooler and radiator fins). */
function fins(x1: number, x2: number, top: number, bottom: number, step: number): Shape {
  const lines: string[] = [];
  for (let y = top; y <= bottom; y += step) lines.push(`M${x1} ${y}H${x2}`);
  return path(lines.join(""), "fin");
}

const VRM = [
  rect(102, 108, 18, 64, 3, "chip"),
  path("M106 114h10M106 120h10M106 126h10M106 132h10M106 138h10M106 144h10M106 150h10M106 156h10M106 162h10", "vent")
];

/** The board by its size: the outline that lights, its marks, and where its name goes. Every size is fixed at the top rear (by the I/O). */
const BOARDS: Readonly<Record<BoardSize, { part: Shape; details: readonly Shape[]; label: { x: number; y: number } }>> = {
  atx: {
    part: rect(98, 104, 156, 184, 6),
    details: [
      // The power-stage heatsink, the rear I/O shield, two expansion slots, the chipset, an M.2 socket, screws and a few traces.
      ...VRM,
      rect(246, 108, 6, 56, 1, "chip"),
      path("M106 234h124M106 262h124"),
      rect(222, 244, 24, 22, 2, "chip"),
      rect(108, 250, 40, 9, 2, "chip"),
      path("M126 176L126 190L134 196M236 176L236 190", "trace"),
      ...screws([[106, 282], [246, 282], [246, 176], [106, 180]])
    ],
    label: { x: 160, y: 277 }
  },
  matx: {
    part: rect(98, 104, 156, 150, 6),
    details: [
      ...VRM,
      rect(246, 108, 6, 56, 1, "chip"),
      path("M106 236h110"),
      rect(224, 234, 22, 16, 2, "chip"),
      rect(108, 241, 28, 8, 2, "chip"),
      path("M126 176L126 190L134 196M236 176L236 190", "trace"),
      ...screws([[106, 248], [246, 248], [246, 176], [106, 180]])
    ],
    label: { x: 228, y: 247 }
  },
  itx: {
    part: rect(120, 104, 134, 124, 6),
    details: [
      rect(246, 108, 6, 56, 1, "chip"),
      rect(168, 179, 22, 10, 2, "chip"),
      path("M236 176L236 190", "trace"),
      ...screws([[124, 224], [250, 224], [124, 108]])
    ],
    label: { x: 226, y: 190 }
  }
};

/** Two Wi-Fi antennas on the rear I/O, standing out of the back of the case. */
const ANTENNAS: readonly Shape[] = [
  rect(288, 116, 7, 7, 1.5, "chip"),
  rect(288, 132, 7, 7, 1.5, "chip"),
  path("M294 119L306 94M294 135L310 112", "antenna"),
  circle(306, 94, 2, "hub"),
  circle(310, 112, 2, "hub")
];

function shell(caseFans: number): Shape[] {
  return [
    // Feet, the dark back wall with three cable grommets beside the board, the top vents, the fan frames,
    // the fans that come with the case (they light and turn with it), the power-supply shroud and the rear expansion covers.
    rect(46, 352, 40, 8, 3, "shell"),
    rect(234, 352, 40, 8, 3, "shell"),
    rect(30, 22, 260, 322, 10, "wall"),
    ...[118, 176, 236].map((y) => rect(88, y, 7, 24, 3, "hole")),
    path("M96 28h50M96 32h50M174 28h50M174 32h50", "vent"),
    ...FAN_MOUNTS.map((mount) => rect(mount.cx - mount.r - 2, mount.cy - mount.r - 2, mount.r * 2 + 4, mount.r * 2 + 4, 4, "mount")),
    ...FAN_MOUNTS.slice(0, caseFans).flatMap(({ cx, cy, r }) => fan(cx, cy, r - 2, 9)),
    rect(30, 290, 260, 54, 6, "shroud"),
    path("M40 300h60M40 308h60M40 316h60M40 324h60M40 332h60", "vent"),
    path("M284 188h4M284 196h4M284 204h4M284 212h4M284 220h4", "vent")
  ];
}

/**
 * The tempered-glass side panel, drawn over the whole case on its own layer: the pane, its dark printed edge, two reflections and the four
 * thumbscrews that hold it.
 */
export const GLASS: readonly Shape[] = [
  rect(30, 22, 260, 322, 10, "pane"),
  rect(33.5, 25.5, 253, 315, 8, "edge"),
  path("M44 30L92 30L36 228L36 180Z", "glare"),
  path("M106 30L120 30L40 336L36 336L36 324Z", "glare"),
  ...[[40, 32], [280, 32], [40, 334], [280, 334]].map(([x, y]) => circle(x!, y!, 2.6, "screw"))
];

function cooler(layout: PcLayout): SlotDrawing {
  if (layout.cooler === "liquid") {
    return {
      // The pump on the processor with its lit ring, two tubes up to the radiator under the top, and the radiator's own fans.
      pieces: [
        {
          parts: [circle(SOCKET.cx, SOCKET.cy, 20)],
          details: [
            circle(SOCKET.cx, SOCKET.cy, 15, "ring"),
            circle(SOCKET.cx, SOCKET.cy, 6, "hub"),
            path("M138 123C132 100 114 92 110 54M146 121C142 98 124 90 120 54", "tube"),
            rect(98, 38, 152, 14, 3, "chip"),
            fins(102, 246, 41, 49, 2.5),
            ...RADIATOR_FANS[layout.radiatorFans].flatMap((cx) => fan(cx, 72, 18, 9))
          ]
        }
      ],
      label: { x: 172, y: 49 }
    };
  }
  return {
    // A tower: the fin stack seen from the side, the fan on its front edge and the cap on top. It hides the processor, whose name is
    // written under it.
    pieces: [
      {
        parts: [rect(116, 107, 58, 68, 3)],
        details: [fins(120, 170, 112, 170, 4), rect(108, 109, 8, 64, 2, "chip"), rect(116, 104, 58, 5, 2, "chip"), circle(145, 106.5, 1.4, "hub")]
      }
    ],
    label: { x: SOCKET.cx, y: 100 }
  };
}

/** Every slot's drawing for this build: the board's size and slots, the card's fans, the cooler, the case's own fans and the antennas follow the parts. */
export function slotDrawings(layout: PcLayout): Readonly<Record<string, SlotDrawing>> {
  const board = BOARDS[layout.board];
  const gpuFans = GPU_FANS[layout.gpuFans];
  const freeMounts = FAN_MOUNTS.filter((_, index) => index >= layout.caseFans && !(layout.cooler === "liquid" && TOP_MOUNTS.has(index)));
  return {
    CASE: { pieces: [{ parts: [rect(22, 14, 276, 338, 16, "shell")], details: shell(layout.caseFans) }], label: { x: 72, y: 318 } },
    BOARD: { pieces: [{ parts: [board.part], details: [...board.details, ...(layout.wifi ? ANTENNAS : [])] }], label: board.label },
    PSU: {
      pieces: [
        {
          parts: [rect(150, 298, 132, 40, 6)],
          details: [
            ...fan(180, 318, 15, 9),
            path("M206 304h66M206 308h66M206 332h66"),
            // The cables to the board and to the card: they light with the supply.
            path("M270 298L270 270L252 252L252 190L207 190L207 196", "cable"),
            path("M262 298L262 282L242 282", "cable")
          ]
        }
      ],
      label: { x: 240, y: 322 }
    },
    SSD: { pieces: DRIVE_PLATES.map(drive) },
    RAM: { pieces: RAM_SLOTS[layout.ramSlots].map(stick), label: { x: 216, y: 107 } },
    GPU: {
      pieces: [
        {
          parts: [rect(104, 196, 142, 34, 4)],
          // The card's fans, the power plug on the top edge, the bracket on the left and the light strip along the bottom edge.
          details: [
            ...gpuFans.xs.flatMap((cx) => fan(cx, 213, gpuFans.r, 7)),
            rect(196, 192, 22, 4, 1, "chip"),
            rect(98, 198, 6, 30, 1, "chip"),
            rect(112, 226.5, 128, 2, 1, "led")
          ]
        }
      ],
      label: { x: 175, y: 243 }
    },
    CPU: {
      pieces: [{ parts: [rect(SOCKET.cx - 17, SOCKET.cy - 17, 34, 34, 4)], details: [rect(SOCKET.cx - 10, SOCKET.cy - 10, 20, 20, 2, "chip")] }],
      label: { x: SOCKET.cx, y: 186 }
    },
    COOL: cooler(layout),
    // The fans chosen on their own take the places after the case's own fans (and leave the top to a radiator).
    FAN: { pieces: freeMounts.map(fanPiece), label: { x: 56, y: 205 } }
  };
}
