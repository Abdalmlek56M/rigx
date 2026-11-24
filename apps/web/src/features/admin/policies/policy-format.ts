/**
 * The two formats the public policy page understands (`parsePolicyBody`): a line that starts with `# ` is a heading, a paragraph of lines that
 * all start with `- ` is a bulleted list, and blank lines separate the blocks. The editor's toolbar writes exactly these marks, and keeps a blank
 * line around what it makes, because a heading or a list only counts when it begins its own block. Pure, so it is tested.
 */
export type PolicyFormat = "heading" | "list";

export interface TextEdit {
  text: string;
  selectionStart: number;
  selectionEnd: number;
}

const HEADING = /^#{1,6}\s+/;
const BULLET = /^[-*]\s+/;

/** Where each line starts in `text` (lines split on "\n"). */
function lineStarts(lines: readonly string[]): number[] {
  const starts: number[] = [];
  let at = 0;
  for (const line of lines) {
    starts.push(at);
    at += line.length + 1;
  }
  return starts;
}

/** The first and last line a selection touches; a selection that ends at the very start of a line does not include that line. */
function touchedLines(lines: readonly string[], start: number, end: number): [number, number] {
  const starts = lineStarts(lines);
  const at = (position: number) => {
    let found = 0;
    starts.forEach((lineStart, index) => {
      if (lineStart <= position) found = index;
    });
    return found;
  };
  const first = at(start);
  const last = end > start ? at(end - 1) : first;
  return [first, Math.max(first, last)];
}

/** Heading: the first line of the selection becomes (or stops being) a heading. List: every non-empty selected line becomes (or stops being) a bullet. */
export function applyPolicyFormat(text: string, selectionStart: number, selectionEnd: number, format: PolicyFormat): TextEdit {
  const lines = text.split("\n");
  const [first, last] = touchedLines(lines, selectionStart, selectionEnd);
  const range = format === "heading" ? [first] : Array.from({ length: last - first + 1 }, (_, index) => first + index);
  const targets = range.filter((index) => (lines[index] ?? "").trim() !== "");
  if (targets.length === 0) return { text, selectionStart, selectionEnd };

  const marked = (line: string) => (format === "heading" ? HEADING.test(line) : BULLET.test(line));
  const removing = targets.every((index) => marked(lines[index] ?? ""));
  const edited = [...lines];
  for (const index of targets) {
    const bare = (lines[index] ?? "").replace(HEADING, "").replace(BULLET, "");
    edited[index] = removing ? bare : `${format === "heading" ? "# " : "- "}${bare}`;
  }

  // A heading or a list only counts at the start of its own block: keep a blank line before and after what was made.
  let from = targets[0] ?? first;
  let to = targets[targets.length - 1] ?? last;
  if (!removing) {
    if (to + 1 < edited.length && (edited[to + 1] ?? "").trim() !== "") edited.splice(to + 1, 0, "");
    if (from > 0 && (edited[from - 1] ?? "").trim() !== "") {
      edited.splice(from, 0, "");
      from += 1;
      to += 1;
    }
  }
  return finish(edited, from, to);
}

/** The edit with the touched lines selected. */
function finish(lines: readonly string[], from: number, to: number): TextEdit {
  const starts = lineStarts(lines);
  const selectionStart = starts[from] ?? 0;
  const selectionEnd = (starts[to] ?? 0) + (lines[to] ?? "").length;
  return { text: lines.join("\n"), selectionStart, selectionEnd };
}
