"use client";

import { useState } from "react";
import { useLocale, useTranslations } from "next-intl";
import { formatDateTime } from "@/lib/format";
import { ConfirmDialog } from "../kit/ConfirmDialog";
import { FormMessage } from "../kit/PageHeader";
import { toFormErrors } from "../kit/form-errors";
import { useToast } from "../kit/Toast";
import { useSubmit } from "../kit/use-submit";
import { deleteArticle, publishArticle, unpublishArticle, type ArticleDetail, type ArticleRevision, type Article } from "./articles-api";
import { ArticleStatusBadge } from "./ArticleStatusBadge";
import { isConflict } from "@/lib/api/conflict";

/** Publishing, revisions, the products the body shows and delete. */
export function ArticlePublishCard({ detail, revisions, onSaved, onDeleted, onStale }: {
  detail: ArticleDetail;
  revisions: readonly ArticleRevision[];
  onSaved: (article: Article) => void;
  onDeleted: () => void;
  onStale: () => void;
}) {
  const t = useTranslations("admin.articles.publishing");
  const locale = useLocale();
  const submit = useSubmit();
  const toast = useToast();
  const [unpublishing, setUnpublishing] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const article = detail.article;

  async function run(action: () => Promise<Article>, success: string) {
    await submit.run(async () => {
      try {
        onSaved(await action());
      } catch (error) {
        if (isConflict(error)) onStale();
        throw error;
      }
    }, success);
  }

  return (
    <section className="flex flex-col gap-3 rounded-[10px] border border-line bg-surface p-5">
      <h2 className="m-0 text-[17px] font-bold">{t("title")}</h2>
      <div className="flex flex-wrap items-center gap-2">
        <ArticleStatusBadge status={article.status} publishedAtUtc={article.publishedAtUtc} />
        {article.status !== "Published" ? (
          <button type="button" disabled={submit.busy} onClick={() => void run(() => publishArticle(article.id, article.rowVersion), t("published"))} className="h-11 rounded-control bg-carbon px-4 text-sm font-bold text-on-carbon disabled:opacity-50">{t("publish")}</button>
        ) : (
          <button type="button" onClick={() => setUnpublishing(true)} className="h-11 rounded-control border border-input-line bg-surface px-4 text-sm font-bold">{t("unpublish")}</button>
        )}
      </div>
      <p className="m-0 text-xs text-ink-muted">{t("saveFirst")}</p>
      <FormMessage message={submit.errors.general} />
      {article.publishedAtUtc && article.status === "Published" ? <p className="m-0 text-xs text-ink-muted">{t("publishedAt", { date: formatDateTime(article.publishedAtUtc, locale) })}</p> : null}

      <div className="flex flex-col gap-1.5 border-t border-line pt-3">
        <h3 className="m-0 text-sm font-bold">{t("revisions")}</h3>
        {revisions.length === 0 ? <p className="m-0 text-xs text-ink-muted">{t("noRevisions")}</p> : (
          <ul className="m-0 flex list-none flex-col gap-1 p-0 text-xs text-ink-muted">
            {revisions.slice(0, 6).map((revision) => (
              <li key={revision.id} className="flex flex-wrap items-center justify-between gap-2">
                <span>{revision.reason}</span>
                <span dir="ltr" className="font-mono">{formatDateTime(revision.createdAtUtc, locale)}</span>
              </li>
            ))}
          </ul>
        )}
      </div>

      {detail.products.length > 0 ? (
        <div className="flex flex-col gap-1.5 border-t border-line pt-3">
          <h3 className="m-0 text-sm font-bold">{t("products")}</h3>
          <ul className="m-0 flex list-none flex-col gap-1 p-0 text-xs">
            {detail.products.map((product) => (
              <li key={product.code} className="flex flex-wrap items-center justify-between gap-2">
                <span dir="auto" className="[unicode-bidi:isolate]">{product.modelName}</span>
                <span dir="ltr" className="font-mono text-orange-text">{product.code}</span>
              </li>
            ))}
          </ul>
        </div>
      ) : null}

      <button type="button" onClick={() => setDeleting(true)} className="h-11 rounded-control border border-error/40 bg-surface px-4 text-sm font-bold text-error">{t("delete")}</button>
      {submit.dialog}
      <ConfirmDialog
        open={unpublishing} title={t("unpublish")} body={t("unpublishBody")} confirmLabel={t("unpublish")} danger
        onClose={() => setUnpublishing(false)}
        onConfirm={() => run(() => unpublishArticle(article.id, article.rowVersion), t("unpublished"))}
      />
      <ConfirmDialog
        open={deleting} title={t("delete")} body={t("deleteBody")} confirmLabel={t("delete")} danger
        onClose={() => setDeleting(false)}
        onConfirm={async () => {
          try { await deleteArticle(article.id); toast(t("deleted")); onDeleted(); }
          catch (error) { toast(toFormErrors(error, t("failed")).general ?? t("failed"), "error"); throw error; }
        }}
      />
    </section>
  );
}
