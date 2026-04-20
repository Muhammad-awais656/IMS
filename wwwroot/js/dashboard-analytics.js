/**
 * Analytics dashboard: date presets and JSON refresh without full reload.
 */
(function () {
    const apiUrl = '/Home/AnalyticsData';

    function formatMoney(n) {
        try {
            return new Intl.NumberFormat('en-PK', { style: 'currency', currency: 'PKR', maximumFractionDigits: 0 }).format(n);
        } catch {
            return 'Rs. ' + (Math.round(n * 100) / 100).toLocaleString();
        }
    }

    function setText(id, text) {
        const el = document.getElementById(id);
        if (el) el.textContent = text;
    }

    function updateCards(s) {
        setText('kpi-sales', formatMoney(s.salesTotal));
        setText('kpi-sales-count', s.salesCount ?? 0);
        setText('kpi-purchase', formatMoney(s.purchaseTotal));
        setText('kpi-purchase-count', s.purchaseCount ?? 0);
        setText('kpi-expenses', formatMoney(s.expensesTotal));
        setText('kpi-expenses-count', s.expensesCount ?? 0);
        setText('kpi-customers', s.newCustomers ?? 0);
        setText('kpi-products-new', s.newProducts ?? 0);
        setText('kpi-employees', s.newEmployees ?? 0);
        setText('kpi-stock', (s.stockAvailableTotal ?? 0).toLocaleString(undefined, { maximumFractionDigits: 2 }));
        setText('kpi-active-products', s.activeProductsTotal ?? 0);
        setText('kpi-netflow', formatMoney(s.netFlowHint ?? 0));
    }

    function renderChart(labels, data) {
        const canvas = document.getElementById('salesTrendChart');
        if (!canvas || typeof Chart === 'undefined') return;
        const ctx = canvas.getContext('2d');
        if (window._salesTrendChartInstance) {
            window._salesTrendChartInstance.destroy();
        }
        window._salesTrendChartInstance = new Chart(ctx, {
            type: 'line',
            data: {
                labels,
                datasets: [{
                    label: 'Sales',
                    data,
                    fill: true,
                    borderColor: '#0ea5e9',
                    backgroundColor: 'rgba(14, 165, 233, 0.12)',
                    tension: 0.35,
                    pointRadius: 3,
                    pointHoverRadius: 6
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: (ctx) => formatMoney(ctx.parsed.y)
                        }
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            callback: (v) => 'Rs.' + v.toLocaleString()
                        }
                    }
                }
            }
        });
    }

    async function loadData(range, fromVal, toVal) {
        const loading = document.getElementById('analytics-loading');
        if (loading) loading.style.display = 'flex';
        try {
            const params = new URLSearchParams();
            if (range) params.set('range', range);
            if (fromVal) params.set('from', fromVal);
            if (toVal) params.set('to', toVal);
            const res = await fetch(`${apiUrl}?${params.toString()}`);
            const json = await res.json();
            if (!json.success) throw new Error(json.message || 'Request failed');
            updateCards(json.summary);
            const labels = (json.trend || []).map(t => t.label);
            const amounts = (json.trend || []).map(t => t.amount);
            renderChart(labels, amounts);
            setText('range-label', `${json.from} → ${json.to}`);
        } catch (e) {
            console.error(e);
            alert('Could not load analytics. Please try again.');
        } finally {
            if (loading) loading.style.display = 'none';
        }
    }

    function bind() {
        document.querySelectorAll('[data-analytics-preset]').forEach(btn => {
            btn.addEventListener('click', () => {
                const preset = btn.getAttribute('data-analytics-preset');
                document.querySelectorAll('[data-analytics-preset]').forEach(b => b.classList.remove('active'));
                btn.classList.add('active');
                const custom = document.getElementById('custom-range-panel');
                if (custom) custom.style.display = preset === 'custom' ? 'flex' : 'none';
                if (preset !== 'custom') loadData(preset, null, null);
            });
        });
        const apply = document.getElementById('apply-custom-range');
        if (apply) {
            apply.addEventListener('click', () => {
                const from = document.getElementById('date-from')?.value;
                const to = document.getElementById('date-to')?.value;
                if (!from || !to) {
                    alert('Please select both start and end dates.');
                    return;
                }
                document.querySelectorAll('[data-analytics-preset]').forEach(b => b.classList.remove('active'));
                document.querySelector('[data-analytics-preset="custom"]')?.classList.add('active');
                loadData('custom', from, to);
            });
        }
    }

    /** Wire preset buttons; KPIs and chart are rendered server-side on first load. */
    window.initAnalyticsDashboard = function () {
        bind();
    };

    /**
     * Stock donut: loads /Home/GetStockStatus when product changes (branch-aware on server).
     * @param {Array<{value:string,text:string}>} products
     * @param {string|null} initialProductId
     */
    window.initStockDonutChart = function (products, initialProductId) {
        const canvas = document.getElementById('stockDonutChart');
        const select = document.getElementById('stock-product-select');
        const nameEl = document.getElementById('stock-selected-name');
        if (!canvas || typeof Chart === 'undefined') return;

        const parseNum = (d, camel, pascal) => {
            const v = d[camel] ?? d[pascal];
            return v == null ? 0 : Number(v);
        };

        function updateLabels(inStock, available, used) {
            const a = document.getElementById('stock-lbl-in');
            const b = document.getElementById('stock-lbl-used');
            const c = document.getElementById('stock-lbl-avail');
            if (a) a.textContent = typeof inStock === 'number' ? inStock.toLocaleString(undefined, { maximumFractionDigits: 2 }) : inStock;
            if (b) b.textContent = typeof used === 'number' ? used.toLocaleString(undefined, { maximumFractionDigits: 2 }) : used;
            if (c) c.textContent = typeof available === 'number' ? available.toLocaleString(undefined, { maximumFractionDigits: 2 }) : available;
        }

        let chart = window._stockDonutInstance;
        function renderDonut(inStock, available, used) {
            const ctx = canvas.getContext('2d');
            if (chart) chart.destroy();
            chart = new Chart(ctx, {
                type: 'doughnut',
                data: {
                    labels: ['In stock', 'Used', 'Available'],
                    datasets: [{
                        data: [inStock, used, available],
                        backgroundColor: ['#34d399', '#f87171', '#fbbf24'],
                        borderWidth: 2,
                        borderColor: '#1a2332',
                        hoverOffset: 6
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: { position: 'bottom', labels: { color: '#a8b8cc', padding: 12, font: { size: 11 } } }
                    }
                }
            });
            window._stockDonutInstance = chart;
            updateLabels(inStock, available, used);
        }

        async function loadStock(productId) {
            if (!productId || productId === '0') {
                renderDonut(0, 0, 0);
                if (nameEl) nameEl.textContent = '';
                return;
            }
            try {
                const res = await fetch(`/Home/GetStockStatus?productId=${encodeURIComponent(productId)}`);
                const data = await res.json();
                if (data.error) throw new Error(data.error);
                const inStock = parseNum(data, 'inStockCount', 'InStockCount');
                const available = parseNum(data, 'lowStockCount', 'LowStockCount');
                const used = parseNum(data, 'outOfStockCount', 'OutOfStockCount');
                renderDonut(inStock, available, used);
                const opt = products.find(p => String(p.value) === String(productId));
                if (nameEl) nameEl.textContent = opt ? opt.text : '';
            } catch (e) {
                console.error(e);
                renderDonut(0, 0, 0);
            }
        }

        if (select) {
            select.addEventListener('change', () => loadStock(select.value));
        }

        const startId = initialProductId && products.some(p => String(p.value) === String(initialProductId))
            ? initialProductId
            : (products[0] && products[0].value) || null;
        if (select && startId) {
            select.value = String(startId);
            loadStock(String(startId));
        } else {
            renderDonut(0, 0, 0);
        }
    };
})();
