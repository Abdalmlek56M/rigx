import { useTranslations } from "next-intl";
import type { Schemas } from "@rigx/api-client";
import { describeIssue } from "./issue-text";

interface IssueListProps {
  issues: Schemas["BuilderIssue"][];
  slotNames: Readonly<Record<string, string>>;
  specNames: Readonly<Record<string, string>>;
}

/** Compatibility problems in plain Arabic: conflicts in red (the build cannot be ordered), warnings in amber. */
export function IssueList({ issues, slotNames, specNames }: IssueListProps) {
  const t = useTranslations("builder.issues");
  if (issues.length === 0) return null;

  return (
    <ul className="m-0 flex list-none flex-col gap-1.5 p-0 text-[13px] leading-6">
      {issues.map((issue, index) => {
        const text = describeIssue(issue, slotNames, specNames);
        return (
          <li key={`${issue.code}-${issue.slot}-${issue.otherSlot}-${issue.specKey}-${index}`} className={`flex gap-2 ${text.conflict ? "text-[#FF8A7A]" : "text-[#FFC94D]"}`}>
            <span aria-hidden="true">{text.conflict ? "✕" : "!"}</span>
            <span>
              <span className="sr-only">{t(text.conflict ? "conflict" : "warning")}: </span>
              {t(text.key, text.values)}
            </span>
          </li>
        );
      })}
    </ul>
  );
}
