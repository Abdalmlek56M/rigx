import { Fragment } from "react";
import { headingId, parsePolicyBody } from "./policy-body";

/** A policy's text: paragraphs, headings (with anchors for the table of contents) and lists as real elements. */
export function PolicyBody({ body }: { body: string }) {
  const blocks = parsePolicyBody(body);
  const headingIndexes = blocks.reduce<number[]>((indexes, block, index) => (block.type === "heading" ? [...indexes, index] : indexes), []);
  const idFor = (blockIndex: number) => headingId(headingIndexes.indexOf(blockIndex));

  return (
    <div className="flex flex-col gap-4 leading-8">
      {blocks.map((block, index) => {
        if (block.type === "heading") {
          return <h2 key={index} id={idFor(index)} className="m-0 mt-3 scroll-mt-24 type-section-title">{block.text}</h2>;
        }
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
