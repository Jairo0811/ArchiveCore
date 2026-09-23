# Phase 5 — Records & Documents

Implemented:

- Authenticated records listing, detail, creation and updates.
- Authenticated document listing, detail and logical deletion.
- Document categories backed by the normalized database model.
- Physical document version upload.
- SHA-256 integrity hash per file version.
- One logical document with multiple immutable file-version rows.
- Local filesystem storage abstraction for development.
- Current-version tracking integrated with the database model.

The storage implementation is deliberately isolated in Infrastructure so a future object-storage adapter can replace local files without changing the application contracts.
