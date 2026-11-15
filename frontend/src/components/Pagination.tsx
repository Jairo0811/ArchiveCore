export function Pagination({
  page,
  totalPages,
  totalCount,
  onPageChange,
  label = "registros",
}: {
  page: number;
  totalPages: number;
  totalCount: number;
  onPageChange: (page: number) => void;
  label?: string;
}) {
  if (totalCount === 0) return null;
  return (
    <nav className="pagination" aria-label="Paginación">
      <span>{totalCount.toLocaleString("es-DO")} {label} · página {page} de {Math.max(totalPages, 1)}</span>
      <div>
        <button type="button" className="secondary compact" disabled={page <= 1} onClick={() => onPageChange(page - 1)}>
          Anterior
        </button>
        <button type="button" className="secondary compact" disabled={page >= totalPages} onClick={() => onPageChange(page + 1)}>
          Siguiente
        </button>
      </div>
    </nav>
  );
}
