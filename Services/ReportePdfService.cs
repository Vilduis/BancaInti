using BancaInti.Models;
using BancaInti.Models.ViewModels;
using InkSharp;

namespace BancaInti.Services
{
    // Reportes PDF del evaluador (InkSharp): ficha individual y reporte del equipo.
    // El reporte del equipo es un solo PDF: una hoja de resumen y luego la ficha de cada evaluado,
    // cada una con su propia numeración de páginas.
    public class ReportePdfService(EvaluacionService evaluacionService)
    {
        // Misma paleta que site.css.
        private const string Azul = "#0B3D6E";
        private const string AzulSuave = "#E7EEF6";
        private const string Gris = "#F4F6F9";
        private const string Borde = "#C9D3DF";
        private const string Texto = "#1E293B";
        private const string Tenue = "#64748B";

        public byte[] Individual(AsignacionEvaluador a, Participacion participacion)
        {
            var pdf = Pdf.Create($"Evaluación de desempeño - {a.Evaluado!.NombreCompleto}").Author("BancaInti");
            pdf.Page(page => Ficha(page, a, participacion, "Página {page} de {pages}"));
            return pdf.ToBytes();
        }

        public byte[] Grupal(PeriodoOperativo periodo, Trabajador evaluador, List<AsignacionEvaluador> asignaciones, Dictionary<int, Participacion> participacion)
        {
            var pdf = Pdf.Create($"Evaluación de desempeño - Equipo de {evaluador.NombreCompleto}").Author("BancaInti");
            pdf.Page(page => Resumen(page, periodo, evaluador, asignaciones, participacion));
            foreach (var a in asignaciones)
                pdf.Page(page => Ficha(page, a, participacion[a.EvaluadoId], "Página {sectionPage} de {sectionPages}"));
            return pdf.ToBytes();
        }

        // "Evaluacion 2026 Pedro Sánchez Díaz" -> "Evaluacion_2026_Pedro_Sanchez_Diaz.pdf"
        public static string NombreArchivo(string texto)
        {
            var sinTildes = new string(texto.Normalize(System.Text.NormalizationForm.FormD)
                .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                .ToArray());
            return string.Join("_", sinTildes.Split(' ', StringSplitOptions.RemoveEmptyEntries)) + ".pdf";
        }

        // ---------- Ficha de un evaluado ----------

        private void Ficha(PageBuilder page, AsignacionEvaluador a, Participacion participacion, string numeracion)
        {
            var periodo = a.PeriodoOperativo!;
            Marco(page, periodo, "Ficha de evaluación de desempeño", a.Evaluado!.NombreCompleto, numeracion);

            page.Content().Column(col =>
            {
                col.Gap(10);
                col.Table(t => DatosPersonas(t, a, participacion)).KeepTogether();
                col.Table(t => TablaObjetivos(t, a));
                col.Table(t => TablaResultado(t, a, periodo.Regla!)).KeepTogether();
                col.Row(row => Firmas(row, a));
            });
        }

        // Evaluado y evaluador lado a lado: cada bloque es una cabecera combinada sobre dos columnas.
        private static void DatosPersonas(TableBlock t, AsignacionEvaluador a, Participacion p)
        {
            var e = a.Evaluado!;
            var ev = a.Evaluador!;
            Base(t);
            t.Columns(90, "*", 90, "*");

            Seccion(t, "DATOS DEL EVALUADO", 2);
            Seccion(t, "DATOS DEL EVALUADOR", 2);

            Par(t, "Nombre", e.NombreCompleto, negrita: true);
            Par(t, "Nombre", ev.NombreCompleto, negrita: true);
            Par(t, "Código / DNI", $"{e.Codigo}  ·  DNI {e.Dni}");
            Par(t, "Código / DNI", $"{ev.Codigo}  ·  DNI {ev.Dni}");
            Par(t, "Cargo", e.Cargo);
            Par(t, "Cargo", ev.Cargo);
            Par(t, "Área", e.Area);
            Par(t, "Área", ev.Area);
            Par(t, "Fecha de ingreso", e.FechaIngreso.ToString("dd/MM/yyyy"));

            // Situación de la evaluación: ocupa las dos filas finales del bloque del evaluador.
            Etiqueta(t, "Estado").RowSpan(2).AlignMiddle();
            t.Cell().RowSpan(2).AlignMiddle().Column(c =>
            {
                c.Text(EvaluacionService.EstadoTexto(a.Estado)).Bold();
                c.Text(a.FechaCierre is { } cierre ? $"Cerrada el {cierre.ToLocalTime():dd/MM/yyyy}" : "Evaluación sin cerrar").FontSize(7.5).Color(Tenue);
            });

            var participa = p.DiasEfectivos >= a.PeriodoOperativo!.Regla!.DiasMinimos;
            Etiqueta(t, "Días efectivos");
            t.Cell($"{p.DiasEfectivos} de {p.DiasPeriodo}" + (p.DiasJustificados > 0 ? $"  ({p.DiasJustificados} justificados)" : "")
                   + (participa ? "" : $"  ·  no participa (mínimo {a.PeriodoOperativo.Regla.DiasMinimos})"))
                .Color(participa ? Texto : "#C0392B");
        }

        // Cada objetivo combina sus datos hacia abajo (RowSpan) sobre las filas de sus avances.
        private static void TablaObjetivos(TableBlock t, AsignacionEvaluador a)
        {
            Base(t);
            t.Columns(22, "3*", "2*", 54, 36, 50, 54, 50, 44, 52, 44, 46);
            t.Header(h =>
            {
                h.Cell("N°").RowSpan(2).AlignMiddle();
                h.Cell("Objetivo").RowSpan(2).AlignMiddle();
                h.Cell("Indicador").RowSpan(2).AlignMiddle();
                h.Cell("Unidad").RowSpan(2).AlignMiddle();
                h.Cell("Peso").RowSpan(2).AlignMiddle();
                h.Cell("Meta").RowSpan(2).AlignMiddle();
                h.Cell("Seguimiento").ColumnSpan(3);
                h.Cell("Evaluación final").ColumnSpan(3);
                h.Row("Fecha", "Avance", "% meta", "Resultado", "Cumpl.", "Puntaje");
            });

            if (a.Objetivos.Count == 0)
            {
                t.Cell("No hay objetivos registrados.").ColumnSpan(12).AlignCenter().Italic().Color(Tenue).Padding(10);
                return;
            }

            var n = 0;
            foreach (var o in a.Objetivos)
            {
                n++;
                var fondo = n % 2 == 0 ? Gris : "#FFFFFF";
                var avances = o.Seguimientos.OrderBy(s => s.Fecha).ThenBy(s => s.Id).ToList();
                var filas = Math.Max(1, avances.Count);

                t.Cell(n.ToString()).RowSpan(filas).AlignMiddle().AlignCenter().Bold().Background(fondo);
                t.Cell(o.Descripcion).RowSpan(filas).AlignMiddle().Background(fondo);
                t.Cell(o.Indicador).RowSpan(filas).AlignMiddle().Background(fondo);
                t.Cell(o.UnidadMedida).RowSpan(filas).AlignMiddle().AlignCenter().Background(fondo);
                t.Cell($"{o.Peso} %").RowSpan(filas).AlignMiddle().AlignRight().Background(fondo);
                t.Cell(Num(o.Meta)).RowSpan(filas).AlignMiddle().AlignRight().Background(fondo);

                for (var i = 0; i < filas; i++)
                {
                    if (avances.Count == 0)
                        t.Cell("Sin avances").ColumnSpan(3).AlignMiddle().AlignCenter().Italic().Color(Tenue).Background(fondo);
                    else
                    {
                        var s = avances[i];
                        t.Cell(s.Fecha.ToString("dd/MM/yyyy")).AlignCenter().Background(fondo);
                        t.Cell(Num(s.Avance)).AlignRight().Background(fondo);
                        t.Cell($"{Math.Round(s.Avance / o.Meta * 100, 1):0.#} %").AlignRight().Background(fondo);
                    }

                    // La evaluación final va una vez por objetivo, combinada a lo alto de sus avances.
                    if (i == 0)
                    {
                        t.Cell(o.ResultadoFinal is { } r ? Num(r) : "—").RowSpan(filas).AlignMiddle().AlignRight().Background(fondo);
                        t.Cell(o.Cumplimiento is { } c ? $"{c} %" : "—").RowSpan(filas).AlignMiddle().AlignRight().Background(fondo);
                        t.Cell(o.Cumplimiento is { } c2 ? $"{(decimal)c2 * o.Peso / 100:0.##}" : "—").RowSpan(filas).AlignMiddle().AlignRight().Bold().Background(fondo);
                    }
                }
            }

            // Totales: etiquetas combinadas sobre las columnas que no suman.
            var suma = a.Objetivos.Sum(o => o.Peso);
            Total(t, "Peso total", 4).AlignRight();
            Total(t, $"{suma} %", 1).AlignRight().Color(suma == 100 ? Texto : "#C0392B");
            Total(t, "Avance ponderado", 3).AlignRight();
            Total(t, $"{EvaluacionService.AvancePonderado(a.Objetivos):0.#} %", 1).AlignRight();
            Total(t, "Puntaje final", 2).AlignRight();
            Total(t, a.PuntajeFinal is { } pf ? pf.ToString("0.##") : "—", 1).AlignRight();
        }

        // Puntaje, calificación y comentario, con la escala del periodo combinada debajo.
        private static void TablaResultado(TableBlock t, AsignacionEvaluador a, ReglaEvaluacion regla)
        {
            Base(t);
            t.Columns(110, 130, "*");
            t.Header("Puntaje final", "Calificación", "Comentario final del evaluador");

            var evaluado = a.PuntajeFinal is not null;
            t.Cell(evaluado ? a.PuntajeFinal!.Value.ToString("0.##") : "—").AlignMiddle().AlignCenter().FontSize(22).Bold().Color(Azul).Padding(10);
            if (evaluado)
                t.Cell(a.Calificacion!).AlignMiddle().AlignCenter().FontSize(12).Bold().Color(ColorTextoCalificacion(a.Calificacion)).Background(ColorCalificacion(a.Calificacion));
            else
                t.Cell("Pendiente").AlignMiddle().AlignCenter().FontSize(11).Italic().Color(Tenue).Background(Gris);
            t.Cell(evaluado ? a.ComentarioFinal ?? "" : "La evaluación aún no se ha cerrado.").AlignMiddle().Padding(8).Color(evaluado ? Texto : Tenue);

            var escala = string.Join("   ·   ", regla.Rangos.OrderBy(r => r.PuntajeMinimo).Select(r => $"{r.Nombre}: {r.PuntajeMinimo} – {r.PuntajeMaximo}"));
            t.Cell($"Escala de calificación del periodo:   {escala}").ColumnSpan(3).FontSize(7.5).Color(Tenue).Background(Gris);
        }

        private static void Firmas(RowBlock row, AsignacionEvaluador a)
        {
            row.Gap(60);
            Firma(row.Item(), a.Evaluador!, "Evaluador");
            Firma(row.Item(), a.Evaluado!, "Evaluado");
        }

        private static void Firma(RowItem item, Trabajador t, string rol) => item.Column(c =>
        {
            c.Space(18);
            c.Divider().Color(Texto).Thickness(0.6);
            c.Text(t.NombreCompleto).Bold().AlignCenter();
            c.Text($"{rol}  ·  {t.Cargo}").FontSize(7.5).Color(Tenue).AlignCenter();
        });

        // ---------- Resumen del equipo ----------

        private void Resumen(PageBuilder page, PeriodoOperativo periodo, Trabajador evaluador,
            List<AsignacionEvaluador> asignaciones, Dictionary<int, Participacion> participacion)
        {
            Marco(page, periodo, "Reporte de evaluación del equipo", evaluador.NombreCompleto, "Resumen del equipo");

            var cerradas = asignaciones.Where(a => a.PuntajeFinal is not null).ToList();
            page.Content().Column(col =>
            {
                col.Gap(10);

                col.Table(t =>
                {
                    Base(t);
                    t.Columns(80, "*", 60, "*", 50, "*");
                    Seccion(t, "DATOS DEL EVALUADOR", 6);
                    Par(t, "Nombre", evaluador.NombreCompleto, negrita: true);
                    Par(t, "Cargo", evaluador.Cargo);
                    Par(t, "Área", evaluador.Area);
                    Par(t, "Evaluados", asignaciones.Count.ToString());
                    Par(t, "Cerrados", $"{cerradas.Count} de {asignaciones.Count}");
                    Par(t, "Promedio", cerradas.Count == 0 ? "—" : $"{cerradas.Average(a => a.PuntajeFinal!.Value):0.##} puntos");
                }).KeepTogether();

                col.Table(t =>
                {
                    Base(t);
                    t.Columns(22, "3*", "3*", 52, 52, 44, 52, "2*", 48, 80);
                    t.Header(h =>
                    {
                        h.Cell("N°").RowSpan(2).AlignMiddle();
                        h.Cell("Colaborador").ColumnSpan(3);
                        h.Cell("Planificación y seguimiento").ColumnSpan(3);
                        h.Cell("Resultado").ColumnSpan(3);
                        h.Row("Evaluado", "Cargo", "Días ef.", "Objetivos", "Peso", "Avance", "Estado", "Puntaje", "Calificación");
                    });

                    var n = 0;
                    foreach (var a in asignaciones)
                    {
                        n++;
                        var fondo = n % 2 == 0 ? Gris : "#FFFFFF";
                        var suma = a.Objetivos.Sum(o => o.Peso);
                        t.Cell(n.ToString()).AlignCenter().Background(fondo);
                        t.Cell(a.Evaluado!.NombreCompleto).Bold().Background(fondo);
                        t.Cell(a.Evaluado.Cargo).Color(Tenue).Background(fondo);
                        var dias = participacion[a.EvaluadoId].DiasEfectivos;
                        t.Cell(dias.ToString()).AlignRight().Color(dias >= periodo.Regla!.DiasMinimos ? Texto : "#C0392B").Background(fondo);
                        t.Cell(a.Objetivos.Count.ToString()).AlignCenter().Background(fondo);
                        t.Cell($"{suma} %").AlignRight().Color(suma == 100 ? Texto : "#C0392B").Background(fondo);
                        t.Cell($"{EvaluacionService.AvancePonderado(a.Objetivos):0.#} %").AlignRight().Background(fondo);
                        t.Cell(EvaluacionService.EstadoTexto(a.Estado)).Background(fondo);
                        t.Cell(a.PuntajeFinal is { } p ? p.ToString("0.##") : "—").AlignRight().Bold().Background(fondo);
                        if (a.Calificacion is { } cal)
                            t.Cell(cal).AlignCenter().Bold().Color(ColorTextoCalificacion(cal)).Background(ColorCalificacion(cal));
                        else
                            t.Cell("Pendiente").AlignCenter().Italic().Color(Tenue).Background(fondo);
                    }
                });

                // Distribución por calificación: una columna por rango del periodo + pendientes.
                var rangos = periodo.Regla!.Rangos.OrderBy(r => r.PuntajeMinimo).ToList();
                col.Table(t =>
                {
                    Base(t);
                    t.Columns(Enumerable.Repeat<ColumnSize>("*", rangos.Count + 2).ToArray());
                    t.Header(h =>
                    {
                        h.Cell("Distribución por calificación").ColumnSpan(rangos.Count + 2);
                        h.Row([.. rangos.Select(r => $"{r.Nombre} ({r.PuntajeMinimo}–{r.PuntajeMaximo})"), "Pendientes", "Total"]);
                    });
                    foreach (var r in rangos)
                        t.Cell(cerradas.Count(a => a.Calificacion == r.Nombre).ToString()).AlignCenter().FontSize(12).Bold().Color(ColorCalificacion(r.Nombre));
                    t.Cell((asignaciones.Count - cerradas.Count).ToString()).AlignCenter().FontSize(12).Bold().Color(Tenue);
                    t.Cell(asignaciones.Count.ToString()).AlignCenter().FontSize(12).Bold().Background(AzulSuave);
                }).KeepTogether();
            });
        }

        // ---------- Piezas comunes ----------

        private void Marco(PageBuilder page, PeriodoOperativo periodo, string titulo, string subtitulo, string numeracion)
        {
            page.Size(PageSize.A4.Landscape()).Margin(22, 36, 18, 36);
            page.DefaultTextStyle(new TextStyle { Font = FontFamily.Helvetica, FontSize = 8.5, Color = PdfColor.FromHex(Texto) });

            page.Header().Column(col =>
            {
                col.Row(row =>
                {
                    row.Item().Column(c =>
                    {
                        c.Text("BancaInti").FontSize(16).Bold().Color(Azul);
                        c.Text("Evaluación de Desempeño").FontSize(8).Color(Tenue);
                    });
                    row.Item().Column(c =>
                    {
                        c.Text(titulo).FontSize(12).Bold().AlignRight();
                        c.Text($"{subtitulo}  ·  Periodo {periodo.Anio} ({periodo.FechaInicio:dd/MM/yyyy} – {periodo.FechaFin:dd/MM/yyyy})").FontSize(8).Color(Tenue).AlignRight();
                    });
                });
                col.Space(2);
                col.Divider().Color(Azul).Thickness(1.5);
            });

            page.Footer().Row(row =>
            {
                row.Item().Text($"Generado el {evaluacionService.Hoy:dd/MM/yyyy}  ·  Documento confidencial").FontSize(7).Color(Tenue);
                row.Item().PageNumber(numeracion).FontSize(7).Color(Tenue).AlignRight();
            });
        }

        private static void Base(TableBlock t) =>
            t.Border(Borde, 0.5).Padding(3.5).HeaderBackground(Azul)
             .HeaderStyle(new TextStyle { Bold = true, Color = PdfColor.White, FontSize = 7.5, Align = TextAlign.Center });

        private static TableCell Seccion(TableBlock t, string texto, int columnas) =>
            t.Cell(texto).ColumnSpan(columnas).Background(Azul).Color("#FFFFFF").Bold().FontSize(7.5);

        private static TableCell Etiqueta(TableBlock t, string texto) =>
            t.Cell(texto).Background(AzulSuave).Bold().FontSize(7.5).Color(Azul);

        private static void Par(TableBlock t, string etiqueta, string valor, bool negrita = false)
        {
            Etiqueta(t, etiqueta);
            t.Cell(valor).Bold(negrita);
        }

        private static TableCell Total(TableBlock t, string texto, int columnas) =>
            t.Cell(texto).ColumnSpan(columnas).Background(AzulSuave).Bold();

        private static string Num(decimal valor) => valor.ToString("#,0.##");

        private static string ColorCalificacion(string? calificacion) => calificacion switch
        {
            "No cumple" => "#C0392B",
            "Bueno" => "#2F80ED",
            "Muy bueno" => "#2E9E5B",
            "Sobresaliente" => "#D4A017",
            _ => "#6C757D",
        };

        private static string ColorTextoCalificacion(string? calificacion) =>
            calificacion == "Sobresaliente" ? "#3D2E00" : "#FFFFFF";
    }
}
