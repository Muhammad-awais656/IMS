/**
 * Sales Form Service - Handles searchable dropdowns in the sales form
 */
class SalesFormService {
    constructor() {
        this.productDropdown = null;
        this.productSizeDropdown = null;
        this.init();
    }

    /**
     * Initialize the sales form service
     */
    init() {
        this.initializeProductDropdown();
        this.bindEvents();
        
        // Start aggressive cleanup to prevent universal service from interfering
        setTimeout(() => {
            this.startAggressiveCleanup();
        }, 2000); // Start after universal service would have run
    }

    /**
     * Initialize the searchable product dropdown
     */
    initializeProductDropdown() {
        // Check if SearchableDropdown class is available
        if (typeof SearchableDropdown === 'undefined') {
            console.error('SearchableDropdown class not found. Make sure searchable-dropdown.js is loaded.');
            return;
        }

        // Get product data from the existing dropdown
        const productSelect = document.getElementById('productSelect');
        if (!productSelect) {
            console.error('Product select element not found');
            return;
        }

        // Check if already converted by universal service
        const existingContainer = document.getElementById('searchable_productSelect') || document.getElementById('searchableProductSelectSales');
        if (existingContainer) {
            console.log('Product dropdown already converted by universal service');
            this.productDropdown = existingContainer.searchableDropdown;
            return;
        }

        console.log('Initializing searchable product dropdown for sales form...');

        // PREVENT UNIVERSAL SERVICE FROM CONVERTING THIS DROPDOWN
        // Add data attribute to prevent universal service conversion
        productSelect.setAttribute('data-no-convert', 'true');
        productSelect.setAttribute('data-sales-form', 'true');
        
        // Mark as converted by sales service to prevent universal service
        if (window.universalDropdownService) {
            window.universalDropdownService.convertedDropdowns.set('productSelect', true);
        }

        // Get product data
        const productData = Array.from(productSelect.options).map(option => ({
            value: option.value,
            text: option.text
        })).filter(item => item.value !== ''); // Remove empty option

        console.log('Product data loaded for sales form:', productData);

        // Create searchable dropdown container
        const container = document.createElement('div');
        container.id = 'searchableProductSelectSales';
        productSelect.parentNode.insertBefore(container, productSelect);
        
        // Completely hide original dropdown
        productSelect.style.display = 'none';
        productSelect.style.visibility = 'hidden';
        productSelect.style.position = 'absolute';
        productSelect.style.left = '-9999px';
        productSelect.style.opacity = '0';
        productSelect.setAttribute('aria-hidden', 'true');

        try {
            // Initialize searchable dropdown
            this.productDropdown = new SearchableDropdown('searchableProductSelectSales', {
                placeholder: '--Select Product--',
                searchPlaceholder: 'Type product name to search...',
                noResultsText: 'No products found',
                dataSource: productData,
                valueField: 'value',
                textField: 'text',
                onSelect: (item) => {
                    this.handleProductSelection(item);
                }
            });
            
            // Store reference on container for universal service to find
            container.searchableDropdown = this.productDropdown;
            
            console.log('Searchable product dropdown initialized successfully for sales form');
        } catch (error) {
            console.error('Error initializing searchable product dropdown:', error);
            // Show original dropdown if searchable dropdown fails
            productSelect.style.display = 'block';
        }
    }

    /**
     * Handle product selection from searchable dropdown
     * @param {Object} item - Selected item
     */
    handleProductSelection(item) {
        const productId = item.value;
        const productText = item.text;
        
        console.log('Product selected in sales form:', { productId, productText });

        // Update the original select element to maintain compatibility
        const originalSelect = document.getElementById('productSelect');
        if (originalSelect) {
            // Find and select the corresponding option
            const option = Array.from(originalSelect.options).find(opt => opt.value === productId);
            if (option) {
                originalSelect.value = productId;
                // Trigger the change event to maintain existing functionality
                const changeEvent = new Event('change', { bubbles: true });
                originalSelect.dispatchEvent(changeEvent);
            }
        }
    }

    /**
     * Bind event listeners
     */
    bindEvents() {
        // Keep existing event listeners working
        // The original select element is hidden but still functional
    }

    /**
     * Update the product dropdown data
     * @param {Array} newData - New product data
     */
    updateProductData(newData) {
        if (this.productDropdown) {
            this.productDropdown.setDataSource(newData);
        }
    }

    /**
     * Get the selected product
     * @returns {Object|null} Selected product or null
     */
    getSelectedProduct() {
        if (this.productDropdown) {
            return {
                value: this.productDropdown.getValue(),
                text: this.productDropdown.getText()
            };
        }
        return null;
    }

    /**
     * Set the selected product
     * @param {string} value - Product value to select
     */
    setSelectedProduct(value) {
        if (this.productDropdown) {
            this.productDropdown.setValue(value);
        }
    }

    /**
     * Clear the product selection
     */
    clearProductSelection() {
        if (this.productDropdown) {
            this.productDropdown.clear();
        }
    }

    /**
     * Clean up duplicate dropdowns
     */
    cleanupDuplicateDropdowns() {
        console.log('Cleaning up duplicate product dropdowns in sales form...');
        
        // Remove any universal service containers
        const universalContainer = document.getElementById('searchable_productSelect');
        if (universalContainer) {
            console.log('Removing universal service container');
            universalContainer.remove();
            
            // Also remove from universal service tracking
            if (window.universalDropdownService) {
                window.universalDropdownService.convertedDropdowns.delete('productSelect');
            }
        }
        
        // Ensure original dropdown is completely hidden and marked
        const originalSelect = document.getElementById('productSelect');
        if (originalSelect) {
            originalSelect.style.display = 'none';
            originalSelect.style.visibility = 'hidden';
            originalSelect.style.position = 'absolute';
            originalSelect.style.left = '-9999px';
            originalSelect.style.opacity = '0';
            originalSelect.setAttribute('data-no-convert', 'true');
            originalSelect.setAttribute('data-sales-form', 'true');
            originalSelect.setAttribute('aria-hidden', 'true');
        }
        
        // Ensure our container is visible
        const ourContainer = document.getElementById('searchableProductSelectSales');
        if (ourContainer) {
            ourContainer.style.display = 'block';
            ourContainer.style.visibility = 'visible';
        }
        
        // Mark as converted by sales service
        if (window.universalDropdownService) {
            window.universalDropdownService.convertedDropdowns.set('productSelect', true);
        }
    }

    /**
     * Reset the product dropdown to default state
     */
    resetProductDropdown() {
        console.log('Resetting product dropdown...');
        
        if (this.productDropdown) {
            // Clear the searchable dropdown
            this.productDropdown.clear();
            this.productDropdown.setValue('');
        }
        
        // Also reset the original select element and trigger change event
        const originalSelect = document.getElementById('productSelect');
        if (originalSelect) {
            originalSelect.value = '';
            
            // Trigger the change event to clear dependent dropdowns (like product size)
            const changeEvent = new Event('change', { bubbles: true });
            originalSelect.dispatchEvent(changeEvent);
            
            console.log('Product dropdown change event triggered');
        }
        
        console.log('Product dropdown reset completed');
    }

    /**
     * Aggressive cleanup that runs periodically
     */
    startAggressiveCleanup() {
        console.log('Starting aggressive cleanup for sales form dropdowns...');
        
        // Run cleanup immediately
        this.cleanupDuplicateDropdowns();
        
        // Run cleanup every 500ms for the first 5 seconds
        let cleanupCount = 0;
        const maxCleanups = 10; // 5 seconds at 500ms intervals
        
        const cleanupInterval = setInterval(() => {
            this.cleanupDuplicateDropdowns();
            cleanupCount++;
            
            if (cleanupCount >= maxCleanups) {
                clearInterval(cleanupInterval);
                console.log('Aggressive cleanup completed');
            }
        }, 500);
    }

    /**
     * Test method to verify dropdown functionality
     */
    testProductDropdown() {
        console.log('Testing sales form product dropdown...');
        console.log('SearchableDropdown class available:', typeof SearchableDropdown !== 'undefined');
        console.log('Sales form service instance:', this);
        console.log('Product dropdown instance:', this.productDropdown);
        
        const container = document.getElementById('searchableProductSelectSales');
        console.log('Product dropdown container:', container);
        
        if (container) {
            const display = container.querySelector('.dropdown-display');
            console.log('Product dropdown display element:', display);
            
            if (display) {
                console.log('Display element classes:', display.className);
                console.log('Display element style:', display.style.cssText);
                console.log('Display element computed style:', window.getComputedStyle(display));
            }
        }
        
        // Check if original dropdown is hidden
        const originalSelect = document.getElementById('productSelect');
        console.log('Original product select:', originalSelect);
        if (originalSelect) {
            console.log('Original select display style:', originalSelect.style.display);
        }
    }
}

// Initialize the service when DOM is ready
$(document).ready(() => {
    // Wait a bit to ensure all scripts are loaded
    setTimeout(() => {
        console.log('Initializing sales form service...');
        window.salesFormService = new SalesFormService();
        console.log('Sales form service initialized');
        
        // Make test function available globally
        window.testSalesDropdown = () => {
            if (window.salesFormService) {
                window.salesFormService.testProductDropdown();
            } else {
                console.error('Sales form service not available');
            }
        };

        // Make cleanup function available globally
        window.cleanupSalesDropdowns = () => {
            if (window.salesFormService) {
                window.salesFormService.cleanupDuplicateDropdowns();
            } else {
                console.error('Sales form service not available');
            }
        };

        // Make aggressive cleanup function available globally
        window.startAggressiveSalesCleanup = () => {
            if (window.salesFormService) {
                window.salesFormService.startAggressiveCleanup();
            } else {
                console.error('Sales form service not available');
            }
        };

        // Make reset function available globally
        window.resetSalesProductDropdown = () => {
            if (window.salesFormService) {
                window.salesFormService.resetProductDropdown();
            } else {
                console.error('Sales form service not available');
            }
        };
        
        console.log('You can test the sales dropdown by running: testSalesDropdown()');
        console.log('You can cleanup duplicate dropdowns by running: cleanupSalesDropdowns()');
        console.log('You can start aggressive cleanup by running: startAggressiveSalesCleanup()');
        console.log('You can reset the product dropdown by running: resetSalesProductDropdown()');
    }, 1000); // Wait longer to ensure all scripts are loaded
});
