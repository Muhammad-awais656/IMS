/**
 * Enhanced Form Validation with Toast Notifications
 * Provides consistent validation across all settings forms
 */

$(document).ready(function() {
    // Initialize form validation for all forms with validation classes
    initializeFormValidation();
});

function initializeFormValidation() {
    // Enhanced form validation with toast notifications
    $('form[id$="Form"]').on('submit', function(e) {
        if (!this.checkValidity()) {
            e.preventDefault();
            e.stopPropagation();
            
            // Show validation errors
            var firstError = $(this).find('.is-invalid').first();
            if (firstError.length) {
                firstError.focus();
                showToast('Please fill in all required fields correctly.', 'error');
            } else {
                showToast('Please check the form for errors.', 'error');
            }
        }
        $(this).addClass('was-validated');
    });

    // Real-time validation
    $('input[required], textarea[required], select[required]').on('blur', function() {
        if (!this.checkValidity()) {
            $(this).addClass('is-invalid');
            showToast('This field is required.', 'error');
        } else {
            $(this).removeClass('is-invalid').addClass('is-valid');
        }
    });

    // Clear validation on input
    $('input, textarea, select').on('input change', function() {
        $(this).removeClass('is-invalid is-valid');
    });

    // Enhanced validation for specific field types
    $('input[type="email"]').on('blur', function() {
        var email = $(this).val();
        if (email && !isValidEmail(email)) {
            $(this).addClass('is-invalid');
            showToast('Please enter a valid email address.', 'error');
        }
    });

    $('input[maxlength]').on('input', function() {
        var maxLength = parseInt($(this).attr('maxlength'));
        var currentLength = $(this).val().length;
        if (currentLength > maxLength) {
            $(this).addClass('is-invalid');
            showToast(`Maximum length is ${maxLength} characters.`, 'error');
        }
    });
}

function showToast(message, type) {
    // Ensure toast container exists
    if ($('.toast-container').length === 0) {
        $('body').append('<div class="toast-container position-fixed top-0 end-0 p-3" style="z-index: 9999;"></div>');
    }
    
    // Create toast element
    var toastHtml = `
        <div class="toast align-items-center text-white bg-${type === 'error' ? 'danger' : 'success'} border-0 show" 
             role="alert" aria-live="assertive" aria-atomic="true" data-bs-delay="4000">
            <div class="d-flex">
                <div class="toast-body">
                    <i class="fa fa-${type === 'error' ? 'exclamation-triangle' : 'check-circle'} me-2"></i>${message}
                </div>
                <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
            </div>
        </div>
    `;
    
    // Add to toast container
    $('.toast-container').append(toastHtml);
    
    // Initialize Bootstrap toast
    var toastElement = $('.toast-container .toast').last()[0];
    var toast = new bootstrap.Toast(toastElement);
    toast.show();
    
    // Auto remove after 4 seconds
    setTimeout(function() {
        $(toastElement).remove();
    }, 4000);
}

function isValidEmail(email) {
    var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}

function showSuccessToast(message) {
    showToast(message, 'success');
}

function showErrorToast(message) {
    showToast(message, 'error');
}

// Export functions for global use
window.showToast = showToast;
window.showSuccessToast = showSuccessToast;
window.showErrorToast = showErrorToast;
