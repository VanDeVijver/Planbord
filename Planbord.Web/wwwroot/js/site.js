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

// Planning board drag-and-drop (desktop). A cell supplies the new date and, depending on
// the view, the new employee or project; the chip keeps the other one.
document.addEventListener('dragstart', e => {
    const chip = e.target.closest('.chip[draggable]');
    if (chip) e.dataTransfer.setData('text/plain', chip.dataset.id);
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
    const chip = document.querySelector(`.chip[data-id="${e.dataTransfer.getData('text/plain')}"]`);
    if (!chip) return;
    const body = new FormData();
    body.append('id', chip.dataset.id);
    body.append('medewerkerId', cel.dataset.medewerker ?? chip.dataset.medewerker);
    body.append('projectId', cel.dataset.project ?? chip.dataset.project);
    body.append('datum', cel.dataset.datum);
    body.append('__RequestVerificationToken', document.querySelector('input[name=__RequestVerificationToken]').value);
    await fetch('?handler=Verplaats', { method: 'POST', body });
    location.reload();
});
