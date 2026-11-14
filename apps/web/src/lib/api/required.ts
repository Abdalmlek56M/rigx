/** The body of a successful call; the generated types allow an empty body, which here is a failure. */
export async function required<T>(body: Promise<T | undefined>): Promise<T> {
  const value = await body;
  if (value === undefined) throw new Error("Empty API response.");
  return value;
}
