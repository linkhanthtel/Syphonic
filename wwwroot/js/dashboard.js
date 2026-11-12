// Draws the dashboard charts with Chart.js from the JSON the server embedded in the page.
(() => {
    const dataElement = document.getElementById('dashboard-data');
    if (!dataElement || typeof Chart === 'undefined') return;

    const data = JSON.parse(dataElement.textContent);

    // One place for colours so the charts match the CSS (see the "Dashboard" block in site.css).
    const colors = {
        blue: '#2a78d6',
        orange: '#eb6834',
        surface: '#ffffff',
        ink: '#212529',
        muted: '#6c757d',
        grid: '#e9ecef',
        axis: '#ced4da'
    };

    Chart.defaults.font.family = 'system-ui, -apple-system, "Segoe UI", sans-serif';
    Chart.defaults.color = colors.muted;
    Chart.defaults.maintainAspectRatio = false;
    Chart.defaults.plugins.legend.position = 'bottom';
    Chart.defaults.plugins.legend.labels.usePointStyle = true;
    Chart.defaults.plugins.legend.labels.boxHeight = 8;
    Chart.defaults.plugins.tooltip.backgroundColor = colors.ink;
    Chart.defaults.plugins.tooltip.padding = 10;

    const quietGrid = { color: colors.grid, drawTicks: false };
    const axisBorder = { color: colors.axis };

    // ---------- Bar chart: activity per day ----------
    const activityCanvas = document.getElementById('activityChart');
    if (activityCanvas) {
        new Chart(activityCanvas, {
            type: 'bar',
            data: {
                labels: data.activity.map(d => d.label),
                datasets: [
                    {
                        label: 'Lessons',
                        data: data.activity.map(d => d.lessons),
                        backgroundColor: colors.blue,
                        pointStyle: 'rectRounded'
                    },
                    {
                        label: 'Quizzes & practice',
                        data: data.activity.map(d => d.quizzes),
                        backgroundColor: colors.orange,
                        pointStyle: 'rectRounded'
                    }
                ].map(dataset => ({
                    ...dataset,
                    borderRadius: 4,
                    // A thin surface-coloured outline separates the two stacked segments.
                    borderColor: colors.surface,
                    borderWidth: 1,
                    maxBarThickness: 28
                }))
            },
            options: {
                interaction: { mode: 'index', intersect: false },
                scales: {
                    x: { stacked: true, grid: { display: false }, border: axisBorder },
                    y: {
                        stacked: true,
                        beginAtZero: true,
                        ticks: { precision: 0, padding: 8 },
                        grid: quietGrid,
                        border: { display: false },
                        title: { display: true, text: 'Activities' }
                    }
                }
            }
        });
    }

    // ---------- Scatter plot: quiz attempts ----------
    const attemptsCanvas = document.getElementById('attemptsChart');
    if (attemptsCanvas) {
        // Draws the dashed pass-mark line and its label behind the dots.
        const passLine = {
            id: 'passLine',
            beforeDatasetsDraw(chart) {
                const { ctx, chartArea, scales } = chart;
                const y = scales.y.getPixelForValue(data.passPercent);
                ctx.save();
                ctx.strokeStyle = colors.muted;
                ctx.setLineDash([6, 4]);
                ctx.lineWidth = 1;
                ctx.beginPath();
                ctx.moveTo(chartArea.left, y);
                ctx.lineTo(chartArea.right, y);
                ctx.stroke();
                ctx.fillStyle = colors.muted;
                ctx.font = `12px ${Chart.defaults.font.family}`;
                ctx.textAlign = 'right';
                ctx.textBaseline = 'top';
                ctx.fillText(`Pass mark ${data.passPercent}%`, chartArea.right, y + 5);
                ctx.restore();
            }
        };

        // Colour AND shape both say passed / not passed, so the chart works without colour vision.
        const series = (label, passed, color, pointStyle, radius) => ({
            label,
            data: data.attempts.filter(a => a.passed === passed),
            backgroundColor: color,
            borderColor: colors.surface,
            borderWidth: 2,
            pointStyle,
            pointRadius: radius,
            pointHoverRadius: radius + 2,
            // Let dots sitting exactly on 0% or 100% draw in full instead of being cut at the plot edge.
            clip: 12
        });

        const lastAttempt = Math.max(data.attempts.length, 5);

        new Chart(attemptsCanvas, {
            type: 'scatter',
            data: {
                datasets: [
                    series('Passed', true, colors.blue, 'circle', 6),
                    series('Below pass mark', false, colors.orange, 'triangle', 8)
                ]
            },
            plugins: [passLine],
            options: {
                scales: {
                    x: {
                        min: 0,
                        max: lastAttempt + 1,
                        // The axis runs one step past each end so the first and last dots have room.
                        ticks: { stepSize: 1, precision: 0, callback: value => (value >= 1 && value <= lastAttempt ? value : '') },
                        grid: { display: false },
                        border: axisBorder,
                        title: { display: true, text: 'Attempt number' }
                    },
                    y: {
                        min: 0,
                        max: 105,
                        ticks: { stepSize: 25, padding: 8, callback: value => (value <= 100 ? `${value}%` : '') },
                        grid: quietGrid,
                        border: { display: false },
                        title: { display: true, text: 'Score' }
                    }
                },
                plugins: {
                    tooltip: {
                        callbacks: {
                            title: items => items[0].raw.lesson,
                            label: item => `${item.raw.y}% (${item.raw.score}) · attempt ${item.raw.x} · ${item.raw.date}`
                        }
                    }
                }
            }
        });
    }
})();
