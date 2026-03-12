import { test as setup, expect } from "@playwright/test";
import * as path from "path";

const AUTH_FILE = path.join(__dirname, ".auth", "admin.json");

/**
 * Playwright auth setup — logs in once as admin and saves the authenticated
 * browser state to disk so subsequent tests can reuse it without repeating
 * the login flow every time.
 *
 * Usage in playwright.config.ts:
 *   projects: [
 *     { name: "setup", testMatch: /auth\.setup\.ts/ },
 *     { name: "chromium", dependencies: ["setup"], use: { storageState: AUTH_FILE } },
 *   ]
 *
 * For now the crud-flows tests use their own beforeEach login helper so they
 * remain self-contained, but this file is ready to be wired into the config
 * whenever the team wants faster test runs.
 */
setup("authenticate as admin", async ({ page }) => {
  // Navigate to login
  await page.goto("/login");
  await expect(page.getByText("K-Charge")).toBeVisible({ timeout: 15000 });

  // Fill credentials
  await page.locator("#email").fill("admin");
  await page.locator("#password").fill("Admin@123");

  // Submit
  await page.getByRole("button", { name: /sign in/i }).click();

  // Wait for redirect to dashboard
  await page.waitForURL("/", { timeout: 15000 });
  await expect(page.getByText(/dashboard/i)).toBeVisible({ timeout: 10000 });

  // Persist storage state (cookies + localStorage)
  await page.context().storageState({ path: AUTH_FILE });
});
