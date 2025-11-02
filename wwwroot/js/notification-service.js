/**
 * Global Notification Service - Provides toast notifications for success, error, warning, and info messages
 */
class NotificationService {
    constructor() {
        this.toastContainer = null;
        this.init();
    }

    /**
     * Initialize the notification service
     */
    init() {
        // Wait for DOM to be ready
        $(document).ready(() => {
            this.createToastContainer();
            this.initializeBootstrapToasts();
        });
    }

    /**
     * Create toast container if it doesn't exist
     */
    createToastContainer() {
        // Check if toast container already exists
        this.toastContainer = document.querySelector('.toast-container');
        
        if (!this.toastContainer) {
            // Create toast container
            const container = document.createElement('div');
            container.className = 'toast-container position-fixed top-0 end-0 p-3';
            container.style.zIndex = '9999';
            
            // Add to main content area - try multiple fallbacks
            let target = document.querySelector('#main');
            if (!target) {
                target = document.body;
            }
            if (!target) {
                target = document.documentElement;
            }
            
            if (target) {
                target.appendChild(container);
                this.toastContainer = container;
            } else {
                console.error('Cannot create toast container: no suitable target element found');
            }
        }
    }

    /**
     * Initialize Bootstrap toasts
     */
    initializeBootstrapToasts() {
        // Initialize existing toasts
        const toastElList = [].slice.call(document.querySelectorAll('.toast'));
        toastElList.map(function (toastEl) {
            const toast = new bootstrap.Toast(toastEl);
            toast.show();
        });
    }

    /**
     * Show success message
     * @param {string} message - Success message
     * @param {number} duration - Duration in milliseconds (default: 4000)
     */
    showSuccessMessage(message, duration = 4000) {
        this.showToast(message, 'success', duration, 'fa-check-circle');
    }

    /**
     * Show error message
     * @param {string} message - Error message
     * @param {number} duration - Duration in milliseconds (default: 5000)
     */
    showErrorMessage(message, duration = 5000) {
        this.showToast(message, 'danger', duration, 'fa-exclamation-triangle');
    }

    /**
     * Show warning message
     * @param {string} message - Warning message
     * @param {number} duration - Duration in milliseconds (default: 4000)
     */
    showWarningMessage(message, duration = 4000) {
        this.showToast(message, 'warning', duration, 'fa-exclamation-triangle');
    }

    /**
     * Show info message
     * @param {string} message - Info message
     * @param {number} duration - Duration in milliseconds (default: 4000)
     */
    showInfoMessage(message, duration = 4000) {
        this.showToast(message, 'info', duration, 'fa-info-circle');
    }

    /**
     * Show toast notification
     * @param {string} message - Message to display
     * @param {string} type - Toast type (success, danger, warning, info)
     * @param {number} duration - Duration in milliseconds
     * @param {string} icon - FontAwesome icon class
     */
    showToast(message, type, duration, icon) {
        // Ensure toast container exists before proceeding
        if (!this.toastContainer) {
            this.createToastContainer();
        }
        
        // If container still doesn't exist, try to find or create it again
        if (!this.toastContainer) {
            this.toastContainer = document.querySelector('.toast-container');
            if (!this.toastContainer) {
                const container = document.createElement('div');
                container.className = 'toast-container position-fixed top-0 end-0 p-3';
                container.style.zIndex = '9999';
                
                // Try to append to body or create as fallback
                const target = document.body || document.documentElement;
                if (target) {
                    target.appendChild(container);
                    this.toastContainer = container;
                } else {
                    console.error('Cannot create toast container: document.body is not available');
                    return null;
                }
            }
        }
        
        // Create unique ID for the toast
        const toastId = 'toast_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
        
        // Create toast element
        const toastElement = document.createElement('div');
        toastElement.id = toastId;
        toastElement.className = `toast align-items-center text-white bg-${type} border-0`;
        toastElement.setAttribute('role', 'alert');
        toastElement.setAttribute('aria-live', 'assertive');
        toastElement.setAttribute('aria-atomic', 'true');
        toastElement.setAttribute('data-bs-delay', duration.toString());
        toastElement.setAttribute('data-bs-autohide', 'true');
        
        toastElement.innerHTML = `
            <div class="d-flex">
                <div class="toast-body">
                    <i class="fa-solid ${icon} me-2"></i>
                    ${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" 
                        data-bs-dismiss="toast" aria-label="Close"></button>
            </div>
        `;
        
        // Add to container (now guaranteed to exist)
        if (this.toastContainer) {
            this.toastContainer.appendChild(toastElement);
            
            // Initialize and show toast
            const toast = new bootstrap.Toast(toastElement);
            toast.show();
            
            // Auto-remove from DOM after toast is hidden
            toastElement.addEventListener('hidden.bs.toast', () => {
                if (toastElement.parentNode) {
                    toastElement.remove();
                }
            });
            
            return toastId;
        }
        
        return null;
    }

    /**
     * Clear all toasts
     */
    clearAllToasts() {
        if (!this.toastContainer) {
            return;
        }
        
        const toasts = this.toastContainer.querySelectorAll('.toast');
        toasts.forEach(toast => {
            const bsToast = bootstrap.Toast.getInstance(toast);
            if (bsToast) {
                bsToast.hide();
            }
        });
    }

    /**
     * Clear specific toast by ID
     * @param {string} toastId - Toast ID to clear
     */
    clearToast(toastId) {
        const toast = document.getElementById(toastId);
        if (toast) {
            const bsToast = bootstrap.Toast.getInstance(toast);
            if (bsToast) {
                bsToast.hide();
            }
        }
    }

    /**
     * Show loading message
     * @param {string} message - Loading message
     */
    showLoadingMessage(message = 'Loading...') {
        return this.showInfoMessage(message, 0); // 0 duration means it won't auto-hide
    }

    /**
     * Hide loading message
     * @param {string} toastId - Toast ID to hide
     */
    hideLoadingMessage(toastId) {
        this.clearToast(toastId);
    }
}

// Initialize the notification service
$(document).ready(() => {
    window.notificationService = new NotificationService();
    
    // Make functions available globally
    window.showSuccessMessage = (message, duration) => window.notificationService.showSuccessMessage(message, duration);
    window.showErrorMessage = (message, duration) => window.notificationService.showErrorMessage(message, duration);
    window.showWarningMessage = (message, duration) => window.notificationService.showWarningMessage(message, duration);
    window.showInfoMessage = (message, duration) => window.notificationService.showInfoMessage(message, duration);
    window.showLoadingMessage = (message) => window.notificationService.showLoadingMessage(message);
    window.hideLoadingMessage = (toastId) => window.notificationService.hideLoadingMessage(toastId);
    window.clearAllToasts = () => window.notificationService.clearAllToasts();
    
    console.log('Notification Service initialized');
    console.log('Available functions:');
    console.log('- showSuccessMessage(message, duration) - Show success toast');
    console.log('- showErrorMessage(message, duration) - Show error toast');
    console.log('- showWarningMessage(message, duration) - Show warning toast');
    console.log('- showInfoMessage(message, duration) - Show info toast');
    console.log('- showLoadingMessage(message) - Show loading toast');
    console.log('- hideLoadingMessage(toastId) - Hide loading toast');
    console.log('- clearAllToasts() - Clear all toasts');
});
