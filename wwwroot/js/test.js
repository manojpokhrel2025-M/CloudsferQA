/**
 * CloudsferQA — test.js
 * All interactivity for the test session page.
 * No external dependencies — pure vanilla JS.
 */

'use strict';

/* ── State ──────────────────────────────────────────────────────────────── */
let activeModule    = null;
let activeSubmodule = null;
let activeFilter    = 'all';
const noteTimers    = {};   // debounce timers keyed by testCaseId
let isSaving        = false; // lightweight concurrency flag for bulk ops

/* ── Entry Point ─────────────────────────────────────────────────────────── */
document.addEventListener('DOMContentLoaded', () => {
    initSidebar();
    initSearch();
    initActionButtons();
    initNoteInputs();
    initFilterBar();
    initDetailToggles();
    initMarkAllButtons();

    // Start with all modules collapsed — user expands what they need
    document.querySelectorAll('.module-group').forEach(g => g.classList.add('collapsed'));
});

/* ═══════════════════════════════════════════════════════════════════════════
   SIDEBAR
   ═══════════════════════════════════════════════════════════════════════════ */

/**
 * initSidebar — wire up module group expand/collapse and submodule clicks.
 */
function initSidebar() {
    // Module header toggle
    document.querySelectorAll('.module-header').forEach(header => {
        header.addEventListener('click', () => {
            const group = header.closest('.module-group');
            group.classList.toggle('collapsed');
        });
    });

    // Submodule item click
    document.querySelectorAll('.submodule-item').forEach(item => {
        item.addEventListener('click', () => {
            const module    = item.dataset.module;
            const submodule = item.dataset.submodule;
            filterBySubmodule(module, submodule);
        });
    });
}

/**
 * initSearch — filter the sidebar module list as the user types.
 */
function initSearch() {
    const input = document.getElementById('moduleSearch');
    if (!input) return;

    input.addEventListener('input', () => {
        const query = input.value.toLowerCase().trim();
        document.querySelectorAll('.module-group').forEach(group => {
            if (!query) {
                group.style.display = '';
                return;
            }
            const moduleName = (group.dataset.module || '').toLowerCase();
            // Also check submodule names
            const hasMatch = moduleName.includes(query) ||
                Array.from(group.querySelectorAll('.submodule-name'))
                    .some(el => el.textContent.toLowerCase().includes(query));
            group.style.display = hasMatch ? '' : 'none';
        });
    });
}

/**
 * filterBySubmodule — show only cards matching the given module/submodule.
 * Updates sidebar active state and content header.
 */
function filterBySubmodule(module, submodule) {
    activeModule    = module;
    activeSubmodule = submodule;

    // Update sidebar active state
    document.querySelectorAll('.submodule-item').forEach(item => {
        const isActive = item.dataset.module === module && item.dataset.submodule === submodule;
        item.classList.toggle('active', isActive);
    });

    // Ensure the parent module group is expanded
    document.querySelectorAll('.module-group').forEach(group => {
        if (group.dataset.module === module) {
            group.classList.remove('collapsed');
        }
    });

    // Update content header
    document.getElementById('breadcrumb').textContent = module;
    document.getElementById('currentSubmodule').textContent = submodule;

    // Reset filter to "all" when switching submodule
    activeFilter = 'all';
    document.querySelectorAll('.filter-btn').forEach(btn => {
        btn.classList.toggle('active', btn.dataset.filter === 'all');
    });

    applyVisibility();
}

/* ═══════════════════════════════════════════════════════════════════════════
   VISIBILITY / FILTERING
   ═══════════════════════════════════════════════════════════════════════════ */

/**
 * applyVisibility — apply both submodule and status filters to all cards.
 * A card is visible when it matches the active submodule AND the active filter.
 */
function applyVisibility() {
    let visibleCount = 0;

    document.querySelectorAll('.test-card').forEach(card => {
        const matchesSubmodule = !activeSubmodule ||
            (card.dataset.module === activeModule && card.dataset.submodule === activeSubmodule);

        const matchesFilter = activeFilter === 'all' ||
            card.dataset.status === activeFilter;

        const show = matchesSubmodule && matchesFilter;
        card.style.display = show ? 'block' : 'none';
        if (show) visibleCount++;
    });

    // Show empty-state placeholder when no cards are visible in the active view
    const emptyState = document.getElementById('emptyState');
    if (emptyState) {
        emptyState.style.display = (activeSubmodule && visibleCount === 0) ? 'block' : 'none';
    }
}

/**
 * initFilterBar — wire up the status filter buttons.
 */
function initFilterBar() {
    document.querySelectorAll('.filter-btn').forEach(btn => {
        btn.addEventListener('click', () => {
            activeFilter = btn.dataset.filter;

            // Toggle active class
            document.querySelectorAll('.filter-btn').forEach(b => {
                b.classList.toggle('active', b === btn);
            });

            applyVisibility();
        });
    });
}

/* ═══════════════════════════════════════════════════════════════════════════
   SAVE RESULT
   ═══════════════════════════════════════════════════════════════════════════ */

/**
 * saveResult — POST a result to the server and update the UI on success.
 * @param {string} testCaseId
 * @param {string} status   — pass | fail | blocked | skip | pending
 * @param {string} notes
 * @returns {Promise<boolean>} — true on success
 */
async function saveResult(testCaseId, status, notes) {
    try {
        const response = await fetch('/Test/SaveResult', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ testCaseId, status, notes })
        });

        if (!response.ok) {
            console.error('SaveResult failed:', response.status);
            return false;
        }

        const data = await response.json();
        if (!data.success) return false;

        // Update card data attribute and border class
        const card = document.querySelector(`.test-card[data-test-id="${testCaseId}"]`);
        if (card) {
            card.dataset.status = status;

            // Replace status-* class
            card.className = card.className.replace(/\bstatus-\S+/g, '').trim();
            card.classList.add(`status-${status}`);

            // Update status badge text and colour
            const badge = document.getElementById(`status-${testCaseId}`);
            if (badge) {
                badge.textContent = status;
                badge.className = `badge-status status-badge-${status}`;
            }

            // Update action button active states
            card.querySelectorAll('.btn-action').forEach(btn => {
                btn.classList.toggle('active', btn.dataset.status === status);
            });
        }

        updateChips();
        updateProgressBar();
        updateSidebarBadges();
        applyVisibility(); // Re-run in case filter hides the newly-updated card

        return true;
    } catch (err) {
        console.error('saveResult error:', err);
        return false;
    }
}

/* ═══════════════════════════════════════════════════════════════════════════
   ACTION BUTTONS
   ═══════════════════════════════════════════════════════════════════════════ */

/**
 * initActionButtons — delegate clicks on .btn-action buttons.
 * Implements toggle behaviour: clicking the active button reverts to pending.
 */
function initActionButtons() {
    document.getElementById('cardsContainer').addEventListener('click', e => {
        const btn = e.target.closest('.btn-action');
        if (!btn) return;

        const testCaseId = btn.dataset.testId;
        const btnStatus  = btn.dataset.status;
        const card       = btn.closest('.test-card');
        const current    = card.dataset.status;

        // Toggle: clicking the active status → revert to pending
        const newStatus = (current === btnStatus) ? 'pending' : btnStatus;
        const notes     = card.querySelector('.notes-input')?.value ?? '';

        saveResult(testCaseId, newStatus, notes);
    });
}

/* ═══════════════════════════════════════════════════════════════════════════
   NOTES INPUT — DEBOUNCED AUTO-SAVE
   ═══════════════════════════════════════════════════════════════════════════ */

/**
 * initNoteInputs — attach debounced auto-save to every notes input.
 */
function initNoteInputs() {
    document.getElementById('cardsContainer').addEventListener('input', e => {
        const input = e.target.closest('.notes-input');
        if (!input) return;

        const testCaseId = input.dataset.testId;
        clearTimeout(noteTimers[testCaseId]);

        noteTimers[testCaseId] = setTimeout(() => {
            const card   = document.querySelector(`.test-card[data-test-id="${testCaseId}"]`);
            const status = card?.dataset.status ?? 'pending';
            saveResult(testCaseId, status, input.value);
        }, 500);
    });
}

/* ═══════════════════════════════════════════════════════════════════════════
   DETAILS TOGGLE
   ═══════════════════════════════════════════════════════════════════════════ */

/**
 * initDetailToggles — show/hide the steps/expected-result section.
 */
function initDetailToggles() {
    document.getElementById('cardsContainer').addEventListener('click', e => {
        const btn = e.target.closest('.btn-details');
        if (!btn) return;

        const targetId = btn.dataset.target;
        const details  = document.getElementById(targetId);
        if (!details) return;

        const isOpen = details.style.display !== 'none';
        details.style.display = isOpen ? 'none' : 'block';
        btn.textContent = isOpen ? 'Details ▾' : 'Details ▴';
    });
}

/* ═══════════════════════════════════════════════════════════════════════════
   BULK ACTIONS
   ═══════════════════════════════════════════════════════════════════════════ */

/**
 * initMarkAllButtons — wire up "Mark All Pass" and "Skip All" buttons.
 */
function initMarkAllButtons() {
    document.getElementById('btnMarkAllPass')?.addEventListener('click', () => bulkSetStatus('pass'));
    document.getElementById('btnSkipAll')?.addEventListener('click',    () => bulkSetStatus('skip'));
}

/**
 * bulkSetStatus — set all cards in the current submodule to the given status.
 * Runs requests sequentially to avoid flooding the server.
 * @param {string} status
 */
async function bulkSetStatus(status) {
    if (isSaving || !activeSubmodule) return;
    isSaving = true;

    const cards = Array.from(document.querySelectorAll('.test-card'))
        .filter(card =>
            card.dataset.module === activeModule &&
            card.dataset.submodule === activeSubmodule
        );

    for (const card of cards) {
        const testCaseId = card.dataset.testId;
        const notes      = card.querySelector('.notes-input')?.value ?? '';
        await saveResult(testCaseId, status, notes);
    }

    isSaving = false;
}

/* ═══════════════════════════════════════════════════════════════════════════
   HEADER UPDATES
   ═══════════════════════════════════════════════════════════════════════════ */

/**
 * updateChips — recount pass/fail/blocked from all cards and refresh header chips.
 */
function updateChips() {
    let pass = 0, fail = 0, blocked = 0;

    document.querySelectorAll('.test-card').forEach(card => {
        switch (card.dataset.status) {
            case 'pass':    pass++;    break;
            case 'fail':    fail++;    break;
            case 'blocked': blocked++; break;
        }
    });

    const chipPass    = document.getElementById('chipPass');
    const chipFail    = document.getElementById('chipFail');
    const chipBlocked = document.getElementById('chipBlocked');

    if (chipPass)    chipPass.textContent    = `${pass} Pass`;
    if (chipFail)    chipFail.textContent    = `${fail} Fail`;
    if (chipBlocked) chipBlocked.textContent = `${blocked} Blocked`;
}

/**
 * updateProgressBar — recalculate tested/total and update the header progress bar.
 */
function updateProgressBar() {
    const cards   = document.querySelectorAll('.test-card');
    const total   = cards.length;
    let   tested  = 0;

    cards.forEach(card => {
        if (card.dataset.status !== 'pending' && card.dataset.status !== '') tested++;
    });

    const pct = total > 0 ? ((tested / total) * 100).toFixed(1) : '0.0';

    const bar   = document.getElementById('progressBar');
    const label = document.getElementById('progressLabel');

    if (bar)   bar.style.width = `${pct}%`;
    if (label) label.textContent = `${tested} / ${total} tested`;
}

/**
 * updateSidebarBadges — refresh done/total counts on every sidebar submodule badge.
 */
function updateSidebarBadges() {
    document.querySelectorAll('.submodule-item').forEach(item => {
        const mod = item.dataset.module;
        const sub = item.dataset.submodule;

        const cards = Array.from(document.querySelectorAll('.test-card'))
            .filter(c => c.dataset.module === mod && c.dataset.submodule === sub);

        const total      = cards.length;
        const pass       = cards.filter(c => c.dataset.status === 'pass').length;
        const fail       = cards.filter(c => c.dataset.status === 'fail').length;
        const blocked    = cards.filter(c => c.dataset.status === 'blocked').length;
        const skip       = cards.filter(c => c.dataset.status === 'skip').length;
        const inprogress = cards.filter(c => c.dataset.status === 'inprogress').length;
        const done       = pass + fail + blocked + skip + inprogress;

        const badge = item.querySelector('.submodule-badge');
        if (badge) badge.textContent = `${done}/${total}`;

        const dots = item.querySelector('.submodule-status-dots');
        if (!dots) return;

        let html = '';
        if (pass       > 0) html += `<span style="font-size:10px;color:#2E7D32;font-weight:600">✓${pass}</span>`;
        if (fail       > 0) html += `<span style="font-size:10px;color:#C62828;font-weight:600">✗${fail}</span>`;
        if (blocked    > 0) html += `<span style="font-size:10px;color:#E65100;font-weight:600">⊘${blocked}</span>`;
        if (skip       > 0) html += `<span style="font-size:10px;color:#1565C0;font-weight:600">↷${skip}</span>`;
        if (inprogress > 0) html += `<span style="font-size:10px;color:#1565C0;font-weight:600">● ${inprogress} IP</span>`;
        dots.innerHTML = html;
    });

    // Module-level tick — show ✓ only when every test case in the module is passed
    document.querySelectorAll('.module-header[data-module]').forEach(header => {
        const mod   = header.dataset.module;
        const cards = Array.from(document.querySelectorAll(`.test-card[data-module="${CSS.escape(mod)}"]`));
        const tick  = header.querySelector('.module-tick');
        if (!tick || cards.length === 0) return;
        const allPassed = cards.every(c => c.dataset.status === 'pass');
        tick.style.display = allPassed ? 'inline' : 'none';
    });
}
