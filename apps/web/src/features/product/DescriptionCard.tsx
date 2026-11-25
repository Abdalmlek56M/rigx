import { useTranslations } from "next-intl";
import { Card } from "@/components/ui/Card";
import { DocumentIcon } from "@/components/ui/icons";
import { descriptionParagraphs } from "./description";

/** The owner's description of the item, in readable paragraphs. */
export function DescriptionCard({ text }: { text: string }) {
  const t = useTranslations("product");
  return (
    <Card title={t("description")} icon={DocumentIcon} padding="md">
      <div className="flex max-w-3xl flex-col gap-3">
        {descriptionParagraphs(text).map((paragraph, index) => (
          <p key={index} dir="auto" className="m-0 whitespace-pre-line text-[15px] leading-7 [overflow-wrap:anywhere]">{paragraph}</p>
        ))}
      </div>
    </Card>
  );
}
