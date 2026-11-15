import { afterEach, beforeEach, describe, expect, it } from "vitest";
import { ApiError, apiJson, fileNameFromDisposition, refreshSession } from "./client";
import { session } from "./session";
import { authResponse, mockFetch } from "../test/utils";

describe("api client session handling", () => {
  beforeEach(() => session.reset());
  afterEach(() => session.reset());

  it("refreshes once on 401 and retries the request with the new token", async () => {
    session.set(authResponse(["User"], "expired-token"));
    const { calls } = mockFetch(({ url, headers }) => {
      if (url.endsWith("/auth/refresh")) return { status: 200, body: authResponse(["User"], "fresh-token") };
      if (url.endsWith("/records")) {
        return headers.get("Authorization") === "Bearer fresh-token"
          ? { status: 200, body: { ok: true } }
          : { status: 401, body: { title: "No autenticado" } };
      }
      return undefined;
    });

    await expect(apiJson("/records")).resolves.toEqual({ ok: true });
    expect(calls.map((c) => c.url.split("/api/v1")[1])).toEqual(["/records", "/auth/refresh", "/records"]);
    expect(session.getAccessToken()).toBe("fresh-token");
    expect(session.getState().status).toBe("authenticated");
  });

  it("shares one refresh between concurrent 401 responses", async () => {
    session.set(authResponse(["User"], "expired-token"));
    let refreshes = 0;
    mockFetch(({ url, headers }) => {
      if (url.endsWith("/auth/refresh")) {
        refreshes += 1;
        return { status: 200, body: authResponse(["User"], "fresh-token") };
      }
      return headers.get("Authorization") === "Bearer fresh-token" ? { status: 200, body: [] } : { status: 401 };
    });

    await Promise.all([apiJson("/records"), apiJson("/dashboard"), apiJson("/catalogs/users")]);
    expect(refreshes).toBe(1);
  });

  it("clears the session (expired) when the refresh fails and never loops", async () => {
    session.set(authResponse(["User"], "expired-token"));
    const { calls } = mockFetch(({ url }) =>
      url.endsWith("/auth/refresh") ? { status: 401, body: { title: "No autenticado" } } : { status: 401 });

    const error = await apiJson("/records").catch((e: unknown) => e);
    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).status).toBe(401);
    expect(calls).toHaveLength(2);
    expect(session.getState()).toMatchObject({ status: "anonymous", reason: "expired" });
    expect(session.getAccessToken()).toBeNull();
  });

  it("does not try to refresh for authentication endpoints", async () => {
    const { calls } = mockFetch(() => ({ status: 401 }));
    await expect(apiJson("/auth/login", { method: "POST", body: {} })).rejects.toBeInstanceOf(ApiError);
    expect(calls).toHaveLength(1);
  });

  it("exposes ProblemDetails field errors in camelCase", async () => {
    session.set(authResponse(["RecordsManager"]));
    mockFetch(() => ({ status: 400, body: { title: "Datos no válidos", errors: { RecordNumber: ["Este campo es obligatorio."] } } }));
    const error = (await apiJson("/records", { method: "POST", body: {} }).catch((e: unknown) => e)) as ApiError;
    expect(error.fieldErrors).toEqual({ recordNumber: "Este campo es obligatorio." });
  });

  it("restores a session from the refresh cookie", async () => {
    mockFetch(({ url }) => (url.endsWith("/auth/refresh") ? { status: 200, body: authResponse(["Administrator"]) } : undefined));
    await expect(refreshSession()).resolves.toBe("access-1");
    expect(session.getState().user?.roles).toEqual(["Administrator"]);
  });
});

describe("fileNameFromDisposition", () => {
  it("prefers the RFC 5987 encoded name", () => {
    expect(fileNameFromDisposition("attachment; filename=informe.pdf; filename*=UTF-8''informe%20a%C3%B1o.pdf", "x"))
      .toBe("informe año.pdf");
    expect(fileNameFromDisposition('attachment; filename="acta.docx"', "x")).toBe("acta.docx");
    expect(fileNameFromDisposition(null, "fallback")).toBe("fallback");
  });
});
