import fs from 'fs';
import lighthouse from 'lighthouse';
import * as chromeLauncher from 'chrome-launcher';

const url_blazor = "https://webfrontend.icypond-3e3cba77.germanywestcentral.azurecontainerapps.io/"
const url_react = "https://react.dev/"
const name = "blazor-server"
const reports = "reports"

function ensureDirectoryExists(directory) {
    if (!fs.existsSync(directory)) {
        fs.mkdirSync(directory, { recursive: true }); // Create the folder and any necessary parent folders
    }
}

const modeSettings = {
    "desktop": {
        throttlingMethod: 'devtools', // Completely disable throttling
        emulatedFormFactor: 'desktop',
        // OR set maximum throughput
        throttling: {
            rttMs: 0,
            throughputKbps: 100000, // Very high bandwidth (100 Mbps)
            uploadThroughputKbps: 100000,
            cpuSlowdownMultiplier: 1
        },
        screenEmulation: {
            mobile: false,
            width: 1350,
            height: 940,
            deviceScaleFactor: 1,
            disabled: false
          },
        emulatedUserAgent: 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_14_6) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/84.0.4143.7 Safari/537.36 Chrome-Lighthouse'
    },
    "mobile": {
        throttlingMethod: 'devtools', // Completely disable throttling
        emulatedFormFactor: 'mobile',
        // OR set maximum throughput
        throttling: {
            rttMs: 150,          // Round Trip Time (latency) in milliseconds
            throughputKbps: 1.6 * 1024, // Download speed in Kbps (1.6Mbps)
            uploadThroughputKbps: 0.750 * 1024, // Upload speed in Kbps (750kbps)
            cpuSlowdownMultiplier: 4 // Optional: simulate slower CPU
        },
        screenEmulation: {
            mobile: true,
            width: 375,
            height: 667,
            deviceScaleFactor: 2,
            disabled: false
        },
        emulatedUserAgent: 'Mozilla/5.0 (iPhone; CPU iPhone OS 13_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.1 Mobile/15E148 Safari/604.1.38'
    }
}

const params = [
    {
        "name": "blazor-server-desktop",
        "url": url_blazor,
        "mode": "desktop",
    },
    {
        "name": "blazor-server-mobile",
        "url": url_blazor,
        "mode": "mobile",
    },

    {
        "name": "react-dev-desktop",
        "url": url_react,
        "mode": "desktop",
    },
    {
        "name": "react-dev-mobile",
        "url": url_react,
        "mode": "mobile",
    }
]


for (const param of params) {
    ensureDirectoryExists(reports + "/" + param.name);

    for (const outputType of ['html', 'json', 'csv']) {
        const chrome = await chromeLauncher.launch({ chromeFlags: ['--headless'] });
        let setting;
        
        if (param.mode == "desktop") {
            setting = modeSettings.desktop
        } else {
            setting = modeSettings.mobile
        }
        let flags = { logLevel: 'info', output: outputType, port: chrome.port };
        let config = {
            extends: 'lighthouse:default',
            settings: {
                formFactor:param.mode,
                throttlingMethod: setting.throttlingMethod,
                throttling: setting.throttling,
                screenEmulation: setting.screenEmulation,
                emulatedUserAgent: setting.emulatedUserAgent,
            }
            
        };

        let runnerResult = await lighthouse(param.url, flags, config);

        // `.report` is the HTML report as a string
        let reportContent = runnerResult.report;
        fs.writeFileSync(reports + "/" + param.name + "/" + 'report.' + outputType, reportContent);
        // `.lhr` is the Lighthouse Result as a JS object
        console.log(`Report is done for ${param.name} ${param.mode}`, runnerResult.lhr.finalDisplayedUrl);
        console.log('Performance score was', runnerResult.lhr.categories.performance.score * 100);
        chrome.kill();
    }
}



