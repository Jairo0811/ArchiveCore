import type { ReactNode } from "react";

const PATHS: Record<string, ReactNode> = {
  home: <><path d="M3 11.5 12 4l9 7.5" /><path d="M5.5 10.5V20h13v-9.5" /><path d="M9 20v-6h6v6" /></>,
  folder: <><path d="M3 6h6l2 2h10v10H3z" /></>,
  file: <><path d="M6 3h8l4 4v14H6z" /><path d="M14 3v5h5" /><path d="M9 13h6M9 17h6" /></>,
  chart: <><path d="M4 20V10h4v10M10 20V5h4v15M16 20V8h4v12" /></>,
  users: <><circle cx="9" cy="8" r="3" /><circle cx="17" cy="9" r="2.5" /><path d="M3.5 20c.5-4 2.7-6 5.5-6s5 2 5.5 6" /><path d="M14 14.5c2.5.2 4.3 2 4.8 5.5" /></>,
  swap: <><path d="M4 8h13l-3-3M20 16H7l3 3" /><path d="M17 5l3 3-3 3M7 13l-3 3 3 3" /></>,
  archive: <><rect x="4" y="7" width="16" height="13" rx="2" /><path d="M3 4h18v4H3z" /><path d="M9 12h6" /></>,
  shield: <><path d="M12 3l8 3v5c0 5-3 8-8 10-5-2-8-5-8-10V6z" /><path d="m9 12 2 2 4-5" /></>,
  database: <><ellipse cx="12" cy="5" rx="7" ry="3" /><path d="M5 5v6c0 1.7 3.1 3 7 3s7-1.3 7-3V5" /><path d="M5 11v6c0 1.7 3.1 3 7 3s7-1.3 7-3v-6" /></>,
  network: <><circle cx="12" cy="5" r="2" /><circle cx="5" cy="18" r="2" /><circle cx="19" cy="18" r="2" /><path d="M12 7v4M12 11 6 16M12 11l6 5" /></>,
  lock: <><rect x="5" y="10" width="14" height="11" rx="2" /><path d="M8 10V7a4 4 0 0 1 8 0v3" /></>,
  search: <><circle cx="10.5" cy="10.5" r="6.5" /><path d="m16 16 5 5" /></>,
  logout: <><path d="M10 4H5v16h5" /><path d="M14 8l4 4-4 4M18 12H9" /></>,
  plus: <><path d="M12 5v14M5 12h14" /></>,
  download: <><path d="M12 4v11" /><path d="m7 10 5 5 5-5" /><path d="M5 20h14" /></>,
  upload: <><path d="M12 20V9" /><path d="m7 14 5-5 5 5" /><path d="M5 4h14" /></>,
  trash: <><path d="M4 7h16" /><path d="M9 7V4h6v3" /><path d="M6 7l1 13h10l1-13" /></>,
  edit: <><path d="M4 20h4L19 9l-4-4L4 16z" /><path d="m13 7 4 4" /></>,
  close: <><path d="M6 6l12 12M18 6 6 18" /></>,
  menu: <><path d="M4 7h16M4 12h16M4 17h16" /></>,
  check: <><path d="m5 12 5 5 9-10" /></>,
  history: <><path d="M4 12a8 8 0 1 0 2.4-5.7" /><path d="M4 4v4h4" /><path d="M12 8v4l3 2" /></>,
  arrowLeft: <><path d="M19 12H5" /><path d="m11 6-6 6 6 6" /></>,
};

export function Icon({ name, size = 20, label }: { name: keyof typeof PATHS | string; size?: number; label?: string }) {
  return (
    <svg
      className="icon"
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.8"
      strokeLinecap="round"
      strokeLinejoin="round"
      role={label ? "img" : undefined}
      aria-label={label}
      aria-hidden={label ? undefined : true}
      focusable="false"
    >
      {PATHS[name] ?? PATHS.file}
    </svg>
  );
}
