URLs

web frontend blazor server:
https://webfrontend.mangocoast-4cf38685.germanywestcentral.azurecontainerapps.io/



todo:

const lighthouse = require('lighthouse');
const chromeLauncher = require('chrome-launcher');

async function runLighthouse() {
  const chrome = await chromeLauncher.launch();
  const options = {
    logLevel: 'info',
    output: 'json',
    onlyCategories: ['performance'],
    throttling: {
      // Custom network throttling settings
      rttMs: 40,          // Round Trip Time (latency) in milliseconds
      throughputKbps: 10 * 1024, // Download speed in Kbps (10 Mbps in this example)
      uploadThroughputKbps: 5 * 1024, // Upload speed in Kbps (5 Mbps in this example)
      cpuSlowdownMultiplier: 4 // Optional: simulate slower CPU
    }
  };

  const runnerResult = await lighthouse('http://your-blazor-app-url', options);
  
  console.log('Lighthouse report', runnerResult.report);
  await chrome.kill();
}

runLighthouse();