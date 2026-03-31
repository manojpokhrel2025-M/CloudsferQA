/**
 * CloudsferQA — dashboard.js
 * All interactivity for the QA Results Dashboard page.
 * Requires Chart.js 4.x (loaded via CDN in _Layout.cshtml).
 */

'use strict';

/* ── Chart instances (kept so they can be destroyed on re-render) ─────── */
const charts = {
    donut:    null,
    bar:      null,
    stacked:  null,
    priority: null,
};

/* ── Brand colours ───────────────────────────────────────────────────── */
const C = {
    pass:    '#2E7D32',
    fail:    '#C62828',
    blocked: '#E65100',
    skip:    '#757575',
    pending: '#BDBDBD',
    blue:    '#1565C0',
};

/* ── Entry Point ─────────────────────────────────────────────────────── */
document.addEventListener('DOMContentLoaded', () => {
    const select = document.getElementById('sessionSelect');
    if (!select) return; // No sessions yet

    // Attach change handler
    select.addEventListener('change', onSessionChange);

    // Read ?session= URL param, fall back to first option
    const params    = new URLSearchParams(window.location.search);
    const urlId     = params.get('session');
    const defaultId = urlId || select.value;

    if (urlId && select.querySelector(`option[value="${urlId}"]`)) {
        select.value = urlId;
    }

    if (defaultId) loadSession(parseInt(defaultId, 10));
});

/* ═══════════════════════════════════════════════════════════════════════
   SESSION LOADING
   ═══════════════════════════════════════════════════════════════════════ */

/**
 * onSessionChange — called when the session dropdown changes.
 * Updates the URL and loads the selected session.
 */
function onSessionChange(e) {
    const id = parseInt(e.target.value, 10);
    window.history.pushState({}, '', `?session=${id}`);
    loadSession(id);
}

/**
 * loadSession — fetch stats for a session from the API and render everything.
 * @param {number} sessionId
 */
async function loadSession(sessionId) {
    try {
        const res = await fetch(`/Dashboard/Data/${sessionId}`);
        if (!res.ok) { console.error('Failed to load session', sessionId); return; }

        const data = await res.json();
        renderAll(data);
    } catch (err) {
        console.error('loadSession error:', err);
    }
}

/**
 * renderAll — orchestrate all renders after data arrives.
 * @param {object} data — SessionStatsDto (camelCase from ASP.NET Core JSON)
 */
function renderAll(data) {
    renderKpiCards(data);
    renderProgressStrip(data);
    renderDonut(data);
    renderBarChart(data);
    renderStackedChart(data);
    renderPriorityChart(data);
    renderModuleTable(data);
    renderFailList(data);
    renderActivityFeed(data);

    // Update session meta label
    const meta = document.getElementById('sessionMeta');
    if (meta) {
        meta.textContent = `Started ${formatDate(data.startedAt)}`;
    }
}

/* ═══════════════════════════════════════════════════════════════════════
   KPI CARDS
   ═══════════════════════════════════════════════════════════════════════ */

/**
 * renderKpiCards — animate count-up on all 6 KPI cards.
 */
function renderKpiCards(data) {
    const pct = v => data.total > 0 ? `${((v / data.total) * 100).toFixed(1)}%` : '—';

    animateCount(document.getElementById('kpiTotal'),   data.total);
    animateCount(document.getElementById('kpiPass'),    data.pass);
    animateCount(document.getElementById('kpiFail'),    data.fail);
    animateCount(document.getElementById('kpiBlocked'), data.blocked);
    animateCount(document.getElementById('kpiSkip'),    data.skip);
    animateCount(document.getElementById('kpiPending'), data.pending);

    setText('kpiTotalPct',   'test cases');
    setText('kpiPassPct',    `${pct(data.pass)} pass rate`);
    setText('kpiFailPct',    pct(data.fail));
    setText('kpiBlockedPct', pct(data.blocked));
    setText('kpiSkipPct',    pct(data.skip));
    setText('kpiPendingPct', pct(data.pending));
}

/* ═══════════════════════════════════════════════════════════════════════
   PROGRESS STRIP
   ═══════════════════════════════════════════════════════════════════════ */

/**
 * renderProgressStrip — update the coloured full-width progress bar.
 */
function renderProgressStrip(data) {
    const t = data.total || 1;
    const pct = v => `${((v / t) * 100).toFixed(2)}%`;

    setStyle('psPass',    'width', pct(data.pass));
    setStyle('psFail',    'width', pct(data.fail));
    setStyle('psBlocked', 'width', pct(data.blocked));
    setStyle('psSkip',    'width', pct(data.skip));

    const tested = data.pass + data.fail + data.blocked + data.skip;
    setText('progressStripLabel', `${tested} / ${data.total} tested (${data.passRate}% pass)`);
}

/* ═══════════════════════════════════════════════════════════════════════
   CHARTS
   ═══════════════════════════════════════════════════════════════════════ */

/**
 * renderDonut — doughnut chart showing overall result distribution.
 */
function renderDonut(data) {
    const ctx = document.getElementById('donutChart');
    if (!ctx) return;

    destroyChart('donut');

    charts.donut = new Chart(ctx, {
        type: 'doughnut',
        data: {
            labels: ['Pass', 'Fail', 'Blocked', 'Skip', 'Pending'],
            datasets: [{
                data: [data.pass, data.fail, data.blocked, data.skip, data.pending],
                backgroundColor: [C.pass, C.fail, C.blocked, C.skip, C.pending],
                borderWidth: 2,
                borderColor: '#fff',
                hoverOffset: 6,
            }]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            cutout: '62%',
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: { font: { family: 'Inter', size: 12 }, padding: 14 }
                },
                tooltip: {
                    callbacks: {
                        label: ctx => {
                            const total = ctx.dataset.data.reduce((a, b) => a + b, 0);
                            const pct   = total > 0 ? ((ctx.parsed / total) * 100).toFixed(1) : 0;
                            return ` ${ctx.label}: ${ctx.parsed} (${pct}%)`;
                        }
                    }
                }
            }
        }
    });
}

/**
 * renderBarChart — horizontal bar chart showing pass rate % per module.
 * Bars are green (≥80%), orange (≥50%), or red (<50%).
 */
function renderBarChart(data) {
    const ctx = document.getElementById('barChart');
    if (!ctx) return;

    destroyChart('bar');

    const labels = data.modules.map(m => m.module);
    const rates  = data.modules.map(m => m.passRate);
    const colors = rates.map(r => r >= 80 ? C.pass : r >= 50 ? C.blocked : C.fail);

    charts.bar = new Chart(ctx, {
        type: 'bar',
        data: {
            labels,
            datasets: [{
                label: 'Pass Rate %',
                data: rates,
                backgroundColor: colors,
                borderRadius: 4,
                borderSkipped: false,
            }]
        },
        options: {
            indexAxis: 'y',
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                x: {
                    min: 0, max: 100,
                    ticks: {
                        callback: v => `${v}%`,
                        font: { family: 'Inter', size: 11 }
                    },
                    grid: { color: '#F0F0F0' }
                },
                y: {
                    ticks: { font: { family: 'Inter', size: 11 } },
                    grid: { display: false }
                }
            },
            plugins: {
                legend: { display: false },
                tooltip: {
                    callbacks: { label: ctx => ` ${ctx.parsed.x.toFixed(1)}%` }
                }
            }
        }
    });
}

/**
 * renderStackedChart — stacked horizontal bar chart with absolute counts per module.
 */
function renderStackedChart(data) {
    const ctx = document.getElementById('stackedChart');
    if (!ctx) return;

    destroyChart('stacked');

    const labels = data.modules.map(m => m.module);

    charts.stacked = new Chart(ctx, {
        type: 'bar',
        data: {
            labels,
            datasets: [
                { label: 'Pass',    data: data.modules.map(m => m.pass),    backgroundColor: C.pass,    borderRadius: 0 },
                { label: 'Fail',    data: data.modules.map(m => m.fail),    backgroundColor: C.fail,    borderRadius: 0 },
                { label: 'Blocked', data: data.modules.map(m => m.blocked), backgroundColor: C.blocked, borderRadius: 0 },
                { label: 'Skip',    data: data.modules.map(m => m.skip),    backgroundColor: C.skip,    borderRadius: 0 },
                { label: 'Pending', data: data.modules.map(m => m.pending), backgroundColor: C.pending, borderRadius: 0 },
            ]
        },
        options: {
            indexAxis: 'y',
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                x: {
                    stacked: true,
                    ticks: { font: { family: 'Inter', size: 11 } },
                    grid: { color: '#F0F0F0' }
                },
                y: {
                    stacked: true,
                    ticks: { font: { family: 'Inter', size: 11 } },
                    grid: { display: false }
                }
            },
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: { font: { family: 'Inter', size: 12 }, padding: 12 }
                }
            }
        }
    });
}

/**
 * renderPriorityChart — grouped (stacked) bar chart broken down by priority.
 * X-axis: High / Medium / Low.  Stacked datasets: Pass / Fail / Blocked / Skip.
 */
function renderPriorityChart(data) {
    const ctx = document.getElementById('priorityChart');
    if (!ctx) return;

    destroyChart('priority');

    const pd     = data.priorityBreakdown || [];
    const labels = pd.map(p => p.priority);

    charts.priority = new Chart(ctx, {
        type: 'bar',
        data: {
            labels,
            datasets: [
                { label: 'Pass',    data: pd.map(p => p.pass),    backgroundColor: C.pass,    borderRadius: 0 },
                { label: 'Fail',    data: pd.map(p => p.fail),    backgroundColor: C.fail,    borderRadius: 0 },
                { label: 'Blocked', data: pd.map(p => p.blocked), backgroundColor: C.blocked, borderRadius: 0 },
                { label: 'Skip',    data: pd.map(p => p.skip),    backgroundColor: C.skip,    borderRadius: 0 },
                { label: 'Pending', data: pd.map(p => p.pending), backgroundColor: C.pending, borderRadius: 0 },
            ]
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            scales: {
                x: {
                    stacked: true,
                    ticks: { font: { family: 'Inter', size: 12 } },
                    grid: { display: false }
                },
                y: {
                    stacked: true,
                    ticks: { font: { family: 'Inter', size: 11 } },
                    grid: { color: '#F0F0F0' }
                }
            },
            plugins: {
                legend: {
                    position: 'bottom',
                    labels: { font: { family: 'Inter', size: 12 }, padding: 12 }
                }
            }
        }
    });
}

/* ═══════════════════════════════════════════════════════════════════════
   MODULE TABLE
   ═══════════════════════════════════════════════════════════════════════ */

/**
 * renderModuleTable — populate the module breakdown table.
 * Rows are sorted by module order (API already returns them in order).
 */
function renderModuleTable(data) {
    const tbody = document.getElementById('moduleTableBody');
    if (!tbody) return;

    if (!data.modules || data.modules.length === 0) {
        tbody.innerHTML = '<tr><td colspan="8" class="text-center text-muted py-3">No data</td></tr>';
        return;
    }

    tbody.innerHTML = data.modules.map(m => {
        const tested   = m.pass + m.fail + m.blocked + m.skip;
        const pct      = m.total > 0 ? (tested / m.total * 100).toFixed(0) : 0;
        const barColor = m.passRate >= 80 ? C.pass : m.passRate >= 50 ? C.blocked : C.fail;

        let pill, pillClass;
        if (tested === 0)         { pill = 'Not Started'; pillClass = 'pill-not-started'; }
        else if (tested < m.total){ pill = 'In Progress'; pillClass = 'pill-in-progress'; }
        else                      { pill = 'Done';        pillClass = 'pill-done'; }

        return `<tr>
            <td><strong>${esc(m.module)}</strong></td>
            <td class="text-center">${m.total}</td>
            <td class="text-center" style="color:${C.pass};font-weight:600">${m.pass}</td>
            <td class="text-center" style="color:${C.fail};font-weight:600">${m.fail}</td>
            <td class="text-center" style="color:${C.blocked};font-weight:600">${m.blocked}</td>
            <td class="text-center">${tested}</td>
            <td>
                <div class="table-progress-bar">
                    <div class="table-progress-fill" style="width:${pct}%;background:${barColor}"></div>
                </div>
                <small style="color:#90A4AE;font-size:10.5px">${pct}% tested</small>
            </td>
            <td class="text-center"><span class="status-pill ${pillClass}">${pill}</span></td>
        </tr>`;
    }).join('');
}

/* ═══════════════════════════════════════════════════════════════════════
   FAIL LIST
   ═══════════════════════════════════════════════════════════════════════ */

/**
 * renderFailList — render the scrollable list of failed & blocked test cases.
 */
function renderFailList(data) {
    const container = document.getElementById('failList');
    if (!container) return;

    const items = data.failedAndBlocked || [];

    if (items.length === 0) {
        container.innerHTML = '<div class="text-muted text-center py-3" style="font-size:13px">No failures or blocks — great! 🎉</div>';
        return;
    }

    container.innerHTML = items.map(item => {
        const badgeClass = item.status === 'fail' ? 'badge-fail' : 'badge-blocked';
        return `<div class="fail-item">
            <div>
                <div class="fail-item-tc">${esc(item.testCaseId)}</div>
                <div class="fail-item-module">${esc(item.module)}</div>
            </div>
            <div class="fail-item-scenario">${esc(item.scenario)}</div>
            <span class="fail-item-badge ${badgeClass}">${item.status}</span>
        </div>`;
    }).join('');
}

/* ═══════════════════════════════════════════════════════════════════════
   ACTIVITY FEED
   ═══════════════════════════════════════════════════════════════════════ */

/**
 * renderActivityFeed — render the last 20 results as a chronological feed.
 */
function renderActivityFeed(data) {
    const container = document.getElementById('activityFeed');
    if (!container) return;

    const items = data.recentActivity || [];

    if (items.length === 0) {
        container.innerHTML = '<div class="text-muted text-center py-3" style="font-size:13px">No activity recorded yet.</div>';
        return;
    }

    container.innerHTML = items.map(item => {
        return `<div class="activity-item">
            <div class="activity-dot dot-${item.status}"></div>
            <div class="activity-scenario" title="${esc(item.scenario)}">${esc(item.scenario)}</div>
            <div class="activity-module">${esc(item.module)}</div>
            <div class="activity-time">${timeAgo(item.testedAt)}</div>
        </div>`;
    }).join('');
}

/* ═══════════════════════════════════════════════════════════════════════
   UTILITIES
   ═══════════════════════════════════════════════════════════════════════ */

/**
 * animateCount — animate a number from 0 to target using requestAnimationFrame.
 * @param {HTMLElement|null} el
 * @param {number} target
 */
function animateCount(el, target) {
    if (!el) return;
    const duration = 600; // ms
    const start    = performance.now();
    const from     = parseInt(el.textContent, 10) || 0;

    function step(now) {
        const elapsed  = now - start;
        const progress = Math.min(elapsed / duration, 1);
        // Ease-out cubic
        const eased = 1 - Math.pow(1 - progress, 3);
        el.textContent = Math.round(from + (target - from) * eased);
        if (progress < 1) requestAnimationFrame(step);
    }

    requestAnimationFrame(step);
}

/**
 * timeAgo — convert an ISO date string to a human-readable relative time.
 * @param {string|null} dateString
 * @returns {string}
 */
function timeAgo(dateString) {
    if (!dateString) return '—';

    const date    = new Date(dateString);
    const seconds = Math.floor((Date.now() - date.getTime()) / 1000);

    if (seconds < 10)  return 'just now';
    if (seconds < 60)  return `${seconds}s ago`;

    const minutes = Math.floor(seconds / 60);
    if (minutes < 60)  return `${minutes}m ago`;

    const hours = Math.floor(minutes / 60);
    if (hours < 24)    return `${hours}h ago`;

    const days = Math.floor(hours / 24);
    if (days < 7)      return `${days}d ago`;

    return formatDate(dateString);
}

/**
 * formatDate — format an ISO date string as YYYY-MM-DD HH:mm.
 * @param {string} dateString
 * @returns {string}
 */
function formatDate(dateString) {
    if (!dateString) return '—';
    const d = new Date(dateString);
    const pad = n => String(n).padStart(2, '0');
    return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())} ${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

/**
 * destroyChart — safely destroy a Chart.js instance if it exists.
 * @param {string} key — key in the `charts` object
 */
function destroyChart(key) {
    if (charts[key]) {
        charts[key].destroy();
        charts[key] = null;
    }
}

/**
 * esc — HTML-escape a string to prevent XSS when setting innerHTML.
 * @param {string} str
 * @returns {string}
 */
function esc(str) {
    return String(str ?? '')
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}

/**
 * setText — set textContent on an element by ID.
 */
function setText(id, text) {
    const el = document.getElementById(id);
    if (el) el.textContent = text;
}

/**
 * setStyle — set a style property on an element by ID.
 */
function setStyle(id, prop, value) {
    const el = document.getElementById(id);
    if (el) el.style[prop] = value;
}
