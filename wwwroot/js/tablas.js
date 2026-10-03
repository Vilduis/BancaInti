// Tablas con data-tabla="Nombre para el Excel": DataTables con 15 filas por página, buscador,
// filtros por columna (<th data-filtro>), "Limpiar filtros" y exportación a Excel de lo filtrado.
// Columnas con <th class="no-exportar"> (acciones) no se ordenan, no se buscan ni se exportan.
(() => {
    const idioma = {
        info: 'Mostrando _START_ a _END_ de _TOTAL_ registros',
        infoEmpty: 'Sin registros',
        infoFiltered: '(filtrado de _MAX_)',
        zeroRecords: 'No se encontraron resultados con los filtros aplicados.',
        emptyTable: 'No hay registros.',
        paginate: { first: '«', previous: '‹', next: '›', last: '»' },
        aria: { sortAscending: ': ordenar ascendente', sortDescending: ': ordenar descendente' },
    };

    const crear = (html) => {
        const t = document.createElement('template');
        t.innerHTML = html.trim();
        return t.content.firstElementChild;
    };
    const texto = (s) => String(s).replace(/[&<>"]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[c]);
    const hoy = () => new Date().toISOString().slice(0, 10).replaceAll('-', '');

    function iniciar(tablaEl) {
        const nombre = tablaEl.dataset.tabla || document.title;
        const ths = [...tablaEl.tHead.rows[0].cells];
        // Si la columna está alineada a la derecha (números), su título también.
        const primeraFila = tablaEl.tBodies[0]?.rows[0];
        ths.forEach((th, i) => { if (primeraFila?.cells[i]?.classList.contains('text-end')) th.classList.add('text-end'); });
        const noExportar = ths.map((th, i) => (th.classList.contains('no-exportar') ? i : -1)).filter((i) => i >= 0);

        const tabla = new DataTable(tablaEl, {
            pageLength: 15,
            lengthChange: false,
            order: [],
            autoWidth: false,
            language: idioma,
            columnDefs: [{ targets: noExportar, orderable: false, searchable: false }],
            layout: { topStart: null, topEnd: null, bottomStart: 'info', bottomEnd: 'paging' },
        });

        new DataTable.Buttons(tabla, {
            buttons: [{
                extend: 'excelHtml5',
                title: nombre,
                filename: `${nombre} ${hoy()}`,
                exportOptions: {
                    columns: ths.map((_, i) => i).filter((i) => !noExportar.includes(i)),
                    // Texto visible de la celda (o data-search si la celda tiene un formulario), con tildes.
                    format: { body: (data, fila, col, td) => (td.dataset.search ?? td.textContent).replace(/\s+/g, ' ').trim() },
                },
            }],
        });

        // Barra: buscador + filtros por columna a la izquierda, acciones a la derecha.
        const barra = crear(`
            <div class="tabla-barra">
                <div class="tabla-filtros">
                    <div class="tabla-buscar">
                        <i class="bi bi-search"></i>
                        <input type="search" class="form-control form-control-sm" placeholder="Buscar..." aria-label="Buscar en la tabla" />
                    </div>
                </div>
                <div class="tabla-acciones">
                    <button type="button" class="btn btn-sm btn-light" data-accion="limpiar"><i class="bi bi-x-circle"></i> Limpiar filtros</button>
                    <button type="button" class="btn btn-sm btn-light" data-accion="excel"><i class="bi bi-file-earmark-excel"></i> Exportar a Excel</button>
                </div>
            </div>`);
        const filtros = barra.querySelector('.tabla-filtros');
        const buscar = barra.querySelector('input');
        buscar.addEventListener('input', () => tabla.search(buscar.value).draw());

        const selects = [];
        ths.forEach((th, i) => {
            if (!th.hasAttribute('data-filtro')) return;
            const valores = [...new Set(tabla.column(i).nodes().toArray().map((td) => td.textContent.replace(/\s+/g, ' ').trim()))]
                .filter((v) => v !== '')
                .sort((a, b) => a.localeCompare(b, 'es', { numeric: true }));
            const titulo = th.dataset.filtro || th.textContent.trim();
            const select = crear(`<select class="form-select form-select-sm" aria-label="Filtrar por ${texto(titulo)}">
                <option value="">${texto(titulo)}: todos</option>
                ${valores.map((v) => `<option>${texto(v)}</option>`).join('')}
            </select>`);
            select.addEventListener('change', () => {
                select.classList.toggle('activo', select.value !== '');
                // Coincidencia exacta, tolerante a espacios y saltos de línea del HTML.
                const v = select.value ? `^\\s*${DataTable.util.escapeRegex(select.value).replaceAll(' ', '\\s+')}\\s*$` : '';
                tabla.column(i).search(v, true, false).draw();
            });
            selects.push(select);
            filtros.append(select);
        });

        barra.querySelector('[data-accion="excel"]').addEventListener('click', () => tabla.button(0).trigger());
        barra.querySelector('[data-accion="limpiar"]').addEventListener('click', () => {
            // Si la página tiene filtros del servidor (querystring), se recarga sin ellos.
            if (location.search) { location.href = location.pathname; return; }
            buscar.value = '';
            selects.forEach((s) => { s.value = ''; s.classList.remove('activo'); });
            tabla.search('').columns().search('').draw();
        });

        tabla.table().container().prepend(barra);

        // Con una sola página, la paginación sobra.
        const paginacion = tabla.table().container().querySelector('.dt-paging');
        const ajustarPaginacion = () => paginacion?.classList.toggle('d-none', tabla.page.info().pages <= 1);
        tabla.on('draw', ajustarPaginacion);
        ajustarPaginacion();
    }

    document.querySelectorAll('table[data-tabla]').forEach(iniciar);
})();
