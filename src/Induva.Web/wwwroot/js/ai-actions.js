// INDUVA Generic AI Action Handler
// Listens for clicks on any element with data-ai-url and data-ai-target

document.addEventListener('click', async (e) => {
    // 1. Generic AI trigger button (data-ai-url)
    const btn = e.target.closest('[data-ai-url]');
    if (btn && !btn.disabled) {
        const target = document.querySelector(btn.dataset.aiTarget);
        if (!target) return;

        const metaCsrf = document.querySelector('meta[name="csrf"]');
        const token = metaCsrf ? metaCsrf.content : '';

        btn.disabled = true;
        target.innerHTML = `
            <div class="d-flex align-items-center gap-2 p-3 bg-white rounded border shadow-sm">
                <div class="spinner-border spinner-border-sm text-primary" role="status"></div>
                <span class="text-secondary small">Analyzing… this can take up to a minute.</span>
            </div>`;

        try {
            const res = await fetch(btn.dataset.aiUrl, {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token,
                    'X-Requested-With': 'XMLHttpRequest'
                },
                body: btn.dataset.aiBody ? new URLSearchParams(JSON.parse(btn.dataset.aiBody)) : null
            });

            if (res.ok) {
                target.innerHTML = await res.text(); // Server-rendered Razor partial
            } else {
                const err = await res.json().catch(() => ({}));
                const box = document.createElement('div');
                box.className = 'alert alert-warning shadow-sm mt-3';
                box.textContent = err.error || 'The analysis could not be completed. Please try again.';
                target.replaceChildren(box);
            }
        } catch {
            const box = document.createElement('div');
            box.className = 'alert alert-danger shadow-sm mt-3';
            box.textContent = 'Network error while contacting the server. Please check your connection.';
            target.replaceChildren(box);
        } finally {
            btn.disabled = false;
        }
        return;
    }

    // 2. Save Product Suggestion
    const saveBtn = e.target.closest('.btn-save-suggestion');
    if (saveBtn) {
        const id = saveBtn.dataset.id;
        const metaCsrf = document.querySelector('meta[name="csrf"]');
        const token = metaCsrf ? metaCsrf.content : '';

        saveBtn.disabled = true;
        try {
            const res = await fetch(`/suggestions/${id}/save`, {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token,
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (res.ok) {
                const badge = document.getElementById(`status-badge-${id}`);
                if (badge) {
                    badge.className = 'badge bg-success';
                    badge.textContent = 'Saved';
                }
                saveBtn.replaceWith(Object.assign(document.createElement('span'), {
                    className: 'badge bg-success-subtle text-success border py-2 px-3',
                    textContent: '✓ Saved to Pipeline'
                }));
                const dismissBtn = document.querySelector(`.btn-dismiss-suggestion[data-id="${id}"]`);
                if (dismissBtn) dismissBtn.remove();
            }
        } catch (err) {
            console.error('Failed to save suggestion:', err);
            saveBtn.disabled = false;
        }
        return;
    }

    // 3. Dismiss Product Suggestion
    const dismissBtn = e.target.closest('.btn-dismiss-suggestion');
    if (dismissBtn) {
        const id = dismissBtn.dataset.id;
        const metaCsrf = document.querySelector('meta[name="csrf"]');
        const token = metaCsrf ? metaCsrf.content : '';

        dismissBtn.disabled = true;
        try {
            const res = await fetch(`/suggestions/${id}/dismiss`, {
                method: 'POST',
                headers: {
                    'RequestVerificationToken': token,
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (res.ok) {
                const card = document.getElementById(`suggestion-card-${id}`);
                if (card) {
                    card.style.transition = 'opacity 0.3s';
                    card.style.opacity = '0';
                    setTimeout(() => card.remove(), 300);
                }
            }
        } catch (err) {
            console.error('Failed to dismiss suggestion:', err);
            dismissBtn.disabled = false;
        }
        return;
    }
});
