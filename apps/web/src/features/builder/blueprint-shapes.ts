import { circle, fan, path, rect, type Piece, type Shape, type SlotDrawing } from "./blueprint-geometry.ts";

/** Drawing order is back to front: the shell and the board first, the cooler and the fans last. */
export const BLUEPRINT_ORDER = ["CASE", "BOARD", "PSU", "SSD", "RAM", "GPU", "CPU", "COOL", "FAN"] as const;

interface FanMount {
  cx: number;
  cy: number;
  r: number;
}

/**
 * The six places a case fan can sit, in the order the fans fill them: front middle, rear, front top, front bottom, top rear, top front.
 * One fan sits at the front; two are an intake and an exhaust; from three up the front fills before the top.
 */
export const FAN_MOUNTS: readonly FanMount[] = [
  { cx: 56, cy: 114, r: 21 },
  { cx: 272, cy: 74, r: 16 },
  { cx: 56, cy: 62, r: 21 },
  { cx: 56, cy: 166, r: 21 },
  { cx: 196, cy: 60, r: 20 },
  { cx: 138, cy: 60, r: 20 }
];

const SOCKET = { cx: 145, cy: 141 };

/** Four memory slots, listed in the order sticks fill them (the second and the fourth first, as a board's manual asks for two sticks). */
const RAM_SLOTS = [206, 226, 196, 216];

/** Three drive plates on the front, top to bottom. */
const DRIVE_PLATES = [216, 238, 260];

const stick = (x: number): Piece => ({
  parts: [rect(x, 112, 7, 62, 2)],
  details: [0, 1, 2, 3].map((index) => rect(x + 1.5, 118 + index * 14, 4, 9, 1, "chip"))
});

const drive = (y: number): Piece => ({
  parts: [rect(34, y, 50, 16, 3)],
  details: [rect(79, y + 4, 4, 8, 1, "chip")],
  label: { x: 55, y: y + 12 }
});

const fanPiece = ({ cx, cy, r }: FanMount): Piece => ({ parts: [circle(cx, cy, r)], details: fan(cx, cy, r - 2, 9).slice(1) });

const shell: Shape[] = [
  // Feet, then the glass side panel with two reflections, the top vents, the fan frames, the power-supply basement and the rear expansion covers.
  rect(46, 352, 40, 8, 3, "shell"),
  rect(234, 352, 40, 8, 3, "shell"),
  rect(40, 361, 240, 8, 4, "shadow"),
  rect(30, 22, 260, 322, 10, "glass"),
  path("M38 26L84 26L30 214L30 168Z", "glare"),
  path("M98 26L112 26L34 330L30 330L30 322Z", "glare"),
  path("M96 28h50M96 32h50M174 28h50M174 32h50", "vent"),
  ...FAN_MOUNTS.map((mount) => rect(mount.cx - mount.r - 2, mount.cy - mount.r - 2, mount.r * 2 + 4, mount.r * 2 + 4, 4, "mount")),
  rect(30, 290, 260, 54, 6, "shroud"),
  path("M40 300h60M40 308h60M40 316h60M40 324h60M40 332h60", "vent"),
  path("M284 188h4M284 196h4M284 204h4M284 212h4M284 220h4", "vent")
];

export const SLOT_DRAWINGS: Readonly<Record<string, SlotDrawing>> = {
  CASE: { pieces: [{ parts: [rect(22, 14, 276, 338, 16, "shell")], details: shell }], label: { x: 160, y: 36 } },
  BOARD: {
    pieces: [
      {
        parts: [rect(98, 104, 156, 184, 6)],
        details: [
          // The processor's power-stage heatsink, the rear I/O shield, two expansion slots, the chipset, an M.2 socket, screws and a few traces.
          rect(102, 108, 18, 64, 3, "chip"),
          path("M106 114h10M106 120h10M106 126h10M106 132h10M106 138h10M106 144h10M106 150h10M106 156h10M106 162h10", "vent"),
          rect(246, 108, 6, 56, 1, "chip"),
          path("M106 234h124M106 262h124"),
          rect(222, 244, 24, 22, 2, "chip"),
          rect(108, 250, 40, 9, 2, "chip"),
          path("M126 176L126 190L134 196M236 176L236 190", "trace"),
          ...[[106, 282], [246, 282], [246, 176], [106, 180]].map(([x, y]) => circle(x!, y!, 1.6, "screw"))
        ]
      }
    ],
    label: { x: 160, y: 277 }
  },
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
  RAM: { pieces: RAM_SLOTS.map(stick), label: { x: 216, y: 107 } },
  GPU: {
    pieces: [
      {
        parts: [rect(104, 196, 142, 34, 4)],
        // Three fans, the power plug on the top edge and the bracket on the left.
        details: [...[128, 175, 222].flatMap((cx) => fan(cx, 213, 12.5, 7)), rect(196, 192, 22, 4, 1, "chip"), rect(98, 198, 6, 30, 1, "chip")]
      }
    ],
    label: { x: 175, y: 243 }
  },
  CPU: {
    pieces: [{ parts: [rect(SOCKET.cx - 17, SOCKET.cy - 17, 34, 34, 4)], details: [rect(SOCKET.cx - 10, SOCKET.cy - 10, 20, 20, 2, "chip")] }],
    label: { x: SOCKET.cx, y: 186 }
  },
  COOL: {
    // The cooler sits over the processor: its fan inside a ring of fins. The processor's name is written under it, since the cooler hides it.
    pieces: [{ parts: [circle(SOCKET.cx, SOCKET.cy, 32)], details: [circle(SOCKET.cx, SOCKET.cy, 27, "ring"), ...fan(SOCKET.cx, SOCKET.cy, 23, 9)] }],
    label: { x: SOCKET.cx, y: 103 }
  },
  FAN: { pieces: FAN_MOUNTS.map(fanPiece), label: { x: 56, y: 205 } }
};
