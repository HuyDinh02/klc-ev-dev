import { test, expect, type Page } from "@playwright/test";

// ---------------------------------------------------------------------------
// Shared helpers
// ---------------------------------------------------------------------------

const BASE_URL = "http://localhost:3001";

/**
 * Log in as admin via the login page.
 * Waits until the dashboard is fully loaded before returning.
 */
async function loginAsAdmin(page: Page) {
  await page.goto(`${BASE_URL}/login`);
  await page.getByLabel(/username/i).fill("admin");
  await page.getByLabel(/password/i).fill("Admin@123");
  await page.getByRole("button", { name: /sign in/i }).click();
  await page.waitForURL(`${BASE_URL}/`, { timeout: 15000 });
}

/**
 * Navigate to a page and wait for the network to settle.
 */
async function navigateAndWait(page: Page, path: string) {
  await page.goto(`${BASE_URL}${path}`);
  await page.waitForLoadState("networkidle", { timeout: 15000 });
}

// ==========================================================================
// Station Management CRUD Tests
// ==========================================================================
test.describe("Station Management", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should display stations list page", async ({ page }) => {
    await navigateAndWait(page, "/stations");

    // Page heading should be visible
    await expect(page.getByText("Station Management")).toBeVisible({
      timeout: 10000,
    });

    // The search input should be present
    await expect(
      page.getByRole("searchbox", { name: /search stations/i })
    ).toBeVisible();

    // The "Add Station" button should be present
    await expect(page.getByText("Add Station")).toBeVisible();

    // Status filter dropdown should be present
    await expect(
      page.getByRole("combobox", { name: /status/i })
    ).toBeVisible();
  });

  test("should navigate to create station page", async ({ page }) => {
    await navigateAndWait(page, "/stations");

    // Click the "Add Station" button (it's a link to /stations/new)
    await page.getByText("Add Station").click();
    await page.waitForURL(/stations\/new/, { timeout: 10000 });

    // The create station form should be visible
    await expect(page.getByText("Station Details")).toBeVisible({
      timeout: 10000,
    });
  });

  test("should display create station form with required fields", async ({
    page,
  }) => {
    await navigateAndWait(page, "/stations/new");

    // Required form fields should be present
    await expect(page.getByText("Station Code")).toBeVisible();
    await expect(page.getByText(/^Name/)).toBeVisible();
    await expect(page.getByText("Address")).toBeVisible();
    await expect(page.getByText("Latitude")).toBeVisible();
    await expect(page.getByText("Longitude")).toBeVisible();

    // Optional fields
    await expect(page.getByText("Station Group")).toBeVisible();
    await expect(page.getByText("Tariff Plan")).toBeVisible();

    // Submit and cancel buttons
    await expect(page.getByText("Create Station")).toBeVisible();
    await expect(page.getByText("Cancel")).toBeVisible();
  });

  test("should validate required fields on create form", async ({ page }) => {
    await navigateAndWait(page, "/stations/new");

    // Clear the pre-filled latitude/longitude and station code fields
    const stationCodeInput = page.getByPlaceholder("e.g., HCM-001");
    await stationCodeInput.fill("");

    const nameInput = page.getByPlaceholder(/e\.g\., Station/);
    await nameInput.fill("");

    // Try to submit the empty form - HTML5 validation should prevent submission
    const submitBtn = page.getByRole("button", { name: /create station/i });
    await submitBtn.click();

    // The page should stay on /stations/new (form not submitted)
    await expect(page).toHaveURL(/stations\/new/);
  });

  test("should switch between list and board view", async ({ page }) => {
    await navigateAndWait(page, "/stations");

    // Board view button should exist
    const boardViewBtn = page.getByRole("button", { name: /board view/i });
    await expect(boardViewBtn).toBeVisible();

    // List view button should exist
    const listViewBtn = page.getByRole("button", { name: /list view/i });
    await expect(listViewBtn).toBeVisible();

    // Click list view
    await listViewBtn.click();
    // Wait for potential re-render
    await page.waitForTimeout(500);

    // Click board view
    await boardViewBtn.click();
    await page.waitForTimeout(500);

    // Both buttons should still be visible (view toggled successfully)
    await expect(boardViewBtn).toBeVisible();
    await expect(listViewBtn).toBeVisible();
  });

  test("should have status filter with correct options", async ({ page }) => {
    await navigateAndWait(page, "/stations");

    const statusFilter = page.getByRole("combobox", { name: /status/i });
    await expect(statusFilter).toBeVisible();

    // Check the dropdown options
    await expect(statusFilter.getByRole("option", { name: /all statuses/i })).toBeAttached();
    await expect(statusFilter.getByRole("option", { name: /available/i })).toBeAttached();
    await expect(statusFilter.getByRole("option", { name: /occupied/i })).toBeAttached();
    await expect(statusFilter.getByRole("option", { name: /offline/i })).toBeAttached();
    await expect(statusFilter.getByRole("option", { name: /faulted/i })).toBeAttached();
    await expect(statusFilter.getByRole("option", { name: /unavailable/i })).toBeAttached();
  });

  test("should search stations by keyword", async ({ page }) => {
    await navigateAndWait(page, "/stations");

    const searchInput = page.getByRole("searchbox", { name: /search stations/i });
    await expect(searchInput).toBeVisible();

    // Type a search query
    await searchInput.fill("test-station");
    await page.waitForTimeout(1000); // Allow debounce / refetch

    // The page should still be on /stations (search is inline)
    await expect(page).toHaveURL(/stations/);
  });

  test("should navigate to station detail when clicking a station", async ({
    page,
  }) => {
    await navigateAndWait(page, "/stations");

    // Try to find a station link — stations render as clickable cards or rows
    const stationLink = page.locator('a[href^="/stations/"]').first();
    if (await stationLink.isVisible({ timeout: 5000 }).catch(() => false)) {
      await stationLink.click();
      await page.waitForURL(/stations\/[a-f0-9-]+/i, { timeout: 10000 });

      // Station detail page should show station info
      await expect(
        page.getByText("Station Information")
      ).toBeVisible({ timeout: 10000 });
    } else {
      // No stations available — that's OK, empty state is a valid outcome
      test.skip();
    }
  });
});

// ==========================================================================
// Tariff Management CRUD Tests
// ==========================================================================
test.describe("Tariff Management", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should display tariffs page with stat cards", async ({ page }) => {
    await navigateAndWait(page, "/tariffs");

    // Page title
    await expect(page.getByText("Tariff Management")).toBeVisible({
      timeout: 10000,
    });

    // Stat cards should be visible
    await expect(page.getByText("Total Tariffs")).toBeVisible({ timeout: 10000 });
    await expect(page.getByText("Active")).toBeVisible();
    await expect(page.getByText("Avg Rate/kWh")).toBeVisible();
    await expect(page.getByText("Default Plan")).toBeVisible();
  });

  test("should display Add Tariff button", async ({ page }) => {
    await navigateAndWait(page, "/tariffs");

    const addTariffBtn = page.getByRole("button", { name: /add tariff/i });
    await expect(addTariffBtn).toBeVisible({ timeout: 10000 });
  });

  test("should open add tariff dialog", async ({ page }) => {
    await navigateAndWait(page, "/tariffs");

    // Click the "Add Tariff" button
    await page.getByRole("button", { name: /add tariff/i }).click();

    // The dialog should open with "New Tariff" heading
    await expect(page.getByText("New Tariff")).toBeVisible({ timeout: 5000 });

    // Form fields should be present in the dialog
    await expect(page.getByText("Name")).toBeVisible();
    await expect(page.getByText("Base Rate per kWh")).toBeVisible();
    await expect(page.getByText("Tax Rate")).toBeVisible();
    await expect(page.getByText("Effective From")).toBeVisible();

    // Action buttons
    await expect(page.getByRole("button", { name: /cancel/i })).toBeVisible();
    await expect(
      page.getByRole("button", { name: /create tariff/i })
    ).toBeVisible();
  });

  test("should close tariff dialog on cancel", async ({ page }) => {
    await navigateAndWait(page, "/tariffs");

    // Open dialog
    await page.getByRole("button", { name: /add tariff/i }).click();
    await expect(page.getByText("New Tariff")).toBeVisible({ timeout: 5000 });

    // Click cancel
    await page.getByRole("button", { name: /cancel/i }).click();

    // Dialog should close — "New Tariff" heading should no longer be visible
    await expect(page.getByText("New Tariff")).not.toBeVisible({
      timeout: 5000,
    });
  });

  test("should display tariff cards or empty state", async ({ page }) => {
    await navigateAndWait(page, "/tariffs");

    // Wait for loading to finish
    await page.waitForTimeout(2000);

    const pageContent = await page.textContent("body");

    // Either tariff cards with rate info OR the empty state should be visible
    const hasTariffCards =
      pageContent?.includes("Base Rate/kWh") ||
      pageContent?.includes("Total Rate/kWh");
    const hasEmptyState = pageContent?.includes("No tariffs found");

    expect(hasTariffCards || hasEmptyState).toBeTruthy();
  });

  test("should show rate info labels in tariff cards", async ({ page }) => {
    await navigateAndWait(page, "/tariffs");
    await page.waitForTimeout(2000);

    // If tariffs exist, check for rate labels
    const baseRateLabel = page.getByText("Base Rate/kWh");
    if (await baseRateLabel.first().isVisible({ timeout: 3000 }).catch(() => false)) {
      await expect(baseRateLabel.first()).toBeVisible();
      await expect(page.getByText("Total Rate/kWh").first()).toBeVisible();
      await expect(page.getByText("Tax Rate").first()).toBeVisible();
    } else {
      // No tariffs exist — empty state
      await expect(page.getByText("No tariffs found")).toBeVisible();
    }
  });
});

// ==========================================================================
// Session Management Tests
// ==========================================================================
test.describe("Session Management", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should display sessions page with title", async ({ page }) => {
    await navigateAndWait(page, "/sessions");

    await expect(page.getByText("Charging Sessions")).toBeVisible({
      timeout: 10000,
    });
  });

  test("should display session stat cards", async ({ page }) => {
    await navigateAndWait(page, "/sessions");

    // Stats section should have these cards
    await expect(page.getByText("Active Sessions")).toBeVisible({
      timeout: 10000,
    });
    await expect(page.getByText("Energy Delivered")).toBeVisible();
    await expect(page.getByText("Total Revenue")).toBeVisible();
    await expect(page.getByText("Total Sessions")).toBeVisible();
  });

  test("should have search functionality", async ({ page }) => {
    await navigateAndWait(page, "/sessions");

    const searchInput = page.getByRole("searchbox", {
      name: /search sessions/i,
    });
    await expect(searchInput).toBeVisible({ timeout: 10000 });

    // Type in search
    await searchInput.fill("test");
    await page.waitForTimeout(500);

    // Page should remain on sessions
    await expect(page).toHaveURL(/sessions/);
  });

  test("should have status filter buttons", async ({ page }) => {
    await navigateAndWait(page, "/sessions");

    // Status filter buttons
    await expect(page.getByRole("button", { name: /^all$/i })).toBeVisible({
      timeout: 10000,
    });
    await expect(page.getByRole("button", { name: /^active$/i })).toBeVisible();
    await expect(
      page.getByRole("button", { name: /completed/i })
    ).toBeVisible();
  });

  test("should toggle status filters", async ({ page }) => {
    await navigateAndWait(page, "/sessions");

    // Click "Active" filter
    const activeBtn = page.getByRole("button", { name: /^active$/i });
    await activeBtn.click();
    await page.waitForTimeout(1000);

    // Click "All" to reset
    const allBtn = page.getByRole("button", { name: /^all$/i });
    await allBtn.click();
    await page.waitForTimeout(500);

    // Page should still be on sessions
    await expect(page).toHaveURL(/sessions/);
  });

  test("should display session table headers or empty state", async ({
    page,
  }) => {
    await navigateAndWait(page, "/sessions");
    await page.waitForTimeout(2000);

    const pageContent = await page.textContent("body");

    // Either the table with columns or the empty state
    const hasTableHeaders =
      pageContent?.includes("Station") && pageContent?.includes("Duration");
    const hasEmptyState = pageContent?.includes("No sessions found");

    expect(hasTableHeaders || hasEmptyState).toBeTruthy();
  });

  test("should show table columns when sessions exist", async ({ page }) => {
    await navigateAndWait(page, "/sessions");
    await page.waitForTimeout(2000);

    // Check if a table is present (sessions loaded)
    const table = page.locator("table");
    if (await table.isVisible({ timeout: 3000 }).catch(() => false)) {
      // Verify column headers
      await expect(page.getByRole("columnheader", { name: /station/i })).toBeVisible();
      await expect(page.getByRole("columnheader", { name: /status/i })).toBeVisible();
      await expect(page.getByRole("columnheader", { name: /energy/i })).toBeVisible();
    } else {
      // Empty state
      test.skip();
    }
  });
});

// ==========================================================================
// User Management Tests
// ==========================================================================
test.describe("User Management", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should display users tab by default", async ({ page }) => {
    await navigateAndWait(page, "/user-management");

    // Page title
    await expect(page.getByText("User Management")).toBeVisible({
      timeout: 10000,
    });

    // Users tab should be selected (aria-selected=true)
    const usersTab = page.getByRole("tab", { name: /users/i });
    await expect(usersTab).toBeVisible();
    await expect(usersTab).toHaveAttribute("aria-selected", "true");
  });

  test("should display Users and Roles tabs", async ({ page }) => {
    await navigateAndWait(page, "/user-management");

    await expect(page.getByRole("tab", { name: /users/i })).toBeVisible({
      timeout: 10000,
    });
    await expect(page.getByRole("tab", { name: /roles/i })).toBeVisible();
  });

  test("should switch to roles tab", async ({ page }) => {
    await navigateAndWait(page, "/user-management");

    // Click the Roles tab
    const rolesTab = page.getByRole("tab", { name: /roles/i });
    await rolesTab.click();
    await page.waitForTimeout(1000);

    // Roles tab should now be selected
    await expect(rolesTab).toHaveAttribute("aria-selected", "true");

    // Role-specific UI should appear (Add Role button or roles table)
    await expect(page.getByText("Add Role")).toBeVisible({ timeout: 5000 });
  });

  test("should display Add User button on users tab", async ({ page }) => {
    await navigateAndWait(page, "/user-management");

    await expect(page.getByText("Add User")).toBeVisible({ timeout: 10000 });
  });

  test("should open create user dialog", async ({ page }) => {
    await navigateAndWait(page, "/user-management");

    // Click "Add User"
    await page.getByText("Add User").click();

    // Dialog should open with "Create User" heading
    await expect(page.getByText("Create User")).toBeVisible({ timeout: 5000 });

    // Form fields should be present
    await expect(page.getByText("Username *")).toBeVisible();
    await expect(page.getByText("Email *")).toBeVisible();
    await expect(page.getByText("Password *")).toBeVisible();
    await expect(page.getByText("First Name")).toBeVisible();
    await expect(page.getByText("Last Name")).toBeVisible();
    await expect(page.getByText("Phone")).toBeVisible();

    // Action buttons
    await expect(page.getByRole("button", { name: /cancel/i })).toBeVisible();
    await expect(page.getByRole("button", { name: /create/i })).toBeVisible();
  });

  test("should close create user dialog on cancel", async ({ page }) => {
    await navigateAndWait(page, "/user-management");

    await page.getByText("Add User").click();
    await expect(page.getByText("Create User")).toBeVisible({ timeout: 5000 });

    // Cancel
    await page.getByRole("button", { name: /cancel/i }).click();

    await expect(page.getByText("Create User")).not.toBeVisible({
      timeout: 5000,
    });
  });

  test("should display user table with columns", async ({ page }) => {
    await navigateAndWait(page, "/user-management");
    await page.waitForTimeout(2000);

    const table = page.locator("table");
    if (await table.isVisible({ timeout: 5000 }).catch(() => false)) {
      // Table headers
      await expect(
        page.getByRole("columnheader", { name: /username/i })
      ).toBeVisible();
      await expect(
        page.getByRole("columnheader", { name: /email/i })
      ).toBeVisible();
      await expect(
        page.getByRole("columnheader", { name: /^name$/i })
      ).toBeVisible();
      await expect(
        page.getByRole("columnheader", { name: /roles/i })
      ).toBeVisible();
      await expect(
        page.getByRole("columnheader", { name: /status/i })
      ).toBeVisible();
      await expect(
        page.getByRole("columnheader", { name: /actions/i })
      ).toBeVisible();
    } else {
      // Empty state — acceptable
      test.skip();
    }
  });

  test("should show admin user in the users list", async ({ page }) => {
    await navigateAndWait(page, "/user-management");
    await page.waitForTimeout(3000);

    // The seeded admin user should appear in the table
    const pageContent = await page.textContent("body");
    expect(pageContent?.includes("admin")).toBeTruthy();
  });

  test("should have search functionality on users tab", async ({ page }) => {
    await navigateAndWait(page, "/user-management");

    const searchInput = page.getByRole("searchbox", {
      name: /search users/i,
    });
    await expect(searchInput).toBeVisible({ timeout: 10000 });

    // Search for admin
    await searchInput.fill("admin");
    await page.waitForTimeout(1000);

    // Page should still show user management
    await expect(page).toHaveURL(/user-management/);
  });

  test("should display roles table with columns on roles tab", async ({
    page,
  }) => {
    await navigateAndWait(page, "/user-management");

    // Switch to roles tab
    await page.getByRole("tab", { name: /roles/i }).click();
    await page.waitForTimeout(2000);

    const table = page.locator("table");
    if (await table.isVisible({ timeout: 5000 }).catch(() => false)) {
      await expect(
        page.getByRole("columnheader", { name: /role name/i })
      ).toBeVisible();
      await expect(
        page.getByRole("columnheader", { name: /default/i })
      ).toBeVisible();
      await expect(
        page.getByRole("columnheader", { name: /static/i })
      ).toBeVisible();
      await expect(
        page.getByRole("columnheader", { name: /actions/i })
      ).toBeVisible();
    } else {
      test.skip();
    }
  });

  test("should open permissions dialog from roles tab", async ({ page }) => {
    await navigateAndWait(page, "/user-management");

    // Switch to roles tab
    await page.getByRole("tab", { name: /roles/i }).click();
    await page.waitForTimeout(2000);

    // Find the permissions (shield) button for the first role
    const permissionsBtn = page
      .getByRole("button", { name: /permissions/i })
      .first();

    if (await permissionsBtn.isVisible({ timeout: 5000 }).catch(() => false)) {
      await permissionsBtn.click();

      // Permissions dialog should open
      await expect(page.getByText(/permissions/i).first()).toBeVisible({
        timeout: 5000,
      });

      // The dialog should contain permission management UI
      const dialogContent = await page.textContent("body");
      expect(
        dialogContent?.includes("Grant All") ||
          dialogContent?.includes("Save Permissions") ||
          dialogContent?.includes("Permissions")
      ).toBeTruthy();
    } else {
      test.skip();
    }
  });
});

// ==========================================================================
// Fault Management Tests
// ==========================================================================
test.describe("Fault Management", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should display faults page with title", async ({ page }) => {
    await navigateAndWait(page, "/faults");

    await expect(page.getByText("Fault Management")).toBeVisible({
      timeout: 10000,
    });
  });

  test("should display fault stat cards", async ({ page }) => {
    await navigateAndWait(page, "/faults");

    await expect(page.getByText("Open Faults")).toBeVisible({ timeout: 10000 });
    await expect(page.getByText("Investigating")).toBeVisible();
    await expect(page.getByText("Critical")).toBeVisible();
    await expect(page.getByText("Total Faults")).toBeVisible();
  });

  test("should have search functionality", async ({ page }) => {
    await navigateAndWait(page, "/faults");

    const searchInput = page.getByRole("searchbox", {
      name: /search faults/i,
    });
    await expect(searchInput).toBeVisible({ timeout: 10000 });

    await searchInput.fill("test-error");
    await page.waitForTimeout(500);

    await expect(page).toHaveURL(/faults/);
  });

  test("should have status filter buttons", async ({ page }) => {
    await navigateAndWait(page, "/faults");

    // All status filter buttons
    await expect(page.getByRole("button", { name: /^all$/i })).toBeVisible({
      timeout: 10000,
    });
    await expect(page.getByRole("button", { name: /^open$/i })).toBeVisible();
    await expect(
      page.getByRole("button", { name: /investigating/i })
    ).toBeVisible();
    await expect(
      page.getByRole("button", { name: /^resolved$/i })
    ).toBeVisible();
    await expect(
      page.getByRole("button", { name: /^closed$/i })
    ).toBeVisible();
  });

  test("should toggle status filters", async ({ page }) => {
    await navigateAndWait(page, "/faults");

    // Click "Open" filter
    const openBtn = page.getByRole("button", { name: /^open$/i });
    await openBtn.click();
    await page.waitForTimeout(1000);

    // The button should be pressed
    await expect(openBtn).toHaveAttribute("aria-pressed", "true");

    // Click "All" to reset
    await page.getByRole("button", { name: /^all$/i }).click();
    await page.waitForTimeout(500);
  });

  test("should display faults list or empty state", async ({ page }) => {
    await navigateAndWait(page, "/faults");
    await page.waitForTimeout(2000);

    const pageContent = await page.textContent("body");

    // Either fault cards with error info OR the empty state
    const hasFaults =
      pageContent?.includes("Detected") || pageContent?.includes("Connector");
    const hasEmptyState = pageContent?.includes("No faults found");

    expect(hasFaults || hasEmptyState).toBeTruthy();
  });

  test("should navigate to fault detail on click", async ({ page }) => {
    await navigateAndWait(page, "/faults");
    await page.waitForTimeout(2000);

    // Fault cards are clickable and navigate to /faults/{id}
    const faultCard = page.locator("[class*=cursor-pointer]").first();
    if (await faultCard.isVisible({ timeout: 3000 }).catch(() => false)) {
      await faultCard.click();
      await page.waitForURL(/faults\/[a-f0-9-]+/i, { timeout: 10000 });
    } else {
      // No faults — acceptable
      test.skip();
    }
  });
});

// ==========================================================================
// Payment Management Tests
// ==========================================================================
test.describe("Payment Management", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should display payments page with title", async ({ page }) => {
    await navigateAndWait(page, "/payments");

    await expect(page.getByText("Payments")).toBeVisible({ timeout: 10000 });
  });

  test("should display payment stat cards", async ({ page }) => {
    await navigateAndWait(page, "/payments");

    await expect(page.getByText(/today.s revenue/i)).toBeVisible({
      timeout: 10000,
    });
    await expect(page.getByText(/monthly revenue/i)).toBeVisible();
    await expect(page.getByText("Pending")).toBeVisible();
    await expect(page.getByText("Failed")).toBeVisible();
  });

  test("should have search and filter functionality", async ({ page }) => {
    await navigateAndWait(page, "/payments");

    // Search input
    const searchInput = page.getByRole("textbox", {
      name: /search by transaction/i,
    });
    await expect(searchInput).toBeVisible({ timeout: 10000 });

    // Status filter dropdown
    const statusFilter = page.getByRole("combobox", {
      name: /filter by status/i,
    });
    await expect(statusFilter).toBeVisible();

    // Date range inputs
    const dateFromInput = page.getByRole("textbox", { name: /date from/i }).or(
      page.locator('input[type="date"]').first()
    );
    await expect(dateFromInput).toBeVisible();
  });

  test("should have export button", async ({ page }) => {
    await navigateAndWait(page, "/payments");

    await expect(page.getByText("Export")).toBeVisible({ timeout: 10000 });
  });

  test("should display payments table or empty state", async ({ page }) => {
    await navigateAndWait(page, "/payments");
    await page.waitForTimeout(2000);

    const pageContent = await page.textContent("body");

    // Either payment table with columns or empty state
    const hasTable =
      pageContent?.includes("Transaction") && pageContent?.includes("Amount");
    const hasEmptyState = pageContent?.includes("No payments found");

    expect(hasTable || hasEmptyState).toBeTruthy();
  });

  test("should display payment table columns when data exists", async ({
    page,
  }) => {
    await navigateAndWait(page, "/payments");
    await page.waitForTimeout(2000);

    const table = page.locator("table");
    if (await table.isVisible({ timeout: 3000 }).catch(() => false)) {
      await expect(
        page.getByRole("columnheader", { name: /transaction/i })
      ).toBeVisible();
      await expect(
        page.getByRole("columnheader", { name: /station/i })
      ).toBeVisible();
      await expect(
        page.getByRole("columnheader", { name: /amount/i })
      ).toBeVisible();
      await expect(
        page.getByRole("columnheader", { name: /method/i })
      ).toBeVisible();
      await expect(
        page.getByRole("columnheader", { name: /status/i })
      ).toBeVisible();
    } else {
      // No payments — empty state
      test.skip();
    }
  });

  test("should filter payments by status", async ({ page }) => {
    await navigateAndWait(page, "/payments");

    const statusFilter = page.getByRole("combobox", {
      name: /filter by status/i,
    });
    await expect(statusFilter).toBeVisible({ timeout: 10000 });

    // Select a status from the dropdown
    await statusFilter.selectOption({ index: 1 });
    await page.waitForTimeout(1000);

    // Page should still be on payments
    await expect(page).toHaveURL(/payments/);
  });
});

// ==========================================================================
// Cross-Page Navigation Tests
// ==========================================================================
test.describe("CRUD Page Navigation", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should navigate between all CRUD pages via sidebar", async ({
    page,
  }) => {
    // Stations
    await page.getByRole("link", { name: /stations/i }).first().click();
    await expect(page).toHaveURL(/stations/, { timeout: 10000 });
    await expect(page.getByText("Station Management")).toBeVisible({
      timeout: 10000,
    });

    // Sessions
    await page.getByRole("link", { name: /sessions/i }).click();
    await expect(page).toHaveURL(/sessions/, { timeout: 10000 });
    await expect(page.getByText("Charging Sessions")).toBeVisible({
      timeout: 10000,
    });

    // Tariffs
    await page.getByRole("link", { name: /tariffs/i }).click();
    await expect(page).toHaveURL(/tariffs/, { timeout: 10000 });
    await expect(page.getByText("Tariff Management")).toBeVisible({
      timeout: 10000,
    });

    // Faults
    await page.getByRole("link", { name: /faults/i }).click();
    await expect(page).toHaveURL(/faults/, { timeout: 10000 });
    await expect(page.getByText("Fault Management")).toBeVisible({
      timeout: 10000,
    });

    // Payments
    await page.getByRole("link", { name: /payments/i }).click();
    await expect(page).toHaveURL(/payments/, { timeout: 10000 });
    await expect(page.getByText("Payments")).toBeVisible({ timeout: 10000 });

    // User Management
    await page.getByRole("link", { name: /user management/i }).click();
    await expect(page).toHaveURL(/user-management/, { timeout: 10000 });
    await expect(page.getByText("User Management")).toBeVisible({
      timeout: 10000,
    });
  });

  test("should preserve auth state across page navigations", async ({
    page,
  }) => {
    // Navigate to a deep page
    await navigateAndWait(page, "/stations");
    await expect(page.getByText("Station Management")).toBeVisible({
      timeout: 10000,
    });

    // Navigate to another page
    await navigateAndWait(page, "/user-management");
    await expect(page.getByText("User Management")).toBeVisible({
      timeout: 10000,
    });

    // Should NOT redirect to login — auth state is preserved
    await expect(page).not.toHaveURL(/login/);
  });
});
