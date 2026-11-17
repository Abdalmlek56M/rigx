import assert from "node:assert/strict";
import { describe, it } from "node:test";
import { ApiError } from "@rigx/api-client";
import { fieldKey, isStepUpRequired, stepUpHeaders, toFormErrors } from "./form-errors.ts";
import { pagerView } from "./paging.ts";
import { ariaSort, formatSort, nextSort, parseSort } from "./table-sort.ts";
import { dropToast, MAX_TOASTS, pushToast } from "./toast-queue.ts";
import { pageNumber, readFilters, writeFilters } from "./url-filters.ts";

describe("pagerView", () => {
  it("shows the row range and page count", () => {
    const view = pagerView({ page: 2, pageSize: 20, total: 45 });
    assert.deepEqual([view.from, view.to, view.pages, view.hasPrevious, view.hasNext], [21, 40, 3, true, true]);
    assert.deepEqual(view.numbers, [1, 2, 3]);
  });
  it("handles an empty result and a page past the end", () => {
    assert.deepEqual(pagerView({ page: 1, pageSize: 20, total: 0 }), { pages: 1, from: 0, to: 0, hasPrevious: false, hasNext: false, numbers: [1] });
    const last = pagerView({ page: 9, pageSize: 20, total: 45 });
    assert.deepEqual([last.from, last.to, last.hasNext], [41, 45, false]);
  });
  it("puts gaps around the current page in long lists", () => {
    assert.deepEqual(pagerView({ page: 10, pageSize: 10, total: 200 }).numbers, [1, null, 9, 10, 11, null, 20]);
    assert.deepEqual(pagerView({ page: 1, pageSize: 10, total: 200 }).numbers, [1, 2, 3, 4, 5, null, 20]);
    assert.deepEqual(pagerView({ page: 20, pageSize: 10, total: 200 }).numbers, [1, null, 16, 17, 18, 19, 20]);
  });
});

describe("url filters", () => {
  const defaults = { q: "", status: "all", sort: "", page: "1" };

  it("reads values with defaults for missing or blank ones", () => {
    assert.deepEqual(readFilters(new URLSearchParams("status=New&q=%20%20"), defaults), { q: "", status: "New", sort: "", page: "1" });
  });
  it("drops defaults, keeps unrelated params and resets the page on a filter change", () => {
    const params = new URLSearchParams("status=New&page=3&tab=x");
    assert.equal(writeFilters(params, defaults, { status: "all" }), "tab=x");
    assert.equal(writeFilters(params, defaults, { q: " rtx " }), "status=New&tab=x&q=rtx");
  });
  it("keeps the other filters when only the page changes", () => {
    assert.equal(writeFilters(new URLSearchParams("status=New"), defaults, { page: "2" }), "status=New&page=2");
    assert.equal(writeFilters(new URLSearchParams("status=New&page=2"), defaults, { page: "1" }), "status=New");
  });
  it("parses page numbers safely", () => {
    assert.equal(pageNumber("3"), 3);
    assert.equal(pageNumber("0"), 1);
    assert.equal(pageNumber("x"), 1);
    assert.equal(pageNumber(undefined), 1);
  });
});

describe("table sort", () => {
  it("round-trips the URL value", () => {
    assert.deepEqual(parseSort("-createdAt"), { key: "createdAt", direction: "desc" });
    assert.deepEqual(parseSort("name"), { key: "name", direction: "asc" });
    assert.equal(parseSort(""), null);
    assert.equal(formatSort({ key: "createdAt", direction: "desc" }), "-createdAt");
    assert.equal(formatSort(null), "");
  });
  it("cycles ascending → descending → none, and a new column starts ascending", () => {
    const asc = nextSort(null, "total");
    assert.deepEqual(asc, { key: "total", direction: "asc" });
    const desc = nextSort(asc, "total");
    assert.deepEqual(desc, { key: "total", direction: "desc" });
    assert.equal(nextSort(desc, "total"), null);
    assert.deepEqual(nextSort(desc, "name"), { key: "name", direction: "asc" });
  });
  it("reports aria-sort per column", () => {
    assert.equal(ariaSort({ key: "total", direction: "desc" }, "total"), "descending");
    assert.equal(ariaSort({ key: "total", direction: "desc" }, "name"), "none");
  });
});

describe("form errors", () => {
  it("normalizes server field keys", () => {
    assert.equal(fieldKey("Name"), "name");
    assert.equal(fieldKey("Lines[0].Code"), "lines.0.code");
    assert.equal(fieldKey("$.price"), "price");
  });
  it("puts field errors on their fields and no general message", () => {
    const error = new ApiError(400, { title: "Validation failed.", errors: { Name: ["Required."], "Lines[1].Price": ["Too low.", "x"] } });
    assert.deepEqual(toFormErrors(error, "failed"), { fields: { name: "Required.", "lines.1.price": "Too low." }, general: null });
  });
  it("uses the problem detail or title for other client errors, and the fallback for server errors or no response", () => {
    assert.equal(toFormErrors(new ApiError(409, { title: "Conflict", detail: "Code already used." }), "failed").general, "Code already used.");
    assert.equal(toFormErrors(new ApiError(404, { title: "Not found." }), "failed").general, "Not found.");
    assert.equal(toFormErrors(new ApiError(500, { title: "Boom" }), "failed").general, "failed");
    assert.equal(toFormErrors(new TypeError("fetch failed"), "failed").general, "failed");
  });
  it("recognizes the step-up demand and never shows it as a field error", () => {
    const error = new ApiError(403, { title: "Confirm.", errors: { stepUpCode: ["Code required."] } });
    assert.equal(isStepUpRequired(error), true);
    assert.equal(isStepUpRequired(new ApiError(403, { title: "Forbidden." })), false);
    assert.deepEqual(toFormErrors(error, "failed").fields, {});
    assert.deepEqual(stepUpHeaders("123456"), { "X-Step-Up-Code": "123456" });
  });
});

describe("toast queue", () => {
  it("keeps the newest few and drops by id", () => {
    let list = [1, 2, 3, 4].reduce((acc, id) => pushToast(acc, { id, message: `m${id}`, tone: "success" }), [] as ReturnType<typeof pushToast>);
    assert.equal(list.length, MAX_TOASTS);
    assert.deepEqual(list.map((item) => item.id), [2, 3, 4]);
    list = dropToast(list, 3);
    assert.deepEqual(list.map((item) => item.id), [2, 4]);
  });
});
