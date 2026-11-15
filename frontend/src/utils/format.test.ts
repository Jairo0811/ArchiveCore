import { describe, expect, it } from "vitest";
import { formatBytes, formatDate, parseUtc } from "./format";

describe("format", () => {
  it("treats timestamps without offset as UTC", () => {
    expect(parseUtc("2026-11-14T12:00:00").toISOString()).toBe("2026-11-14T12:00:00.000Z");
    expect(parseUtc("2026-11-14T12:00:00Z").toISOString()).toBe("2026-11-14T12:00:00.000Z");
  });

  it("formats dates in Spanish and handles missing values", () => {
    expect(formatDate(null)).toBe("—");
    expect(formatDate("not-a-date")).toBe("—");
    expect(formatDate("2026-11-14T12:00:00Z")).toMatch(/2026/);
  });

  it("formats byte sizes", () => {
    expect(formatBytes(512)).toBe("512 B");
    expect(formatBytes(5 * 1024 * 1024)).toBe("5 MB");
  });
});
