#!/usr/bin/env bash
# Regresión end-to-end de BancaInti (Git Bash). Uso:
#   1) Levantar la app:  ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5199 dotnet bin/Release/net10.0/BancaInti.dll
#   2) PGPASSWORD=<clave de postgres> bash tests/regresion.sh
#      (opcional PDF_DIR=<carpeta> para guardar los PDFs generados y revisarlos)
# Mueve temporalmente las fechas de las etapas para recorrer Registro → Seguimiento → Evaluación con María → Pedro (T010)
# y al final las restaura y limpia los objetivos de Pedro. Requiere los datos del seed.
: "${PGPASSWORD:?Defina PGPASSWORD con la clave de postgres}"
export PGCLIENTENCODING=UTF8
S=$(mktemp -d)
q() { "/c/Program Files/PostgreSQL/18/bin/psql.exe" -U postgres -h localhost -p 5433 -d BancaIntiDB -A -t -c "$1"; }
etapas() { q "update \"Etapas\" set \"FechaInicio\"='$1',\"FechaFin\"='$2' where \"Tipo\"=1; update \"Etapas\" set \"FechaInicio\"='$3',\"FechaFin\"='$4' where \"Tipo\"=2; update \"Etapas\" set \"FechaInicio\"='$5',\"FechaFin\"='$6' where \"Tipo\"=3;" >/dev/null; }
B=http://localhost:5199; OK=0; KO=0
chk() { if [ "$2" = "1" ]; then OK=$((OK+1)); echo "  [OK] $1"; else KO=$((KO+1)); echo "  [FALLA] $1"; fi; }
has() { grep -qF -- "$1" && echo 1 || echo 0; }
hasnot() { grep -qF -- "$1" && echo 0 || echo 1; }
dec() { tr -d '\r' | python -X utf8 -c "import sys,html; print(html.unescape(sys.stdin.read()))"; }
fila() { tr -d '\n' | python -X utf8 -c "
import sys,re
s=sys.stdin.read()
for r in re.findall(r'(?s)<tr>(.*?)</tr>',s):
    if sys.argv[1] in r: print(' | '.join(re.sub(r'\s+',' ',re.sub(r'<[^>]+>','',c)).strip() for c in re.findall(r'(?s)<td[^>]*>(.*?)</td>',r)))" "$1"; }
login() { J=$S/cj_$2.txt; rm -f $J; local t=$(curl -s -c $J -b $J $B/Identity/Account/Login | grep -o 'name="__RequestVerificationToken" type="hidden" value="[^"]*"' | sed 's/.*value="//;s/"$//'); curl -s -c $J -b $J -o /dev/null --data-urlencode "Input.Email=$1" --data-urlencode "Input.Password=$2" --data-urlencode "__RequestVerificationToken=$t" $B/Identity/Account/Login; }
tok() { curl -s -c $J -b $J "$B$1" | grep -o 'name="__RequestVerificationToken" type="hidden" value="[^"]*"' | head -1 | sed 's/.*value="//;s/"$//'; }
post() { curl -s -c $J -b $J -L --data "$3&__RequestVerificationToken=$(tok $1)" "$B$2" | dec; }
get() { curl -s -c $J -b $J -L "$B$1" | dec; }
for i in $(seq 1 60); do curl -s -o /dev/null $B/ && break; sleep 1; done

echo "== Paginas en espanol =="
P=$(curl -s $B/Identity/Account/Login | dec)
chk "Login en espanol, sin Register" $( [ "$(echo "$P" | has 'Ingrese con su correo institucional')$(echo "$P" | hasnot 'Register')" = "11" ] && echo 1 || echo 0)
J=$S/cj_x.txt; rm -f $J
chk "Contrasena incorrecta" $(post /Identity/Account/Login /Identity/Account/Login "Input.Email=pedro.sanchez%40bancainti.pe&Input.Password=XX" | has "Correo o contraseña incorrectos.")
chk "Campos vacios" $(post /Identity/Account/Login /Identity/Account/Login "Input.Email=&Input.Password=" | has "El campo Correo es obligatorio.")
chk "Register redirige al login" $(curl -s -o /dev/null -w "%{redirect_url}" $B/Identity/Account/Register | has "/Identity/Account/Login")
login luis.rojas@bancainti.pe LR
chk "404 en espanol" $(get /Admin/NoExiste/Nada | has "Página no encontrada")
chk "Acceso denegado en espanol" $(get /Admin/Consultas/Auditoria | has "Su usuario no tiene permiso")
chk "Validacion cliente cargada" $(get /Colaborador/Home/Objetivos | has "jquery.validate.unobtrusive.min.js")

echo "== RRHH =="
login rosa.quispe@bancainti.pe RQ
PID=$(q "select \"Id\" from \"PeriodosOperativos\" where \"Activo\"")
chk "Model binding en espanol" $(post /Admin/PeriodosOperativos/Reglas/$PID /Admin/PeriodosOperativos/Reglas "PeriodoOperativoId=$PID&PeriodoNombre=x&MinObjetivos=abc&MaxObjetivos=6&PesoMinimo=10&PesoMaximo=40&DiasMinimos=90&Rangos%5B0%5D.Nombre=A&Rangos%5B0%5D.PuntajeMinimo=0&Rangos%5B0%5D.PuntajeMaximo=100" | has "no es válido para Mínimo de objetivos")
DI=$(q "select \"Id\" from \"Trabajadores\" where \"Codigo\"='T013'"); TID=$(q "select \"Id\" from \"TiposJustificacion\" where \"Nombre\" like 'Descanso%'")
if [ -z "$(q "select 1 from \"JustificacionesTrabajador\" where \"TrabajadorId\"=$DI")" ]; then
  chk "Justificacion de Diana registrada" $(post /Admin/Justificaciones/Create /Admin/Justificaciones/Save "Id=0&TrabajadorId=$DI&TipoJustificacionId=$TID&FechaInicio=2026-08-10&FechaFin=2026-10-20&Observacion=Descanso+m%C3%A9dico+prolongado" | has "Justificación guardada.")
fi
F=$(get "/Admin/Consultas/Participantes?todos=true" | fila "Diana Ruiz"); echo "     $F"
chk "Diana: 153 - 72 = 81 dias, no participa" $(echo "$F" | grep -qE "\| 153 \| 72 \| 81 \| No$" && echo 1 || echo 0)
chk "Filtro por defecto excluye a Diana" $(get /Admin/Consultas/Participantes | hasnot "Diana Ruiz")
chk "Asignaciones marca no participa" $(get /Admin/Asignaciones | has "81 días · no participa")
chk "Etapas: etapa vigente" $(get /Admin/Etapas | has "Vigente")

echo "== Ciclo completo: Maria evalua a Pedro (T010) =="
AID=$(q "select a.\"Id\" from \"Asignaciones\" a join \"Trabajadores\" t on t.\"Id\"=a.\"EvaluadoId\" where t.\"Codigo\"='T010'")
etapas 2026-01-01 2026-10-31 2026-11-01 2026-11-30 2026-12-01 2026-12-31
login maria.torres@bancainti.pe MT
N="/Evaluador/Evaluados/NuevoObjetivo?asignacionId=$AID"; G=/Evaluador/Evaluados/GuardarObjetivo; D=/Evaluador/Evaluados/Detalle/$AID
post "$N" $G "Id=0&AsignacionId=$AID&Descripcion=Atenci%C3%B3n+sin+errores&Indicador=Operaciones+sin+descuadre&UnidadMedida=%25&Peso=40&Meta=98" >/dev/null
post "$N" $G "Id=0&AsignacionId=$AID&Descripcion=Tiempo+de+atenci%C3%B3n&Indicador=Minutos+promedio&UnidadMedida=min&Peso=30&Meta=5" >/dev/null
post "$N" $G "Id=0&AsignacionId=$AID&Descripcion=Venta+cruzada&Indicador=Productos+colocados&UnidadMedida=productos&Peso=30&Meta=50" >/dev/null
chk "3 objetivos registrados (100 %)" $([ "$(q "select count(*)||'-'||sum(\"Peso\") from \"Objetivos\" where \"AsignacionId\"=$AID")" = "3-100" ] && echo 1 || echo 0)
login pedro.sanchez@bancainti.pe PS
chk "Pedro no ve objetivos en borrador" $(get /Colaborador/Home/Objetivos | has "aún no ha confirmado")
login maria.torres@bancainti.pe MT
chk "Confirmar objetivos" $(post $D /Evaluador/Evaluados/Confirmar/$AID "x=1" | has "Objetivos confirmados. El colaborador")
etapas 2026-01-01 2026-03-31 2026-04-01 2026-10-31 2026-11-01 2026-12-31
O=($(q "select \"Id\" from \"Objetivos\" where \"AsignacionId\"=$AID order by \"Id\""))
post $D /Evaluador/Evaluados/RegistrarAvance "asignacionId=$AID&ObjetivoId=${O[0]}&Avance=50" >/dev/null
post $D /Evaluador/Evaluados/RegistrarAvance "asignacionId=$AID&ObjetivoId=${O[0]}&Avance=96" >/dev/null
post $D /Evaluador/Evaluados/RegistrarAvance "asignacionId=$AID&ObjetivoId=${O[2]}&Avance=20" >/dev/null
post $D /Evaluador/Evaluados/RegistrarAvance "asignacionId=$AID&ObjetivoId=${O[1]}&Avance=4" >/dev/null
chk "Avances registrados" $(post $D /Evaluador/Evaluados/RegistrarAvance "asignacionId=$AID&ObjetivoId=${O[2]}&Avance=40" | has "Avance registrado.")
post $D /Evaluador/Evaluados/Comentar "asignacionId=$AID&texto=Buen+avance%2C+reforzar+venta+cruzada." >/dev/null
login pedro.sanchez@bancainti.pe PS
R=$(get /Colaborador/Home/Objetivos)
chk "Pedro ve objetivos, avance y comentario" $( [ "$(echo "$R" | has 'Venta cruzada')$(echo "$R" | has 'reforzar venta cruzada')$(echo "$R" | has 'Atención sin errores')" = "111" ] && echo 1 || echo 0)
chk "Pedro responde" $(post /Colaborador/Home/Objetivos /Colaborador/Home/Comentar "asignacionId=$AID&texto=De+acuerdo." | has "Comentario enviado.")
etapas 2026-01-01 2026-03-31 2026-04-01 2026-09-30 2026-10-01 2026-12-31
login maria.torres@bancainti.pe MT
chk "Evaluacion: 40+27+18 = 85 Muy bueno" $(post /Evaluador/Evaluados/Evaluar/$AID /Evaluador/Evaluados/Evaluar "AsignacionId=$AID&Objetivos%5B0%5D.ObjetivoId=${O[0]}&Objetivos%5B0%5D.Cumplimiento=100&Objetivos%5B1%5D.ObjetivoId=${O[1]}&Objetivos%5B1%5D.Cumplimiento=90&Objetivos%5B2%5D.ObjetivoId=${O[2]}&Objetivos%5B2%5D.Cumplimiento=60&ComentarioFinal=Buen+desempe%C3%B1o." | has "Evaluación cerrada: 85 puntos (Muy bueno).")
login pedro.sanchez@bancainti.pe PS
R=$(get /Colaborador/Home/Objetivos)
chk "Pedro ve su evaluacion final" $( [ "$(echo "$R" | has '85 puntos')$(echo "$R" | has 'Buen desempeño.')" = "11" ] && echo 1 || echo 0)
login maria.torres@bancainti.pe MT
pdf() { curl -s -c $J -b $J -o "$S/$2" -w "%{content_type}" "$B$1" | grep -q "application/pdf" && [ "$(head -c 5 "$S/$2")" = "%PDF-" ] && echo 1 || echo 0; }
chk "PDF individual de Pedro" $(pdf /Evaluador/Evaluados/Pdf/$AID individual.pdf)
chk "PDF del equipo de Maria" $(pdf /Evaluador/Home/PdfEquipo equipo.pdf)
[ -n "$PDF_DIR" ] && cp "$S/individual.pdf" "$S/equipo.pdf" "$PDF_DIR"/
login rosa.quispe@bancainti.pe RQ
F=$(get /Admin/Consultas/Evaluadores | fila "Pedro Sánchez"); echo "     $F"
chk "Consulta evaluadores refleja a Pedro" $(echo "$F" | grep -q "Evaluado | 3 | 100 |" && echo "$F" | grep -q "| 85 | Muy bueno" && echo 1 || echo 0)
chk "Auditoria registra acciones de Maria" $([ "$(q "select count(*) from \"LogsAuditoria\" where \"Usuario\"='maria.torres@bancainti.pe' and \"Entidad\" in ('Objetivo','Seguimiento','AsignacionEvaluador','Comentario')")" -ge 8 ] && echo 1 || echo 0)

echo "== Restauracion =="
etapas 2026-01-01 2026-03-31 2026-04-01 2026-10-31 2026-11-01 2026-12-31
q "delete from \"Comentarios\" where \"AsignacionId\"=$AID; delete from \"Seguimientos\" where \"ObjetivoId\" in (select \"Id\" from \"Objetivos\" where \"AsignacionId\"=$AID); delete from \"Objetivos\" where \"AsignacionId\"=$AID; update \"Asignaciones\" set \"Estado\"=0,\"PuntajeFinal\"=null,\"Calificacion\"=null,\"ComentarioFinal\"=null,\"FechaCierre\"=null where \"Id\"=$AID;" >/dev/null
echo "  Etapas: $(q "select string_agg(\"FechaInicio\"||'..'||\"FechaFin\", ', ' order by \"Tipo\") from \"Etapas\"")"
echo "  Pedro: estado=$(q "select \"Estado\" from \"Asignaciones\" where \"Id\"=$AID") objetivos=$(q "select count(*) from \"Objetivos\" where \"AsignacionId\"=$AID")"
echo; echo "RESULTADO: $OK OK, $KO fallas"
