import { useUserLookup } from "../hooks/useUserLookup";

/** Active users by full name (never by raw id). */
export function UserSelect({
  value,
  onChange,
  emptyLabel,
  excludeUserId,
  id,
  ...aria
}: {
  value: number | null;
  onChange: (value: number | null) => void;
  emptyLabel: string;
  excludeUserId?: number | null;
  id?: string;
  "aria-invalid"?: boolean;
  "aria-describedby"?: string;
}) {
  const users = useUserLookup();
  return (
    <select
      id={id}
      {...aria}
      value={value ?? ""}
      disabled={users.isLoading}
      onChange={(event) => onChange(event.target.value ? Number(event.target.value) : null)}
    >
      <option value="">{users.isLoading ? "Cargando usuarios…" : emptyLabel}</option>
      {(users.data ?? [])
        .filter((user) => user.userId !== excludeUserId)
        .map((user) => (
          <option key={user.userId} value={user.userId}>{user.fullName}</option>
        ))}
    </select>
  );
}
