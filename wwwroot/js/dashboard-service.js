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
        // Try to initialize searchable dropdown, but don't fail if it doesn't work
        try {
            this.initializeSearchableDropdown();
        } catch (error) {
            console.error('Failed to initialize searchable dropdown:', error);
            // Show original dropdown as fallback
            const productDropdown = document.getElementById('productDropdown');
            if (productDropdown) {
                productDropdown.style.display = 'block';
            }
        }
    }

    /**
     * Initialize the searchable dropdown
     */
    initializeSearchableDropdown() {
        // Check if SearchableDropdown class is available
        if (typeof SearchableDropdown === 'undefined') {
            console.error('SearchableDropdown class not found. Make sure searchable-dropdown.js is loaded.');
            return;
        }

        // Get product data from the existing dropdown
        const productDropdown = document.getElementById('productDropdown');
        if (!productDropdown) {
            console.error('Product dropdown element not found');
            return;
        }

        console.log('Initializing searchable dropdown...');

        this.productData = Array.from(productDropdown.options).map(option => ({
            value: option.value,
            text: option.text
        })).filter(item => item.value !== ''); // Remove empty option

        console.log('Product data loaded:', this.productData);

        // Create searchable dropdown container
        const container = document.createElement('div');
        container.id = 'searchableProductDropdown';
        productDropdown.parentNode.insertBefore(container, productDropdown);
        productDropdown.style.display = 'none'; // Hide original dropdown

        try {
            // Initialize searchable dropdown
            this.searchableDropdown = new SearchableDropdown('searchableProductDropdown', {
                placeholder: 'Select a product...',
                searchPlaceholder: 'Type product name to search...',
                noResultsText: 'No products found',
                dataSource: this.productData,
                valueField: 'value',
                textField: 'text',
                onSelect: (item) => {
                    console.log('Product selected:', item);
                    this.handleProductSelection(item);
                }
            });
            console.log('Searchable dropdown initialized successfully');
            
            // Test if dropdown is actually working
            setTimeout(() => {
                this.testDropdownClickability();
            }, 1000);
            
        } catch (error) {
            console.error('Error initializing searchable dropdown:', error);
            // Show original dropdown if searchable dropdown fails
            productDropdown.style.display = 'block';
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
     * Update the searchable dropdown data
     * @param {Array} newData - New product data
     */
    updateProductData(newData) {
        this.productData = newData;
        if (this.searchableDropdown) {
            this.searchableDropdown.setDataSource(newData);
        }
    }

    /**
     * Get the selected product from searchable dropdown
     * @returns {Object|null} Selected product or null
     */
    getSelectedProduct() {
        if (this.searchableDropdown) {
            return {
                value: this.searchableDropdown.getValue(),
                text: this.searchableDropdown.getText()
            };
        }
        return null;
    }

    /**
     * Test method to verify dropdown functionality
     */
    testDropdown() {
        console.log('Testing dropdown functionality...');
        console.log('SearchableDropdown class available:', typeof SearchableDropdown !== 'undefined');
        console.log('Dashboard service instance:', this);
        console.log('Searchable dropdown instance:', this.searchableDropdown);
        console.log('Product data:', this.productData);
        
        const container = document.getElementById('searchableProductDropdown');
        console.log('Dropdown container:', container);
        
        if (container) {
            const display = container.querySelector('.dropdown-display');
            console.log('Dropdown display element:', display);
            
            if (display) {
                console.log('Display element classes:', display.className);
                console.log('Display element style:', display.style.cssText);
                console.log('Display element computed style:', window.getComputedStyle(display));
                console.log('Display element pointer events:', window.getComputedStyle(display).pointerEvents);
                console.log('Display element z-index:', window.getComputedStyle(display).zIndex);
                
                // Test if element is clickable
                const rect = display.getBoundingClientRect();
                console.log('Display element position:', rect);
                console.log('Element is visible:', rect.width > 0 && rect.height > 0);
                
                // Test click event manually
                console.log('Testing manual click...');
                display.click();
            }
        }
        
        // Check if original dropdown is hidden
        const originalDropdown = document.getElementById('productDropdown');
        console.log('Original dropdown:', originalDropdown);
        if (originalDropdown) {
            console.log('Original dropdown display style:', originalDropdown.style.display);
        }
    }

    /**
     * Test dropdown clickability
     */
    testDropdownClickability() {
        const container = document.getElementById('searchableProductDropdown');
        if (!container) {
            console.error('Dropdown container not found');
            return;
        }

        const display = container.querySelector('.dropdown-display');
        if (!display) {
            console.error('Dropdown display element not found');
            return;
        }

        // Check if element is actually clickable
        const rect = display.getBoundingClientRect();
        const isVisible = rect.width > 0 && rect.height > 0;
        const hasPointerEvents = window.getComputedStyle(display).pointerEvents !== 'none';
        
        console.log('Dropdown clickability test:');
        console.log('- Element visible:', isVisible);
        console.log('- Has pointer events:', hasPointerEvents);
        console.log('- Element position:', rect);
        
        if (!isVisible || !hasPointerEvents) {
            console.warn('Dropdown appears to be not clickable, creating simple fallback');
            this.createSimpleDropdown();
        }
    }

    /**
     * Create a simple fallback dropdown
     */
    createSimpleDropdown() {
        const container = document.getElementById('searchableProductDropdown');
        if (!container) return;

        console.log('Creating simple dropdown fallback...');
        
        // Create a simple select element
        const simpleSelect = document.createElement('select');
        simpleSelect.className = 'form-control';
        simpleSelect.id = 'simpleProductDropdown';
        
        // Add options
        const defaultOption = document.createElement('option');
        defaultOption.value = '';
        defaultOption.textContent = 'Select a product...';
        simpleSelect.appendChild(defaultOption);
        
        this.productData.forEach(product => {
            const option = document.createElement('option');
            option.value = product.value;
            option.textContent = product.text;
            simpleSelect.appendChild(option);
        });
        
        // Add change event
        simpleSelect.addEventListener('change', (e) => {
            if (e.target.value) {
                this.handleProductSelection({
                    value: e.target.value,
                    text: e.target.options[e.target.selectedIndex].text
                });
            }
        });
        
        // Replace the complex dropdown with simple one
        container.innerHTML = '';
        container.appendChild(simpleSelect);
        
        console.log('Simple dropdown created successfully');
    }
}

// Initialize the service when DOM is ready
$(document).ready(() => {
    // Wait a bit to ensure all scripts are loaded
    setTimeout(() => {
        console.log('Initializing dashboard service...');
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

        window.forceOpenDropdown = () => {
            if (window.dashboardService && window.dashboardService.searchableDropdown) {
                window.dashboardService.searchableDropdown.forceOpen();
            } else {
                console.error('Searchable dropdown not available');
            }
        };

        window.forceCloseDropdown = () => {
            if (window.dashboardService && window.dashboardService.searchableDropdown) {
                window.dashboardService.searchableDropdown.forceClose();
            } else {
                console.error('Searchable dropdown not available');
            }
        };

        window.createSimpleDropdown = () => {
            if (window.dashboardService) {
                window.dashboardService.createSimpleDropdown();
            } else {
                console.error('Dashboard service not available');
            }
        };

        window.forceShowMenu = () => {
            if (window.dashboardService && window.dashboardService.searchableDropdown) {
                window.dashboardService.searchableDropdown.forceShowMenu();
            } else {
                console.error('Searchable dropdown not available');
            }
        };

        window.manualCloseDropdown = () => {
            if (window.dashboardService && window.dashboardService.searchableDropdown) {
                window.dashboardService.searchableDropdown.close();
            } else {
                console.error('Searchable dropdown not available');
            }
        };
        
        console.log('You can test the dropdown by running:');
        console.log('- testDropdown() - Get dropdown info');
        console.log('- forceOpenDropdown() - Force open dropdown');
        console.log('- forceCloseDropdown() - Force close dropdown');
        console.log('- forceShowMenu() - Force show dropdown menu');
        console.log('- manualCloseDropdown() - Manually close dropdown');
        console.log('- createSimpleDropdown() - Create simple fallback dropdown');
    }, 500); // Increased delay to ensure all scripts are loaded
});
