export interface FaqEntry {
  id: string;
  question: string;
  answer: string;
  /** A YouTube embed address; the video loads only once the answer is opened. */
  embedUrl?: string;
}

/** FAQ entries as native `<details>` disclosures. */
export function FaqList({ entries, videoTitle }: { entries: FaqEntry[]; videoTitle?: (question: string) => string }) {
  return (
    <div className="flex flex-col gap-2.5">
      {entries.map((entry) => (
        <details key={entry.id} className="rounded-card border border-line bg-surface px-5 py-4">
          <summary className="cursor-pointer font-bold">{entry.question}</summary>
          <p className="mb-0 mt-2.5 whitespace-pre-line leading-7 text-ink-muted">{entry.answer}</p>
          {entry.embedUrl ? (
            <iframe
              src={entry.embedUrl}
              title={videoTitle ? videoTitle(entry.question) : entry.question}
              loading="lazy"
              referrerPolicy="strict-origin-when-cross-origin"
              allow="encrypted-media; picture-in-picture"
              allowFullScreen
              className="mt-4 aspect-video w-full rounded-control border-0"
            />
          ) : null}
        </details>
      ))}
    </div>
  );
}
