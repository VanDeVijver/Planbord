// Realtime: the server only signals "something changed"; pages refetch from the database.
window.luisterNaarSignalen = function (cb) {
    const es = new EventSource('/signalen');
    es.onmessage = e => {
        const [soort, medewerkerId, datum] = e.data.split(':');
        cb({ soort, medewerkerId: Number(medewerkerId), datum });
    };
};

// Copy a table as tab-separated text (pastes cleanly into a spreadsheet).
document.addEventListener('click', async e => {
    const knop = e.target.closest('[data-kopieer]');
    if (!knop) return;
    const doel = document.getElementById(knop.dataset.kopieer);
    const rijen = [...doel.querySelectorAll('tr')].map(tr => [...tr.cells].map(c => c.innerText.trim()).join('\t'));
    await navigator.clipboard.writeText(rijen.join('\n'));
    knop.textContent = 'Gekopieerd';
});

// Planning board drag-and-drop (desktop). A cell supplies the date and, depending on the view,
// the employee or the project. Dragging a chip moves it (the chip keeps the other one); dragging
// an item from the list above the board adds a new assignment (the item supplies the other one).
document.addEventListener('dragstart', e => {
    const bron = e.target.closest('.chip[draggable], .palet-item');
    if (bron) e.dataTransfer.setData('text/plain', bron.dataset.id ? 'chip:' + bron.dataset.id : bron.dataset.nieuw);
});
document.addEventListener('dragover', e => {
    const cel = e.target.closest('td[data-datum]');
    if (cel) { e.preventDefault(); cel.classList.add('drop'); }
});
document.addEventListener('dragleave', e => e.target.closest?.('td[data-datum]')?.classList.remove('drop'));
document.addEventListener('drop', async e => {
    const cel = e.target.closest('td[data-datum]');
    if (!cel) return;
    e.preventDefault();
    const [soort, id] = e.dataTransfer.getData('text/plain').split(':');
    const body = new FormData();
    let handler;
    if (soort === 'chip') {
        const chip = document.querySelector(`.chip[data-id="${id}"]`);
        if (!chip) return;
        handler = 'Verplaats';
        body.append('id', id);
        body.append('medewerkerId', cel.dataset.medewerker ?? chip.dataset.medewerker);
        body.append('projectId', cel.dataset.project ?? chip.dataset.project);
    } else {
        const medewerker = cel.dataset.medewerker ?? (soort === 'medewerker' ? id : null);
        const project = cel.dataset.project ?? (soort === 'project' ? id : null);
        if (!medewerker || !project) return;
        handler = 'Toevoegen';
        body.append('medewerkerId', medewerker);
        body.append('projectId', project);
    }
    body.append('datum', cel.dataset.datum);
    body.append('__RequestVerificationToken', document.querySelector('input[name=__RequestVerificationToken]').value);
    await fetch('?handler=' + handler, { method: 'POST', body });
    location.reload();
});

// Live filter for a list: <input data-filter="listId"> hides items whose text doesn't match.
document.addEventListener('input', e => {
    const lijst = e.target.dataset?.filter && document.getElementById(e.target.dataset.filter);
    if (!lijst) return;
    const q = e.target.value.trim().toLowerCase();
    for (const item of lijst.children) item.hidden = q !== '' && !item.textContent.toLowerCase().includes(q);
});
