import { AlertIcon } from "./icons";
import { Button } from "./Button";
import { Card } from "./Card";

interface ErrorBlockProps {
  message: string;
  /** The retry action; omit on a page that cannot retry by itself. */
  onRetry?: () => void;
  retryLabel?: string;
  className?: string;
}

/** A failed load, in a card: what happened and one retry action. Used by every list and panel. */
export function ErrorBlock({ message, onRetry, retryLabel, className }: ErrorBlockProps) {
  return (
    <Card className={`items-start gap-3 ${className ?? ""}`} padding="md">
      <div role="alert" className="flex items-start gap-3">
        <span aria-hidden="true" className="flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-error/10 text-error">
          <AlertIcon size={18} />
        </span>
        <p className="m-0 text-sm font-bold leading-6">{message}</p>
      </div>
      {onRetry && retryLabel ? <Button variant="secondary" onClick={onRetry}>{retryLabel}</Button> : null}
    </Card>
  );
}
