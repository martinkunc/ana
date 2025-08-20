import fs from 'fs';
import puppeteer from 'puppeteer';

const url_blazor = "http://localhost:7003/"
const url_react = "http://localhost:7004/"

const reports = "reports"

function ensureDirectoryExists(directory) {
    if (!fs.existsSync(directory)) {
        fs.mkdirSync(directory, { recursive: true });
    }
}

function getParamsColdSection(nameSuffix, initialLink, navigateTo) {
    return [
        {
            "name": "blazor-desktop-" + nameSuffix,
            "url": url_blazor,
            "mode": "desktop",
            "with_login": true,
            "initialLink": initialLink,
            "navigateTo": navigateTo
        },
        {
            "name": "blazor-mobile-" + nameSuffix,
            "url": url_blazor,
            "mode": "mobile",
            "with_login": true,
            "initialLink": initialLink,
            "navigateTo": navigateTo
        },
        {
            "name": "react-desktop-" + nameSuffix,
            "url": url_react,
            "mode": "desktop",
            "with_login": true,
            "initialLink": initialLink,
            "navigateTo": navigateTo
        },
        {
            "name": "react-mobile-" + nameSuffix,
            "url": url_react,
            "mode": "mobile",
            "with_login": true,
            "initialLink": initialLink,
            "navigateTo": navigateTo
        }
    ];
}

const params = getParamsColdSection("anniversaries", "Settings", "Anniversaries")
    .concat(getParamsColdSection("members", "Settings", "Members"))
    .concat(getParamsColdSection("myothergroups", "Settings", "My other groups"))
    .concat(getParamsColdSection("settings", "Members", "Settings"))

async function applyMobileViewport(page) {
    await page.setViewport({
        width: 375,
        height: 667,
        deviceScaleFactor: 2,
        isMobile: true,
        hasTouch: true
    });
}

async function emulateMobile(page, client) {
    await client.send('Emulation.setDeviceMetricsOverride', {
        width: 375,
        height: 667,
        deviceScaleFactor: 2,
        mobile: true,
        screenWidth: 375,
        screenHeight: 667,
        positionX: 0,
        positionY: 0,
        dontSetVisibleSize: false
    });
    await page.setUserAgent('Mozilla/5.0 (iPhone; CPU iPhone OS 13_4 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/13.1 Mobile/15E148 Safari/604.1.38');

    await client.send('Network.enable');

    const downloadKbps = 1.6 * 1024;
    const uploadKbps = 0.750 * 1024;
    const kbpsToBps = kb => kb * 1024;
    const bpsToBytesPerSec = bps => bps / 8;

    await client.send('Network.emulateNetworkConditions', {
        offline: false,
        latency: 150,
        downloadThroughput: bpsToBytesPerSec(kbpsToBps(downloadKbps)),
        uploadThroughput: bpsToBytesPerSec(kbpsToBps(uploadKbps))
    });
    await client.send('Emulation.setCPUThrottlingRate', { rate: 4 });
    await client.send('Network.setCacheDisabled', { cacheDisabled: true });

    await applyMobileViewport(page);
}

async function clickToLink(page, linkText) {
    await page.evaluate((linkText) => {
        const xpath = `//a[contains(@class, 'nav-link') and contains(., '${linkText}')]`;
        const result = document.evaluate(xpath, document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null);
        const el = result.singleNodeValue;
        if (el) el.click();
    }, linkText);
}

async function clickToggle(page) {
    await page.waitForSelector('button.navbar-toggler[title="Navigation menu"]', { visible: true });
    await page.click('button.navbar-toggler[title="Navigation menu"]');
}

async function runFlowWithPuppeteer(param) {
    ensureDirectoryExists(`${reports}/${param.name}`);

    const browser = await puppeteer.launch({
        headless: false,
        args: ['--remote-debugging-port=9222']
    });

    const wsEndpoint = browser.wsEndpoint();
    const url = new URL(wsEndpoint);
    const port = url.port;
    let page = await browser.newPage();
    const client = await page.target().createCDPSession();
    await client.send('Network.clearBrowserCache');
    await client.send('Network.clearBrowserCookies');
    await client.send('Storage.clearDataForOrigin', {
        origin: 'http://localhost:7003',
        storageTypes: 'all'
    });

    if (param.mode === 'mobile') {
        await emulateMobile(page, client);
    }
    let networkRequests = [];
    page.on('request', req => {
        networkRequests.push({
            url: req.url(),
            method: req.method(),
            type: req.resourceType(),
            startTime: Date.now(),
            requestId: req._requestId
        });
    });
    page.on('response', async res => {
        const req = networkRequests.find(r => r.url === res.url() && !r.endTime);
        if (req) {
            req.status = res.status();
            req.contentType = res.headers()['content-type'] || '';
            req.endTime = Date.now();
            req.duration = req.endTime - req.startTime;
            try {
                const buffer = await res.buffer();
                req.size = buffer.length;
            } catch {
                req.size = 0;
            }
        }
    });

    if (param.with_login) {
        await page.goto(param.url, { waitUntil: 'networkidle0' });
        if (param.mode === 'mobile') await applyMobileViewport(page);
        await page.waitForSelector('button.btn.btn-primary[type="submit"]', { visible: true });
        await page.type('#Input_Email', 'admin');
        await page.type('#Input_Password', '');
        await Promise.all([
            page.click('button.btn.btn-primary[type="submit"]'),
            page.waitForNavigation({ waitUntil: 'networkidle2' })
        ]);
    }
    if (param.mode === 'mobile') {
        await page.evaluate(() => new Promise(resolve => setTimeout(resolve, 2000)));
        await clickToggle(page);

    }
    // Start on some initial page and measure the navigation to our destination page
    await clickToLink(page, param.initialLink);
    await page.evaluate(() => new Promise(resolve => setTimeout(resolve, 1000)));
    
    networkRequests = [];

    await page.evaluate(() => window.__webVitalsReset());

    // Invoke interaction to collect INP metrics
    await page.click('body');
    await page.keyboard.press('Tab');

    await page.evaluate(() => new Promise(resolve => setTimeout(resolve, 200)));

    if (param.mode === 'mobile') {
        await clickToggle(page);
    }
    await clickToLink(page, param.navigateTo);

    await page.evaluate(() => new Promise(resolve => setTimeout(resolve, 1000)));
    if (param.mode === 'mobile') {
        await new Promise(resolve => setTimeout(resolve, 2000));
    }

    const webVitals = await page.evaluate(() => window.__webVitals || {});

    if (param.mode === 'mobile') {
        await applyMobileViewport(page);
        await new Promise(resolve => setTimeout(resolve, 1000));
    }
    await page.screenshot({
        path: `${reports}/${param.name}/screenshot.png`,
        fullPage: false
    });

    const result = {
        webVitals,
        networkRequests
    };
    fs.writeFileSync(`${reports}/${param.name}/metrics.json`, JSON.stringify(result, null, 2));

    await browser.close();
}

(async () => {
    for (const param of params) {
        await runFlowWithPuppeteer(param);
    }
})();

