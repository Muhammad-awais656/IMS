/**
 * Searchable Dropdown Component - Similar to Kendo UI DropDownList
 */
class SearchableDropdown {
    constructor(containerId, options = {}) {
        this.container = document.getElementById(containerId);
        this.options = {
            placeholder: options.placeholder || 'Select an option...',
            searchPlaceholder: options.searchPlaceholder || 'Type to search...',
            noResultsText: options.noResultsText || 'No results found',
            dataSource: options.dataSource || [],
            valueField: options.valueField || 'value',
            textField: options.textField || 'text',
            onSelect: options.onSelect || null,
            onSearch: options.onSearch || null,
            ...options
        };
        
        this.isOpen = false;
        this.selectedItem = null;
        this.filteredData = [...this.options.dataSource];
        this.isToggling = false; // Prevent rapid toggles
        
        this.init();
    }

    /**
     * Initialize the dropdown
     */
    init() {
        this.createHTML();
        this.bindEvents();
        this.render();
    }

    /**
     * Create the HTML structure
     */
    createHTML() {
        this.container.innerHTML = `
            <div class="searchable-dropdown">
                <div class="dropdown-display" tabindex="0">
                    <span class="selected-text">${this.options.placeholder}</span>
                    <span class="dropdown-arrow">▼</span>
                </div>
                <div class="dropdown-menu">
                    <div class="search-container">
                        <input type="text" class="search-input" placeholder="${this.options.searchPlaceholder}">
                    </div>
                    <div class="dropdown-list">
                        ${this.renderOptions()}
                    </div>
                </div>
            </div>
        `;
    }

    /**
     * Render the options list
     */
    renderOptions() {
        if (this.filteredData.length === 0) {
            return `<div class="dropdown-item no-results">${this.options.noResultsText}</div>`;
        }

        return this.filteredData.map(item => {
            const isSelected = this.selectedItem && 
                this.selectedItem[this.options.valueField] === item[this.options.valueField];
            
            return `
                <div class="dropdown-item ${isSelected ? 'selected' : ''}" 
                     data-value="${item[this.options.valueField]}"
                     data-text="${item[this.options.textField]}">
                    ${item[this.options.textField]}
                </div>
            `;
        }).join('');
    }

    /**
     * Bind event listeners
     */
    bindEvents() {
        const display = this.container.querySelector('.dropdown-display');
        const searchInput = this.container.querySelector('.search-input');
        const dropdownList = this.container.querySelector('.dropdown-list');

        console.log('Binding events to dropdown elements:', { display, searchInput, dropdownList });

        // Toggle dropdown
        display.addEventListener('click', (e) => {
            console.log('Dropdown display clicked, current state:', this.isOpen);
            e.preventDefault();
            e.stopPropagation();
            this.toggle();
        });

        // Handle keyboard navigation
        display.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                this.toggle();
            } else if (e.key === 'Escape') {
                this.close();
            }
        });

        // Search functionality
        searchInput.addEventListener('input', (e) => {
            this.search(e.target.value);
        });

        // Handle search input keyboard events
        searchInput.addEventListener('keydown', (e) => {
            if (e.key === 'Escape') {
                this.close();
            } else if (e.key === 'ArrowDown') {
                e.preventDefault();
                this.focusNextItem();
            } else if (e.key === 'ArrowUp') {
                e.preventDefault();
                this.focusPreviousItem();
            } else if (e.key === 'Enter') {
                e.preventDefault();
                this.selectFocusedItem();
            }
        });

        // Handle option selection
        dropdownList.addEventListener('click', (e) => {
            const item = e.target.closest('.dropdown-item');
            if (item && !item.classList.contains('no-results')) {
                this.selectItem(item);
            }
        });

        // Close dropdown when clicking outside - use capture phase
        document.addEventListener('click', (e) => {
            console.log('Document clicked, target:', e.target);
            console.log('Container contains target:', this.container.contains(e.target));
            console.log('Dropdown is open:', this.isOpen);
            
            if (this.isOpen && !this.container.contains(e.target)) {
                console.log('Clicking outside, closing dropdown');
                this.close();
            } else if (this.container.contains(e.target)) {
                console.log('Clicking inside dropdown, keeping open');
            }
        }, true); // Use capture phase
    }

    /**
     * Toggle dropdown open/close
     */
    toggle() {
        if (this.isToggling) {
            console.log('Toggle already in progress, ignoring');
            return;
        }
        
        this.isToggling = true;
        console.log('Toggle called, current state:', this.isOpen);
        
        if (this.isOpen) {
            this.close();
        } else {
            this.open();
        }
        
        // Reset toggle flag after a short delay
        setTimeout(() => {
            this.isToggling = false;
        }, 100);
    }

    /**
     * Open dropdown
     */
    open() {
        console.log('Opening dropdown...');
        this.isOpen = true;
        this.container.classList.add('open');
        
        // Debug dropdown menu visibility
        const dropdownMenu = this.container.querySelector('.dropdown-menu');
        console.log('Dropdown menu element:', dropdownMenu);
        if (dropdownMenu) {
            console.log('Dropdown menu classes:', dropdownMenu.className);
            console.log('Dropdown menu style:', dropdownMenu.style.cssText);
            console.log('Dropdown menu computed style:', window.getComputedStyle(dropdownMenu));
            console.log('Dropdown menu display:', window.getComputedStyle(dropdownMenu).display);
            console.log('Dropdown menu visibility:', window.getComputedStyle(dropdownMenu).visibility);
            console.log('Dropdown menu position:', dropdownMenu.getBoundingClientRect());
        }
        
        const searchInput = this.container.querySelector('.search-input');
        console.log('Search input element:', searchInput);
        if (searchInput) {
            searchInput.focus();
        }
        
        // Smart positioning to prevent overflow
        this.adjustDropdownPosition();
        
        this.render();
        console.log('Dropdown opened, isOpen:', this.isOpen);
        
        // Force show dropdown menu if it's not visible
        if (dropdownMenu && window.getComputedStyle(dropdownMenu).display === 'none') {
            console.log('Forcing dropdown menu to be visible...');
            dropdownMenu.style.display = 'block';
            dropdownMenu.style.visibility = 'visible';
            dropdownMenu.style.opacity = '1';
        }
    }

    /**
     * Adjust dropdown position to prevent overflow
     */
    adjustDropdownPosition() {
        const dropdownMenu = this.container.querySelector('.dropdown-menu');
        if (!dropdownMenu) return;

        const containerRect = this.container.getBoundingClientRect();
        const viewportHeight = window.innerHeight;
        const viewportWidth = window.innerWidth;
        
        // Check if dropdown would go off-screen vertically
        const spaceBelow = viewportHeight - containerRect.bottom;
        const spaceAbove = containerRect.top;
        const estimatedMenuHeight = 300; // Max height from CSS
        
        // If not enough space below but enough space above, position above
        if (spaceBelow < estimatedMenuHeight && spaceAbove > estimatedMenuHeight) {
            dropdownMenu.classList.add('dropdown-menu-up');
        } else {
            dropdownMenu.classList.remove('dropdown-menu-up');
        }
        
        // Ensure dropdown doesn't go off-screen horizontally
        if (containerRect.left + 200 > viewportWidth) { // 200px is min-width from CSS
            dropdownMenu.style.left = 'auto';
            dropdownMenu.style.right = '0';
        } else {
            dropdownMenu.style.left = '0';
            dropdownMenu.style.right = 'auto';
        }
    }

    /**
     * Close dropdown
     */
    close() {
        console.log('Closing dropdown...');
        this.isOpen = false;
        this.container.classList.remove('open');
        
        // Force hide dropdown menu
        const dropdownMenu = this.container.querySelector('.dropdown-menu');
        if (dropdownMenu) {
            dropdownMenu.style.display = 'none';
            dropdownMenu.style.visibility = 'hidden';
            console.log('Forced dropdown menu to be hidden');
        }
        
        const searchInput = this.container.querySelector('.search-input');
        if (searchInput) {
            searchInput.value = '';
        }
        this.filteredData = [...this.options.dataSource];
        console.log('Dropdown closed, isOpen:', this.isOpen);
    }

    /**
     * Search functionality
     */
    search(query) {
        const searchTerm = query.toLowerCase().trim();
        
        if (searchTerm === '') {
            this.filteredData = [...this.options.dataSource];
        } else {
            this.filteredData = this.options.dataSource.filter(item => 
                item[this.options.textField].toLowerCase().includes(searchTerm)
            );
        }

        // Call custom search callback if provided
        if (this.options.onSearch) {
            this.options.onSearch(searchTerm, this.filteredData);
        }

        this.render();
    }

    /**
     * Select an item
     */
    selectItem(itemElement) {
        console.log('Selecting item:', itemElement);
        const value = itemElement.dataset.value;
        const text = itemElement.dataset.text;
        
        console.log('Selected value:', value, 'text:', text);
        
        this.selectedItem = {
            [this.options.valueField]: value,
            [this.options.textField]: text
        };

        // Update display
        const selectedTextElement = this.container.querySelector('.selected-text');
        if (selectedTextElement) {
            selectedTextElement.textContent = text;
            console.log('Updated display text to:', text);
        }
        
        // Close dropdown
        console.log('Closing dropdown after selection...');
        this.close();

        // Call selection callback
        if (this.options.onSelect) {
            console.log('Calling onSelect callback with:', this.selectedItem);
            this.options.onSelect(this.selectedItem);
        }
    }

    /**
     * Focus next item (keyboard navigation)
     */
    focusNextItem() {
        const items = this.container.querySelectorAll('.dropdown-item:not(.no-results)');
        const currentFocused = this.container.querySelector('.dropdown-item.focused');
        
        if (currentFocused) {
            currentFocused.classList.remove('focused');
            const currentIndex = Array.from(items).indexOf(currentFocused);
            const nextIndex = (currentIndex + 1) % items.length;
            items[nextIndex].classList.add('focused');
        } else if (items.length > 0) {
            items[0].classList.add('focused');
        }
    }

    /**
     * Focus previous item (keyboard navigation)
     */
    focusPreviousItem() {
        const items = this.container.querySelectorAll('.dropdown-item:not(.no-results)');
        const currentFocused = this.container.querySelector('.dropdown-item.focused');
        
        if (currentFocused) {
            currentFocused.classList.remove('focused');
            const currentIndex = Array.from(items).indexOf(currentFocused);
            const prevIndex = currentIndex === 0 ? items.length - 1 : currentIndex - 1;
            items[prevIndex].classList.add('focused');
        } else if (items.length > 0) {
            items[items.length - 1].classList.add('focused');
        }
    }

    /**
     * Select the currently focused item
     */
    selectFocusedItem() {
        const focusedItem = this.container.querySelector('.dropdown-item.focused');
        if (focusedItem) {
            this.selectItem(focusedItem);
        }
    }

    /**
     * Render the dropdown
     */
    render() {
        const dropdownList = this.container.querySelector('.dropdown-list');
        dropdownList.innerHTML = this.renderOptions();
    }

    /**
     * Set the data source
     */
    setDataSource(data) {
        this.options.dataSource = data;
        this.filteredData = [...data];
        this.render();
    }

    /**
     * Get the selected value
     */
    getValue() {
        return this.selectedItem ? this.selectedItem[this.options.valueField] : null;
    }

    /**
     * Get the selected text
     */
    getText() {
        return this.selectedItem ? this.selectedItem[this.options.textField] : null;
    }

    /**
     * Set the selected value
     */
    setValue(value) {
        const item = this.options.dataSource.find(item => 
            item[this.options.valueField] == value
        );
        
        if (item) {
            this.selectedItem = item;
            this.container.querySelector('.selected-text').textContent = item[this.options.textField];
        }
    }

    /**
     * Clear the selection
     */
    clear() {
        this.selectedItem = null;
        this.container.querySelector('.selected-text').textContent = this.options.placeholder;
    }

    /**
     * Force open dropdown (for testing)
     */
    forceOpen() {
        console.log('Force opening dropdown...');
        this.isOpen = false; // Reset state
        this.open();
    }

    /**
     * Force close dropdown (for testing)
     */
    forceClose() {
        console.log('Force closing dropdown...');
        this.close();
    }

    /**
     * Force show dropdown menu (for testing)
     */
    forceShowMenu() {
        console.log('Force showing dropdown menu...');
        const dropdownMenu = this.container.querySelector('.dropdown-menu');
        if (dropdownMenu) {
            dropdownMenu.style.display = 'block';
            dropdownMenu.style.visibility = 'visible';
            dropdownMenu.style.opacity = '1';
            dropdownMenu.style.position = 'absolute';
            dropdownMenu.style.top = '100%';
            dropdownMenu.style.left = '0';
            dropdownMenu.style.right = '0';
            dropdownMenu.style.zIndex = '9999';
            dropdownMenu.style.backgroundColor = '#fff';
            dropdownMenu.style.border = '1px solid #ddd';
            dropdownMenu.style.boxShadow = '0 4px 6px rgba(0, 0, 0, 0.1)';
            console.log('Dropdown menu forced to be visible');
        } else {
            console.error('Dropdown menu element not found');
        }
    }
}
