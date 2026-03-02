/**
 * Common loader for long-running operations (e.g. Export Excel/PDF).
 * Use ShowExportLoader() before starting, HideExportLoader() when done.
 * Use exportWithLoader(url, suggestedFilename, message) to fetch a file with loader.
 */
(function () {
    'use strict';

    var OVERLAY_ID = 'ims-export-loader-overlay';

    function escapeHtml(text) {
        var div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    /**
     * Show full-page loader with optional message.
     * @param {string} [message='Preparing export... Please wait.'] - Text shown below the spinner.
     */
    window.ShowExportLoader = function (message) {
        if (document.getElementById(OVERLAY_ID)) return;
        var msg = message || 'Preparing export... Please wait.';
        var overlay = document.createElement('div');
        overlay.id = OVERLAY_ID;
        overlay.className = 'ims-export-loader-overlay';
        overlay.setAttribute('aria-busy', 'true');
        overlay.setAttribute('aria-live', 'polite');
        overlay.innerHTML =
            '<div class="ims-export-loader-box">' +
            '  <div class="ims-export-loader-spinner-wrap">' +
            '    <div class="ims-export-loader-spinner"></div>' +
            '    <div class="ims-export-loader-icon"><i class="fa-solid fa-file-export"></i></div>' +
            '  </div>' +
            '  <p class="ims-export-loader-message">' + escapeHtml(msg) + '</p>' +
            '  <p class="ims-export-loader-sub">This may take a moment for large data</p>' +
            '</div>';
        document.body.appendChild(overlay);
    };

    /**
     * Hide the export loader.
     */
    window.HideExportLoader = function () {
        var el = document.getElementById(OVERLAY_ID);
        if (el && el.parentNode) el.parentNode.removeChild(el);
    };

    /**
     * Fetch a file from url, show loader during request, then trigger download.
     * Hides loader when done (success or error).
     * @param {string} url - Full URL to the export action (e.g. /Sales/ExportExcel?...).
     * @param {string} suggestedFilename - Default filename if server does not send Content-Disposition.
     * @param {string} [message] - Loader message.
     * @returns {Promise<void>}
     */
    window.exportWithLoader = function (url, suggestedFilename, message) {
        ShowExportLoader(message || 'Preparing export... Please wait.');
        return fetch(url, { credentials: 'same-origin' })
            .then(function (response) {
                if (!response.ok) throw new Error('Export failed: ' + (response.statusText || response.status));
                return response.blob().then(function (blob) { return { response: response, blob: blob }; });
            })
            .then(function (result) {
                var filename = suggestedFilename || 'download';
                try {
                    var disposition = result.response.headers.get('Content-Disposition');
                    if (disposition && disposition.indexOf('filename=') !== -1) {
                        var match = disposition.match(/filename[*]?=(?:UTF-8'')?([^;\n]+)/i);
                        if (match && match[1]) filename = match[1].replace(/^["']|["']$/g, '').trim();
                    }
                } catch (e) {}
                var a = document.createElement('a');
                var blobUrl = window.URL.createObjectURL(result.blob);
                a.href = blobUrl;
                a.download = filename;
                a.style.display = 'none';
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                window.URL.revokeObjectURL(blobUrl);
            })
            .catch(function (err) {
                if (typeof console !== 'undefined' && console.error) console.error(err);
                if (typeof toastr !== 'undefined') {
                    toastr.error(err.message || 'Export failed. Please try again.');
                } else {
                    alert(err.message || 'Export failed. Please try again.');
                }
            })
            .then(function () {
                HideExportLoader();
            });
    };
})();
