// Formularios con data-confirm="mensaje": piden confirmación antes de enviarse.
document.addEventListener('submit', async (e) => {
    const form = e.target;
    const mensaje = form.dataset.confirm;
    // Si la validación (jQuery Validation) ya bloqueó el envío, no se pide confirmación.
    if (!mensaje || form.dataset.confirmado || e.defaultPrevented) return;

    e.preventDefault();
    const r = await Swal.fire({
        text: mensaje,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Sí, continuar',
        cancelButtonText: 'Cancelar',
        confirmButtonColor: getComputedStyle(document.documentElement).getPropertyValue('--bi-azul').trim(),
    });
    if (r.isConfirmed) {
        form.dataset.confirmado = '1';
        form.requestSubmit();
    }
});

// Botones data-ver-clave="#campo": muestran u ocultan la contraseña.
document.addEventListener('click', (e) => {
    const boton = e.target.closest('[data-ver-clave]');
    if (!boton) return;
    const campo = document.querySelector(boton.dataset.verClave);
    const visible = campo.type === 'text';
    campo.type = visible ? 'password' : 'text';
    boton.querySelector('i').className = visible ? 'bi bi-eye' : 'bi bi-eye-slash';
    boton.title = visible ? 'Mostrar contraseña' : 'Ocultar contraseña';
});

// Sidebar colapsable a iconos (escritorio): botón [data-sidebar-toggle] o Ctrl+B; se recuerda en localStorage.
(() => {
    const raiz = document.documentElement;
    const boton = document.querySelector('[data-sidebar-toggle]');
    if (!boton) return;
    const enlaces = [...document.querySelectorAll('.sidebar-nav [data-titulo]')];
    const tooltips = enlaces.map((el) => new bootstrap.Tooltip(el, { title: el.dataset.titulo, placement: 'right', trigger: 'hover' }));

    const aplicar = (colapsado) => {
        raiz.classList.toggle('sidebar-colapsado', colapsado);
        boton.setAttribute('aria-expanded', String(!colapsado));
        boton.title = colapsado ? 'Mostrar menú (Ctrl+B)' : 'Ocultar menú (Ctrl+B)';
        boton.setAttribute('aria-label', colapsado ? 'Mostrar menú' : 'Ocultar menú');
        // Los nombres solo aparecen como tooltip cuando el sidebar muestra únicamente iconos.
        tooltips.forEach((t) => { t.hide(); colapsado ? t.enable() : t.disable(); });
    };
    const alternar = () => {
        const colapsado = !raiz.classList.contains('sidebar-colapsado');
        aplicar(colapsado);
        try { localStorage.setItem('sidebar', colapsado ? 'colapsado' : 'expandido'); } catch { }
    };

    aplicar(raiz.classList.contains('sidebar-colapsado'));
    boton.addEventListener('click', alternar);
    document.addEventListener('keydown', (e) => {
        if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'b' && !e.target.closest('input, textarea, select, [contenteditable]')) {
            e.preventDefault();
            alternar();
        }
    });

    // Con el sidebar colapsado, un grupo expande el menú y abre sus opciones (en vez de solo alternarlas).
    document.addEventListener('click', (e) => {
        const grupo = e.target.closest('.nav-grupo');
        if (!grupo || !raiz.classList.contains('sidebar-colapsado') || !window.matchMedia('(min-width: 992px)').matches) return;
        e.preventDefault();
        e.stopPropagation();
        alternar();
        bootstrap.Collapse.getOrCreateInstance(grupo.dataset.bsTarget, { toggle: false }).show();
    }, true);
})();

// Enlaces con data-modal (o data-modal="lg" para uno ancho): abren la vista (p. ej. Create/Edit) en un modal en vez de navegar.
// La petición lleva la cabecera X-Modal, así el servidor la pinta con _LayoutModal (ver _ViewStart de Admin).
// Al guardar, el servidor redirige (se recarga la página y _Alertas muestra el mensaje);
// si hay errores, devuelve el formulario otra vez y se reemplaza el contenido del modal.
(() => {
    let modalEl, modal, contenido;

    const asegurarModal = () => {
        if (modal) return;
        modalEl = document.createElement('div');
        modalEl.className = 'modal fade';
        modalEl.tabIndex = -1;
        modalEl.setAttribute('aria-hidden', 'true');
        modalEl.innerHTML = '<div class="modal-dialog modal-dialog-centered modal-dialog-scrollable"><div class="modal-content"></div></div>';
        document.body.append(modalEl);
        contenido = modalEl.querySelector('.modal-content');
        modal = new bootstrap.Modal(modalEl);
        modalEl.addEventListener('shown.bs.modal', () => contenido.querySelector('input:not([type=hidden]), select, textarea')?.focus());
    };

    const pintar = (html) => {
        contenido.innerHTML = html;
        modalEl.setAttribute('aria-labelledby', 'modal-titulo');
        contenido.querySelector('.modal-title')?.setAttribute('id', 'modal-titulo');
        const form = contenido.querySelector('form');
        if (form && window.jQuery?.validator) {
            $.validator.unobtrusive.parse(form);
        }
    };

    const error = () => Swal.fire({ icon: 'error', text: 'No se pudo completar la operación. Intente nuevamente.' });

    document.addEventListener('click', async (e) => {
        const enlace = e.target.closest('a[data-modal]');
        if (!enlace || e.ctrlKey || e.metaKey || e.shiftKey) return;
        e.preventDefault();
        asegurarModal();
        modalEl.querySelector('.modal-dialog').classList.toggle('modal-lg', enlace.dataset.modal === 'lg');
        try {
            const r = await fetch(enlace.href, { headers: { 'X-Modal': '1' } });
            if (!r.ok) throw new Error(r.status);
            pintar(await r.text());
            modal.show();
        } catch { error(); }
    });

    // "Cancelar" dentro del modal lo cierra (fuera del modal sigue siendo un enlace a la lista).
    document.addEventListener('click', (e) => {
        if (e.target.closest('[data-cancelar]') && modalEl?.contains(e.target)) {
            e.preventDefault();
            modal.hide();
        }
    });

    document.addEventListener('submit', async (e) => {
        const form = e.target;
        // jQuery Validation ya canceló el envío si hay campos inválidos.
        if (!modalEl?.contains(form) || e.defaultPrevented) return;
        e.preventDefault();
        const boton = form.querySelector('button:not([type=button])');
        if (boton) boton.disabled = true;
        try {
            const r = await fetch(form.action, { method: 'POST', body: new FormData(form), headers: { 'X-Modal': '1' }, redirect: 'manual' });
            if (r.type === 'opaqueredirect') { location.reload(); return; }
            if (!r.ok) throw new Error(r.status);
            pintar(await r.text());
        } catch {
            if (boton) boton.disabled = false;
            error();
        }
    });
})();

// Filas dinámicas: <tbody data-filas="Prefijo"> con inputs data-campo="Campo"; un botón data-agregar-fila="id-template"
// agrega una fila y data-quitar-fila la quita. Al enviar se renumeran los nombres a Prefijo[i].Campo (model binding).
document.addEventListener('click', (e) => {
    const agregar = e.target.closest('[data-agregar-fila]');
    if (agregar) {
        const plantilla = document.getElementById(agregar.dataset.agregarFila);
        agregar.closest('form').querySelector('[data-filas]').append(plantilla.content.cloneNode(true));
        return;
    }
    e.target.closest('[data-quitar-fila]')?.closest('tr').remove();
});
// En fase de captura: se renumera antes de que otros manejadores (p. ej. el del modal) lean el formulario.
document.addEventListener('submit', (e) => {
    e.target.querySelectorAll('[data-filas]').forEach((tbody) => {
        [...tbody.rows].forEach((fila, i) => {
            fila.querySelectorAll('[data-campo]').forEach((input) => { input.name = `${tbody.dataset.filas}[${i}].${input.dataset.campo}`; });
        });
    });
}, true);
