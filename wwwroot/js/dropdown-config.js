/**
 * Dropdown Configuration - Configure which dropdowns to exclude from conversion
 */
window.DropdownConfig = {
    // Selectors to exclude from conversion
    excludeSelectors: [
        '#productDropdown', // Dashboard dropdown (handled by dashboard service)
        '#searchableProductDropdown', // Already converted dropdowns
        '#productSelect', // Sales form product dropdown (handled by sales service)
        '#searchableProductSelect', // Sales form dropdown (handled by sales service)
        '#searchableProductSelectSales', // Sales form dropdown (handled by sales service)
        '#searchableProductSelectGenerateBills', // Generate Bills dropdown (handled by generate bills service)
        '#pageSize', // Pagination dropdown (Items Per Page) - has onchange functionality
        'select[onchange*="changePageSize"]', // All pagination dropdowns with changePageSize function
        'select[multiple]', // Multi-select dropdowns
        'select[data-no-convert]', // Explicitly excluded dropdowns
        'select[data-keep-original]', // Keep original dropdowns
        // Add more selectors here as needed
    ],

    // Classes to exclude
    excludeClasses: [
        'no-convert',
        'keep-original'
    ],

    // Attributes to exclude
    excludeAttributes: [
        'data-no-convert',
        'data-keep-original'
    ],

    // Custom placeholder text for specific dropdowns
    customPlaceholders: {
        // Example: '#customerSelect': 'Select Customer...',
        // Example: '#categorySelect': 'Choose Category...',
    },

    // Custom search placeholders for specific dropdowns
    customSearchPlaceholders: {
        // Example: '#customerSelect': 'Type customer name...',
        // Example: '#categorySelect': 'Search categories...',
    },

    // Dropdowns that should have different no-results text
    customNoResultsText: {
        // Example: '#customerSelect': 'No customers found',
        // Example: '#categorySelect': 'No categories found',
    }
};
