import { useState, type FormEvent } from "react";
import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { ApiError, errorMessage } from "../../api/client";
import { useAuth } from "../../auth/AuthContext";
import { PageState } from "../../components/PageState";

export function LoginPage() {
  const { state, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: string } | null)?.from ?? "/";

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  if (state.status === "loading") return <PageState kind="loading" text="Restaurando sesión…" fullScreen />;
  if (state.status === "authenticated") return <Navigate to={from} replace />;

  const notice =
    state.reason === "expired" ? "Tu sesión expiró. Inicia sesión de nuevo para continuar."
      : state.reason === "logout" ? "Cerraste sesión correctamente."
        : null;

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError("");

    try {
      await login(email.trim(), password);
      navigate(from, { replace: true });
    } catch (err) {
      if (err instanceof ApiError && err.status === 401) {
        setError("No pudimos iniciar sesión. Verifica tu correo y contraseña.");
      } else if (err instanceof ApiError && err.status === 429) {
        setError("Demasiados intentos. Espera un minuto antes de volver a intentarlo.");
      } else {
        setError(errorMessage(err, "No pudimos iniciar sesión."));
      }
      setPassword("");
    } finally {
      setBusy(false);
    }
  };

  return (
    <main className="login-page">
      <section className="login-card" aria-labelledby="login-title">
        <h1 id="login-title" className="visually-hidden">Iniciar sesión en ArchiveCore</h1>
        <picture>
          <source srcSet="/archivecore-logo-full.webp" type="image/webp" />
          <img
            className="login-logo login-logo-full"
            src="/archivecore-logo-full.png"
            alt="ArchiveCore — Documents, Data, Audit, Trust"
            width="360"
            height="203"
          />
        </picture>

        {notice && <p className="login-notice" role="status">{notice}</p>}

        <form onSubmit={submit} noValidate={false}>
          <label>
            Correo
            <input type="email" name="email" autoComplete="username" value={email}
              onChange={(event) => setEmail(event.target.value)} required maxLength={256} />
          </label>
          <label>
            Contraseña
            <input type="password" name="password" autoComplete="current-password" value={password}
              onChange={(event) => setPassword(event.target.value)} required maxLength={128} />
          </label>
          {error && <p className="error" role="alert">{error}</p>}
          <button type="submit" className="primary" disabled={busy}>{busy ? "Entrando…" : "Iniciar sesión"}</button>
        </form>
      </section>
    </main>
  );
}
