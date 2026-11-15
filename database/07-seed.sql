USE ArchiveCoreDb;
GO

/* Role names are stable technical identifiers used by the API authorization
   policies; the frontend shows Spanish labels for them. */
INSERT INTO dbo.Roles (Name, Description, IsSystemRole)
VALUES
(N'Administrator', N'Administración completa: usuarios, auditoría y SQL Lab.', 1),
(N'RecordsManager', N'Gestiona expedientes, documentos, versiones y workflow.', 1),
(N'User', N'Acceso operativo de solo lectura.', 1);
GO

INSERT INTO dbo.RecordStatuses (Code, Name, Description, IsFinal, SortOrder)
VALUES
('OPEN', N'Abierto', N'El expediente está activo y disponible para su gestión.', 0, 10),
('IN_REVIEW', N'En revisión', N'El expediente se encuentra en revisión.', 0, 20),
('CLOSED', N'Cerrado', N'El ciclo de vida del expediente ha concluido.', 1, 90),
('CANCELLED', N'Cancelado', N'El expediente fue cancelado.', 1, 99);
GO

INSERT INTO dbo.MovementTypes (Code, Name, Description)
VALUES
('ASSIGN', N'Asignación', N'Asigna un expediente a un usuario.'),
('TRANSFER', N'Transferencia', N'Transfiere la responsabilidad entre usuarios.'),
('RETURN', N'Devolución', N'Devuelve un expediente a un responsable anterior.'),
('CLOSE', N'Cierre', N'Cierra el flujo de trabajo del expediente.');
GO

INSERT INTO dbo.DocumentCategories (Code, Name, Description)
VALUES
('GENERAL', N'General', N'Documentación de propósito general.'),
('REQUEST', N'Solicitud', N'Solicitud o presentación inicial.'),
('EVIDENCE', N'Evidencia', N'Evidencias o anexos de soporte.'),
('REPORT', N'Informe', N'Informe formal o documento de resultado.');
GO
