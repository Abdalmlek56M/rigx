import { Fragment } from "react";
import { parsePolicyBody } from "./policy-body";

/** A policy's text: paragraphs, headings and lists as real elements (text only, no HTML from the content). */
export function PolicyBody({ body }: { body: string }) {
  return (
    <div className="flex flex-col gap-4 leading-8">
      {parsePolicyBody(body).map((block, index) => {
        if (block.type === "heading") return <h2 key={index} className="m-0 mt-2 text-xl font-bold">{block.text}</h2>;
        if (block.type === "list") {
          return (
            <ul key={index} className="m-0 flex list-disc flex-col gap-1.5 ps-6">
              {block.items.map((item, i) => <li key={i}>{item}</li>)}
            </ul>
          );
        }
        return (
          <p key={index} className="m-0">
            {block.lines.map((line, i) => (
              <Fragment key={i}>
                {i > 0 ? <br /> : null}
                {line}
              </Fragment>
            ))}
          </p>
        );
      })}
    </div>
  );
}
