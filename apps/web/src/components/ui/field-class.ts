/** The classes shared by every text input, select and textarea, so fields have one height, radius and border. */
export const FIELD_BASE =
  "w-full rounded-control border border-input-line bg-surface px-3 text-sm font-normal text-ink placeholder:text-muted";

/** The classes of a standard 48 px field. */
export const fieldClass = `h-12 ${FIELD_BASE}`;

/** The classes of a multi-line field. */
export const textAreaClass = `min-h-24 py-2.5 ${FIELD_BASE}`;
