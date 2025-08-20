// web-vitals-expose.js
// Collects web-vitals metrics and exposes them on window.__webVitals
// To be included in ana.Web/wwwroot

// Assumes web-vitals.umd.js is loaded first and webVitals is available globally
window.__webVitals = {};
window.__webVitalsReset = function() {
    // Clear previous metrics
    window.__webVitals = {};

    // Remove old observers by re-registering them
    webVitals.getCLS(metric => exposeMetric('CLS', metric.value));
    webVitals.getFID(metric => exposeMetric('FID', metric.value));
    webVitals.getLCP(metric => exposeMetric('LCP', metric.value));
    webVitals.getFCP(metric => exposeMetric('FCP', metric.value));
    webVitals.getTTFB(metric => exposeMetric('TTFB', metric.value));
    if (webVitals.getINP) webVitals.getINP(metric => exposeMetric('INP', metric.value));
};

function exposeMetric(name, value) {
    window.__webVitals[name] = value;
}
