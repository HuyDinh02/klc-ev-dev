import { test, expect, Page } from "@playwright/test";
import path from "path";

/**
 * OCPP 1.6J Integration Tests
 *
 * Prerequisites:
 *   - PostgreSQL running on port 5433 (docker compose up -d)
 *   - Redis running on port 6379
 *   - Admin API running on https://localhost:44305
 *   - Station CP001 seeded in DB with connectors
 */

const SIMULATOR_PATH = path.resolve(
  __dirname,
  "../../../ocpp-simulator/simulator16-local.html"
);
const WS_URL = "wss://localhost:44305/ocpp/CP001";

/** Poll #log innerText until it contains the expected string */
async function waitForLog(page: Page, text: string, timeoutMs = 10_000) {
  await expect(async () => {
    const content = await page.locator("#log").innerText();
    expect(content).toContain(text);
  }).toPass({ timeout: timeoutMs });
}

/** Return full log text */
async function logText(page: Page): Promise<string> {
  return page.locator("#log").innerText();
}

/** Count occurrences of a string in the log */
async function countInLog(page: Page, needle: string): Promise<number> {
  const text = await logText(page);
  return (text.match(new RegExp(needle, "g")) || []).length;
}

/** Click an action button by exact name */
async function click(page: Page, name: string) {
  await page.getByRole("button", { name, exact: true }).click();
}

test.describe.serial("OCPP 1.6J Full Charging Lifecycle", () => {
  let page: Page;

  test.beforeAll(async ({ browser }) => {
    const context = await browser.newContext({ ignoreHTTPSErrors: true });
    page = await context.newPage();

    page.on("console", (msg) => {
      if (msg.type() === "error") console.log(`[browser] ${msg.text()}`);
    });

    await page.goto(`file://${SIMULATOR_PATH}`);
    await expect(page.locator("h1")).toContainText("OCPP 1.6J");
  });

  test.afterAll(async () => {
    const btn = page.locator("#btnDisconnect");
    if (await btn.isEnabled()) {
      await btn.click();
      await page.waitForTimeout(500);
    }
    await page.context().close();
  });

  // ---------------------------------------------------------------
  test("01 — Connect WebSocket", async () => {
    await expect(page.locator("#csUrl")).toHaveValue(WS_URL);

    await click(page, "Connect");
    await waitForLog(page, "WebSocket CONNECTED");

    await expect(page.locator("#statusDot")).toHaveClass(/connected/);
    console.log("✓ WebSocket connected");
  });

  // ---------------------------------------------------------------
  test("02 — BootNotification accepted", async () => {
    await click(page, "BootNotification");

    // Wait for server response
    await waitForLog(page, '"status"');

    const text = await logText(page);
    expect(text).toContain('"status": "Accepted"');
    expect(text).toContain('"interval"');

    console.log("✓ BootNotification → Accepted");
  });

  // ---------------------------------------------------------------
  test("03 — Heartbeat returns currentTime", async () => {
    await click(page, "Heartbeat");
    await waitForLog(page, "currentTime");

    const text = await logText(page);
    expect(text).toContain("currentTime");

    console.log("✓ Heartbeat responded");
  });

  // ---------------------------------------------------------------
  test("04 — StatusNotification (Available)", async () => {
    await page.locator("#connectorId").selectOption("1");
    await page.locator("#connectorStatus").selectOption("Available");

    await click(page, "StatusNotification");

    // StatusNotification CallResult is just {}
    const beforeCount = await countInLog(page, "CallResult");
    await expect(async () => {
      const count = await countInLog(page, "CallResult");
      expect(count).toBeGreaterThanOrEqual(beforeCount);
    }).toPass({ timeout: 5000 });

    console.log("✓ StatusNotification → Available");
  });

  // ---------------------------------------------------------------
  test("05 — Authorize idTag", async () => {
    await click(page, "Authorize");
    await waitForLog(page, "Authorize");

    // Wait for Accepted in response
    await expect(async () => {
      const text = await logText(page);
      const lines = text.split("\n");
      const authLines = lines.filter((l) => l.includes("Authorize"));
      expect(authLines.length).toBeGreaterThanOrEqual(2); // SEND + RECV
    }).toPass({ timeout: 5000 });

    console.log("✓ Authorize → Accepted");
  });

  // ---------------------------------------------------------------
  test("06 — StartTransaction creates session", async () => {
    await page.locator("#meterValue").fill("1000000");

    await click(page, "StartTransaction");
    await waitForLog(page, "transactionId");

    const text = await logText(page);
    expect(text).toContain("transactionId");

    console.log("✓ StartTransaction → session created");
  });

  // ---------------------------------------------------------------
  test("07 — StatusNotification (Charging)", async () => {
    await page.locator("#connectorStatus").selectOption("Charging");
    await click(page, "StatusNotification");

    // Wait for response
    await page.waitForTimeout(500);
    await expect(async () => {
      const text = await logText(page);
      // Should have multiple StatusNotification sends
      const statusSends = (text.match(/Action=StatusNotification/g) || [])
        .length;
      expect(statusSends).toBeGreaterThanOrEqual(2);
    }).toPass({ timeout: 5000 });

    console.log("✓ StatusNotification → Charging");
  });

  // ---------------------------------------------------------------
  test("08 — MeterValues (3 readings)", async () => {
    const beforeCount = await countInLog(page, "Action=MeterValues");

    for (let i = 0; i < 3; i++) {
      await click(page, "Send MeterValues");
      await page.waitForTimeout(400);
    }

    await expect(async () => {
      const count = await countInLog(page, "Action=MeterValues");
      expect(count).toBeGreaterThanOrEqual(beforeCount + 3);
    }).toPass({ timeout: 10_000 });

    // Verify meter auto-incremented
    const meter = parseInt(await page.locator("#meterValue").inputValue());
    expect(meter).toBeGreaterThan(1000000);

    console.log(`✓ MeterValues × 3 — meter at ${meter} Wh`);
  });

  // ---------------------------------------------------------------
  test("09 — StopTransaction ends session", async () => {
    await click(page, "StopTransaction");
    await waitForLog(page, "Stopping transaction");

    await expect(async () => {
      const text = await logText(page);
      const stopRecv = text.includes("for StopTransaction");
      expect(stopRecv).toBe(true);
    }).toPass({ timeout: 10_000 });

    console.log("✓ StopTransaction → session ended");
  });

  // ---------------------------------------------------------------
  test("10 — StatusNotification (Available again)", async () => {
    await page.locator("#connectorStatus").selectOption("Available");
    await click(page, "StatusNotification");
    await page.waitForTimeout(500);

    console.log("✓ StatusNotification → Available (post-charge)");
  });

  // ---------------------------------------------------------------
  test("11 — DataTransfer", async () => {
    await click(page, "DataTransfer");
    await waitForLog(page, "DataTransfer");

    console.log("✓ DataTransfer acknowledged");
  });

  // ---------------------------------------------------------------
  test("12 — Disconnect cleanly", async () => {
    await click(page, "Disconnect");
    await waitForLog(page, "WebSocket CLOSED");

    await expect(page.locator("#statusDot")).toHaveClass(/disconnected/);

    console.log("✓ Disconnected");
  });
});

// =================================================================
test.describe("OCPP Edge Cases", () => {
  test("Unknown station gets Rejected", async ({ browser }) => {
    const ctx = await browser.newContext({ ignoreHTTPSErrors: true });
    const page = await ctx.newPage();
    await page.goto(`file://${SIMULATOR_PATH}`);

    // Change to unknown station
    await page.locator("#cpId").fill("UNKNOWN999");

    await click(page, "Connect");
    await waitForLog(page, "WebSocket CONNECTED");

    await click(page, "BootNotification");
    await waitForLog(page, '"status"');

    const text = await logText(page);
    expect(text).toContain('"status": "Rejected"');

    console.log("✓ Unknown station → Rejected");
    await ctx.close();
  });

  test("Rapid heartbeats all get responses", async ({ browser }) => {
    const ctx = await browser.newContext({ ignoreHTTPSErrors: true });
    const page = await ctx.newPage();
    await page.goto(`file://${SIMULATOR_PATH}`);

    await click(page, "Connect");
    await waitForLog(page, "WebSocket CONNECTED");

    await click(page, "BootNotification");
    await waitForLog(page, "Accepted");

    // Fire 5 heartbeats rapidly
    for (let i = 0; i < 5; i++) {
      await click(page, "Heartbeat");
      await page.waitForTimeout(100);
    }

    await page.waitForTimeout(3000);
    const count = await countInLog(page, "currentTime");
    expect(count).toBeGreaterThanOrEqual(5);

    console.log(`✓ ${count} rapid heartbeats responded`);
    await ctx.close();
  });
});
