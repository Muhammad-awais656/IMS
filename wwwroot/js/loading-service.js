/**
 * Loading Service - Handles loading indicators and data population
 */
class LoadingService {
    constructor() {
        this.isLoading = false;
        this.loadingOverlay = null;
        this.init();
    }

    /**
     * Initialize the loading service
     */
    init() {
        this.createLoadingOverlay();
    }

    /**
     * Create the loading overlay element
     */
    createLoadingOverlay() {
        this.loadingOverlay = document.createElement('div');
        this.loadingOverlay.className = 'loading-overlay';
        this.loadingOverlay.style.display = 'none';
        
        this.loadingOverlay.innerHTML = `
            <div style="text-align: center;">
                <div class="loading-spinner"></div>
                <div class="loading-text">Loading data...</div>
            </div>
        `;
        
        document.body.appendChild(this.loadingOverlay);
    }

    /**
     * Show loading indicator
     * @param {string} message - Loading message to display
     */
    showLoading(message = 'Loading data...') {
        if (this.loadingOverlay) {
            const loadingText = this.loadingOverlay.querySelector('.loading-text');
            if (loadingText) {
                loadingText.textContent = message;
            }
            this.loadingOverlay.style.display = 'flex';
            this.isLoading = true;
        }
    }

    /**
     * Hide loading indicator
     */
    hideLoading() {
        if (this.loadingOverlay) {
            this.loadingOverlay.style.display = 'none';
            this.isLoading = false;
        }
    }

    /**
     * Show loading with promise
     * @param {Promise} promise - Promise to wait for
     * @param {string} message - Loading message
     * @returns {Promise} The original promise
     */
    async withLoading(promise, message = 'Loading data...') {
        this.showLoading(message);
        try {
            const result = await promise;
            return result;
        } finally {
            this.hideLoading();
        }
    }

    /**
     * Wait for all dropdowns to be populated
     * @param {number} maxWaitTime - Maximum time to wait in milliseconds
     * @returns {Promise} Promise that resolves when all data is loaded
     */
    async waitForDataPopulation(maxWaitTime = 5000) {
        return new Promise((resolve) => {
            const startTime = Date.now();
            
            const checkDataLoaded = () => {
                const allSelects = document.querySelectorAll('select');
                let allDataLoaded = true;
                
                allSelects.forEach(select => {
                    // Check if select has options (more than just placeholder)
                    if (select.options.length <= 1) {
                        allDataLoaded = false;
                    }
                });
                
                const elapsedTime = Date.now() - startTime;
                
                if (allDataLoaded || elapsedTime >= maxWaitTime) {
                    resolve(allDataLoaded);
                } else {
                    setTimeout(checkDataLoaded, 100);
                }
            };
            
            checkDataLoaded();
        });
    }

    /**
     * Initialize page with loading
     * @param {Function} initFunction - Function to call during loading
     * @param {string} message - Loading message
     */
    async initializePage(initFunction, message = 'Initializing page...') {
        this.showLoading(message);
        
        try {
            // Wait for data to be populated
            await this.waitForDataPopulation();
            
            // Call the initialization function
            if (typeof initFunction === 'function') {
                await initFunction();
            }
            
            // Wait a bit more to ensure everything is ready
            await new Promise(resolve => setTimeout(resolve, 500));
            
        } finally {
            this.hideLoading();
        }
    }

    /**
     * Check if data is properly loaded
     * @returns {boolean} True if data is loaded
     */
    isDataLoaded() {
        const allSelects = document.querySelectorAll('select');
        
        for (const select of allSelects) {
            // Check if select has meaningful options (more than just placeholder)
            if (select.options.length <= 1) {
                return false;
            }
            
            // Check if first option is not a placeholder
            const firstOption = select.options[0];
            if (firstOption && (firstOption.value === '' || firstOption.text.includes('Select'))) {
                if (select.options.length <= 1) {
                    return false;
                }
            }
        }
        
        return true;
    }

    /**
     * Force refresh all dropdown data
     */
    async refreshDropdownData() {
        this.showLoading('Refreshing dropdown data...');
        
        try {
            // Trigger a page refresh or data reload
            if (window.universalDropdownService) {
                // Revert and reconvert all dropdowns
                window.universalDropdownService.revertAllDropdowns();
                await new Promise(resolve => setTimeout(resolve, 100));
                window.universalDropdownService.convertAllDropdowns();
            }
            
            // Wait for data to be populated
            await this.waitForDataPopulation();
            
        } finally {
            this.hideLoading();
        }
    }
}

// Initialize the loading service
$(document).ready(() => {
    window.loadingService = new LoadingService();
    
    // Make functions available globally
    window.showLoading = (message) => window.loadingService.showLoading(message);
    window.hideLoading = () => window.loadingService.hideLoading();
    window.refreshDropdownData = () => window.loadingService.refreshDropdownData();
    
    console.log('Loading Service initialized');
    console.log('Available functions:');
    console.log('- showLoading(message) - Show loading indicator');
    console.log('- hideLoading() - Hide loading indicator');
    console.log('- refreshDropdownData() - Refresh all dropdown data');
});

