import { useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { ApiError, errorMessage } from "../../api/client";
import { usersApi } from "../../api/endpoints";
import type { RoleName, UserListItem } from "../../api/types";
import { useAuth } from "../../auth/AuthContext";
import { ROLE_LABELS, roleLabel } from "../../auth/permissions";
import { Field } from "../../components/Field";
import { Icon } from "../../components/Icon";
import { ConfirmDialog, Modal } from "../../components/Modal";
import { PageState, QueryError } from "../../components/PageState";
import { Tag } from "../../components/StatusTag";
import { PageHeader } from "../../layout/Shell";
import { formatDate } from "../../utils/format";

const PASSWORD_HINT = "Contraseña temporal de 12 a 128 caracteres con mayúscula, minúscula, número y símbolo. Compártela por un canal seguro.";

function useInvalidateUsers() {
  const queryClient = useQueryClient();
  return () => Promise.all([
    queryClient.invalidateQueries({ queryKey: ["users"] }),
    queryClient.invalidateQueries({ queryKey: ["catalogs", "users"] }),
    queryClient.invalidateQueries({ queryKey: ["dashboard"] }),
  ]);
}

export function UsersPage() {
  const { user: me } = useAuth();
  const [creating, setCreating] = useState(false);
  const [toggling, setToggling] = useState<UserListItem | null>(null);
  const users = useQuery({ queryKey: ["users"], queryFn: usersApi.list });

  return (
    <>
      <PageHeader
        eyebrow="Administración"
        title="Usuarios"
        description="Alta mínima de usuarios y control de acceso. Las cuentas nuevas inician sesión con la contraseña temporal asignada."
        actions={<button type="button" className="primary compact" onClick={() => setCreating(true)}><Icon name="plus" size={18} /> Nuevo usuario</button>}
      />

      <section className="table-card">
        {users.isLoading && <PageState kind="loading" text="Cargando usuarios…" />}
        {users.error && <QueryError error={users.error} onRetry={() => users.refetch()} text="No se pudieron cargar los usuarios" />}
        {users.data && (
          <div className="table-scroll">
            <table>
              <caption className="visually-hidden">Usuarios de ArchiveCore</caption>
              <thead><tr><th scope="col">Nombre</th><th scope="col">Correo</th><th scope="col">Roles</th><th scope="col">Estado</th><th scope="col">Alta</th><th scope="col"><span className="visually-hidden">Acciones</span></th></tr></thead>
              <tbody>
                {users.data.map((item) => (
                  <tr key={item.userId}>
                    <td>{item.firstName} {item.lastName}</td>
                    <td>{item.email}</td>
                    <td className="wrap">{item.roles.map(roleLabel).join(", ")}</td>
                    <td><Tag tone={item.isActive ? "open" : "closed"}>{item.isActive ? "Activo" : "Inactivo"}</Tag></td>
                    <td>{formatDate(item.createdAtUtc)}</td>
                    <td>
                      {item.userId !== me?.userId && (
                        <button type="button" className={item.isActive ? "secondary compact" : "primary compact"} onClick={() => setToggling(item)}>
                          {item.isActive ? "Desactivar" : "Activar"}
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
                {users.data.length === 0 && <tr><td colSpan={6} className="empty">No hay usuarios.</td></tr>}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {creating && <CreateUserModal onClose={() => setCreating(false)} />}
      {toggling && <ToggleUserDialog user={toggling} onClose={() => setToggling(null)} />}
    </>
  );
}

function ToggleUserDialog({ user, onClose }: { user: UserListItem; onClose: () => void }) {
  const invalidate = useInvalidateUsers();
  const mutation = useMutation({
    mutationFn: () => usersApi.setStatus(user.userId, !user.isActive),
    onSuccess: async () => {
      await invalidate();
      onClose();
    },
  });

  return (
    <ConfirmDialog
      title={user.isActive ? "Desactivar usuario" : "Activar usuario"}
      message={user.isActive
        ? `${user.firstName} ${user.lastName} no podrá iniciar sesión y sus sesiones activas se revocarán.`
        : `${user.firstName} ${user.lastName} podrá volver a iniciar sesión.`}
      confirmLabel={user.isActive ? "Desactivar" : "Activar"}
      tone={user.isActive ? "danger" : "primary"}
      busy={mutation.isPending}
      error={mutation.error ? errorMessage(mutation.error) : null}
      onConfirm={() => mutation.mutate()}
      onClose={onClose}
    />
  );
}

function generatePassword(): string {
  const groups = ["ABCDEFGHJKLMNPQRSTUVWXYZ", "abcdefghijkmnopqrstuvwxyz", "23456789", "!@#$%*-_=+?"];
  const all = groups.join("");
  const random = new Uint32Array(16);
  crypto.getRandomValues(random);
  const chars = Array.from(random, (value, index) => {
    const source = index < groups.length ? groups[index] : all;
    return source[value % source.length];
  });
  for (let i = chars.length - 1; i > 0; i -= 1) {
    const j = random[i] % (i + 1);
    [chars[i], chars[j]] = [chars[j], chars[i]];
  }
  return chars.join("");
}

function CreateUserModal({ onClose }: { onClose: () => void }) {
  const invalidate = useInvalidateUsers();
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [roles, setRoles] = useState<RoleName[]>(["User"]);
  const [isActive, setIsActive] = useState(true);
  const [errors, setErrors] = useState<Record<string, string>>({});

  const mutation = useMutation({
    mutationFn: () => usersApi.create({
      firstName: firstName.trim(),
      lastName: lastName.trim(),
      email: email.trim(),
      password,
      roles,
      isActive,
    }),
    onSuccess: async () => {
      await invalidate();
      onClose();
    },
    onError: (error) => setErrors(error instanceof ApiError ? error.fieldErrors : {}),
  });

  const toggleRole = (role: RoleName) =>
    setRoles((current) => (current.includes(role) ? current.filter((r) => r !== role) : [...current, role]));

  const submit = (event: FormEvent) => {
    event.preventDefault();
    const next: Record<string, string> = {};
    if (!firstName.trim()) next.firstName = "El nombre es obligatorio.";
    if (!lastName.trim()) next.lastName = "El apellido es obligatorio.";
    if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email.trim())) next.email = "Indica un correo válido.";
    if (password.length < 12) next.password = "La contraseña debe tener al menos 12 caracteres.";
    if (roles.length === 0) next.roles = "Asigna al menos un rol.";
    setErrors(next);
    if (Object.keys(next).length === 0) mutation.mutate();
  };

  const formError = mutation.error && Object.keys(errors).length === 0 ? errorMessage(mutation.error) : null;

  return (
    <Modal title="Nuevo usuario" onClose={onClose} busy={mutation.isPending}>
      <form className="form-grid two-columns" onSubmit={submit} noValidate>
        <Field label="Nombre" required error={errors.firstName}>
          <input value={firstName} onChange={(event) => setFirstName(event.target.value)} maxLength={100} autoFocus autoComplete="off" />
        </Field>
        <Field label="Apellido" required error={errors.lastName}>
          <input value={lastName} onChange={(event) => setLastName(event.target.value)} maxLength={100} autoComplete="off" />
        </Field>
        <div className="span-all">
          <Field label="Correo" required error={errors.email}>
            <input type="email" value={email} onChange={(event) => setEmail(event.target.value)} maxLength={256} autoComplete="off" />
          </Field>
        </div>
        <div className="span-all">
          <Field label="Contraseña temporal" required hint={PASSWORD_HINT} error={errors.password}>
            <input type={showPassword ? "text" : "password"} value={password} onChange={(event) => setPassword(event.target.value)} maxLength={128} autoComplete="new-password" />
          </Field>
          <div className="inline-tools">
            <button type="button" className="secondary compact" onClick={() => { setPassword(generatePassword()); setShowPassword(true); }}>Generar contraseña</button>
            <button type="button" className="secondary compact" onClick={() => setShowPassword((value) => !value)} aria-pressed={showPassword}>
              {showPassword ? "Ocultar" : "Mostrar"}
            </button>
          </div>
        </div>
        <fieldset className="span-all role-fieldset" aria-describedby={errors.roles ? "roles-error" : undefined}>
          <legend>Roles</legend>
          {(Object.keys(ROLE_LABELS) as RoleName[]).map((role) => (
            <label key={role} className="checkbox">
              <input type="checkbox" checked={roles.includes(role)} onChange={() => toggleRole(role)} />
              {ROLE_LABELS[role]}
            </label>
          ))}
          {errors.roles && <small id="roles-error" className="field-error">{errors.roles}</small>}
        </fieldset>
        <label className="checkbox span-all">
          <input type="checkbox" checked={isActive} onChange={(event) => setIsActive(event.target.checked)} />
          Cuenta activa
        </label>
        {formError && <p className="form-error span-all" role="alert">{formError}</p>}
        <div className="form-actions span-all">
          <button type="button" className="secondary" onClick={onClose} disabled={mutation.isPending}>Cancelar</button>
          <button type="submit" className="primary" disabled={mutation.isPending}>{mutation.isPending ? "Creando…" : "Crear usuario"}</button>
        </div>
      </form>
    </Modal>
  );
}
