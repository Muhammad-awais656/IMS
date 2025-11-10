/**
 * Generate Bills Service - Handles searchable dropdowns in the Generate Bills form
 */
class GenerateBillsService {
    constructor() {
        this.productDropdown = null;
        this.productSizeDropdown = null;
        this.init();
    }

    /**
     * Initialize the generate bills service
     */
    init() {
        this.initializeProductDropdown();
        this.bindEvents();
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
            console.error('Product select element not found in Generate Bills form');
            return;
        }

        // Check if already converted by universal service
        const existingContainer = document.getElementById('searchable_productSelect');
        if (existingContainer) {
            console.log('Product dropdown already converted by universal service');
            this.productDropdown = existingContainer.searchableDropdown;
            return;
        }

        console.log('Initializing searchable product dropdown for Generate Bills form...');

        // Get product data
        const productData = Array.from(productSelect.options).map(option => ({
            value: option.value,
            text: option.text
        })).filter(item => item.value !== ''); // Remove empty option

        console.log('Product data loaded for Generate Bills form:', productData);

        // Create searchable dropdown container
        const container = document.createElement('div');
        container.id = 'searchableProductSelectGenerateBills';
        productSelect.parentNode.insertBefore(container, productSelect);
        productSelect.style.display = 'none'; // Hide original dropdown

        try {
            // Initialize searchable dropdown
            this.productDropdown = new SearchableDropdown('searchableProductSelectGenerateBills', {
                placeholder: '--Select a value--',
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
            
            console.log('Searchable product dropdown initialized successfully for Generate Bills form');
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
        
        console.log('Product selected in Generate Bills form:', { productId, productText });

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
     * Test method to verify dropdown functionality
     */
    testProductDropdown() {
        console.log('Testing Generate Bills product dropdown...');
        console.log('SearchableDropdown class available:', typeof SearchableDropdown !== 'undefined');
        console.log('Generate Bills service instance:', this);
        console.log('Product dropdown instance:', this.productDropdown);
        
        const container = document.getElementById('searchableProductSelectGenerateBills');
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
        console.log('Initializing Generate Bills service...');
        window.generateBillsService = new GenerateBillsService();
        console.log('Generate Bills service initialized');
        
        // Make test function available globally
        window.testGenerateBillsDropdown = () => {
            if (window.generateBillsService) {
                window.generateBillsService.testProductDropdown();
            } else {
                console.error('Generate Bills service not available');
            }
        };
        
        console.log('You can test the Generate Bills dropdown by running: testGenerateBillsDropdown()');
    }, 1000); // Wait longer to ensure all scripts are loaded
});

