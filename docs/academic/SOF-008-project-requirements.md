# Original SOF-008 assignment requirements

## Course

**Bases de Datos Avanzadas (SOF-008)**

**Professor:** Carlos Caraballos

## Project brief

The original practical project required selecting a company or business and designing a database that addressed its information needs.

The required deliverables were:

- Describe the business and its information needs.
- Propose an application that responds to those needs.
- Present the database design for the proposed application.
  - Conceptual model.
  - Physical model.
- Present examples of SQL queries the application would execute.

The assignment explicitly clarified that the objective was **not to build the application**, but to propose it and design the database that application would consume.

## Database requirements

The design had to consider:

- Basic normalization rules.
- Necessary indexes so queries could execute with the required performance.
- Audit tracking for the most important entities.
- Use of database mechanisms such as **triggers** and **stored procedures** for that tracking.

## ArchiveCore reconstruction interpretation

The 2026 reconstruction keeps the database as the central technical artifact and adds a real application only as a modern implementation layer around it.
