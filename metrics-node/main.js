import fs from 'fs';
import lighthouse from 'lighthouse';
import puppeteer from 'puppeteer';

const url_blazor = "http://localhost:7003/"
const url_react = "http://localhost:7004/"

const reports = "reports"

function ensureDirectoryExists(directory) {
    if (!fs.existsSync(directory)) {
        fs.mkdirSync(directory, { recursive: true });
    }
}

const modeSettings = {
    "desktop": {
        throttlingMethod: 'devtools',
        emulatedFormFactor: 'desktop',
        throttling: {
            rttMs: 0,
            throughputKbps: 100000,
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
        throttlingMethod: 'devtools',
        emulatedFormFactor: 'mobile',
        throttling: {
            rttMs: 150,
            throughputKbps: 1.6 * 1024,
            uploadThroughputKbps: 0.750 * 1024,
            cpuSlowdownMultiplier: 4
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

const paramsList = [
    // {
    //     "name": "blazor-desktop-bootstrap",
    //     "url": url_blazor,
    //     "mode": "desktop",
    // },
    // {
    //     "name": "blazor-mobile-bootstrap",
    //     "url": url_blazor,
    //     "mode": "mobile",
    // },

    {
        "name": "react-desktop-bootstrap",
        "url": url_react,
        "mode": "desktop",
    },
    {
        "name": "react-mobile-bootstrap",
        "url": url_react,
        "mode": "mobile",
    },

    // {
    //     "name": "react-desktop",
    //     "url": url_react,
    //     "mode": "desktop",
    // },
    // {
    //     "name": "react-mobile",
    //     "url": url_react,
    //     "mode": "mobile",
    // }
]


const params = paramsList;

async function runLighthouseWithPuppeteer(param) {
    ensureDirectoryExists(`${reports}/${param.name}`);

    const browser = await puppeteer.launch({
        headless: false,
        args: ['--remote-debugging-port=9222']
    });

    const wsEndpoint = browser.wsEndpoint();
    const url = new URL(wsEndpoint);
    const port = url.port;

    let setting = param.mode === "desktop" ? modeSettings.desktop : modeSettings.mobile;
    let outputTypes = ['html', 'json','csv'];
    let flags = { logLevel: 'info', output: outputTypes, port: port };
    let config = {
        extends: 'lighthouse:default',
        settings: {
            formFactor: param.mode,
            screenEmulation: setting.screenEmulation,
            emulatedUserAgent: setting.emulatedUserAgent,
        }
    };

    let runnerResult = await lighthouse(param.url, flags, config);

    outputTypes.forEach((type, idx) => {
        fs.writeFileSync(`${reports}/${param.name}/report.${type}`, runnerResult.report[idx]);
    });

    console.log(`Report is done for ${param.name} ${param.mode}`, runnerResult.lhr.finalDisplayedUrl);
    console.log('Performance score was', runnerResult.lhr.categories.performance.score * 100);

    await browser.close();
}

(async () => {
    for (const param of params) {
        await runLighthouseWithPuppeteer(param);
    }
})();
