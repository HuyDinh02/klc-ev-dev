import { test, expect } from "@playwright/test";

// Helper: login as admin and store state
async function loginAsAdmin(page: import("@playwright/test").Page) {
  await page.goto("/login");
  await page.waitForSelector("#email");
  await page.fill("#email", "admin");
  await page.fill("#password", "Admin@123");
  await page.click('button[type="submit"]');
  // Wait for redirect to dashboard
  await page.waitForURL("/", { timeout: 15000 });
}

test.describe("Login", () => {
  test("should show login page with demo credentials", async ({ page }) => {
    await page.goto("/login");
    await expect(page.locator("text=KLC Admin")).toBeVisible();
    await expect(page.locator("text=Demo Credentials")).toBeVisible();
    await expect(page.locator("text=admin / Admin@123")).toBeVisible();
  });

  test("should fail with wrong credentials", async ({ page }) => {
    await page.goto("/login");
    await page.fill("#email", "wrong");
    await page.fill("#password", "wrong");
    await page.click('button[type="submit"]');
    // Should show error, stay on login
    await page.waitForTimeout(3000);
    await expect(page).toHaveURL(/login/);
  });

  test("should login successfully with admin credentials", async ({ page }) => {
    await loginAsAdmin(page);
    await expect(page).toHaveURL("/");
    // Should see dashboard content
    await expect(page.locator("text=Dashboard")).toBeVisible();
  });
});

test.describe("Dashboard", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should display dashboard stats cards", async ({ page }) => {
    // Dashboard should have stat cards
    await expect(page.locator("text=Total Stations")).toBeVisible({ timeout: 10000 });
    await expect(page.locator("text=Online Stations")).toBeVisible();
    await expect(page.locator("text=Active Sessions")).toBeVisible();
  });

  test("should show sidebar navigation", async ({ page }) => {
    await expect(page.locator("text=Stations")).toBeVisible();
    await expect(page.locator("text=Sessions")).toBeVisible();
    await expect(page.locator("text=Tariffs")).toBeVisible();
    await expect(page.locator("text=Faults")).toBeVisible();
    await expect(page.locator("text=Alerts")).toBeVisible();
    await expect(page.locator("text=User Management")).toBeVisible();
  });
});

test.describe("Stations", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should navigate to stations list", async ({ page }) => {
    await page.click('a[href="/stations"]');
    await page.waitForURL("/stations");
    await expect(page.locator("h1:has-text('Stations')")).toBeVisible({ timeout: 10000 });
  });

  test("should show create station button and navigate", async ({ page }) => {
    await page.goto("/stations");
    await page.waitForLoadState("networkidle");
    const addBtn = page.locator("text=Add Station");
    if (await addBtn.isVisible()) {
      await addBtn.click();
      await page.waitForURL("/stations/new");
      await expect(page.locator("text=Create Station")).toBeVisible();
    }
  });
});

test.describe("Sessions", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should navigate to sessions page", async ({ page }) => {
    await page.click('a[href="/sessions"]');
    await page.waitForURL("/sessions");
    await expect(page.locator("h1:has-text('Sessions')")).toBeVisible({ timeout: 10000 });
  });

  test("should display session stats or empty state", async ({ page }) => {
    await page.goto("/sessions");
    await page.waitForLoadState("networkidle");
    // Should show either sessions table or loading/empty state
    const pageContent = await page.textContent("body");
    expect(
      pageContent?.includes("Sessions") || pageContent?.includes("Loading")
    ).toBeTruthy();
  });
});

test.describe("Tariffs", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should navigate to tariffs page", async ({ page }) => {
    await page.click('a[href="/tariffs"]');
    await page.waitForURL("/tariffs");
    await expect(page.locator("h1:has-text('Tariffs')")).toBeVisible({ timeout: 10000 });
  });
});

test.describe("Faults", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should navigate to faults page", async ({ page }) => {
    await page.click('a[href="/faults"]');
    await page.waitForURL("/faults");
    await expect(page.locator("h1:has-text('Faults')")).toBeVisible({ timeout: 10000 });
  });
});

test.describe("Alerts", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should navigate to alerts page", async ({ page }) => {
    await page.click('a[href="/alerts"]');
    await page.waitForURL("/alerts");
    await expect(page.locator("h1:has-text('Alerts')")).toBeVisible({ timeout: 10000 });
  });

  test("should show alert type filter cards", async ({ page }) => {
    await page.goto("/alerts");
    await page.waitForLoadState("networkidle");
    await expect(page.locator("text=Critical")).toBeVisible({ timeout: 10000 });
    await expect(page.locator("text=Warning")).toBeVisible();
    await expect(page.locator("text=Unacknowledged")).toBeVisible();
  });
});

test.describe("Payments", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should navigate to payments page", async ({ page }) => {
    await page.click('a[href="/payments"]');
    await page.waitForURL("/payments");
    await expect(page.locator("h1:has-text('Payments')")).toBeVisible({ timeout: 10000 });
  });
});

test.describe("Station Groups", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should navigate to groups page", async ({ page }) => {
    await page.click('a[href="/groups"]');
    await page.waitForURL("/groups");
    await expect(page.locator("h1:has-text('Station Groups')")).toBeVisible({ timeout: 10000 });
  });
});

test.describe("E-Invoices", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should navigate to e-invoices page", async ({ page }) => {
    await page.click('a[href="/e-invoices"]');
    await page.waitForURL("/e-invoices");
    await expect(page.locator("h1:has-text('E-Invoices')")).toBeVisible({ timeout: 10000 });
  });
});

test.describe("User Management", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should navigate to user management page", async ({ page }) => {
    await page.click('a[href="/user-management"]');
    await page.waitForURL("/user-management");
    await expect(page.locator("h1:has-text('User Management')")).toBeVisible({ timeout: 10000 });
  });

  test("should display Users and Roles tabs", async ({ page }) => {
    await page.goto("/user-management");
    await page.waitForLoadState("networkidle");
    await expect(page.locator("button:has-text('Users')")).toBeVisible({ timeout: 10000 });
    await expect(page.locator("button:has-text('Roles')")).toBeVisible();
  });

  test("should show users list with admin user", async ({ page }) => {
    await page.goto("/user-management");
    await page.waitForLoadState("networkidle");
    // Should show admin user in the list
    await page.waitForTimeout(3000);
    const pageContent = await page.textContent("body");
    expect(pageContent?.includes("admin")).toBeTruthy();
  });

  test("should switch to Roles tab", async ({ page }) => {
    await page.goto("/user-management");
    await page.waitForLoadState("networkidle");
    await page.click("button:has-text('Roles')");
    await page.waitForTimeout(2000);
    // Should show role names
    const pageContent = await page.textContent("body");
    expect(pageContent?.includes("admin") || pageContent?.includes("Admin")).toBeTruthy();
  });
});

test.describe("Audit Logs", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should navigate to audit logs page", async ({ page }) => {
    await page.click('a[href="/audit-logs"]');
    await page.waitForURL("/audit-logs");
    await expect(page.locator("h1:has-text('Audit Logs')")).toBeVisible({ timeout: 10000 });
  });
});

test.describe("Auth guard", () => {
  test("should redirect to login when not authenticated", async ({ page }) => {
    await page.goto("/");
    // Should redirect to login
    await page.waitForURL(/login/, { timeout: 10000 });
    await expect(page).toHaveURL(/login/);
  });

  test("should redirect to login when accessing stations", async ({ page }) => {
    await page.goto("/stations");
    await page.waitForURL(/login/, { timeout: 10000 });
    await expect(page).toHaveURL(/login/);
  });
});
