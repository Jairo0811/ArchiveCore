import { screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { afterEach, beforeEach, describe, expect, it } from "vitest";
import App from "./App";
import { session } from "./api/session";
import type { RoleName } from "./api/types";
import { authResponse, dashboardBody, emptyPage, mockFetch, renderApp } from "./test/utils";

function apiFor(roles: RoleName[] | null) {
  return mockFetch(({ url }) => {
    if (url.endsWith("/auth/refresh")) return roles ? { status: 200, body: authResponse(roles) } : { status: 401 };
    if (url.endsWith("/auth/login")) return roles ? { status: 200, body: authResponse(roles) } : { status: 401 };
    if (url.endsWith("/auth/revoke")) return { status: 204 };
    if (url.endsWith("/health")) return { status: 200, body: "Healthy" };
    if (url.includes("/dashboard")) return { status: 200, body: dashboardBody() };
    if (url.includes("/records?")) return { status: 200, body: emptyPage() };
    if (url.includes("/catalogs/")) return { status: 200, body: [] };
    if (url.includes("/audit")) return { status: 200, body: { ...emptyPage(), pageSize: 25 } };
    return undefined;
  });
}

describe("routing, roles and session lifecycle", () => {
  beforeEach(() => session.reset());
  afterEach(() => session.reset());

  it("redirects anonymous visitors of protected routes to the login page", async () => {
    apiFor(null);
    renderApp(<App />, "/records");
    expect(await screen.findByRole("button", { name: "Iniciar sesión" })).toBeInTheDocument();
    expect(screen.getByLabelText("Correo")).toBeInTheDocument();
  });

  it("logs in and lands on the dashboard with real metrics only", async () => {
    const user = userEvent.setup();
    const { calls } = apiFor(["RecordsManager"]);
    // No refresh cookie on first load.
    session.clear();
    renderApp(<App />, "/login");

    await user.type(screen.getByLabelText("Correo"), "ana@example.test");
    await user.type(screen.getByLabelText("Contraseña"), "Una-Clave-Segura-1");
    await user.click(screen.getByRole("button", { name: "Iniciar sesión" }));

    expect(await screen.findByRole("heading", { name: "Centro de control" })).toBeInTheDocument();
    expect(await screen.findByText("API y base de datos disponibles")).toBeInTheDocument();
    expect(screen.queryByText(/\+\d+%/)).not.toBeInTheDocument();
    expect(calls.find((c) => c.url.endsWith("/auth/login"))).toBeTruthy();
  });

  it("shows an error message for invalid credentials", async () => {
    const user = userEvent.setup();
    apiFor(null);
    session.clear();
    renderApp(<App />, "/login");

    await user.type(screen.getByLabelText("Correo"), "ana@example.test");
    await user.type(screen.getByLabelText("Contraseña"), "incorrecta-123");
    await user.click(screen.getByRole("button", { name: "Iniciar sesión" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Verifica tu correo y contraseña");
  });

  it("hides administration for the User role and blocks direct navigation", async () => {
    apiFor(["User"]);
    renderApp(<App />, "/audit");

    expect(await screen.findByText("Acceso restringido")).toBeInTheDocument();
    const nav = screen.getByRole("navigation", { name: "Navegación principal" });
    expect(within(nav).queryByText("Auditoría")).not.toBeInTheDocument();
    expect(within(nav).queryByText("Usuarios")).not.toBeInTheDocument();
    expect(within(nav).queryByText(/SQL Lab/)).not.toBeInTheDocument();
  });

  it("does not offer record mutations to the User role", async () => {
    apiFor(["User"]);
    renderApp(<App />, "/records");
    expect(await screen.findByText("Todavía no hay expedientes registrados.")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Nuevo expediente/ })).not.toBeInTheDocument();
  });

  it("shows administration links and pages to Administrators", async () => {
    apiFor(["Administrator"]);
    renderApp(<App />, "/audit");
    expect(await screen.findByRole("heading", { name: "Auditoría" })).toBeInTheDocument();
    expect(await screen.findByText("No hay eventos que coincidan con los filtros.")).toBeInTheDocument();
    const nav = screen.getByRole("navigation", { name: "Navegación principal" });
    expect(within(nav).getByText("Usuarios")).toBeInTheDocument();
    expect(within(nav).getByText("SQL Lab (SOF-008)")).toBeInTheDocument();
  });

  it("logout revokes the refresh token before clearing the local session", async () => {
    const user = userEvent.setup();
    const { calls } = apiFor(["RecordsManager"]);
    renderApp(<App />, "/");

    await user.click(await screen.findByRole("button", { name: "Cerrar sesión" }));

    expect(await screen.findByText("Cerraste sesión correctamente.")).toBeInTheDocument();
    expect(calls.some((c) => c.url.endsWith("/auth/revoke") && c.method === "POST")).toBe(true);
    expect(session.getAccessToken()).toBeNull();
  });

  it("renders an error state with retry when the API fails", async () => {
    mockFetch(({ url }) => {
      if (url.endsWith("/auth/refresh")) return { status: 200, body: authResponse(["User"]) };
      if (url.includes("/dashboard")) return { status: 500, body: { title: "Error inesperado", detail: "Ocurrió un error inesperado." } };
      return { status: 200, body: emptyPage() };
    });
    renderApp(<App />, "/");

    const alert = await screen.findByRole("alert");
    expect(alert).toHaveTextContent("No se pudo cargar el dashboard");
    expect(within(alert).getByRole("button", { name: "Reintentar" })).toBeInTheDocument();
  });

  it("restores the session after a reload through the refresh cookie", async () => {
    apiFor(["RecordsManager"]);
    renderApp(<App />, "/records");
    await waitFor(() => expect(screen.getByRole("heading", { name: "Expedientes" })).toBeInTheDocument());
    expect(screen.getByRole("button", { name: /Nuevo expediente/ })).toBeInTheDocument();
  });
});
