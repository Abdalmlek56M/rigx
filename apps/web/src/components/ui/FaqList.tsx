import { cardClass } from "./Card";

export interface FaqEntry {
  id: string;
  question: string;
  answer: string;
  /** A YouTube embed address; the video loads only once the answer is opened. */
  embedUrl?: string;
}

interface FaqListProps {
  entries: FaqEntry[];
  videoTitle?: (question: string) => string;
  /** Plain rows for a list already inside a card; the default is one card per question. */
  plain?: boolean;
}

/**
 * FAQ entries as native `<details>` disclosures: a chevron that turns when opened, and no JavaScript. `plain` keeps the
 * rows inside a group card; the default gives each question its own card.
 */
export function FaqList({ entries, videoTitle, plain = false }: FaqListProps) {
  return (
    <div className={plain ? "flex flex-col" : "flex flex-col gap-2.5"}>
      {entries.map((entry) => (
        <details
          key={entry.id}
          className={`group ${plain ? "border-b border-line py-3 last:border-b-0" : cardClass({ padding: "sm" })}`}
        >
          <summary className="flex cursor-pointer list-none items-center justify-between gap-3 text-sm font-bold [&::-webkit-details-marker]:hidden">
            <span>{entry.question}</span>
            <span
              aria-hidden="true"
              className="flex shrink-0 text-ink-muted transition-transform duration-150 group-open:rotate-180 motion-reduce:transition-none"
            >
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M6 9l6 6 6-6" />
              </svg>
            </span>
          </summary>
          <p className="mb-0 mt-3 whitespace-pre-line text-sm leading-7 text-ink-muted">{entry.answer}</p>
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
