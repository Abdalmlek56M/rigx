/**
 * The PDF export (4.10b C): the server's CSV of a dataset becomes a printable Arabic table, saved as PDF from the print dialog.
 * Pure, so it is tested.
 */
export interface ExportSheet {
  columns: string[];
  rows: string[][];
}

/** RFC 4180 CSV as the server writes it (UTF-8 with a byte-order mark, quoted cells, CRLF rows); the first row is the header. */
export function parseCsv(text: string): ExportSheet {
  const rows: string[][] = [];
  let row: string[] = [];
  let cell = "";
  let quoted = false;
  const body = text.charCodeAt(0) === 0xfeff ? text.slice(1) : text;
  for (let index = 0; index < body.length; index += 1) {
    const char = body[index];
    if (quoted) {
      if (char === '"' && body[index + 1] === '"') {
        cell += '"';
        index += 1;
      } else if (char === '"') quoted = false;
      else cell += char;
    } else if (char === '"') quoted = true;
    else if (char === ",") {
      row.push(cell);
      cell = "";
    } else if (char === "\n" || char === "\r") {
      if (char === "\r" && body[index + 1] === "\n") index += 1;
      row.push(cell);
      rows.push(row);
      row = [];
      cell = "";
    } else cell += char;
  }
  if (cell !== "" || row.length > 0) rows.push([...row, cell]);
  const [columns = [], ...rest] = rows;
  // The CSV guards formula-like text with a leading apostrophe for spreadsheets; a printed page does not need it.
  return { columns, rows: rest.map((cells) => cells.map((value) => (/^'[=+\-@\t\r]/.test(value) ? value.slice(1) : value))) };
}

/** The message key of a server column name: "price with board" → "priceWithBoard", "trade-in" → "tradeIn". */
export function columnKey(column: string): string {
  return column.trim().toLowerCase().replace(/[\s-]+(\w)/g, (_, letter: string) => letter.toUpperCase());
}

const INSTANT = /^(\d{4})-(\d{2})-(\d{2}) (\d{2}):(\d{2})$/;
const DATE = /^(\d{4})-(\d{2})-(\d{2})$/;

/**
 * A cell as staff read it: yes/no in words, dates as day/month/year and the server's 24-hour business time on the 12-hour clock
 * (the server already wrote it in Cairo time, so it is only re-written, never shifted).
 */
export function displayCell(value: string, locale: string, words: { yes: string; no: string }): string {
  if (value === "yes") return words.yes;
  if (value === "no") return words.no;
  const instant = INSTANT.exec(value);
  if (instant) {
    const [, year, month, day, hour, minute] = instant;
    const hours = Number(hour);
    const period = hours < 12 ? (locale === "ar" ? "ص" : "AM") : locale === "ar" ? "م" : "PM";
    return `${day}/${month}/${year} · ${hours % 12 || 12}:${minute} ${period}`;
  }
  const date = DATE.exec(value);
  if (date) return `${date[3]}/${date[2]}/${date[1]}`;
  return value;
}

/** Numbers line up at the end of their column. */
export function isNumeric(value: string): boolean {
  return /^-?\d+(\.\d+)?$/.test(value);
}
