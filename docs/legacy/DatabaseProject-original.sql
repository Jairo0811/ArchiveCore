Create Database DataBaseProject
Use DatabaseProject


--TABLA ADMINISTRADOR--
Create Table Administrador(
IdAdmin INT Primary Key,
IdArchivo INT          NOT NULL,
Nombre   Varchar (100) NOT NULL,
Apellido Varchar (100) NOT NULL,
Cedula   Varchar (13)  NOT NULL,
Telefono Numeric,
)
INSERT INTO Administrador (IdAdmin,IdArchivo,Nombre,Apellido,Cedula,Telefono) Values ('1','08','Nombre','Apellido','00000000000','00000000000')


SELECT * FROM Administrador



--TABLA USUARIOS--
Create Table Usuarios(
IdUser     INT Identity Primary Key,
IdRegistro INT           NOT NULL,
Nombre   Varchar (100)   NOT NULL,
Apellido Varchar (100)   NOT NULL,
Sexo     Varchar (20)    NOT NULL,
Cedula   Varchar (13)    NOT NULL,
FechaNacimiento DATETIME NOT NULL,
)

INSERT INTO Usuarios (IdRegistro,Nombre,Apellido,Sexo,Cedula,FechaNacimiento) Values ('0001','Nombre','Apellido','Masculino','00000000000','2000-01-01')

SELECT * FROM Usuarios
