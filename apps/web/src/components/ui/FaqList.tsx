export interface FaqEntry {
  id: string;
  question: string;
  answer: string;
}

/** FAQ entries as native `<details>` disclosures. */
export function FaqList({ entries }: { entries: FaqEntry[] }) {
  return (
    <div className="flex flex-col gap-2.5">
      {entries.map((entry) => (
        <details key={entry.id} className="rounded-card border border-line bg-surface px-5 py-4">
          <summary className="cursor-pointer font-bold">{entry.question}</summary>
          <p className="mb-0 mt-2.5 whitespace-pre-line leading-7 text-ink-muted">{entry.answer}</p>
        </details>
      ))}
    </div>
  );
}
