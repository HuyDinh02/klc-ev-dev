import { test, expect, Page } from "@playwright/test";
import path from "path";

/**
 * Demo Station Charging Test
 *
 * Opens the local simulator, connects to cloud as station "demo",
 * runs auto meter values for 60 seconds, then stops.
 *
 * Run: cd tests/ocpp-e2e && npx playwright test demo-charging
 * Run headed: npx playwright test demo-charging --headed
 */

const OCPP_WS = "wss://ocpp.ev.odcall.com/ocpp/demo";
const SIMULATOR_PATH = path.resolve(__dirname, "../../../ocpp-simulator/simulator16-local.html");

async function waitForLog(page: Page, text: string, timeoutMs = 15_000) {
  await expect(async () => {
    const content = await page.locator("#log").innerText();
    expect(content).toContain(text);
  }).toPass({ timeout: timeoutMs });
}

test.describe.serial("Demo Station: Auto Charging Simulation", () => {
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    const ctx = await browser.newContext({ ignoreHTTPSErrors: true });
    page = await ctx.newPage();
    await page.goto(`file://${SIMULATOR_PATH}`);
    await expect(page.locator("h1")).toContainText("OCPP");
  });

  test.afterAll(async () => {
    try {
      const disconnectBtn = page.locator("#btnDisconnect");
      if (await disconnectBtn.isEnabled()) await disconnectBtn.click();
    } catch {}
    await page.context().close();
  });

  test("1. Connect to cloud OCPP gateway as demo station", async () => {
    await page.locator("#csUrl").fill(OCPP_WS);
    await page.locator("#btnConnect").click();
    await waitForLog(page, "WebSocket CONNECTED");
  });

  test("2. Send BootNotification", async () => {
    await page.locator("#btnBoot").click();
    await waitForLog(page, "Accepted");
  });

  test("3. Send StatusNotification: Available for connector 1", async () => {
    await page.locator("#connectorId").selectOption("1");
    await page.locator("#connectorStatus").selectOption("Available");
    await page.locator('button:has-text("StatusNotification")').click();
    await waitForLog(page, "StatusNotification");
  });

  test("4. Send StatusNotification: Available for connector 2", async () => {
    await page.locator("#connectorId").selectOption("2");
    await page.locator("#connectorStatus").selectOption("Available");
    await page.locator('button:has-text("StatusNotification")').click();
    await waitForLog(page, "StatusNotification");
  });

  test("5. Start charging on connector 1 (manual)", async () => {
    test.setTimeout(45_000);
    // Set connector 1, reset meter to 0
    await page.locator("#connectorId").selectOption("1");
    await page.locator("#meterValue").fill("0");
    await page.locator("#socValue").fill("20");
    await page.locator("#powerValue").fill("50000");
    await page.locator("#idTag").fill("TEST001");

    // Send StartTransaction
    await page.locator('button:has-text("StartTransaction")').click();
    await waitForLog(page, "Transaction ID captured", 30_000);

    // Send StatusNotification: Charging
    await page.locator("#connectorStatus").selectOption("Charging");
    await page.locator('button:has-text("StatusNotification")').click();
  });

  test("6. Stream MeterValues for 30 seconds", async () => {
    test.setTimeout(60_000);

    // Verify transaction is active
    const txDisplay = page.locator("#txIdDisplay");
    await expect(txDisplay).not.toHaveText("—", { timeout: 5_000 });

    // Send manual MeterValues first
    await page.locator('button:has-text("Send MeterValues")').click();
    await waitForLog(page, "MeterValues");

    // Start auto meter values
    await page.locator("#btnAutoCharge").click();
    await waitForLog(page, "Auto MeterValues started");

    // Wait 30 seconds — auto sends every 10s = 3 meter values
    console.log("Streaming meter values for 30 seconds...");
    await page.waitForTimeout(30_000);

    // Stop auto meter values
    await page.locator("#btnAutoCharge").click();
    await waitForLog(page, "Auto MeterValues stopped");

    // Verify meter value increased (1 manual + 3 auto = ~2000Wh)
    const meterValue = await page.locator("#meterValue").inputValue();
    const meter = parseInt(meterValue);
    console.log(`Final meter value: ${meter} Wh (${(meter / 1000).toFixed(1)} kWh)`);
    expect(meter).toBeGreaterThanOrEqual(1500);
  });

  test("7. Stop charging", async () => {
    await page.locator('button:has-text("StopTransaction")').click();
    await waitForLog(page, "Stopping transaction");

    // Send StatusNotification: Available
    await page.locator("#connectorStatus").selectOption("Available");
    await page.locator('button:has-text("StatusNotification")').click();
  });

  test("8. Verify session via BFF API", async () => {
    const ctx = await page.context().request;

    // Login as seed user
    const loginRes = await ctx.post("https://bff.ev.odcall.com/api/v1/auth/login", {
      data: { phoneNumber: "0901234001", password: "Admin@123" },
    });
    expect(loginRes.ok()).toBeTruthy();
    const { accessToken } = await loginRes.json();

    // Check session history
    const historyRes = await ctx.get("https://bff.ev.odcall.com/api/v1/sessions/history?pageSize=1", {
      headers: { Authorization: `Bearer ${accessToken}` },
    });

    if (historyRes.ok()) {
      const history = await historyRes.json();
      console.log(`Recent sessions: ${history.items?.length ?? 0}`);
    }
  });
});
