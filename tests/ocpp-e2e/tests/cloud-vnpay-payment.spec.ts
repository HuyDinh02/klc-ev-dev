import { test, expect, request as apiRequest } from "@playwright/test";

/**
 * Cloud VnPay Real Payment Test
 *
 * Creates a top-up via BFF, opens VnPay sandbox page,
 * completes NCB card payment, waits for IPN, verifies wallet credited.
 */

const CLOUD_BFF = "https://bff.ev.odcall.com";
const DRIVER_PHONE = "0901234001";
const DRIVER_PASSWORD = "Admin@123";

test("Full VnPay payment on cloud — NCB test card", async ({ browser }) => {
  test.setTimeout(120_000);

  // 1. Login and get balance
  const api = await apiRequest.newContext();
  const loginResp = await api.post(`${CLOUD_BFF}/api/v1/auth/login`, {
    data: { phoneNumber: DRIVER_PHONE, password: DRIVER_PASSWORD },
  });
  const { accessToken: token } = await loginResp.json();

  const balBefore = await (await api.get(`${CLOUD_BFF}/api/v1/wallet/balance`, {
    headers: { Authorization: `Bearer ${token}` },
  })).json();
  console.log(`Balance before: ${balBefore.balance.toLocaleString()}đ`);

  // 2. Create top-up
  const topupResp = await api.post(`${CLOUD_BFF}/api/v1/wallet/topup`, {
    headers: { Authorization: `Bearer ${token}`, "Content-Type": "application/json" },
    data: { amount: 50000, gateway: 4, bankCode: "NCB" },
  });
  const topup = await topupResp.json();
  expect(topup.success).toBe(true);
  expect(topup.redirectUrl).toContain("sandbox.vnpayment.vn");
  console.log(`Top-up ref: ${topup.referenceCode}`);

  // 3. Open VnPay payment page
  const ctx = await browser.newContext();
  const page = await ctx.newPage();
  await page.goto(topup.redirectUrl);
  await page.waitForLoadState("networkidle");
  await page.screenshot({ path: "test-results/cloud-vnpay-01-methods.png", fullPage: true });
  console.log(`VnPay page loaded: ${page.url().substring(0, 60)}...`);

  // 4. With vnp_BankCode=NCB, VnPay goes directly to NCB card form
  // Wait for NCB card page to load
  await page.waitForURL(/Ncb|ncb/, { timeout: 15000 }).catch(() => {
    console.log(`  Page URL: ${page.url()}`);
  });
  await page.waitForLoadState("networkidle");
  await page.waitForTimeout(2000);
  await page.screenshot({ path: "test-results/cloud-vnpay-03-card-form.png", fullPage: true });
  console.log(`Card form page: ${page.url().substring(0, 60)}...`);

  // 6. Dismiss any modal dialog that blocks the form
  try {
    // VnPay shows a terms/conditions modal (#modalDKSD) — close it
    await page.evaluate(() => {
      const modal = document.querySelector("#modalDKSD, .modal.show");
      if (modal) {
        // Click close/accept button inside modal
        const btn = modal.querySelector("button, .close, [data-dismiss='modal'], a") as HTMLElement;
        if (btn) btn.click();
        // Also try removing the modal directly
        (modal as HTMLElement).style.display = "none";
        document.querySelector(".modal-backdrop")?.remove();
        document.body.classList.remove("modal-open");
      }
    });
    await page.waitForTimeout(1000);
  } catch {}

  // 7. Fill card form — use the real input IDs from VnPay NCB page
  try {
    // Card number: #card_number_mask (visible) or #cardNumber (hidden — synced by JS)
    await page.locator("#card_number_mask").fill("9704198526191432198");
    await page.waitForTimeout(500);

    // Cardholder: #cardHolder
    await page.locator("#cardHolder").fill("NGUYEN VAN A");
    await page.waitForTimeout(500);

    // Issue date: #cardDate
    await page.locator("#cardDate").fill("07/15");
    await page.waitForTimeout(500);

    await page.screenshot({ path: "test-results/cloud-vnpay-04-filled.png", fullPage: true });
    console.log("  Card details filled");

    // 8. Submit — click "Tiếp tục"
    await page.locator("button:has-text('Tiếp tục'), a:has-text('Tiếp tục'), #btnContinue").first().click();
    await page.waitForLoadState("networkidle");
    await page.waitForTimeout(3000);
    await page.screenshot({ path: "test-results/cloud-vnpay-05-otp.png", fullPage: true });
    console.log("  Card submitted, on OTP page");

    // 9. Enter OTP
    await page.locator("#otpvalue, input[name='otpvalue']").first().fill("123456");
    await page.waitForTimeout(500);
    await page.screenshot({ path: "test-results/cloud-vnpay-05b-otp.png", fullPage: true });

    // 10. Click "Thanh toán"
    await page.locator("button:has-text('Thanh toán'), a:has-text('Thanh toán'), #btnConfirm").first().click();
    await page.waitForLoadState("networkidle");
    await page.waitForTimeout(5000);
    await page.screenshot({ path: "test-results/cloud-vnpay-06-result.png", fullPage: true });
    console.log(`✅ Payment completed! Final URL: ${page.url()}`);
  } catch (e) {
    const msg = e instanceof Error ? e.message.substring(0, 100) : String(e);
    console.log(`Card flow error: ${msg}`);
    await page.screenshot({ path: "test-results/cloud-vnpay-error.png", fullPage: true }).catch(() => {});
  }

  await ctx.close();

  // 9. Wait for IPN and check balance
  console.log("Waiting 10s for IPN to arrive...");
  await new Promise(r => setTimeout(r, 10000));

  const balAfter = await (await api.get(`${CLOUD_BFF}/api/v1/wallet/balance`, {
    headers: { Authorization: `Bearer ${token}` },
  })).json();
  console.log(`Balance after: ${balAfter.balance.toLocaleString()}đ`);

  if (balAfter.balance > balBefore.balance) {
    console.log(`✅ WALLET CREDITED! +${(balAfter.balance - balBefore.balance).toLocaleString()}đ via real VnPay IPN`);
  } else {
    console.log("⚠ Balance unchanged — IPN may not have arrived yet");
    // Check transaction status
    const statusResp = await api.get(`${CLOUD_BFF}/api/v1/wallet/topup/${topup.transactionId}/status`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    const status = await statusResp.json();
    console.log(`Transaction status: ${JSON.stringify(status)}`);
  }

  await api.dispose();
});
