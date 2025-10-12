/**
 * Error Handler - Handles 404 and other errors gracefully
 */
class ErrorHandler {
    constructor() {
        this.init();
    }

    /**
     * Initialize error handling
     */
    init() {
        // Handle 404 errors
        this.handle404Errors();
        
        // Handle navigation validation
        this.validateNavigation();
        
        // Handle AJAX errors
        this.handleAjaxErrors();
    }

    /**
     * Handle 404 errors
     */
    handle404Errors() {
        // Check if current page is a 404 error
        if (window.location.href.includes('chrome-error://chromewebdata/')) {
            this.showErrorPage();
        }

        // Listen for navigation errors
        window.addEventListener('error', (event) => {
            if (event.message.includes('404') || event.message.includes('Not Found')) {
                this.showErrorNotification('The requested page was not found.');
            }
        });
    }

    /**
     * Validate navigation links
     */
    validateNavigation() {
        // Add click handlers to all edit/delete links
        document.addEventListener('click', (event) => {
            const link = event.target.closest('a[href*="/Edit/"], a[href*="/Delete/"]');
            if (link) {
                const href = link.getAttribute('href');
                if (href && href.includes('/Edit/') || href.includes('/Delete/')) {
                    // Extract ID from URL
                    const idMatch = href.match(/\/(\d+)$/);
                    if (idMatch) {
                        const id = parseInt(idMatch[1]);
                        if (isNaN(id) || id <= 0) {
                            event.preventDefault();
                            this.showErrorNotification('Invalid record ID.');
                            return false;
                        }
                    }
                }
            }
        });
    }

    /**
     * Handle AJAX errors
     */
    handleAjaxErrors() {
        // Override fetch to handle errors
        const originalFetch = window.fetch;
        window.fetch = async (...args) => {
            try {
                const response = await originalFetch(...args);
                if (!response.ok) {
                    if (response.status === 404) {
                        this.showErrorNotification('The requested resource was not found.');
                    } else if (response.status === 500) {
                        this.showErrorNotification('A server error occurred. Please try again.');
                    }
                }
                return response;
            } catch (error) {
                this.showErrorNotification('Network error occurred. Please check your connection.');
                throw error;
            }
        };

        // Handle jQuery AJAX errors
        if (typeof $ !== 'undefined') {
            $(document).ajaxError((event, xhr, settings, thrownError) => {
                if (xhr.status === 404) {
                    this.showErrorNotification('The requested resource was not found.');
                } else if (xhr.status === 500) {
                    this.showErrorNotification('A server error occurred. Please try again.');
                } else if (xhr.status === 0) {
                    this.showErrorNotification('Network error occurred. Please check your connection.');
                }
            });
        }
    }

    /**
     * Show error page
     */
    showErrorPage() {
        const errorHtml = `
            <div class="container mt-5">
                <div class="row justify-content-center">
                    <div class="col-md-6">
                        <div class="card shadow">
                            <div class="card-body text-center">
                                <div class="mb-4">
                                    <i class="fas fa-exclamation-triangle fa-5x text-warning"></i>
                                </div>
                                <h2 class="card-title">Page Not Found</h2>
                                <p class="card-text">
                                    The page you're looking for doesn't exist or has been moved.
                                </p>
                                <div class="mt-4">
                                    <a href="/" class="btn btn-primary me-2">Go Home</a>
                                    <button onclick="history.back()" class="btn btn-secondary">Go Back</button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        `;
        
        document.body.innerHTML = errorHtml;
    }

    /**
     * Show error notification
     * @param {string} message - Error message
     */
    showErrorNotification(message) {
        // Create notification element
        const notification = document.createElement('div');
        notification.className = 'alert alert-danger alert-dismissible fade show position-fixed';
        notification.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
        notification.innerHTML = `
            <strong>Error:</strong> ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;
        
        // Add to page
        document.body.appendChild(notification);
        
        // Auto-remove after 5 seconds
        setTimeout(() => {
            if (notification.parentNode) {
                notification.remove();
            }
        }, 5000);
    }

    /**
     * Validate record ID
     * @param {string|number} id - Record ID to validate
     * @returns {boolean} True if valid
     */
    validateRecordId(id) {
        const numId = parseInt(id);
        return !isNaN(numId) && numId > 0;
    }

    /**
     * Safe navigation to edit/delete pages
     * @param {string} action - Action (Edit/Delete)
     * @param {string|number} id - Record ID
     * @param {string} controller - Controller name
     */
    safeNavigate(action, id, controller) {
        if (!this.validateRecordId(id)) {
            this.showErrorNotification('Invalid record ID.');
            return false;
        }

        const url = `/${controller}/${action}/${id}`;
        window.location.href = url;
        return true;
    }

    /**
     * Check if URL is valid
     * @param {string} url - URL to check
     * @returns {boolean} True if valid
     */
    isValidUrl(url) {
        try {
            new URL(url);
            return true;
        } catch {
            return false;
        }
    }
}

// Initialize error handler
$(document).ready(() => {
    window.errorHandler = new ErrorHandler();
    
    // Make functions available globally
    window.showErrorNotification = (message) => window.errorHandler.showErrorNotification(message);
    window.validateRecordId = (id) => window.errorHandler.validateRecordId(id);
    window.safeNavigate = (action, id, controller) => window.errorHandler.safeNavigate(action, id, controller);
    
    console.log('Error Handler initialized');
    console.log('Available functions:');
    console.log('- showErrorNotification(message) - Show error notification');
    console.log('- validateRecordId(id) - Validate record ID');
    console.log('- safeNavigate(action, id, controller) - Safe navigation to edit/delete pages');
});


