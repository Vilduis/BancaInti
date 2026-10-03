# BancaInti — Evaluación de Desempeño

Aplicación web para gestionar la **evaluación de desempeño anual** de los trabajadores de una entidad financiera. RRHH configura el proceso, cada jefe evalúa a su equipo y cada colaborador sigue sus objetivos y resultados.

## Funcionalidades

**RRHH (Administrador)**
- Periodos estratégicos y operativos (un periodo activo a la vez) con sus etapas: Registro, Seguimiento y Evaluación.
- Reglas por periodo: número de objetivos, pesos, días mínimos de participación y rangos de calificación.
- Justificaciones de ausencia (descansos médicos, licencias, vacaciones) y cálculo automático de días efectivos: quien no supera los días mínimos no participa.
- Asignación evaluador → evaluados.
- Consultas filtrables con exportación a Excel y auditoría de cambios.

**Evaluador (jefe directo)**
- Registra los objetivos de cada evaluado (con pesos que suman 100 %) y los confirma.
- Registra avances durante el seguimiento y comenta con el colaborador.
- Cierra la evaluación: el puntaje se calcula por cumplimiento × peso y se ubica en la escala del periodo.
- Exporta a PDF la ficha de un evaluado o el reporte de todo su equipo.

**Colaborador**
- Consulta sus objetivos, su avance, los comentarios y su evaluación final.

## Tecnologías

- **ASP.NET Core MVC** (.NET 10), con un área por rol
- **Entity Framework Core** + **PostgreSQL**
- **ASP.NET Core Identity** con roles
- **Bootstrap 5** y **DataTables** (búsqueda, filtros y exportación a Excel)
- **[InkSharp](https://github.com/Vilduis/InkSharp)** para los reportes PDF
