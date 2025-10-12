/**
 * Universal Dropdown Service - Automatically converts all dropdowns to searchable dropdowns
 */
class UniversalDropdownService {
    constructor() {
        this.convertedDropdowns = new Map();
        this.config = window.DropdownConfig || {};
        this.excludeSelectors = this.config.excludeSelectors || [
            '#productDropdown', // Dashboard dropdown (already handled)
            '#searchableProductDropdown', // Already converted
            '#searchableProductSelect', // Sales form dropdown (already handled)
            'select[multiple]', // Multi-select dropdowns
            'select[data-no-convert]' // Explicitly excluded dropdowns
        ];
        this.init();
    }

    /**
     * Initialize the universal dropdown service
     */
    init() {
        // Wait for DOM to be ready and all scripts loaded
        $(document).ready(() => {
            setTimeout(async () => {
                // Check if we're on a page that needs special handling
                const currentPath = window.location.pathname;
                const isProductPage = currentPath.includes('/Product/');
                const isVendorBillsPage = currentPath.includes('/VendorBills/');
                
                // Show loading while converting dropdowns
                if (window.loadingService) {
                    const loadingMessage = isProductPage ? 'Loading product data...' : 
                                         isVendorBillsPage ? 'Loading bill data...' : 
                                         'Converting dropdowns...';
                    
                    await window.loadingService.initializePage(async () => {
                        this.convertAllDropdowns();
                        this.observeNewDropdowns();
                        
                        // Check for data integrity issues on product pages
                        if (isProductPage) {
                            setTimeout(() => {
                                const issues = this.checkDataIntegrity();
                                // If issues found, try to fix them
                                if (issues && issues.length > 0) {
                                    console.log('🔧 Auto-fixing data mixing issues...');
                                    this.forceFixAllDropdowns();
                                }
                            }, 1000);
                        }
                    }, loadingMessage);
                } else {
                    this.convertAllDropdowns();
                    this.observeNewDropdowns();
                    
                    // Check for data integrity issues on product pages
                    if (isProductPage) {
                        setTimeout(() => {
                            const issues = this.checkDataIntegrity();
                            // If issues found, try to fix them
                            if (issues && issues.length > 0) {
                                console.log('🔧 Auto-fixing data mixing issues...');
                                this.forceFixAllDropdowns();
                            }
                        }, 1000);
                    }
                }
            }, 2000); // Wait longer to let other services initialize first
        });
    }

    /**
     * Convert all dropdowns to searchable dropdowns
     */
    convertAllDropdowns() {
        console.log('Universal Dropdown Service: Starting conversion of all dropdowns...');
        
        // Find all select elements
        const allSelects = document.querySelectorAll('select');
        let convertedCount = 0;
        
        allSelects.forEach((select, index) => {
            if (this.shouldConvertDropdown(select)) {
                this.convertDropdown(select, index);
                convertedCount++;
            }
        });
        
        console.log(`Universal Dropdown Service: Converted ${convertedCount} dropdowns`);
    }

    /**
     * Check if a dropdown should be converted
     * @param {HTMLSelectElement} select - The select element
     * @returns {boolean} True if should be converted
     */
    shouldConvertDropdown(select) {
        const selectId = select.id || select.name;
        
        // Check if already converted
        if (this.convertedDropdowns.has(selectId)) {
            console.log(`Skipping ${selectId} - already converted by universal service`);
            return false;
        }

        // Check if there's already a searchable dropdown for this element
        if (selectId) {
            const existingContainer = document.getElementById(`searchable_${selectId}`);
            if (existingContainer) {
                console.log(`Skipping ${selectId} - already has searchable dropdown`);
                return false;
            }
        }

        // Check if explicitly excluded
        for (const selector of this.excludeSelectors) {
            if (select.matches && select.matches(selector)) {
                console.log(`Skipping ${selectId} - matches exclusion selector: ${selector}`);
                return false;
            }
            if (select.id === selector.replace('#', '') || select.name === selector.replace('#', '')) {
                console.log(`Skipping ${selectId} - matches exclusion ID: ${selector}`);
                return false;
            }
        }

        // Special check for sales form product dropdown
        if (selectId === 'productSelect') {
            console.log(`Skipping ${selectId} - sales form product dropdown (handled by sales service)`);
            return false;
        }

        // Check if it's a multi-select
        if (select.multiple) {
            console.log(`Skipping ${selectId} - multi-select dropdown`);
            return false;
        }

        // Check if it has the no-convert attribute
        if (select.hasAttribute('data-no-convert')) {
            console.log(`Skipping ${selectId} - has data-no-convert attribute`);
            return false;
        }

        // Check if SearchableDropdown class is available
        if (typeof SearchableDropdown === 'undefined') {
            console.warn('SearchableDropdown class not available, skipping conversion');
            return false;
        }

        console.log(`Converting dropdown: ${selectId}`);
        return true;
    }

    /**
     * Convert a single dropdown to searchable dropdown
     * @param {HTMLSelectElement} select - The select element to convert
     * @param {number} index - Index for unique ID generation
     */
    convertDropdown(select, index) {
        try {
            const originalId = select.id || select.name || `dropdown_${index}`;
            const containerId = `searchable_${originalId}`;
            
            console.log(`Converting dropdown: ${originalId}`);
            console.log(`Dropdown element:`, select);
            console.log(`Dropdown options count:`, select.options.length);

            // Get dropdown data
            const dropdownData = this.extractDropdownData(select);
            
            if (dropdownData.length === 0) {
                console.log(`Skipping empty dropdown: ${originalId}`);
                return;
            }

            // Create container
            const container = document.createElement('div');
            container.id = containerId;
            container.className = 'universal-searchable-dropdown';
            
            // Insert container before the original select
            select.parentNode.insertBefore(container, select);
            
            // Hide original select
            select.style.display = 'none';
            
            // Get the current selected value from the original select
            const currentValue = select.value;
            const currentText = select.options[select.selectedIndex]?.text || '';

            // Create searchable dropdown
            const searchableDropdown = new SearchableDropdown(containerId, {
                placeholder: this.getPlaceholder(select),
                searchPlaceholder: 'Type to search...',
                noResultsText: 'No options found',
                dataSource: dropdownData,
                valueField: 'value',
                textField: 'text',
                onSelect: (item) => {
                    this.handleSelection(select, item, searchableDropdown);
                }
            });

            // Set the initial value if there's a current selection
            if (currentValue && currentValue !== '') {
                console.log(`Setting initial value for ${originalId}: ${currentValue} - ${currentText}`);
                searchableDropdown.setValue(currentValue);
            }

            // Store reference
            this.convertedDropdowns.set(originalId, {
                original: select,
                searchable: searchableDropdown,
                container: container
            });

            // Listen for changes to the original select (for dynamic updates)
            this.setupOriginalSelectListener(select, searchableDropdown);

            console.log(`Successfully converted dropdown: ${originalId}`);

        } catch (error) {
            console.error(`Error converting dropdown ${select.id || select.name}:`, error);
        }
    }

    /**
     * Setup listener for changes to the original select element
     * @param {HTMLSelectElement} originalSelect - Original select element
     * @param {SearchableDropdown} searchableDropdown - Searchable dropdown instance
     */
    setupOriginalSelectListener(originalSelect, searchableDropdown) {
        // Listen for changes to the original select element
        const observer = new MutationObserver((mutations) => {
            mutations.forEach((mutation) => {
                if (mutation.type === 'childList' && mutation.target === originalSelect) {
                    // Options have been added/removed, update the searchable dropdown
                    const newData = this.extractDropdownData(originalSelect);
                    searchableDropdown.setDataSource(newData);
                    console.log('Updated searchable dropdown data from original select changes');
                }
            });
        });

        observer.observe(originalSelect, {
            childList: true,
            subtree: true
        });

        // Also listen for value changes
        originalSelect.addEventListener('change', () => {
            const originalValue = originalSelect.value;
            const searchableValue = searchableDropdown.getValue();
            
            if (originalValue !== searchableValue) {
                console.log(`Syncing value change: ${searchableValue} -> ${originalValue}`);
                searchableDropdown.setValue(originalValue);
            }
        });

        // Listen for programmatic value changes (when options are updated)
        const valueObserver = new MutationObserver((mutations) => {
            mutations.forEach((mutation) => {
                if (mutation.type === 'attributes' && mutation.attributeName === 'value') {
                    const originalValue = originalSelect.value;
                    const searchableValue = searchableDropdown.getValue();
                    
                    if (originalValue !== searchableValue) {
                        console.log(`Syncing programmatic value change: ${searchableValue} -> ${originalValue}`);
                        searchableDropdown.setValue(originalValue);
                    }
                }
            });
        });

        valueObserver.observe(originalSelect, {
            attributes: true,
            attributeFilter: ['value']
        });
    }

    /**
     * Extract data from a select element
     * @param {HTMLSelectElement} select - The select element
     * @returns {Array} Array of dropdown data
     */
    extractDropdownData(select) {
        const data = [];
        const selectId = select.id || select.name || 'unnamed';
        
        console.log(`Extracting data for dropdown: ${selectId}`);
        
        Array.from(select.options).forEach((option, index) => {
            if (option.value !== '' || option.text.trim() !== '') {
                const optionData = {
                    value: option.value,
                    text: option.text,
                    selected: option.selected,
                    disabled: option.disabled
                };
                data.push(optionData);
                
                // Log first few options for debugging
                if (index < 5) {
                    console.log(`  Option ${index}: ${optionData.value} - ${optionData.text} ${optionData.selected ? '(SELECTED)' : ''}`);
                }
            }
        });
        
        console.log(`Extracted ${data.length} options for ${selectId}`);
        return data;
    }

    /**
     * Get placeholder text for dropdown
     * @param {HTMLSelectElement} select - The select element
     * @returns {string} Placeholder text
     */
    getPlaceholder(select) {
        const selectId = select.id || select.name;
        
        // Check for custom placeholder
        if (this.config.customPlaceholders && this.config.customPlaceholders[selectId]) {
            return this.config.customPlaceholders[selectId];
        }
        
        // Try to find a default option
        const defaultOption = Array.from(select.options).find(option => 
            option.value === '' && option.text.includes('Select')
        );
        
        if (defaultOption) {
            return defaultOption.text;
        }
        
        // Generate placeholder based on label or name
        const label = document.querySelector(`label[for="${select.id}"]`);
        if (label) {
            return `Select ${label.textContent.replace(':', '').trim()}...`;
        }
        
        return 'Select an option...';
    }

    /**
     * Handle selection from searchable dropdown
     * @param {HTMLSelectElement} originalSelect - Original select element
     * @param {Object} item - Selected item
     * @param {SearchableDropdown} searchableDropdown - Searchable dropdown instance
     */
    handleSelection(originalSelect, item, searchableDropdown) {
        console.log('Selection made:', item);
        
        // Update original select
        originalSelect.value = item.value;
        
        // Trigger change event to maintain existing functionality
        const changeEvent = new Event('change', { bubbles: true });
        originalSelect.dispatchEvent(changeEvent);
        
        // Also trigger input event for better compatibility
        const inputEvent = new Event('input', { bubbles: true });
        originalSelect.dispatchEvent(inputEvent);
        
        // Handle dependent dropdowns (like Category -> Product)
        this.handleDependentDropdowns(originalSelect, item);
    }

    /**
     * Handle dependent dropdowns that need to be updated when a selection is made
     * @param {HTMLSelectElement} originalSelect - Original select element
     * @param {Object} item - Selected item
     */
    handleDependentDropdowns(originalSelect, item) {
        const selectId = originalSelect.id || originalSelect.name;
        
        // Check if this is a category dropdown that should update product dropdown
        if (selectId && (selectId.includes('Category') || selectId.includes('category'))) {
            this.updateDependentProductDropdown(originalSelect, item);
        }
        
        // Add more dependent dropdown logic here as needed
    }

    /**
     * Update dependent product dropdown when category changes
     * @param {HTMLSelectElement} categorySelect - Category select element
     * @param {Object} categoryItem - Selected category item
     */
    updateDependentProductDropdown(categorySelect, categoryItem) {
        // Find the product dropdown (look for common patterns)
        const productSelect = this.findDependentProductDropdown(categorySelect);
        
        if (productSelect) {
            const convertedProduct = this.convertedDropdowns.get(productSelect.id || productSelect.name);
            
            if (convertedProduct) {
                // Update the searchable dropdown data source
                this.updateSearchableDropdownDataSource(convertedProduct, categoryItem.value);
            }
        }
    }

    /**
     * Find the dependent product dropdown
     * @param {HTMLSelectElement} categorySelect - Category select element
     * @returns {HTMLSelectElement|null} Product select element
     */
    findDependentProductDropdown(categorySelect) {
        // Look for product dropdown in the same form
        const form = categorySelect.closest('form');
        if (form) {
            // Common patterns for product dropdowns
            const productSelectors = [
                'select[id*="Product"]',
                'select[id*="product"]',
                'select[name*="Product"]',
                'select[name*="product"]',
                '#ProductIdFk',
                '#productSelect'
            ];
            
            for (const selector of productSelectors) {
                const productSelect = form.querySelector(selector);
                if (productSelect && productSelect !== categorySelect) {
                    return productSelect;
                }
            }
        }
        
        return null;
    }

    /**
     * Update searchable dropdown data source
     * @param {Object} convertedDropdown - Converted dropdown object
     * @param {string} categoryId - Category ID to filter products
     */
    updateSearchableDropdownDataSource(convertedDropdown, categoryId) {
        if (!categoryId || categoryId === '') {
            // Clear the product dropdown
            convertedDropdown.searchable.setDataSource([]);
            return;
        }

        // Make AJAX call to get products for the category
        // This will depend on your specific endpoint
        const url = this.getProductsByCategoryUrl();
        
        if (url) {
            fetch(`${url}?categoryId=${categoryId}`)
                .then(response => response.json())
                .then(data => {
                    const productData = data.map(item => ({
                        value: item.value,
                        text: item.text
                    }));
                    
                    // Update the searchable dropdown
                    convertedDropdown.searchable.setDataSource(productData);
                    
                    // Also update the original select element
                    this.updateOriginalSelectOptions(convertedDropdown.original, productData);
                })
                .catch(error => {
                    console.error('Error loading products for category:', error);
                });
        }
    }

    /**
     * Get the URL for fetching products by category
     * @returns {string|null} URL for the API endpoint
     */
    getProductsByCategoryUrl() {
        // Try to find the URL from existing AJAX calls or form actions
        const scripts = document.querySelectorAll('script');
        for (const script of scripts) {
            const content = script.textContent || script.innerText;
            if (content.includes('GetProductsByCategory')) {
                // Extract URL from the script
                const match = content.match(/url:\s*['"`]([^'"`]+GetProductsByCategory[^'"`]*)['"`]/);
                if (match) {
                    return match[1];
                }
            }
        }
        
        // Fallback URLs based on common patterns
        const currentPath = window.location.pathname;
        if (currentPath.includes('/Stock/')) {
            return '/Stock/GetProductsByCategory';
        } else if (currentPath.includes('/Product/')) {
            return '/Product/GetProductsByCategory';
        }
        
        return null;
    }

    /**
     * Update original select element options
     * @param {HTMLSelectElement} select - Original select element
     * @param {Array} data - New options data
     */
    updateOriginalSelectOptions(select, data) {
        // Clear existing options
        select.innerHTML = '';
        
        // Add default option
        const defaultOption = document.createElement('option');
        defaultOption.value = '';
        defaultOption.textContent = '<-- Select Product -->';
        select.appendChild(defaultOption);
        
        // Add new options
        data.forEach(item => {
            const option = document.createElement('option');
            option.value = item.value;
            option.textContent = item.text;
            select.appendChild(option);
        });
    }

    /**
     * Observe for new dropdowns added dynamically
     */
    observeNewDropdowns() {
        const observer = new MutationObserver((mutations) => {
            mutations.forEach((mutation) => {
                mutation.addedNodes.forEach((node) => {
                    if (node.nodeType === Node.ELEMENT_NODE) {
                        // Check if the added node is a select
                        if (node.tagName === 'SELECT' && this.shouldConvertDropdown(node)) {
                            this.convertDropdown(node, Date.now());
                        }
                        
                        // Check for selects within the added node
                        const selects = node.querySelectorAll && node.querySelectorAll('select');
                        if (selects) {
                            selects.forEach((select, index) => {
                                if (this.shouldConvertDropdown(select)) {
                                    this.convertDropdown(select, Date.now() + index);
                                }
                            });
                        }
                    }
                });
            });
        });

        observer.observe(document.body, {
            childList: true,
            subtree: true
        });
    }

    /**
     * Get all converted dropdowns
     * @returns {Map} Map of converted dropdowns
     */
    getConvertedDropdowns() {
        return this.convertedDropdowns;
    }

    /**
     * Revert a dropdown to original state
     * @param {string} originalId - Original dropdown ID
     */
    revertDropdown(originalId) {
        const converted = this.convertedDropdowns.get(originalId);
        if (converted) {
            converted.original.style.display = 'block';
            converted.container.remove();
            this.convertedDropdowns.delete(originalId);
            console.log(`Reverted dropdown: ${originalId}`);
        }
    }

    /**
     * Revert all dropdowns to original state
     */
    revertAllDropdowns() {
        this.convertedDropdowns.forEach((converted, originalId) => {
            this.revertDropdown(originalId);
        });
    }

    /**
     * Clean up duplicate dropdowns
     */
    cleanupDuplicateDropdowns() {
        console.log('Cleaning up duplicate dropdowns...');
        
        // Find all searchable dropdown containers
        const searchableContainers = document.querySelectorAll('[id^="searchable_"]');
        const duplicates = [];
        
        searchableContainers.forEach(container => {
            const originalId = container.id.replace('searchable_', '');
            const originalSelect = document.getElementById(originalId);
            
            if (originalSelect) {
                // Check if original select is hidden (should be)
                if (originalSelect.style.display !== 'none') {
                    duplicates.push({ container, originalSelect, originalId });
                }
            }
        });
        
        console.log(`Found ${duplicates.length} duplicate dropdowns`);
        
        // Remove duplicates
        duplicates.forEach(duplicate => {
            console.log(`Removing duplicate dropdown: ${duplicate.originalId}`);
            duplicate.container.remove();
            duplicate.originalSelect.style.display = 'block';
            this.convertedDropdowns.delete(duplicate.originalId);
        });
        
        return duplicates.length;
    }

    /**
     * Force cleanup of sales form dropdowns
     */
    forceCleanupSalesFormDropdowns() {
        console.log('🔧 Force cleaning up sales form dropdowns...');
        
        // Remove any universal service containers for productSelect
        const universalContainer = document.getElementById('searchable_productSelect');
        if (universalContainer) {
            console.log('Removing universal service container for productSelect');
            universalContainer.remove();
            this.convertedDropdowns.delete('productSelect');
        }
        
        // Ensure original productSelect is hidden
        const originalSelect = document.getElementById('productSelect');
        if (originalSelect) {
            originalSelect.style.display = 'none';
            console.log('Hidden original productSelect');
        }
        
        // Check if sales service container exists
        const salesContainer = document.getElementById('searchableProductSelectSales');
        if (salesContainer) {
            console.log('Sales service container exists');
        } else {
            console.log('Sales service container not found - may need to reinitialize');
        }
        
        return true;
    }

    /**
     * Force cleanup of pagination dropdowns
     */
    forceCleanupPaginationDropdowns() {
        console.log('🔧 Force cleaning up pagination dropdowns...');
        
        // Find all pageSize dropdowns
        const pageSizeDropdowns = document.querySelectorAll('#pageSize, select[onchange*="changePageSize"]');
        let cleanedCount = 0;
        
        pageSizeDropdowns.forEach(select => {
            const selectId = select.id || select.name || 'pageSize';
            
            // Remove any universal service containers
            const universalContainer = document.getElementById(`searchable_${selectId}`);
            if (universalContainer) {
                console.log(`Removing universal service container for ${selectId}`);
                universalContainer.remove();
                this.convertedDropdowns.delete(selectId);
                cleanedCount++;
            }
            
            // Ensure original dropdown is visible
            select.style.display = 'block';
            select.style.visibility = 'visible';
            console.log(`Restored original ${selectId} dropdown`);
        });
        
        console.log(`Cleaned up ${cleanedCount} pagination dropdowns`);
        return cleanedCount;
    }

    /**
     * Sync all dropdown values with their original select elements
     */
    syncAllValues() {
        console.log('Syncing all dropdown values...');
        let syncedCount = 0;
        
        this.convertedDropdowns.forEach((converted, originalId) => {
            const originalSelect = converted.original;
            const searchableDropdown = converted.searchable;
            
            if (originalSelect && searchableDropdown) {
                const originalValue = originalSelect.value;
                const searchableValue = searchableDropdown.getValue();
                
                if (originalValue !== searchableValue) {
                    console.log(`Syncing ${originalId}: ${searchableValue} -> ${originalValue}`);
                    searchableDropdown.setValue(originalValue);
                    syncedCount++;
                }
            }
        });
        
        console.log(`Synced ${syncedCount} dropdown values`);
        return syncedCount;
    }

    /**
     * Check for data mixing issues
     */
    checkDataIntegrity() {
        console.log('=== CHECKING DATA INTEGRITY ===');
        
        const allSelects = document.querySelectorAll('select');
        const dataMap = new Map();
        const issues = [];
        
        allSelects.forEach((select, index) => {
            const selectId = select.id || select.name || `dropdown_${index}`;
            const options = Array.from(select.options).map(opt => opt.text);
            
            // Check if this data already exists for another dropdown
            const dataKey = options.slice(0, 3).join('|'); // Use first 3 options as key
            
            if (dataMap.has(dataKey)) {
                const existingSelectId = dataMap.get(dataKey);
                const issue = {
                    selectId,
                    existingSelectId,
                    options: options.slice(0, 3)
                };
                issues.push(issue);
                console.warn(`⚠️ DATA MIXING DETECTED!`);
                console.warn(`Dropdown "${selectId}" has same data as "${existingSelectId}"`);
                console.warn(`First 3 options: ${options.slice(0, 3).join(', ')}`);
            } else {
                dataMap.set(dataKey, selectId);
            }
        });
        
        if (issues.length > 0) {
            console.warn(`Found ${issues.length} data mixing issues`);
            this.fixDataMixingIssues(issues);
        } else {
            console.log('✅ No data mixing issues found');
        }
        
        console.log('=== END DATA INTEGRITY CHECK ===');
        return issues;
    }

    /**
     * Fix data mixing issues
     * @param {Array} issues - Array of data mixing issues
     */
    fixDataMixingIssues(issues) {
        console.log('🔧 Attempting to fix data mixing issues...');
        
        issues.forEach(issue => {
            const select = document.getElementById(issue.selectId) || document.querySelector(`select[name="${issue.selectId}"]`);
            if (select) {
                console.log(`Fixing dropdown: ${issue.selectId}`);
                
                // Try to identify the correct data source based on the select ID
                const correctData = this.getCorrectDataForDropdown(issue.selectId);
                if (correctData) {
                    this.updateDropdownData(select, correctData);
                }
            }
        });
    }

    /**
     * Get correct data for a dropdown based on its ID
     * @param {string} selectId - The select element ID
     * @returns {Array|null} Correct data or null
     */
    getCorrectDataForDropdown(selectId) {
        // This is a placeholder - in a real scenario, you'd need to make AJAX calls
        // to get the correct data based on the dropdown type
        
        if (selectId.includes('Category') || selectId.includes('category')) {
            // Return correct category data
            return this.getCategoryData();
        } else if (selectId.includes('Vendor') || selectId.includes('vendor')) {
            // Return correct vendor data
            return this.getVendorData();
        } else if (selectId.includes('MeasuringUnit') || selectId.includes('MUT') || selectId.includes('MeasuringUnitType')) {
            // Return correct measuring unit data
            return this.getMeasuringUnitData();
        } else if (selectId.includes('Label') || selectId.includes('label')) {
            // Return correct label data
            return this.getLabelData();
        }
        
        return null;
    }

    /**
     * Get category data (placeholder)
     * @returns {Array} Category data
     */
    getCategoryData() {
        // This would typically make an AJAX call to get categories
        return [
            { value: '', text: '-- Select Category --' },
            { value: '1', text: 'Pure' },
            { value: '2', text: 'Print' },
            { value: '3', text: 'Plain' }
        ];
    }

    /**
     * Get vendor data (placeholder)
     * @returns {Array} Vendor data
     */
    getVendorData() {
        // This would typically make an AJAX call to get vendors
        return [
            { value: '', text: '-- Select Vendor --' },
            { value: '1', text: 'US Pet IND' },
            { value: '2', text: 'IM Traders' },
            { value: '3', text: 'Sweetner' }
        ];
    }

    /**
     * Get measuring unit data (placeholder)
     * @returns {Array} Measuring unit data
     */
    getMeasuringUnitData() {
        // This would typically make an AJAX call to get measuring units
        return [
            { value: '', text: '-- Select MU Type --' },
            { value: '1', text: 'Mass' },
            { value: '2', text: 'Liquid' },
            { value: '3', text: 'Synthetic' },
            { value: '4', text: 'Gas' },
            { value: '5', text: 'Volume' },
            { value: '6', text: 'Length' },
            { value: '7', text: 'Area' }
        ];
    }

    /**
     * Get label data (placeholder)
     * @returns {Array} Label data
     */
    getLabelData() {
        // This would typically make an AJAX call to get labels
        return [
            { value: '', text: '-- Select Label --' },
            { value: '1', text: 'SMS' },
            { value: '2', text: 'Pure' },
            { value: '3', text: 'Print' },
            { value: '4', text: 'Plain' },
            { value: '5', text: 'Premium' },
            { value: '6', text: 'Standard' }
        ];
    }

    /**
     * Update dropdown data
     * @param {HTMLSelectElement} select - The select element
     * @param {Array} data - New data
     */
    updateDropdownData(select, data) {
        // Clear existing options
        select.innerHTML = '';
        
        // Add new options
        data.forEach(item => {
            const option = document.createElement('option');
            option.value = item.value;
            option.textContent = item.text;
            select.appendChild(option);
        });
        
        // Update searchable dropdown if it exists
        const selectId = select.id || select.name;
        const converted = this.convertedDropdowns.get(selectId);
        if (converted && converted.searchable) {
            converted.searchable.setDataSource(data);
        }
        
        console.log(`Updated dropdown data for: ${selectId}`);
    }

    /**
     * Force fix measuring unit dropdown data
     */
    forceFixMeasuringUnitDropdown() {
        console.log('🔧 Force fixing Measuring Unit Type dropdown...');
        
        const measuringUnitSelect = document.getElementById('MeasuringUnitTypeId');
        if (measuringUnitSelect) {
            const correctData = this.getMeasuringUnitData();
            this.updateDropdownData(measuringUnitSelect, correctData);
            console.log('✅ Measuring Unit Type dropdown fixed');
            return true;
        } else {
            console.warn('⚠️ Measuring Unit Type dropdown not found');
            return false;
        }
    }

    /**
     * Force fix all dropdown data based on their IDs
     */
    forceFixAllDropdowns() {
        console.log('🔧 Force fixing all dropdowns...');
        let fixedCount = 0;
        
        const allSelects = document.querySelectorAll('select');
        allSelects.forEach(select => {
            const selectId = select.id || select.name;
            const correctData = this.getCorrectDataForDropdown(selectId);
            
            if (correctData) {
                this.updateDropdownData(select, correctData);
                fixedCount++;
                console.log(`✅ Fixed dropdown: ${selectId}`);
            }
        });
        
        console.log(`🔧 Fixed ${fixedCount} dropdowns`);
        return fixedCount;
    }

    /**
     * Debug method to show all dropdown data
     */
    debugAllDropdowns() {
        console.log('=== DEBUGGING ALL DROPDOWNS ===');
        
        const allSelects = document.querySelectorAll('select');
        console.log(`Total select elements found: ${allSelects.length}`);
        
        allSelects.forEach((select, index) => {
            const selectId = select.id || select.name || `dropdown_${index}`;
            const isConverted = this.convertedDropdowns.has(selectId);
            const isHidden = select.style.display === 'none';
            const currentValue = select.value;
            
            console.log(`\n--- Select ${index}: ${selectId} ---`);
            console.log(`Converted: ${isConverted ? 'YES' : 'NO'}`);
            console.log(`Hidden: ${isHidden ? 'YES' : 'NO'}`);
            console.log(`Current Value: ${currentValue}`);
            console.log(`Options Count: ${select.options.length}`);
            
            // Show first 5 options
            console.log('First 5 options:');
            for (let i = 0; i < Math.min(5, select.options.length); i++) {
                const option = select.options[i];
                console.log(`  ${i}: ${option.value} - ${option.text} ${option.selected ? '(SELECTED)' : ''}`);
            }
            
            // If converted, show searchable dropdown info
            if (isConverted) {
                const converted = this.convertedDropdowns.get(selectId);
                if (converted && converted.searchable) {
                    console.log('Searchable dropdown info:');
                    console.log(`  Current Value: ${converted.searchable.getValue()}`);
                    console.log(`  Current Text: ${converted.searchable.getText()}`);
                    console.log(`  Is Open: ${converted.searchable.isOpen}`);
                }
            }
        });
        
        console.log('=== END DEBUG ===');
    }

    /**
     * Test method to show conversion status
     */
    testConversion() {
        console.log('Universal Dropdown Service Test:');
        console.log('SearchableDropdown class available:', typeof SearchableDropdown !== 'undefined');
        console.log('Converted dropdowns:', this.convertedDropdowns.size);
        console.log('Converted dropdowns details:', Array.from(this.convertedDropdowns.keys()));
        
        // Show all select elements
        const allSelects = document.querySelectorAll('select');
        console.log('Total select elements found:', allSelects.length);
        
        allSelects.forEach((select, index) => {
            const isConverted = this.convertedDropdowns.has(select.id || select.name || `dropdown_${index}`);
            const isHidden = select.style.display === 'none';
            const currentValue = select.value;
            console.log(`Select ${index}: ${select.id || select.name || 'unnamed'} - ${isConverted ? 'CONVERTED' : 'NOT CONVERTED'} - ${isHidden ? 'HIDDEN' : 'VISIBLE'} - Value: ${currentValue}`);
        });
        
        // Check for duplicates
        const duplicateCount = this.cleanupDuplicateDropdowns();
        if (duplicateCount > 0) {
            console.log(`Cleaned up ${duplicateCount} duplicate dropdowns`);
        }
        
        // Sync values
        const syncedCount = this.syncAllValues();
        if (syncedCount > 0) {
            console.log(`Synced ${syncedCount} dropdown values`);
        }
    }
}

// Initialize the universal service
$(document).ready(() => {
    setTimeout(() => {
        window.universalDropdownService = new UniversalDropdownService();
        
        // Make test functions available globally
        window.testUniversalDropdowns = () => {
            if (window.universalDropdownService) {
                window.universalDropdownService.testConversion();
            } else {
                console.error('Universal dropdown service not available');
            }
        };

        window.revertAllDropdowns = () => {
            if (window.universalDropdownService) {
                window.universalDropdownService.revertAllDropdowns();
            } else {
                console.error('Universal dropdown service not available');
            }
        };

        window.cleanupDuplicateDropdowns = () => {
            if (window.universalDropdownService) {
                const count = window.universalDropdownService.cleanupDuplicateDropdowns();
                console.log(`Cleaned up ${count} duplicate dropdowns`);
                return count;
            } else {
                console.error('Universal dropdown service not available');
                return 0;
            }
        };

        window.syncDropdownValues = () => {
            if (window.universalDropdownService) {
                const count = window.universalDropdownService.syncAllValues();
                console.log(`Synced ${count} dropdown values`);
                return count;
            } else {
                console.error('Universal dropdown service not available');
                return 0;
            }
        };

        window.debugAllDropdowns = () => {
            if (window.universalDropdownService) {
                window.universalDropdownService.debugAllDropdowns();
            } else {
                console.error('Universal dropdown service not available');
            }
        };

        window.checkDataIntegrity = () => {
            if (window.universalDropdownService) {
                window.universalDropdownService.checkDataIntegrity();
            } else {
                console.error('Universal dropdown service not available');
            }
        };

        window.forceFixMeasuringUnitDropdown = () => {
            if (window.universalDropdownService) {
                return window.universalDropdownService.forceFixMeasuringUnitDropdown();
            } else {
                console.error('Universal dropdown service not available');
                return false;
            }
        };

        window.forceFixAllDropdowns = () => {
            if (window.universalDropdownService) {
                return window.universalDropdownService.forceFixAllDropdowns();
            } else {
                console.error('Universal dropdown service not available');
                return 0;
            }
        };

        window.forceCleanupSalesFormDropdowns = () => {
            if (window.universalDropdownService) {
                return window.universalDropdownService.forceCleanupSalesFormDropdowns();
            } else {
                console.error('Universal dropdown service not available');
                return false;
            }
        };

        window.forceCleanupPaginationDropdowns = () => {
            if (window.universalDropdownService) {
                return window.universalDropdownService.forceCleanupPaginationDropdowns();
            } else {
                console.error('Universal dropdown service not available');
                return 0;
            }
        };
        
        console.log('Universal Dropdown Service initialized');
        console.log('Available test functions:');
        console.log('- testUniversalDropdowns() - Show conversion status');
        console.log('- revertAllDropdowns() - Revert all dropdowns to original');
        console.log('- cleanupDuplicateDropdowns() - Clean up duplicate dropdowns');
        console.log('- syncDropdownValues() - Sync all dropdown values with original selects');
        console.log('- debugAllDropdowns() - Debug all dropdown data and conversion status');
        console.log('- checkDataIntegrity() - Check for data mixing issues');
        console.log('- forceFixMeasuringUnitDropdown() - Force fix Measuring Unit Type dropdown');
        console.log('- forceFixAllDropdowns() - Force fix all dropdowns with correct data');
        console.log('- forceCleanupSalesFormDropdowns() - Force cleanup sales form duplicate dropdowns');
        console.log('- forceCleanupPaginationDropdowns() - Force cleanup pagination duplicate dropdowns');
    }, 2500); // Wait longer to ensure all other scripts are loaded
});
