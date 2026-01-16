/**
 * Admin Dashboard - Interactive Features
 * Provides customization, real-time updates, and enhanced UX
 */

(function() {
    'use strict';

    // Dashboard Configuration
    const DashboardConfig = {
        refreshInterval: 300000, // 5 minutes
        animationDuration: 300,
        chartColors: {
            primary: '#2563eb',
            success: '#10b981',
            warning: '#f59e0b',
            danger: '#ef4444',
            info: '#06b6d4'
        }
    };

    // Dashboard Manager
    const Dashboard = {
        init: function() {
            this.setupEventListeners();
            this.loadUserPreferences();
            this.initializeWidgets();
            this.setupAutoRefresh();
        },

        setupEventListeners: function() {
            // Card minimize/maximize
            document.querySelectorAll('.card-minimize').forEach(btn => {
                btn.addEventListener('click', this.toggleCardMinimize);
            });

            // Export functionality
            document.querySelectorAll('[data-export]').forEach(btn => {
                btn.addEventListener('click', this.exportData);
            });

            // Filter controls
            document.querySelectorAll('[data-filter]').forEach(control => {
                control.addEventListener('change', this.applyFilters);
            });

            // Refresh button
            document.querySelectorAll('[data-refresh]').forEach(btn => {
                btn.addEventListener('click', this.refreshWidget);
            });
        },

        loadUserPreferences: function() {
            // Load saved layout preferences
            const savedLayout = localStorage.getItem('dashboardLayout');
            if (savedLayout) {
                try {
                    const layout = JSON.parse(savedLayout);
                    this.applyLayout(layout);
                } catch (e) {
                    console.error('Failed to load dashboard layout:', e);
                }
            }

            // Load theme preference
            const theme = localStorage.getItem('dashboardTheme');
            if (theme) {
                document.body.setAttribute('data-theme', theme);
            }
        },

        saveUserPreferences: function() {
            const layout = this.getCurrentLayout();
            localStorage.setItem('dashboardLayout', JSON.stringify(layout));
        },

        getCurrentLayout: function() {
            // Capture current dashboard layout state
            const widgets = [];
            document.querySelectorAll('[data-widget]').forEach(widget => {
                widgets.push({
                    id: widget.dataset.widget,
                    visible: !widget.classList.contains('d-none'),
                    minimized: widget.classList.contains('minimized'),
                    order: widget.style.order || 0
                });
            });
            return { widgets };
        },

        applyLayout: function(layout) {
            if (!layout || !layout.widgets) return;

            layout.widgets.forEach(widgetConfig => {
                const widget = document.querySelector(`[data-widget="${widgetConfig.id}"]`);
                if (!widget) return;

                if (!widgetConfig.visible) {
                    widget.classList.add('d-none');
                }
                if (widgetConfig.minimized) {
                    widget.classList.add('minimized');
                }
                if (widgetConfig.order) {
                    widget.style.order = widgetConfig.order;
                }
            });
        },

        toggleCardMinimize: function(e) {
            const card = e.target.closest('.card');
            const cardBody = card.querySelector('.card-body');
            
            if (cardBody) {
                cardBody.style.display = cardBody.style.display === 'none' ? 'block' : 'none';
                card.classList.toggle('minimized');
                Dashboard.saveUserPreferences();
            }
        },

        initializeWidgets: function() {
            // Initialize any interactive widgets
            this.initializeCounters();
            this.initializeProgressBars();
        },

        initializeCounters: function() {
            // Animated number counters
            document.querySelectorAll('[data-counter]').forEach(counter => {
                const target = parseInt(counter.dataset.counter);
                const duration = 1000;
                const step = target / (duration / 16);
                let current = 0;

                const updateCounter = () => {
                    current += step;
                    if (current < target) {
                        counter.textContent = Math.floor(current).toLocaleString('vi-VN');
                        requestAnimationFrame(updateCounter);
                    } else {
                        counter.textContent = target.toLocaleString('vi-VN');
                    }
                };

                // Start animation when element is in viewport
                const observer = new IntersectionObserver((entries) => {
                    entries.forEach(entry => {
                        if (entry.isIntersecting) {
                            updateCounter();
                            observer.unobserve(entry.target);
                        }
                    });
                });

                observer.observe(counter);
            });
        },

        initializeProgressBars: function() {
            // Animated progress bars
            document.querySelectorAll('.progress-bar[data-progress]').forEach(bar => {
                const progress = parseInt(bar.dataset.progress);
                
                const observer = new IntersectionObserver((entries) => {
                    entries.forEach(entry => {
                        if (entry.isIntersecting) {
                            setTimeout(() => {
                                bar.style.width = progress + '%';
                            }, 100);
                            observer.unobserve(entry.target);
                        }
                    });
                });

                observer.observe(bar);
            });
        },

        exportData: function(e) {
            const format = e.target.dataset.export;
            const widgetId = e.target.closest('[data-widget]')?.dataset.widget;

            if (!widgetId) {
                console.error('No widget ID found for export');
                return;
            }

            // Show loading state
            const btn = e.target;
            const originalText = btn.innerHTML;
            btn.innerHTML = '<i class="fas fa-spinner fa-spin me-1"></i>Đang xuất...';
            btn.disabled = true;

            // Simulate export (replace with actual API call)
            setTimeout(() => {
                btn.innerHTML = originalText;
                btn.disabled = false;
                
                // Show success message
                Dashboard.showNotification('Xuất dữ liệu thành công!', 'success');
            }, 1500);
        },

        applyFilters: function(e) {
            const filterType = e.target.dataset.filter;
            const filterValue = e.target.value;

            // Apply filter logic here
            console.log(`Applying filter: ${filterType} = ${filterValue}`);
            
            // Show loading overlay
            Dashboard.showLoading();

            // Simulate filter application (replace with actual API call)
            setTimeout(() => {
                Dashboard.hideLoading();
                Dashboard.showNotification('Đã áp dụng bộ lọc', 'info');
            }, 1000);
        },

        refreshWidget: function(e) {
            const widget = e.target.closest('[data-widget]');
            const widgetId = widget?.dataset.widget;

            if (!widgetId) return;

            // Add loading state to widget
            widget.classList.add('loading');

            // Simulate refresh (replace with actual API call)
            setTimeout(() => {
                widget.classList.remove('loading');
                Dashboard.showNotification('Đã làm mới dữ liệu', 'success');
            }, 1500);
        },

        setupAutoRefresh: function() {
            // Auto-refresh dashboard data
            if (DashboardConfig.refreshInterval > 0) {
                setInterval(() => {
                    this.refreshAllWidgets();
                }, DashboardConfig.refreshInterval);
            }
        },

        refreshAllWidgets: function() {
            console.log('Auto-refreshing dashboard data...');
            // Implement actual refresh logic here
            // This could fetch new data from the server without full page reload
        },

        showNotification: function(message, type = 'info') {
            const notification = document.createElement('div');
            notification.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
            notification.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
            notification.innerHTML = `
                ${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            `;

            document.body.appendChild(notification);

            // Auto-remove after 5 seconds
            setTimeout(() => {
                notification.classList.remove('show');
                setTimeout(() => notification.remove(), 300);
            }, 5000);
        },

        showLoading: function() {
            const overlay = document.getElementById('loadingOverlay');
            if (overlay) {
                overlay.classList.add('active');
            }
        },

        hideLoading: function() {
            const overlay = document.getElementById('loadingOverlay');
            if (overlay) {
                overlay.classList.remove('active');
            }
        }
    };

    // Chart Utilities
    const ChartUtils = {
        defaultOptions: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
                legend: {
                    display: true,
                    position: 'bottom'
                }
            }
        },

        createLineChart: function(ctx, data, options = {}) {
            return new Chart(ctx, {
                type: 'line',
                data: data,
                options: { ...this.defaultOptions, ...options }
            });
        },

        createBarChart: function(ctx, data, options = {}) {
            return new Chart(ctx, {
                type: 'bar',
                data: data,
                options: { ...this.defaultOptions, ...options }
            });
        },

        createDoughnutChart: function(ctx, data, options = {}) {
            return new Chart(ctx, {
                type: 'doughnut',
                data: data,
                options: { ...this.defaultOptions, ...options }
            });
        },

        updateChart: function(chart, newData) {
            chart.data = newData;
            chart.update('active');
        }
    };

    // Widget Customization
    const WidgetCustomizer = {
        init: function() {
            this.setupDragAndDrop();
            this.setupResizing();
        },

        setupDragAndDrop: function() {
            // Enable drag and drop for dashboard widgets
            const widgets = document.querySelectorAll('[data-widget]');
            
            widgets.forEach(widget => {
                widget.setAttribute('draggable', 'true');
                
                widget.addEventListener('dragstart', (e) => {
                    e.dataTransfer.effectAllowed = 'move';
                    e.dataTransfer.setData('text/html', widget.innerHTML);
                    widget.classList.add('dragging');
                });

                widget.addEventListener('dragend', () => {
                    widget.classList.remove('dragging');
                    Dashboard.saveUserPreferences();
                });
            });

            // Setup drop zones
            const containers = document.querySelectorAll('.row');
            containers.forEach(container => {
                container.addEventListener('dragover', (e) => {
                    e.preventDefault();
                    e.dataTransfer.dropEffect = 'move';
                });

                container.addEventListener('drop', (e) => {
                    e.preventDefault();
                    // Handle drop logic here
                });
            });
        },

        setupResizing: function() {
            // Enable widget resizing (optional feature)
            // Implementation depends on requirements
        }
    };

    // Performance Monitoring
    const PerformanceMonitor = {
        init: function() {
            this.measurePageLoad();
            this.monitorChartPerformance();
        },

        measurePageLoad: function() {
            window.addEventListener('load', () => {
                const perfData = window.performance.timing;
                const pageLoadTime = perfData.loadEventEnd - perfData.navigationStart;
                console.log(`Dashboard loaded in ${pageLoadTime}ms`);
            });
        },

        monitorChartPerformance: function() {
            // Monitor chart rendering performance
            const observer = new PerformanceObserver((list) => {
                for (const entry of list.getEntries()) {
                    if (entry.name.includes('chart')) {
                        console.log(`Chart rendered in ${entry.duration}ms`);
                    }
                }
            });

            observer.observe({ entryTypes: ['measure'] });
        }
    };

    // Initialize on DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            Dashboard.init();
            WidgetCustomizer.init();
            PerformanceMonitor.init();
        });
    } else {
        Dashboard.init();
        WidgetCustomizer.init();
        PerformanceMonitor.init();
    }

    // Export for global access
    window.Dashboard = Dashboard;
    window.ChartUtils = ChartUtils;

})();
