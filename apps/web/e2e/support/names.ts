/** Test data names carry a unique stamp (time plus a counter) so reruns and repeated calls never collide. */
let counter = 0;
const stamp = () => `${Date.now()}${counter++ % 10}`;

export const uniqueEmail = (prefix = "e2e") => `${prefix}-${stamp()}@example.com`;
export const uniqueName = (prefix: string) => `${prefix} ${stamp()}`;
/** A valid Egyptian mobile number (`010` + 8 digits) that differs on every call. */
export const uniquePhone = () => `010${stamp().slice(-8)}`;
