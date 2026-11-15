import { cloneElement, isValidElement, useId, type ReactElement } from "react";

type ControlProps = { id?: string; "aria-invalid"?: boolean; "aria-describedby"?: string };

/** Label + control + hint/error wiring (aria-invalid, aria-describedby). */
export function Field({
  label,
  hint,
  error,
  required,
  children,
}: {
  label: string;
  hint?: string;
  error?: string;
  required?: boolean;
  children: ReactElement<ControlProps>;
}) {
  const id = useId();
  const hintId = `${id}-hint`;
  const errorId = `${id}-error`;
  const describedBy = [hint ? hintId : null, error ? errorId : null].filter(Boolean).join(" ") || undefined;

  const control = isValidElement(children)
    ? cloneElement(children, { id, "aria-invalid": error ? true : undefined, "aria-describedby": describedBy })
    : children;

  return (
    <div className={`field${error ? " has-error" : ""}`}>
      <label htmlFor={id}>
        {label}
        {required && <span className="required" aria-hidden="true"> *</span>}
      </label>
      {control}
      {hint && <small id={hintId} className="field-hint">{hint}</small>}
      {error && <small id={errorId} className="field-error">{error}</small>}
    </div>
  );
}
