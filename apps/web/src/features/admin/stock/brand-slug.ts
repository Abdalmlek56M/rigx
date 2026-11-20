/** The slug a new brand gets from its name: a-z/0-9 words joined by dashes; an Arabic-only name leaves nothing. */
export function brandSlug(name: string): string {
  return name
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}
