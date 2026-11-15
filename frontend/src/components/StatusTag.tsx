const TONE_BY_CODE: Record<string, string> = {
  OPEN: "open",
  IN_REVIEW: "review",
  CLOSED: "closed",
  CANCELLED: "closed",
};

export function StatusTag({ code, label }: { code: string; label: string }) {
  const tone = TONE_BY_CODE[code] ?? "open";
  return <span className={`status-tag ${tone}`}><i aria-hidden="true" />{label}</span>;
}

export function Tag({ children, tone = "open" }: { children: string; tone?: "open" | "review" | "closed" | "danger" }) {
  return <span className={`status-tag ${tone}`}><i aria-hidden="true" />{children}</span>;
}
