# Reglas de negocio — Evaluación de Desempeño BancaInti

Aplicativo para que RRHH gestione la evaluación de desempeño anual de los trabajadores.

## Roles

| Rol | Quién | Alcance |
|---|---|---|
| Admin | RRHH | Configura periodos, etapas, justificaciones, reglas y asignaciones. Consultas y auditoría. |
| Evaluador | Jefe directo | Gestiona objetivos, seguimiento y evaluación **solo de sus evaluados**. |
| Colaborador | Trabajador | Consulta sus objetivos, avance, comentarios y evaluación final. |

Un usuario puede tener varios roles (un evaluador también es colaborador de su jefe).

## Jerarquía de periodos

```
Periodo Estratégico (varios años)
 └── Periodo Operativo (un año, dentro del estratégico)
      └── Etapas: Registro → Seguimiento → Evaluación
```

- El operativo debe estar contenido en las fechas del estratégico.
- Las etapas se registran aparte (módulo Etapas), una por tipo en cada operativo; no se solapan, van en ese orden y dentro del operativo. Un operativo no puede cambiar sus fechas si deja etapas fuera. Las etapas del periodo activo no se eliminan (solo se ajustan sus fechas).
- Los periodos estratégicos no se cruzan entre sí; tampoco los operativos (uno por año).
- Solo un periodo operativo activo a la vez; activar uno desactiva el anterior. El activo no se puede eliminar.
- Un estratégico con operativos no se puede eliminar.
- La etapa vigente habilita las acciones del evaluador:
  - **Registro**: crear/editar objetivos.
  - **Seguimiento**: registrar avance y comentarios.
  - **Evaluación**: registrar resultados y calificación.

## Justificaciones

- Catálogo de tipos (descanso médico, licencia, maternidad, vacaciones, etc.).
- Por cada periodo operativo, RRHH define qué tipos aplican ese año (al crear el periodo se marcan todos los activos).
- Los tipos no se eliminan, se desactivan.
- Cada trabajador puede tener justificaciones con fecha inicio y fin: sin cruces entre sí, desde su ingreso y hasta su cese.

## Participación (> 90 días)

- Días efectivos = días calendario (lunes a domingo) del trabajador dentro del periodo operativo − días justificados (de tipos que aplican).
- Participa en la evaluación si días efectivos **> 90**.
- El tramo del trabajador inicia en el mayor entre su fecha de ingreso y el inicio del periodo.

## Asignación evaluador → evaluados

- Por periodo operativo; cada evaluado tiene un solo evaluador en el periodo.
- El evaluador debe tener el rol Evaluador y no puede evaluarse a sí mismo.
- Solo se asignan trabajadores vigentes durante el periodo (ingreso ≤ fin del periodo y sin cese antes del inicio).
- Quienes no superan los días mínimos se muestran como "no participa", pero RRHH puede asignarlos igual.

## Reglas de evaluación (por periodo operativo)

- Mínimo y máximo de objetivos por evaluado.
- Peso mínimo/máximo por objetivo; la suma de pesos debe ser **100 %**.
- Días mínimos de participación (90).
- Mín. objetivos × peso mín. ≤ 100 y máx. objetivos × peso máx. ≥ 100.
- Valores por defecto al crear el periodo: 3–6 objetivos, peso 10–40 %, 90 días.
- Puntaje final 0–100 = Σ (cumplimiento del objetivo × peso). Rangos enteros configurables, contiguos y que cubren 0–100:

| Puntaje | Calificación |
|---|---|
| 0 – 74 | No cumple |
| 75 – 79 | Bueno |
| 80 – 89 | Muy bueno |
| 90 – 100 | Sobresaliente |

## Flujo de evaluación

Estados de la asignación: **Borrador → Confirmado → Evaluado**. Solo opera sobre el periodo activo y la etapa vigente (fecha de hoy).

| Etapa | Evaluador puede | Condición |
|---|---|---|
| Registro | Crear/editar/eliminar objetivos; confirmar; reabrir | Editar solo en Borrador. Confirmar exige mín. de objetivos y suma de pesos = 100 %. Reabrir solo en Registro. |
| Seguimiento | Registrar avance acumulado por objetivo (fecha = hoy) | Estado Confirmado |
| Evaluación | Registrar resultado y % de cumplimiento (0–100) por objetivo + comentario final; cerrar | Estado Confirmado. Al cerrar queda Evaluado y no se modifica. |

- Objetivo: descripción, indicador, unidad de medida, peso (entre peso mín. y máx., suma ≤ 100 %), meta > 0.
- Avance % = último avance / meta × 100. Avance ponderado del evaluado = Σ min(avance %, 100) × peso.
- En la evaluación se sugiere como cumplimiento el último avance % (topado en 100); el evaluador lo ajusta.
- Puntaje final = Σ cumplimiento × peso (0–100, 2 decimales). Para la calificación se redondea al entero (74.5 → 75).
- Comentarios: evaluador y evaluado escriben en cualquier etapa; generales o sobre un objetivo.
- El evaluador solo ve y opera a sus evaluados. RRHH no puede quitar una asignación con objetivos.

## Experiencia por rol

- **Evaluador**: dashboard "Mi equipo" con el estado de cada evaluado; objetivos (descripción, peso, meta), seguimiento, evaluación y comentarios.
- **Colaborador**: al ingresar ve un mensaje de texto plano (configurable por periodo; si está vacío se usa uno por defecto); al pulsar **OK** pasa a "Mis objetivos" del periodo activo. Solo ve sus objetivos cuando están **Confirmados**; ve peso, meta, avance (por objetivo y general ponderado), historial de seguimiento, comentarios (puede responder) y, si está Evaluado, puntaje, calificación y comentario final. El menú "Mis objetivos" va directo, sin el mensaje.

## Consultas (Admin)

Sin periodo indicado se usa el activo. Los filtros de consulta cambian los datos traídos; además, como toda tabla del sistema, tienen búsqueda, filtros por columna, 15 filas por página y exportación a Excel de lo filtrado.

| Consulta | Contenido | Filtros de consulta · por columna |
|---|---|---|
| Participantes | Trabajadores vigentes en el periodo: días en periodo, justificados, efectivos y si participa (> días mínimos de la regla) | Periodo; incluir a quienes no participan · área, cargo, participa |
| Justificaciones | Justificaciones que se cruzan con el periodo: tipo, inicio, fin, días y si el tipo aplica ese año | Periodo · trabajador, área, tipo, aplica |
| Mayores de 60 | Trabajadores vigentes (sin cese a hoy) con edad **mayor** a la indicada; edad y años de servicio a hoy | Edad (60 por defecto) · área, cargo |
| Evaluadores y sus evaluados | Estado, objetivos, peso total, avance ponderado, puntaje y calificación | Periodo · evaluador, área, estado, calificación |
| Auditoría | Fecha, usuario, acción, entidad, Id, valores antes/después (JSON), IP. Máx. 2000 filas | Fechas, usuario, entidad · acción |

## Auditoría

- Se registra automáticamente toda alta, edición (solo campos cambiados) y baja de las entidades del dominio, más cada inicio de sesión.
- No se auditan las tablas de Identity (contienen hashes de contraseñas).
- Usuario = correo del usuario conectado; "sistema" para el seed.

## Datos de prueba

- 20 trabajadores ficticios (`Data/DbSeeder.cs`): 1 RRHH (Admin), 4 evaluadores (uno por área), 15 colaboradores.
- Usuario = `primernombre.primerapellido@bancainti.pe` sin tildes; contraseña = iniciales de nombre y apellido (Rosa Quispe → `RQ`). Solo en desarrollo.
- Cuenta técnica: `admin@bancainti.pe` / `AD`.
- Casos de prueba: mayores de 60 (T002, T008, T012, T018), ingresos recientes (T013, T016), cesada (T019), justificaciones en T006, T007, T010, T011, T013, T014, T017. No participa en 2026: T013 (81 días).
- Periodo base: Plan Estratégico 2025–2028 y operativo 2026 activo (Registro 01/01–31/03, Seguimiento 01/04–31/10, Evaluación 01/11–31/12). Equipos: T002 evalúa a Créditos y a los jefes T003–T005; T003 Operaciones; T004 Tecnología; T005 Comercial.
