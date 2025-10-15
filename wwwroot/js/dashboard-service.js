/**
 * Dashboard Service - Handles product selection and stock status updates
 */
class DashboardService {
    constructor() {
        this.stockChart = null;
        this.apiBaseUrl = '/Home';
        this.searchableDropdown = null;
        this.productData = [];
        this.init();
    }

    /**
     * Initialize the dashboard service
     */
    init() {
        this.bindEvents();
        // Try to initialize Kendo UI combobox, but don't fail if it doesn't work
        try {
            this.initializeKendoComboBox();
        } catch (error) {
            console.error('Failed to initialize Kendo UI combobox:', error);
            // Show original dropdown as fallback
            const productDropdown = document.getElementById('productDropdown');
            if (productDropdown) {
                productDropdown.style.display = 'block';
            }
        }
    }

    /**
     * Initialize the Kendo UI combobox
     */
    initializeKendoComboBox() {
        // Check if Kendo UI is available
        if (typeof kendo === 'undefined' || typeof $.fn.kendoComboBox === 'undefined') {
            console.error('Kendo UI not found. Make sure Kendo UI scripts are loaded in _Layout.cshtml.');
            console.log('Will retry initialization in 2 seconds...');
            setTimeout(() => this.retryInitialization(), 2000);
            return;
        }

        // Get product data from the existing dropdown
        const productDropdown = document.getElementById('productDropdown');
        if (!productDropdown) {
            console.error('Product dropdown element not found');
            return;
        }

        console.log('Initializing Kendo UI combobox...');
        console.log('jQuery available:', typeof $ !== 'undefined');
        console.log('Kendo available:', typeof kendo !== 'undefined');
        console.log('Kendo ComboBox available:', typeof $.fn.kendoComboBox !== 'undefined');

        // Get product data from global variable set in the view
        this.productData = window.dashboardProductData || [];
        console.log('Product data loaded from server:', this.productData);

        // Hide original dropdown
        productDropdown.style.display = 'none';

        try {
            // Initialize Kendo UI combobox
            $(productDropdown).kendoComboBox({
                dataSource: {
                    data: this.productData
                },
                dataTextField: "Text",
                dataValueField: "Value",
                placeholder: "-- Select Product --",
                filter: "contains",
                suggest: true,
                minLength: 1,
                change: (e) => {
                    console.log('Product selection changed:', e.sender.value());
                    const selectedItem = e.sender.dataItem();
                    if (selectedItem) {
                        this.handleProductSelection({
                            value: selectedItem.Value,
                            text: selectedItem.Text
                        });
                    }
                },
                select: (e) => {
                    console.log('Product selected:', e.item);
                    const selectedItem = e.sender.dataItem();
                    if (selectedItem) {
                        this.handleProductSelection({
                            value: selectedItem.Value,
                            text: selectedItem.Text
                        });
                    }
                }
            });
            
            this.searchableDropdown = $(productDropdown).data("kendoComboBox");
            console.log('Kendo UI combobox initialized successfully');
            
        } catch (error) {
            console.error('Error initializing Kendo UI combobox:', error);
            // Show original dropdown if Kendo UI fails
            productDropdown.style.display = 'block';
        }
    }

    /**
     * Retry initialization if Kendo UI is not available initially
     */
    retryInitialization() {
        console.log('Retrying Kendo UI combobox initialization...');
        if (typeof kendo !== 'undefined' && typeof $.fn.kendoComboBox !== 'undefined') {
            this.initializeKendoComboBox();
        } else {
            console.log('Kendo UI still not available, will retry in 2 seconds...');
            setTimeout(() => this.retryInitialization(), 2000);
        }
    }

    /**
     * Bind event listeners
     */
    bindEvents() {
        $(document).ready(() => {
            // Keep the original dropdown event for backward compatibility
            $('#productDropdown').on('change', (e) => {
                this.handleProductChange(e);
            });
        });
    }

    /**
     * Handle product selection from searchable dropdown
     * @param {Object} item - Selected item
     */
    async handleProductSelection(item) {
        const productId = item.value;
        
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
     * Handle product dropdown change (legacy method)
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

    /**
     * Update the Kendo UI combobox data
     * @param {Array} newData - New product data
     */
    updateProductData(newData) {
        this.productData = newData;
        if (this.searchableDropdown) {
            this.searchableDropdown.dataSource.data(newData);
            this.searchableDropdown.refresh();
        }
    }

    /**
     * Get the selected product from Kendo UI combobox
     * @returns {Object|null} Selected product or null
     */
    getSelectedProduct() {
        if (this.searchableDropdown) {
            const value = this.searchableDropdown.value();
            const dataItem = this.searchableDropdown.dataItem();
            return {
                value: value,
                text: dataItem ? dataItem.Text : ''
            };
        }
        return null;
    }

    /**
     * Test method to verify dropdown functionality
     */
    testDropdown() {
        console.log('Testing Kendo UI combobox functionality...');
        console.log('Kendo UI available:', typeof kendo !== 'undefined');
        console.log('jQuery Kendo ComboBox available:', typeof $.fn.kendoComboBox !== 'undefined');
        console.log('Dashboard service instance:', this);
        console.log('Kendo combobox instance:', this.searchableDropdown);
        console.log('Product data:', this.productData);
        
        const productDropdown = document.getElementById('productDropdown');
        console.log('Product dropdown element:', productDropdown);
        
        if (this.searchableDropdown) {
            console.log('Combobox value:', this.searchableDropdown.value());
            console.log('Combobox data source:', this.searchableDropdown.dataSource.data());
            console.log('Combobox enabled:', this.searchableDropdown.enabled);
            console.log('Combobox readonly:', this.searchableDropdown.readonly);
        }
        
        // Check if original dropdown is hidden
        if (productDropdown) {
            console.log('Original dropdown display style:', productDropdown.style.display);
        }
    }

    /**
     * Test Kendo UI combobox functionality
     */
    testKendoComboBox() {
        if (!this.searchableDropdown) {
            console.error('Kendo UI combobox not initialized');
            return;
        }

        console.log('Testing Kendo UI combobox...');
        console.log('- Value:', this.searchableDropdown.value());
        console.log('- Enabled:', this.searchableDropdown.enabled);
        console.log('- Data source count:', this.searchableDropdown.dataSource.data().length);
        
        // Test opening the dropdown
        try {
            this.searchableDropdown.open();
            console.log('Combobox opened successfully');
        } catch (error) {
            console.error('Error opening combobox:', error);
        }
    }
}

// Initialize the service when DOM is ready
$(document).ready(() => {
    // Wait a bit to ensure all scripts are loaded
    setTimeout(() => {
        console.log('Initializing dashboard service...');
        console.log('Script loading check:');
        console.log('- jQuery:', typeof $ !== 'undefined');
        console.log('- Kendo:', typeof kendo !== 'undefined');
        console.log('- Kendo ComboBox:', typeof $.fn.kendoComboBox !== 'undefined');
        
        window.dashboardService = new DashboardService();
        console.log('Dashboard service initialized');
        
        // Make test functions available globally
        window.testDropdown = () => {
            if (window.dashboardService) {
                window.dashboardService.testDropdown();
            } else {
                console.error('Dashboard service not available');
            }
        };

        window.testKendoComboBox = () => {
            if (window.dashboardService) {
                window.dashboardService.testKendoComboBox();
            } else {
                console.error('Dashboard service not available');
            }
        };

        window.openComboBox = () => {
            if (window.dashboardService && window.dashboardService.searchableDropdown) {
                window.dashboardService.searchableDropdown.open();
            } else {
                console.error('Kendo UI combobox not available');
            }
        };

        window.closeComboBox = () => {
            if (window.dashboardService && window.dashboardService.searchableDropdown) {
                window.dashboardService.searchableDropdown.close();
            } else {
                console.error('Kendo UI combobox not available');
            }
        };

        window.getComboBoxValue = () => {
            if (window.dashboardService && window.dashboardService.searchableDropdown) {
                return window.dashboardService.searchableDropdown.value();
            } else {
                console.error('Kendo UI combobox not available');
                return null;
            }
        };
        
        console.log('You can test the Kendo UI combobox by running:');
        console.log('- testDropdown() - Get general dropdown info');
        console.log('- testKendoComboBox() - Test Kendo UI combobox functionality');
        console.log('- openComboBox() - Open the combobox');
        console.log('- closeComboBox() - Close the combobox');
        console.log('- getComboBoxValue() - Get current combobox value');
    }, 1000); // Increased delay to ensure all Kendo UI scripts are loaded
});
