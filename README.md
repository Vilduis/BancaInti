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

Cada acción está habilitada solo en la etapa que le corresponde. Las reglas completas están en [docs/negocio.md](docs/negocio.md).

## Tecnologías

- **ASP.NET Core MVC** (.NET 10), con un área por rol
- **Entity Framework Core** + **PostgreSQL**
- **ASP.NET Core Identity** con roles
- **Bootstrap 5** y **DataTables** (búsqueda, filtros y exportación a Excel)
- **[InkSharp](https://github.com/Vilduis/InkSharp)** para los reportes PDF

## Puesta en marcha

Requisitos: [.NET 10 SDK](https://dotnet.microsoft.com/download) y PostgreSQL.

1. Clonar el repositorio.
2. Configurar la cadena de conexión fuera del código, con user-secrets:

   ```bash
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=BancaIntiDB;Username=postgres;Password=<clave>"
   ```

3. Ejecutar:

   ```bash
   dotnet run
   ```

   Al iniciar se aplican las migraciones y se cargan datos de ejemplo: el periodo 2026 activo, 20 trabajadores, justificaciones y asignaciones.

## Usuarios de prueba

En el entorno de desarrollo la contraseña de cada trabajador son sus iniciales en mayúscula (nombre + apellido).

| Rol | Usuario | Contraseña |
|---|---|---|
| Administrador | admin@bancainti.pe | `AD` |
| RRHH (Admin + Colaborador) | rosa.quispe@bancainti.pe | `RQ` |
| Evaluador | maria.torres@bancainti.pe | `MT` |
| Colaborador | pedro.sanchez@bancainti.pe | `PS` |

El resto de trabajadores sigue el formato `nombre.apellido@bancainti.pe`.

> Estas contraseñas cortas solo se aceptan en desarrollo; en otros entornos rige la política de contraseñas por defecto de Identity.

## Estructura

```
Areas/
  Admin/         RRHH: configuración, personal, consultas
  Evaluador/     equipo, objetivos, seguimiento, evaluación y PDF
  Colaborador/   consulta de objetivos y resultados
  Identity/      login en español
Data/            DbContext, migraciones, datos de ejemplo y auditoría
Models/          entidades y view models
Services/        reglas de negocio y generación de PDF
Views/Shared/    layouts y parciales
wwwroot/         estilos y scripts
tests/           regresión end-to-end (bash + curl)
```

## Pruebas

`tests/regresion.sh` recorre el proceso completo: un evaluador registra objetivos, hace el seguimiento, evalúa y exporta los PDF, y el colaborador consulta su resultado. Se ejecuta con la app corriendo en el puerto 5199:

```bash
ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5199 dotnet run
PGPASSWORD=<clave> bash tests/regresion.sh
```
