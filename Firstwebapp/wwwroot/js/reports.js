// Reports Dashboard JavaScript
// Handles tab switching, CSV export, and filter reset

/**
 * Switch between report tabs
 * @param {string} tab - Tab name (jobs, contractors, workers, assignments)
 */
function switchTab(tab) {
    const tabInput = document.getElementById('tabInput');
    const tradeFilter = document.getElementById('tradeFilterContainer');

    if (tabInput) {
        tabInput.value = tab;
    }

    // Show/hide trade filter based on active tab
    if (tradeFilter) {
        tradeFilter.style.display = tab === 'workers' ? 'block' : 'none';
    }

    // Submit the form to reload with new tab
    const filterForm = document.getElementById('filterForm');
    if (filterForm) {
        filterForm.submit();
    }
}

/**
 * Export current filtered data to CSV file
 * @param {Event} event - Click event (optional)
 */
async function exportCSV(event) {
    const exportBtn = event?.target?.closest('button') || document.querySelector('.btn-success');
    const originalText = exportBtn?.innerHTML;

    // Show loading state
    if (exportBtn) {
        exportBtn.disabled = true;
        exportBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i>Exporting...';
    }

    try {
        const form = document.getElementById('filterForm');
        if (!form) {
            throw new Error('Filter form not found');
        }

        // Get anti-forgery token
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        if (!token) {
            console.warn('Anti-forgery token not found');
        }

        // Build query parameters from form
        const formData = new FormData(form);
        const params = new URLSearchParams();

        // Add all form fields to params
        for (const [key, value] of formData.entries()) {
            if (value && value !== '') {
                params.append(key, value);
            }
        }

        const response = await fetch(`/Report/ExportCSV?${params.toString()}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': token || ''
            }
        });

        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`Export failed: ${response.status} ${errorText}`);
        }

        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        const tabInput = document.getElementById('tabInput');
        const tabName = tabInput?.value || 'report';
        const dateStr = new Date().toISOString().split('T')[0];

        a.href = url;
        a.download = `${tabName}_report_${dateStr}.csv`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);

    } catch (error) {
        console.error('Export error:', error);
        alert(`Failed to export report: ${error.message}`);
    } finally {
        // Restore button state
        if (exportBtn) {
            exportBtn.disabled = false;
            exportBtn.innerHTML = originalText;
        }
    }
}

/**
 * Reset all filters to default values
 */
function resetFilters() {
    const form = document.getElementById('filterForm');
    if (!form) return;

    // Reset all select elements
    form.querySelectorAll('select').forEach(select => {
        select.selectedIndex = 0;
    });

    // Reset all date inputs
    form.querySelectorAll('input[type="date"]').forEach(input => {
        input.value = '';
    });

    // Reset tab to jobs (default)
    const tabInput = document.getElementById('tabInput');
    if (tabInput) {
        tabInput.value = 'jobs';
    }

    // Show trade filter (jobs tab hides it, so reset to default)
    const tradeFilter = document.getElementById('tradeFilterContainer');
    if (tradeFilter) {
        tradeFilter.style.display = 'none';
    }

    // Submit the form
    form.submit();
}

/**
 * Initialize trade filter visibility on page load
 */
document.addEventListener('DOMContentLoaded', function() {
    const tabInput = document.getElementById('tabInput');
    const currentTab = tabInput?.value || 'jobs';
    const tradeFilter = document.getElementById('tradeFilterContainer');

    if (tradeFilter) {
        tradeFilter.style.display = currentTab === 'workers' ? 'block' : 'none';
    }

    // Add keyboard support for buttons
    const exportBtn = document.querySelector('.btn-success');
    const resetBtn = document.querySelector('.btn-secondary');

    if (exportBtn) {
        exportBtn.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                exportCSV(e);
            }
        });
    }

    if (resetBtn) {
        resetBtn.addEventListener('keydown', (e) => {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                resetFilters();
            }
        });
    }
});