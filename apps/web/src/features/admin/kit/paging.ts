/** Paging math for the API's `PagedResult<T>` (`page` is 1-based). Pure, so it is tested. */
export interface PageInfo {
  page: number;
  pageSize: number;
  total: number;
}

export interface PagerView {
  pages: number;
  /** 1-based index of the first and last row shown; both 0 when there are no rows. */
  from: number;
  to: number;
  hasPrevious: boolean;
  hasNext: boolean;
  /** Page numbers to show as buttons; `null` is a gap ("…"). */
  numbers: (number | null)[];
}

/** Pages around the current one, always with the first and last: `1 … 4 5 6 … 20`. */
function pageNumbers(page: number, pages: number): (number | null)[] {
  if (pages <= 7) return Array.from({ length: pages }, (_, index) => index + 1);
  const start = Math.max(2, Math.min(page - 1, pages - 4));
  const end = Math.min(pages - 1, Math.max(page + 1, 5));
  const middle = Array.from({ length: end - start + 1 }, (_, index) => start + index);
  return [1, ...(start > 2 ? [null] : []), ...middle, ...(end < pages - 1 ? [null] : []), pages];
}

export function pagerView({ page, pageSize, total }: PageInfo): PagerView {
  const pages = Math.max(1, Math.ceil(total / Math.max(1, pageSize)));
  const current = Math.min(Math.max(1, page), pages);
  const from = total === 0 ? 0 : (current - 1) * pageSize + 1;
  const to = Math.min(total, current * pageSize);
  return { pages, from, to, hasPrevious: current > 1, hasNext: current < pages, numbers: pageNumbers(current, pages) };
}
