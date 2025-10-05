/**
 * Dashboard Service - Handles product selection and stock status updates
 */
class DashboardService {
    constructor() {
        this.stockChart = null;
        this.apiBaseUrl = '/Home';
        this.init();
    }

    /**
     * Initialize the dashboard service
     */
    init() {
        this.bindEvents();
    }

    /**
     * Bind event listeners
     */
    bindEvents() {
        $(document).ready(() => {
            // Bind product dropdown change event
            $('#productDropdown').on('change', (e) => {
                this.handleProductChange(e);
            });
        });
    }

    /**
     * Handle product dropdown change
     * @param {Event} event - The change event
     */
    async handleProductChange(event) {
        const productId = $(event.target).val();
        
        if (!productId || productId === "0") {
            console.log('No product selected or invalid product ID');
            return;
        }

        try {
            // Show loading state
            this.showLoadingState();
            
            // Fetch stock data
            const stockData = await this.fetchStockData(productId);
            
            // Update chart and labels
            this.updateStockDisplay(stockData);
            
        } catch (error) {
            console.error('Error updating stock status:', error);
            this.showErrorState('Failed to load stock data');
        }
    }

    /**
     * Fetch stock data from API
     * @param {string} productId - The product ID
     * @returns {Promise<Object>} Stock data
     */
    async fetchStockData(productId) {
        return new Promise((resolve, reject) => {
            $.ajax({
                url: `${this.apiBaseUrl}/GetStockStatus`,
                method: 'GET',
                data: { productId: productId },
                dataType: 'json',
                success: (data) => {
                    console.log('Stock data received:', data);
                    resolve(data);
                },
                error: (xhr, status, error) => {
                    console.error('API Error:', error);
                    reject(new Error(`API Error: ${error}`));
                }
            });
        });
    }

    /**
     * Update stock display (chart and labels)
     * @param {Object} data - Stock data from API
     */
    updateStockDisplay(data) {
        debugger;
        if (!data) {
            console.error('No data received');
            this.showErrorState('No data available');
            return;
        }

        // Ensure we have valid numeric values
        const inStock = this.parseNumericValue(data.inStockCount);
        const outOfStock = this.parseNumericValue(data.outOfStockCount);
        const available = this.parseNumericValue(data.lowStockCount);

        console.log('Updating display with values:', { inStock, outOfStock, available });

        // Update chart if it exists
        if (this.stockChart) {
            this.updateChart(inStock, outOfStock, available);
        }

        // Update labels
        this.updateLabels(inStock, outOfStock, available);
    }

    /**
     * Parse numeric value safely
     * @param {any} value - Value to parse
     * @returns {number} Parsed number or 0
     */
    parseNumericValue(value) {
        if (value === null || value === undefined || value === '') {
            return 0;
        }
        const parsed = parseFloat(value);
        return isNaN(parsed) ? 0 : parsed;
    }

    /**
     * Update the stock chart
     * @param {number} inStock - In stock count
     * @param {number} outOfStock - Out of stock count
     * @param {number} available - Available stock count
     */
    updateChart(inStock, outOfStock, available) {
        try {
            if (!this.stockChart) {
                console.warn('Chart instance not available, attempting to reinitialize...');
                this.reinitializeChart();
                
                if (!this.stockChart) {
                    console.error('Failed to reinitialize chart');
                    return;
                }
            }
            
            console.log('Updating chart with data:', [inStock, outOfStock, available]);
            
            // Update chart data
            this.stockChart.data.datasets[0].data = [inStock, outOfStock, available];
            
            // Force chart update with animation
            this.stockChart.update('active');
            
            console.log('Chart updated successfully');
        } catch (error) {
            console.error('Error updating chart:', error);
            // Try to reinitialize on error
            this.reinitializeChart();
        }
    }

    /**
     * Update stock status labels
     * @param {number} inStock - In stock count
     * @param {number} outOfStock - Out of stock count
     * @param {number} available - Available stock count
     */
    updateLabels(inStock, outOfStock, available) {
        debugger;
        const labelsHtml = `
            <div><span class="badge bg-success me-1">&nbsp;</span>In stock: ${inStock}</div>
            <div><span class="badge bg-warning me-1">&nbsp;</span>Available: ${available}</div>
            <div><span class="badge bg-danger me-1">&nbsp;</span>Used: ${outOfStock}</div>
        `;
        
        $('#stockLabels').html(labelsHtml);
        console.log('Labels updated successfully');
    }

    /**
     * Show loading state
     */
    showLoadingState() {
        $('#stockLabels').html(`
            <div class="text-center">
                <div class="spinner-border spinner-border-sm" role="status">
                    <span class="visually-hidden">Loading...</span>
                </div>
                <div>Loading stock data...</div>
            </div>
        `);
    }

    /**
     * Show error state
     * @param {string} message - Error message
     */
    showErrorState(message) {
        $('#stockLabels').html(`
            <div class="text-center text-danger">
                <i class="fas fa-exclamation-triangle"></i>
                <div>${message}</div>
            </div>
        `);
    }

    /**
     * Set the chart instance
     * @param {Chart} chart - Chart.js instance
     */
    setChart(chart) {
        this.stockChart = chart;
        console.log('Chart instance set in dashboard service:', chart);
    }

    /**
     * Check if chart is properly initialized
     * @returns {boolean} True if chart is ready
     */
    isChartReady() {
        return this.stockChart !== null && this.stockChart !== undefined;
    }

    /**
     * Reinitialize chart if needed
     */
    reinitializeChart() {
        const stockDonutElement = document.getElementById('stockDonut');
        if (stockDonutElement && !this.isChartReady()) {
            console.log('Reinitializing chart...');
            const donutCtx = stockDonutElement.getContext('2d');
            
            this.stockChart = new Chart(donutCtx, {
                type: 'doughnut',
                data: {
                    labels: ['In Stock','Out of Stock', 'Available Stock' ],
                    datasets: [{
                        data: [0, 0, 0],
                        backgroundColor: ['#28a745','#dc3545','#ffc107'],
                        borderWidth: 2,
                        borderColor: '#fff'
                    }]
                },
                options: { 
                    plugins: { 
                        legend: { 
                            position: 'bottom',
                            display: true
                        } 
                    },
                    responsive: true,
                    maintainAspectRatio: false,
                    animation: {
                        animateRotate: true,
                        animateScale: true
                    }
                }
            });
            
            console.log('Chart reinitialized successfully');
        }
    }
}

// Initialize the service when DOM is ready
$(document).ready(() => {
    // Wait a bit to ensure all scripts are loaded
    setTimeout(() => {
        window.dashboardService = new DashboardService();
        console.log('Dashboard service initialized');
    }, 100);
});
