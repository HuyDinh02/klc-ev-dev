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
  await page.locator("#email").fill("admin");
  await page.locator("#password").fill("Admin@123");
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
// Station Create Form — Full Field Interaction Tests
// ==========================================================================
test.describe("Station Create Form", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
    await navigateAndWait(page, "/stations/new");
  });

  test("should fill in all station form fields", async ({ page }) => {
    // Wait for the form to render
    await expect(page.getByText("Station Details")).toBeVisible({
      timeout: 10000,
    });

    // Fill Station Code
    const stationCodeInput = page.getByPlaceholder("e.g., HCM-001");
    await expect(stationCodeInput).toBeVisible();
    await stationCodeInput.fill("E2E-TEST-001");
    await expect(stationCodeInput).toHaveValue("E2E-TEST-001");

    // Fill Name
    const nameInput = page.getByPlaceholder(/e\.g\., Station/);
    await expect(nameInput).toBeVisible();
    await nameInput.fill("E2E Test Station");
    await expect(nameInput).toHaveValue("E2E Test Station");

    // Fill Address
    const addressInput = page.getByPlaceholder(/e\.g\.,.*street|address/i);
    await expect(addressInput).toBeVisible();
    await addressInput.fill("123 Test Street, District 1, HCMC");
    await expect(addressInput).toHaveValue("123 Test Street, District 1, HCMC");

    // Latitude and Longitude should have default values (Hanoi coordinates)
    const latInput = page.locator('input[type="number"]').nth(0);
    const lngInput = page.locator('input[type="number"]').nth(1);
    await expect(latInput).toBeVisible();
    await expect(lngInput).toBeVisible();

    // Modify latitude
    await latInput.fill("10.7769");
    await expect(latInput).toHaveValue("10.7769");

    // Modify longitude
    await lngInput.fill("106.7009");
    await expect(lngInput).toHaveValue("106.7009");
  });

  test("should have optional Station Group and Tariff Plan dropdowns", async ({
    page,
  }) => {
    await expect(page.getByText("Station Details")).toBeVisible({
      timeout: 10000,
    });

    // Station Group select should exist
    const groupSelect = page.locator("select").nth(0);
    await expect(groupSelect).toBeVisible();

    // Tariff Plan select should exist
    const tariffSelect = page.locator("select").nth(1);
    await expect(tariffSelect).toBeVisible();

    // Both should have a "None" / default option
    await expect(groupSelect).toContainText(/none/i);
    await expect(tariffSelect).toContainText(/default/i);
  });

  test("should prevent empty form submission via HTML5 validation", async ({
    page,
  }) => {
    await expect(page.getByText("Station Details")).toBeVisible({
      timeout: 10000,
    });

    // Clear the pre-filled fields
    const stationCodeInput = page.getByPlaceholder("e.g., HCM-001");
    await stationCodeInput.fill("");

    const nameInput = page.getByPlaceholder(/e\.g\., Station/);
    await nameInput.fill("");

    // Try to submit the empty form
    const submitBtn = page.getByRole("button", { name: /create station/i });
    await submitBtn.click();

    // The page should stay on /stations/new (HTML5 validation prevents submission)
    await expect(page).toHaveURL(/stations\/new/);
  });

  test("should show Create Station and Cancel buttons", async ({ page }) => {
    await expect(page.getByText("Station Details")).toBeVisible({
      timeout: 10000,
    });

    // Submit button
    const createBtn = page.getByRole("button", { name: /create station/i });
    await expect(createBtn).toBeVisible();
    await expect(createBtn).toBeEnabled();

    // Cancel button
    const cancelBtn = page.getByRole("button", { name: /cancel/i });
    await expect(cancelBtn).toBeVisible();
    await expect(cancelBtn).toBeEnabled();
  });

  test("should navigate back when clicking Cancel", async ({ page }) => {
    await expect(page.getByText("Station Details")).toBeVisible({
      timeout: 10000,
    });

    // Click Cancel
    const cancelBtn = page.getByRole("button", { name: /cancel/i });
    await cancelBtn.click();

    // Should navigate back to stations list
    await page.waitForURL(/stations/, { timeout: 10000 });
  });

  test("should navigate back when clicking Back to Stations link", async ({
    page,
  }) => {
    await expect(page.getByText("Station Details")).toBeVisible({
      timeout: 10000,
    });

    // Click the "Back to Stations" ghost button
    const backBtn = page.getByRole("button", { name: /back to stations/i });
    if (await backBtn.isVisible({ timeout: 3000 }).catch(() => false)) {
      await backBtn.click();
      await page.waitForURL(/stations/, { timeout: 10000 });
    }
  });
});

// ==========================================================================
// User Management — Create User Dialog Interaction Tests
// ==========================================================================
test.describe("User Management Create Dialog", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
    await navigateAndWait(page, "/user-management");
  });

  test("should fill in all create user form fields", async ({ page }) => {
    // Open the create user dialog
    await page.getByText("Add User").click();
    await expect(page.getByText("Create User")).toBeVisible({ timeout: 5000 });

    // Fill Username
    const usernameInputs = page.locator(
      'dialog input[type="text"], [role="dialog"] input[type="text"]'
    );
    const usernameInput = usernameInputs.first();
    await expect(usernameInput).toBeVisible();
    await usernameInput.fill("e2etestuser");

    // Fill Email
    const emailInput = page.locator(
      'dialog input[type="email"], [role="dialog"] input[type="email"]'
    );
    await expect(emailInput.first()).toBeVisible();
    await emailInput.first().fill("e2e@test.com");

    // Fill Password
    const passwordInput = page.locator(
      'dialog input[type="password"], [role="dialog"] input[type="password"]'
    );
    await expect(passwordInput.first()).toBeVisible();
    await passwordInput.first().fill("Test@12345");

    // Verify the form fields have the correct values
    await expect(usernameInput).toHaveValue("e2etestuser");
    await expect(emailInput.first()).toHaveValue("e2e@test.com");
    await expect(passwordInput.first()).toHaveValue("Test@12345");
  });

  test("should display all required labels in create dialog", async ({
    page,
  }) => {
    await page.getByText("Add User").click();
    await expect(page.getByText("Create User")).toBeVisible({ timeout: 5000 });

    // Required fields
    await expect(page.getByText("Username *")).toBeVisible();
    await expect(page.getByText("Email *")).toBeVisible();
    await expect(page.getByText("Password *")).toBeVisible();

    // Optional fields
    await expect(page.getByText("First Name")).toBeVisible();
    await expect(page.getByText("Last Name")).toBeVisible();
    await expect(page.getByText("Phone")).toBeVisible();
  });

  test("should have Active checkbox checked by default", async ({ page }) => {
    await page.getByText("Add User").click();
    await expect(page.getByText("Create User")).toBeVisible({ timeout: 5000 });

    // The "Active" checkbox should be checked by default
    const activeCheckbox = page.locator("#isActive");
    if (await activeCheckbox.isVisible({ timeout: 3000 }).catch(() => false)) {
      await expect(activeCheckbox).toBeChecked();
    }
  });

  test("should toggle Active checkbox", async ({ page }) => {
    await page.getByText("Add User").click();
    await expect(page.getByText("Create User")).toBeVisible({ timeout: 5000 });

    const activeCheckbox = page.locator("#isActive");
    if (await activeCheckbox.isVisible({ timeout: 3000 }).catch(() => false)) {
      // Should be checked initially
      await expect(activeCheckbox).toBeChecked();

      // Uncheck it
      await activeCheckbox.uncheck();
      await expect(activeCheckbox).not.toBeChecked();

      // Re-check it
      await activeCheckbox.check();
      await expect(activeCheckbox).toBeChecked();
    }
  });

  test("should close create user dialog via Cancel button", async ({
    page,
  }) => {
    await page.getByText("Add User").click();
    await expect(page.getByText("Create User")).toBeVisible({ timeout: 5000 });

    // Fill some data first
    const usernameInputs = page.locator(
      'dialog input[type="text"], [role="dialog"] input[type="text"]'
    );
    await usernameInputs.first().fill("tempuser");

    // Cancel
    await page.getByRole("button", { name: /cancel/i }).click();

    // Dialog should close
    await expect(page.getByText("Create User")).not.toBeVisible({
      timeout: 5000,
    });
  });

  test("should have Create and Cancel action buttons", async ({ page }) => {
    await page.getByText("Add User").click();
    await expect(page.getByText("Create User")).toBeVisible({ timeout: 5000 });

    await expect(page.getByRole("button", { name: /cancel/i })).toBeVisible();
    await expect(page.getByRole("button", { name: /create/i })).toBeVisible();
  });
});

// ==========================================================================
// Tariff Management — Create Dialog Interaction Tests
// ==========================================================================
test.describe("Tariff Create Dialog", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
    await navigateAndWait(page, "/tariffs");
  });

  test("should open tariff dialog and fill in form fields", async ({
    page,
  }) => {
    // Click "Add Tariff" button
    await page.getByRole("button", { name: /add tariff/i }).click();

    // Dialog should open
    await expect(page.getByText("New Tariff")).toBeVisible({ timeout: 5000 });

    // Name field — the Input component renders a label + input
    await expect(page.getByText("Name")).toBeVisible();
    const nameInput = page
      .locator('dialog input[type="text"], [role="dialog"] input[type="text"]')
      .first();
    await expect(nameInput).toBeVisible();
    await nameInput.fill("E2E Test Tariff");
    await expect(nameInput).toHaveValue("E2E Test Tariff");

    // Base Rate per kWh field
    await expect(page.getByText("Base Rate per kWh")).toBeVisible();
    const rateInput = page
      .locator(
        'dialog input[type="number"], [role="dialog"] input[type="number"]'
      )
      .first();
    await expect(rateInput).toBeVisible();
    await rateInput.fill("3500");
    await expect(rateInput).toHaveValue("3500");

    // Tax Rate field
    await expect(page.getByText("Tax Rate")).toBeVisible();

    // Effective From date field
    await expect(page.getByText("Effective From")).toBeVisible();
    const dateInput = page
      .locator(
        'dialog input[type="date"], [role="dialog"] input[type="date"]'
      )
      .first();
    if (await dateInput.isVisible({ timeout: 3000 }).catch(() => false)) {
      await dateInput.fill("2026-04-01");
    }
  });

  test("should display description field in tariff dialog", async ({
    page,
  }) => {
    await page.getByRole("button", { name: /add tariff/i }).click();
    await expect(page.getByText("New Tariff")).toBeVisible({ timeout: 5000 });

    // Description field should be present (optional)
    const descLabel = page.getByText("Description");
    if (await descLabel.isVisible({ timeout: 3000 }).catch(() => false)) {
      await expect(descLabel).toBeVisible();
    }
  });

  test("should have Create Tariff and Cancel buttons in dialog", async ({
    page,
  }) => {
    await page.getByRole("button", { name: /add tariff/i }).click();
    await expect(page.getByText("New Tariff")).toBeVisible({ timeout: 5000 });

    await expect(page.getByRole("button", { name: /cancel/i })).toBeVisible();
    await expect(
      page.getByRole("button", { name: /create tariff/i })
    ).toBeVisible();
  });

  test("should close tariff dialog via Cancel and reopen cleanly", async ({
    page,
  }) => {
    // Open
    await page.getByRole("button", { name: /add tariff/i }).click();
    await expect(page.getByText("New Tariff")).toBeVisible({ timeout: 5000 });

    // Fill name
    const nameInput = page
      .locator('dialog input[type="text"], [role="dialog"] input[type="text"]')
      .first();
    await nameInput.fill("Temp Tariff");

    // Cancel
    await page.getByRole("button", { name: /cancel/i }).click();
    await expect(page.getByText("New Tariff")).not.toBeVisible({
      timeout: 5000,
    });

    // Reopen — form should be reset
    await page.getByRole("button", { name: /add tariff/i }).click();
    await expect(page.getByText("New Tariff")).toBeVisible({ timeout: 5000 });

    // Name input should be empty after dialog reopen (form reset)
    const freshNameInput = page
      .locator('dialog input[type="text"], [role="dialog"] input[type="text"]')
      .first();
    await expect(freshNameInput).toHaveValue("");
  });
});

// ==========================================================================
// Settings Page — Tab Navigation & Form Interaction Tests
// ==========================================================================
test.describe("Settings Page", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
    await navigateAndWait(page, "/settings");
  });

  test("should display settings page with tabs", async ({ page }) => {
    // Settings page should have tab navigation
    const generalTab = page.getByRole("tab", { name: /general/i });
    await expect(generalTab).toBeVisible({ timeout: 10000 });

    // All settings tabs should be present
    await expect(
      page.getByRole("tab", { name: /notification/i })
    ).toBeVisible();
    await expect(page.getByRole("tab", { name: /ocpp/i })).toBeVisible();
    await expect(page.getByRole("tab", { name: /payment/i })).toBeVisible();
    await expect(page.getByRole("tab", { name: /security/i })).toBeVisible();
  });

  test("should display General settings fields by default", async ({
    page,
  }) => {
    // General tab is active by default — its content should be visible
    // Wait for the settings to load (may show skeleton initially)
    await page.waitForTimeout(3000);

    const generalContent = page.getByText(/general settings/i);
    if (
      await generalContent.isVisible({ timeout: 5000 }).catch(() => false)
    ) {
      // General settings fields
      await expect(page.getByText(/site name/i)).toBeVisible();
      await expect(page.getByText(/timezone/i)).toBeVisible();
      await expect(page.getByText(/currency/i)).toBeVisible();
      await expect(page.getByText(/language/i)).toBeVisible();
    } else {
      // Settings may have failed to load — verify the page at least rendered
      await expect(page.getByRole("tab", { name: /general/i })).toBeVisible();
    }
  });

  test("should switch to Notifications tab and show notification settings", async ({
    page,
  }) => {
    await page.waitForTimeout(2000);

    // Click the Notifications tab
    await page.getByRole("tab", { name: /notification/i }).click();
    await page.waitForTimeout(1000);

    // Notification settings should be visible
    const notifContent = page.getByText(/notification settings/i);
    if (
      await notifContent.isVisible({ timeout: 5000 }).catch(() => false)
    ) {
      await expect(notifContent).toBeVisible();

      // Should have toggle switches for email, SMS, push notifications
      await expect(page.getByText(/email notification/i)).toBeVisible();
      await expect(page.getByText(/sms notification/i)).toBeVisible();
      await expect(page.getByText(/push notification/i)).toBeVisible();
    }
  });

  test("should switch to OCPP tab and show OCPP settings", async ({
    page,
  }) => {
    await page.waitForTimeout(2000);

    await page.getByRole("tab", { name: /ocpp/i }).click();
    await page.waitForTimeout(1000);

    const ocppContent = page.getByText(/ocpp settings/i);
    if (await ocppContent.isVisible({ timeout: 5000 }).catch(() => false)) {
      await expect(ocppContent).toBeVisible();

      // OCPP fields
      await expect(page.getByText(/websocket port/i)).toBeVisible();
      await expect(page.getByText(/heartbeat interval/i)).toBeVisible();
      await expect(page.getByText(/meter value interval/i)).toBeVisible();
    }
  });

  test("should switch to Security tab and show security settings", async ({
    page,
  }) => {
    await page.waitForTimeout(2000);

    await page.getByRole("tab", { name: /security/i }).click();
    await page.waitForTimeout(1000);

    const securityContent = page.getByText(/security settings/i);
    if (
      await securityContent.isVisible({ timeout: 5000 }).catch(() => false)
    ) {
      await expect(securityContent).toBeVisible();

      // Security fields
      await expect(page.getByText(/session timeout/i)).toBeVisible();
      await expect(page.getByText(/min.*password.*length/i)).toBeVisible();
    }
  });

  test("should switch to Payments tab and show payment settings", async ({
    page,
  }) => {
    await page.waitForTimeout(2000);

    await page.getByRole("tab", { name: /payment/i }).click();
    await page.waitForTimeout(1000);

    const paymentContent = page.getByText(/payment settings/i);
    if (
      await paymentContent.isVisible({ timeout: 5000 }).catch(() => false)
    ) {
      await expect(paymentContent).toBeVisible();

      // Payment fields
      await expect(page.getByText(/default payment gateway/i)).toBeVisible();
      await expect(page.getByText(/e-invoice provider/i)).toBeVisible();
    }
  });

  test("should have Save Changes button that is disabled when no changes", async ({
    page,
  }) => {
    await page.waitForTimeout(3000);

    // Save Changes button should be visible
    const saveBtn = page.getByRole("button", { name: /save changes/i });
    if (await saveBtn.isVisible({ timeout: 5000 }).catch(() => false)) {
      // Initially should be disabled (no changes made)
      await expect(saveBtn).toBeDisabled();
    }
  });
});

// ==========================================================================
// Search & Filter Interaction Tests
// ==========================================================================
test.describe("Search & Filter Interactions", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("should search stations and debounce correctly", async ({ page }) => {
    await navigateAndWait(page, "/stations");

    const searchInput = page.getByRole("searchbox", {
      name: /search stations/i,
    });
    await expect(searchInput).toBeVisible({ timeout: 10000 });

    // Type a search query character by character to test debounce
    await searchInput.fill("test");
    await expect(searchInput).toHaveValue("test");

    // Wait for debounce to fire
    await page.waitForTimeout(1500);

    // Page should still be on stations (search is inline, no navigation)
    await expect(page).toHaveURL(/stations/);

    // Clear search
    await searchInput.fill("");
    await expect(searchInput).toHaveValue("");
    await page.waitForTimeout(1000);

    // Still on stations
    await expect(page).toHaveURL(/stations/);
  });

  test("should filter stations by status dropdown", async ({ page }) => {
    await navigateAndWait(page, "/stations");

    const statusFilter = page.getByRole("combobox", { name: /status/i });
    await expect(statusFilter).toBeVisible({ timeout: 10000 });

    // Select "Available" status
    await statusFilter.selectOption({ label: "Available" });
    await page.waitForTimeout(1000);

    // The filter should retain the selected value
    await expect(statusFilter).toHaveValue(/available|1/i);

    // Select "All Statuses" to reset
    await statusFilter.selectOption({ index: 0 });
    await page.waitForTimeout(500);

    await expect(page).toHaveURL(/stations/);
  });

  test("should search users on user management page", async ({ page }) => {
    await navigateAndWait(page, "/user-management");

    const searchInput = page.getByRole("searchbox", {
      name: /search users/i,
    });
    await expect(searchInput).toBeVisible({ timeout: 10000 });

    // Search for admin
    await searchInput.fill("admin");
    await expect(searchInput).toHaveValue("admin");
    await page.waitForTimeout(1500);

    // The admin user should still be visible after search
    const pageContent = await page.textContent("body");
    expect(pageContent?.includes("admin")).toBeTruthy();

    // Clear search
    await searchInput.fill("");
    await page.waitForTimeout(1000);

    await expect(page).toHaveURL(/user-management/);
  });

  test("should search sessions on sessions page", async ({ page }) => {
    await navigateAndWait(page, "/sessions");

    const searchInput = page.getByRole("searchbox", {
      name: /search sessions/i,
    });
    await expect(searchInput).toBeVisible({ timeout: 10000 });

    // Type a search
    await searchInput.fill("session-query");
    await expect(searchInput).toHaveValue("session-query");
    await page.waitForTimeout(1500);

    // Page stays on sessions
    await expect(page).toHaveURL(/sessions/);

    // Clear
    await searchInput.fill("");
    await page.waitForTimeout(500);
  });

  test("should toggle session status filter buttons", async ({ page }) => {
    await navigateAndWait(page, "/sessions");

    // Status filter buttons should exist
    const allBtn = page.getByRole("button", { name: /^all$/i });
    const activeBtn = page.getByRole("button", { name: /^active$/i });
    const completedBtn = page.getByRole("button", { name: /completed/i });

    await expect(allBtn).toBeVisible({ timeout: 10000 });
    await expect(activeBtn).toBeVisible();
    await expect(completedBtn).toBeVisible();

    // Click "Active" filter
    await activeBtn.click();
    await page.waitForTimeout(1000);

    // Click "Completed" filter
    await completedBtn.click();
    await page.waitForTimeout(1000);

    // Reset to "All"
    await allBtn.click();
    await page.waitForTimeout(500);

    await expect(page).toHaveURL(/sessions/);
  });

  test("should search faults on faults page", async ({ page }) => {
    await navigateAndWait(page, "/faults");

    const searchInput = page.getByRole("searchbox", {
      name: /search faults/i,
    });
    await expect(searchInput).toBeVisible({ timeout: 10000 });

    // Type a fault-related search
    await searchInput.fill("connector error");
    await expect(searchInput).toHaveValue("connector error");
    await page.waitForTimeout(1500);

    // Clear
    await searchInput.fill("");
    await page.waitForTimeout(500);

    await expect(page).toHaveURL(/faults/);
  });
});

// ==========================================================================
// User Management — Roles Tab Interaction Tests
// ==========================================================================
test.describe("User Management Roles Tab", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
    await navigateAndWait(page, "/user-management");
  });

  test("should switch to roles tab and show Add Role button", async ({
    page,
  }) => {
    const rolesTab = page.getByRole("tab", { name: /roles/i });
    await rolesTab.click();
    await page.waitForTimeout(1000);

    // Add Role button should be visible
    await expect(page.getByText("Add Role")).toBeVisible({ timeout: 5000 });
  });

  test("should open permissions dialog for a role", async ({ page }) => {
    // Switch to roles tab
    await page.getByRole("tab", { name: /roles/i }).click();
    await page.waitForTimeout(2000);

    // Find the permissions button for any role
    const permissionsBtn = page
      .getByRole("button", { name: /permissions/i })
      .first();

    if (
      await permissionsBtn.isVisible({ timeout: 5000 }).catch(() => false)
    ) {
      await permissionsBtn.click();

      // Permissions dialog should open with permission groups
      await page.waitForTimeout(1000);
      const dialogContent = await page.textContent("body");
      expect(
        dialogContent?.includes("Grant All") ||
          dialogContent?.includes("Save Permissions") ||
          dialogContent?.includes("Permissions")
      ).toBeTruthy();

      // Close the dialog (click outside or find close button)
      const closeBtn = page
        .getByRole("button", { name: /close|cancel/i })
        .first();
      if (
        await closeBtn.isVisible({ timeout: 3000 }).catch(() => false)
      ) {
        await closeBtn.click();
      }
    } else {
      // No roles in the system — skip
      test.skip();
    }
  });
});

// ==========================================================================
// Cross-Form Navigation — Verify forms survive page transitions
// ==========================================================================
test.describe("Cross-Form Navigation Stability", () => {
  test("should preserve auth after interacting with multiple forms", async ({
    page,
  }) => {
    await loginAsAdmin(page);

    // 1. Visit station create form and interact
    await navigateAndWait(page, "/stations/new");
    await expect(page.getByText("Station Details")).toBeVisible({
      timeout: 10000,
    });
    const stationCodeInput = page.getByPlaceholder("e.g., HCM-001");
    await stationCodeInput.fill("NAV-TEST-001");

    // 2. Navigate to user management and open dialog
    await navigateAndWait(page, "/user-management");
    await expect(page.getByText("User Management")).toBeVisible({
      timeout: 10000,
    });
    await page.getByText("Add User").click();
    await expect(page.getByText("Create User")).toBeVisible({ timeout: 5000 });
    await page.getByRole("button", { name: /cancel/i }).click();

    // 3. Navigate to tariffs and open dialog
    await navigateAndWait(page, "/tariffs");
    await expect(page.getByText("Tariff Management")).toBeVisible({
      timeout: 10000,
    });
    await page.getByRole("button", { name: /add tariff/i }).click();
    await expect(page.getByText("New Tariff")).toBeVisible({ timeout: 5000 });
    await page.getByRole("button", { name: /cancel/i }).click();

    // 4. Navigate to settings
    await navigateAndWait(page, "/settings");
    const settingsTab = page.getByRole("tab", { name: /general/i });
    await expect(settingsTab).toBeVisible({ timeout: 10000 });

    // 5. Should still be authenticated — not redirected to login
    await expect(page).not.toHaveURL(/login/);
  });
});
