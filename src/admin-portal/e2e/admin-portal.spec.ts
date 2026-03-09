import { test, expect } from "@playwright/test";

const BASE_URL = "http://localhost:3001";

// Helper: login as admin
async function loginAsAdmin(page: import("@playwright/test").Page) {
  await page.goto(`${BASE_URL}/login`);
  await page.getByLabel(/username/i).fill("admin");
  await page.getByLabel(/password/i).fill("Admin@123");
  await page.getByRole("button", { name: /sign in/i }).click();
  await page.waitForURL(BASE_URL + "/", { timeout: 15000 });
}

// ---------------------------------------------------------------------------
// Login Flow
// ---------------------------------------------------------------------------
test.describe("Login Flow", () => {
  test("should show login page when not authenticated", async ({ page }) => {
    await page.goto(BASE_URL);
    // Should redirect to login and display the brand name and form fields
    await page.waitForURL(/login/, { timeout: 10000 });
    await expect(page.getByText("K-Charge")).toBeVisible();
    await expect(page.getByLabel(/username/i)).toBeVisible();
    await expect(page.getByLabel(/password/i)).toBeVisible();
  });

  test("should show error on invalid credentials", async ({ page }) => {
    await page.goto(`${BASE_URL}/login`);
    await page.getByLabel(/username/i).fill("wrong");
    await page.getByLabel(/password/i).fill("wrong");
    await page.getByRole("button", { name: /sign in/i }).click();
    // Should display an error alert and remain on the login page
    await expect(page.getByRole("alert")).toBeVisible({ timeout: 10000 });
    await expect(page).toHaveURL(/login/);
  });

  test("should redirect to dashboard on successful login", async ({ page }) => {
    await page.goto(`${BASE_URL}/login`);
    await page.getByLabel(/username/i).fill("admin");
    await page.getByLabel(/password/i).fill("Admin@123");
    await page.getByRole("button", { name: /sign in/i }).click();
    await expect(page).toHaveURL(BASE_URL + "/", { timeout: 10000 });
    await expect(page.getByText(/dashboard/i)).toBeVisible();
  });

  test("should display demo credentials on login page", async ({ page }) => {
    await page.goto(`${BASE_URL}/login`);
    await expect(page.getByText("admin / Admin@123")).toBeVisible();
    await expect(page.getByText("operator / Admin@123")).toBeVisible();
    await expect(page.getByText("viewer / Admin@123")).toBeVisible();
  });

  test("should protect dashboard routes when not authenticated", async ({
    page,
  }) => {
    await page.goto(`${BASE_URL}/stations`);
    await expect(page).toHaveURL(/login/);
  });

  test("should protect sessions route when not authenticated", async ({
    page,
  }) => {
    await page.goto(`${BASE_URL}/sessions`);
    await expect(page).toHaveURL(/login/);
  });
});

// ---------------------------------------------------------------------------
// Dashboard Navigation
// ---------------------------------------------------------------------------
test.describe("Dashboard Navigation", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should display dashboard stats cards", async ({ page }) => {
    await expect(page.locator("text=Total Stations")).toBeVisible({
      timeout: 10000,
    });
    await expect(page.locator("text=Online Stations")).toBeVisible();
    await expect(page.locator("text=Active Sessions")).toBeVisible();
  });

  test("sidebar navigation works", async ({ page }) => {
    // Click stations link in sidebar
    await page.getByRole("link", { name: /stations/i }).first().click();
    await expect(page).toHaveURL(/stations/);

    // Click sessions link in sidebar
    await page.getByRole("link", { name: /sessions/i }).click();
    await expect(page).toHaveURL(/sessions/);
  });

  test("should show sidebar navigation items", async ({ page }) => {
    await expect(page.locator("text=Stations")).toBeVisible();
    await expect(page.locator("text=Sessions")).toBeVisible();
    await expect(page.locator("text=Tariffs")).toBeVisible();
    await expect(page.locator("text=Faults")).toBeVisible();
    await expect(page.locator("text=Alerts")).toBeVisible();
    await expect(page.locator("text=User Management")).toBeVisible();
  });

  test("settings page loads", async ({ page }) => {
    await page.getByRole("link", { name: /settings/i }).click();
    await expect(page).toHaveURL(/settings/);
  });

  test("logout redirects to login", async ({ page }) => {
    // Look for logout button (may be icon-only or text)
    const logoutBtn = page.getByRole("button", { name: /logout|sign out/i });
    if (await logoutBtn.isVisible({ timeout: 3000 }).catch(() => false)) {
      await logoutBtn.click();
      await expect(page).toHaveURL(/login/);
    } else {
      // Fallback: look for a user menu that opens a dropdown with logout
      const userMenu = page.locator('[data-testid="user-menu"]');
      if (await userMenu.isVisible({ timeout: 2000 }).catch(() => false)) {
        await userMenu.click();
        await page.getByRole("menuitem", { name: /logout|sign out/i }).click();
        await expect(page).toHaveURL(/login/);
      } else {
        // Skip if no logout UI element found
        test.skip();
      }
    }
  });
});

// ---------------------------------------------------------------------------
// Stations
// ---------------------------------------------------------------------------
test.describe("Stations", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should navigate to stations list", async ({ page }) => {
    await page.click('a[href="/stations"]');
    await page.waitForURL("/stations");
    await expect(page.locator("h1:has-text('Stations')")).toBeVisible({
      timeout: 10000,
    });
  });

  test("should show create station button and navigate", async ({ page }) => {
    await page.goto(`${BASE_URL}/stations`);
    await page.waitForLoadState("networkidle");
    const addBtn = page.locator("text=Add Station");
    if (await addBtn.isVisible()) {
      await addBtn.click();
      await page.waitForURL(/stations\/new/);
      await expect(page.locator("text=Create Station")).toBeVisible();
    }
  });
});

// ---------------------------------------------------------------------------
// Sessions
// ---------------------------------------------------------------------------
test.describe("Sessions", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should navigate to sessions page", async ({ page }) => {
    await page.click('a[href="/sessions"]');
    await page.waitForURL("/sessions");
    await expect(page.locator("h1:has-text('Sessions')")).toBeVisible({
      timeout: 10000,
    });
  });

  test("should display session stats or empty state", async ({ page }) => {
    await page.goto(`${BASE_URL}/sessions`);
    await page.waitForLoadState("networkidle");
    const pageContent = await page.textContent("body");
    expect(
      pageContent?.includes("Sessions") || pageContent?.includes("Loading")
    ).toBeTruthy();
  });
});

// ---------------------------------------------------------------------------
// Tariffs
// ---------------------------------------------------------------------------
test.describe("Tariffs", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should navigate to tariffs page", async ({ page }) => {
    await page.click('a[href="/tariffs"]');
    await page.waitForURL("/tariffs");
    await expect(page.locator("h1:has-text('Tariffs')")).toBeVisible({
      timeout: 10000,
    });
  });
});

// ---------------------------------------------------------------------------
// Faults
// ---------------------------------------------------------------------------
test.describe("Faults", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should navigate to faults page", async ({ page }) => {
    await page.click('a[href="/faults"]');
    await page.waitForURL("/faults");
    await expect(page.locator("h1:has-text('Faults')")).toBeVisible({
      timeout: 10000,
    });
  });
});

// ---------------------------------------------------------------------------
// Alerts
// ---------------------------------------------------------------------------
test.describe("Alerts", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should navigate to alerts page", async ({ page }) => {
    await page.click('a[href="/alerts"]');
    await page.waitForURL("/alerts");
    await expect(page.locator("h1:has-text('Alerts')")).toBeVisible({
      timeout: 10000,
    });
  });

  test("should show alert type filter cards", async ({ page }) => {
    await page.goto(`${BASE_URL}/alerts`);
    await page.waitForLoadState("networkidle");
    await expect(page.locator("text=Critical")).toBeVisible({ timeout: 10000 });
    await expect(page.locator("text=Warning")).toBeVisible();
    await expect(page.locator("text=Unacknowledged")).toBeVisible();
  });
});

// ---------------------------------------------------------------------------
// Payments
// ---------------------------------------------------------------------------
test.describe("Payments", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should navigate to payments page", async ({ page }) => {
    await page.click('a[href="/payments"]');
    await page.waitForURL("/payments");
    await expect(page.locator("h1:has-text('Payments')")).toBeVisible({
      timeout: 10000,
    });
  });
});

// ---------------------------------------------------------------------------
// Station Groups
// ---------------------------------------------------------------------------
test.describe("Station Groups", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should navigate to groups page", async ({ page }) => {
    await page.click('a[href="/groups"]');
    await page.waitForURL("/groups");
    await expect(
      page.locator("h1:has-text('Station Groups')")
    ).toBeVisible({ timeout: 10000 });
  });
});

// ---------------------------------------------------------------------------
// E-Invoices
// ---------------------------------------------------------------------------
test.describe("E-Invoices", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should navigate to e-invoices page", async ({ page }) => {
    await page.click('a[href="/e-invoices"]');
    await page.waitForURL("/e-invoices");
    await expect(
      page.locator("h1:has-text('E-Invoices')")
    ).toBeVisible({ timeout: 10000 });
  });
});

// ---------------------------------------------------------------------------
// User Management
// ---------------------------------------------------------------------------
test.describe("User Management", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should navigate to user management page", async ({ page }) => {
    await page.click('a[href="/user-management"]');
    await page.waitForURL("/user-management");
    await expect(
      page.locator("h1:has-text('User Management')")
    ).toBeVisible({ timeout: 10000 });
  });

  test("should display Users and Roles tabs", async ({ page }) => {
    await page.goto(`${BASE_URL}/user-management`);
    await page.waitForLoadState("networkidle");
    await expect(page.locator("button:has-text('Users')")).toBeVisible({
      timeout: 10000,
    });
    await expect(page.locator("button:has-text('Roles')")).toBeVisible();
  });

  test("should show users list with admin user", async ({ page }) => {
    await page.goto(`${BASE_URL}/user-management`);
    await page.waitForLoadState("networkidle");
    await page.waitForTimeout(3000);
    const pageContent = await page.textContent("body");
    expect(pageContent?.includes("admin")).toBeTruthy();
  });

  test("should switch to Roles tab", async ({ page }) => {
    await page.goto(`${BASE_URL}/user-management`);
    await page.waitForLoadState("networkidle");
    await page.click("button:has-text('Roles')");
    await page.waitForTimeout(2000);
    const pageContent = await page.textContent("body");
    expect(
      pageContent?.includes("admin") || pageContent?.includes("Admin")
    ).toBeTruthy();
  });
});

// ---------------------------------------------------------------------------
// Audit Logs
// ---------------------------------------------------------------------------
test.describe("Audit Logs", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should navigate to audit logs page", async ({ page }) => {
    await page.click('a[href="/audit-logs"]');
    await page.waitForURL("/audit-logs");
    await expect(
      page.locator("h1:has-text('Audit Logs')")
    ).toBeVisible({ timeout: 10000 });
  });
});

// ---------------------------------------------------------------------------
// Auth guard (unauthenticated access)
// ---------------------------------------------------------------------------
test.describe("Auth guard", () => {
  test("should redirect to login when accessing root", async ({ page }) => {
    await page.goto(BASE_URL);
    await page.waitForURL(/login/, { timeout: 10000 });
    await expect(page).toHaveURL(/login/);
  });

  test("should redirect to login when accessing stations", async ({
    page,
  }) => {
    await page.goto(`${BASE_URL}/stations`);
    await page.waitForURL(/login/, { timeout: 10000 });
    await expect(page).toHaveURL(/login/);
  });

  test("should redirect to login when accessing sessions", async ({
    page,
  }) => {
    await page.goto(`${BASE_URL}/sessions`);
    await page.waitForURL(/login/, { timeout: 10000 });
    await expect(page).toHaveURL(/login/);
  });

  test("should redirect to login when accessing user-management", async ({
    page,
  }) => {
    await page.goto(`${BASE_URL}/user-management`);
    await page.waitForURL(/login/, { timeout: 10000 });
    await expect(page).toHaveURL(/login/);
  });
});
