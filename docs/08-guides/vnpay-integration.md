# VNPay Payment Integration Guide

> KLC EV Charging — Payment Gateway Integration with VNPAY-QR

## 1. Credentials

### Sandbox (TEST environment)

| Item | Value |
|------|-------|
| Terminal Code (vnp_TmnCode) | `KLCTTE11` |
| Hash Secret | `JRNC2DVZ0U8IQJV1CP2ALSAI8OKLPEQ4` |
| Payment URL | `https://sandbox.vnpayment.vn/paymentv2/vpcpay.html` |
| Query/Refund API | `https://sandbox.vnpayment.vn/merchant_webapi/api/transaction` |
| API Version | `2.1.0` |
| Partner Code | `0319356829` |
| Partner Name | CONG TY CO PHAN PHAT TRIEN NANG LUONG KLC ENERGY |

### Production

| Item | Value |
|------|-------|
| Payment URL | `https://pay.vnpay.vn/paymentv2/vpcpay.html` |
| Query/Refund API | `https://merchant.vnpay.vn/merchant_webapi/api/transaction` |
| TmnCode / HashSecret | Stored in GCP Secret Manager — NEVER in code |

### Configuration

**Admin API** (`src/backend/src/KLC.HttpApi.Host/appsettings.json`):
```json
"Payment": {
  "VnPay": {
    "TmnCode": "KLCTTE11",
    "HashSecret": "JRNC2DVZ0U8IQJV1CP2ALSAI8OKLPEQ4",
    "BaseUrl": "https://sandbox.vnpayment.vn",
    "Version": "2.1.0",
    "QueryApiUrl": "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction"
  }
}
```

**Driver BFF** (`src/backend/src/KLC.Driver.BFF/appsettings.json`): Same config under `Payment:VnPay`.

---

## 2. Payment Flow

### 2.1 Create Payment (Redirect flow)

```
Driver App                  KLC Backend                    VNPay Sandbox
    |                           |                              |
    |-- POST /wallet/topup ---->|                              |
    |                           |-- Build payment URL -------->|
    |                           |   (HMAC-SHA512 signed)       |
    |<-- RedirectUrl -----------|                              |
    |                           |                              |
    |-- Open URL in WebView --->|                              |
    |                           |         VNPay payment page   |
    |                           |<---- IPN GET callback -------|
    |                           |   (server-to-server)         |
    |                           |---- {"RspCode":"00"} ------->|
    |                           |                              |
    |<------ ReturnUrl redirect (browser) ---------------------|
```

### 2.2 Payment URL Parameters

All parameters are sorted alphabetically, URL-encoded, then HMAC-SHA512 signed.

| Parameter | Required | Description |
|-----------|----------|-------------|
| vnp_Amount | Yes | Amount * 100 (50,000 VND = `5000000`) |
| vnp_Command | Yes | Always `pay` |
| vnp_CreateDate | Yes | `yyyyMMddHHmmss` (GMT+7) |
| vnp_CurrCode | Yes | Always `VND` |
| vnp_ExpireDate | Yes | 15 min from creation (`yyyyMMddHHmmss`) |
| vnp_IpAddr | Yes | Customer real IP address |
| vnp_Locale | Yes | `vn` (Vietnamese) or `en` |
| vnp_OrderInfo | Yes | Description — **no Vietnamese diacritics, no special chars** |
| vnp_OrderType | Yes | `topup` for wallet, `billpayment` for session |
| vnp_ReturnUrl | Yes | Browser redirect URL after payment |
| vnp_TmnCode | Yes | Terminal code |
| vnp_TxnRef | Yes | Unique reference — **unique per day** |
| vnp_Version | Yes | `2.1.0` |
| vnp_BankCode | No | Omit to let user choose at VNPay gateway |
| vnp_SecureHash | Yes | HMAC-SHA512 signature (appended, not in hash input) |

### 2.3 Signature Algorithm (Payment URL)

```
1. Collect all vnp_* params (excluding vnp_SecureHash)
2. Sort alphabetically by key (StringComparer.Ordinal)
3. Build: urlEncode(key1)=urlEncode(val1)&urlEncode(key2)=urlEncode(val2)&...
4. secureHash = HMAC-SHA512(hashSecret, queryString).ToHex().ToLower()
5. Final URL: baseUrl/paymentv2/vpcpay.html?{queryString}&vnp_SecureHash={secureHash}
```

---

## 3. IPN (Instant Payment Notification)

VNPay sends a **GET request** to the IPN URL with all `vnp_*` params as query string.

### 3.1 IPN Endpoints

| Context | Endpoint | Handler |
|---------|----------|---------|
| Session payments | `GET /api/v1/payments/vnpay-ipn` | `PaymentController.VnPayIpnAsync` |
| Wallet top-ups | `GET /api/v1/wallet/topup/vnpay-ipn` | `WalletEndpoints.VnPayTopUpIpn` |

### 3.2 IPN Processing Steps

1. Parse query parameters from `Request.Query`
2. Verify HMAC-SHA512 signature (exclude `vnp_SecureHash`, `vnp_SecureHashType`)
3. Find order by `vnp_TxnRef` (maps to `ReferenceCode`)
4. Check idempotency (already completed → return `02`)
5. Verify amount: `vnp_Amount / 100` must match stored amount
6. Update order status based on `vnp_ResponseCode` (`00` = success)
7. Return JSON response

### 3.3 IPN Response Codes

| RspCode | Message | VNPay behavior |
|---------|---------|----------------|
| `00` | Confirm Success | VNPay stops retrying |
| `01` | Order not found | VNPay retries (up to 10x, 5 min interval) |
| `02` | Order already confirmed | VNPay stops retrying |
| `04` | Invalid amount | VNPay retries |
| `97` | Invalid signature | VNPay retries |
| `99` | Unknown error | VNPay retries |

### 3.4 VNPay Response Codes (`vnp_ResponseCode`)

| Code | Meaning |
|------|---------|
| `00` | Transaction successful |
| `07` | Suspicious transaction (deducted) |
| `09` | Card not registered for Internet Banking |
| `10` | Authentication failed 3+ times |
| `11` | Payment timeout |
| `12` | Card/account locked |
| `13` | Wrong OTP |
| `24` | Customer cancelled |
| `51` | Insufficient balance |
| `65` | Daily transaction limit exceeded |
| `75` | Bank under maintenance |
| `79` | Wrong payment password too many times |
| `99` | Other error |

---

### 3.5 IPN Security & Compliance (VnPay Acceptance Cases)

**Case 11 — System Error Handling:**
All IPN processing is wrapped in `try/catch`. Unhandled exceptions return:
```json
{"RspCode":"99","Message":"Unknow error"}
```
Implementation: `WalletEndpoints.cs` — explicit try/catch around `ProcessVnPayIpnAsync()`.

**Case 12 — Transaction Status Only Updated at IPN:**
- IPN URL (`/api/v1/wallet/topup/vnpay-ipn`): Validates signature → credits wallet → updates transaction status. **This is the ONLY place where transaction status is updated.**
- Return URL (`klc://wallet/topup/callback`): Mobile deep link for UI display only. **Does NOT update any transaction status.**

**Case 13 — IP Whitelist + Logging:**

IP whitelist configured via `Payment:VnPay:IpnWhitelist` (comma-separated):

| Environment | IPs |
|-------------|-----|
| **TEST** | `113.160.92.202, 203.205.17.226, 202.93.156.34, 103.220.84.4` |
| **PROD** | `113.52.45.78, 116.97.245.130, 42.118.107.252, 113.20.97.250, 203.171.19.146, 103.220.87.4, 103.220.86.4, 103.220.86.10, 103.220.87.10, 103.220.86.139, 103.220.87.139` |

Logging on every IPN request:
```
[VnPay IPN] Received: TxnRef={TxnRef}, CallerIP={CallerIP}
[VnPay IPN] Response: TxnRef={TxnRef}, RspCode={RspCode}, CallerIP={CallerIP}
[VnPay IPN] REJECTED: IP {CallerIP} not in whitelist, TxnRef={TxnRef}
```

Logs retained on Cloud Run Logging for minimum 2 months. Empty whitelist = allow all (for dev/staging).

### 3.6 IPN Validation Order

Validation runs BEFORE idempotency lock to ensure correct error codes:
```
1. Parse vnp_TxnRef → not found → return 01
2. Find transaction in DB → not found → return 01
3. Validate signature (HMAC-SHA512) → invalid → return 97
4. Validate amount (vnp_Amount/100 == stored amount) → mismatch → return 04
5. Check already completed → yes → return 02
6. Acquire idempotency lock (Redis) → duplicate → return 02
7. Credit wallet + mark completed → return 00
8. Any exception → return 99
```

---

## 4. Query Transaction API (querydr)

Server-to-server API to check transaction status — useful for reconciliation.

**Endpoint:** `POST {QueryApiUrl}` with JSON body.

**Signature:** Pipe-delimited HMAC-SHA512 (different from payment URL!):
```
hashData = vnp_RequestId|vnp_Version|querydr|vnp_TmnCode|vnp_TxnRef|vnp_TransactionDate|vnp_CreateDate|vnp_IpAddr|vnp_OrderInfo
secureHash = HMAC-SHA512(hashSecret, hashData)
```

**Admin endpoint:** `GET /api/v1/payments/{id}/query-vnpay`

---

## 5. Refund API

**Endpoint:** `POST {QueryApiUrl}` with JSON body.

**Signature:** Pipe-delimited HMAC-SHA512:
```
hashData = vnp_RequestId|vnp_Version|refund|vnp_TmnCode|vnp_TransactionType|vnp_TxnRef|vnp_Amount|vnp_TransactionNo|vnp_TransactionDate|vnp_CreateBy|vnp_CreateDate|vnp_IpAddr|vnp_OrderInfo
secureHash = HMAC-SHA512(hashSecret, hashData)
```

- `vnp_TransactionType`: `02` = full refund, `03` = partial refund
- `vnp_Amount`: Refund amount * 100

---

## 6. Sandbox Testing

### 6.1 Test Cards

**Domestic cards (NCB bank — choose bank NCB on VNPay page):**

| Card Number | Cardholder | Expiry | OTP | Result |
|-------------|-----------|--------|-----|--------|
| `9704198526191432198` | NGUYEN VAN A | 07/15 | `123456` | Success |
| `9704195798459170488` | NGUYEN VAN A | 07/15 | `123456` | Insufficient funds |
| `9704192181368742` | NGUYEN VAN A | 07/15 | `123456` | Card not activated |
| `9704193370791314` | NGUYEN VAN A | 07/15 | `123456` | Card locked |
| `9704194841945513` | NGUYEN VAN A | 07/15 | `123456` | Card expired |

**International cards (VISA/Mastercard/JCB):**

| Card Number | Type | CVV | Expiry | 3DS |
|-------------|------|-----|--------|-----|
| `4456530000001005` | VISA | 123 | 12/26 | No |
| `4456530000001096` | VISA | 123 | 12/26 | Yes |
| `5200000000001005` | Mastercard | 123 | 12/26 | No |
| `5200000000001096` | Mastercard | 123 | 12/26 | Yes |
| `3337000000000008` | JCB | 123 | 12/26 | No |
| `3337000000200004` | JCB | 123 | 12/24 | Yes |

**NAPAS ATM test cards:**

| Card Number | OTP |
|-------------|-----|
| `9704000000000018` | `otp` |
| `9704020000000016` | `otp` |

### 6.2 Sandbox Account Activation

**Status**: The hash signature is verified correct (no "Invalid signature" error). The sandbox returns a generic "An error occurred" which indicates the merchant terminal needs activation.

**Action required**: Contact VNPay tech support at `kdctt@vnpay.vn` to confirm:
- The terminal `KLCTTE11` is active and ready for testing in sandbox
- The allowed payment method types (ATM, INTCARD, QRCODE) are enabled
- The `vnp_OrderType=other` is configured for this terminal
- Configure the IPN URL in the VNPay merchant portal sidebar ("Cấu hình IPN URL"):
  - Admin API IPN: `https://api.ev.odcall.com/api/v1/payments/vnpay-ipn`
  - Driver BFF IPN: `https://bff.ev.odcall.com/api/v1/wallet/topup/vnpay-ipn`

You can also configure IPN URLs via the VNPay SIT Testing console at:
`https://sandbox.vnpayment.vn/vnpaygw-sit-testing/order` → sidebar → "Cấu hình IPN URL"

### 6.3 Testing with VNPay Demo Tool

VNPay provides a sandbox testing tool at:
`http://sandbox.vnpayment.vn/tryitnow/Home/CreateOrder`

### 6.4 Manual Payment URL Test

Generate a payment URL and open in browser:

```bash
python3 << 'EOF'
import hmac, hashlib, urllib.parse
from datetime import datetime, timedelta, timezone

tmn_code = "KLCTTE11"
hash_secret = "JRNC2DVZ0U8IQJV1CP2ALSAI8OKLPEQ4"

now = datetime.now(timezone(timedelta(hours=7)))
create_date = now.strftime("%Y%m%d%H%M%S")
expire_date = (now + timedelta(minutes=15)).strftime("%Y%m%d%H%M%S")
txn_ref = "KLC" + create_date + "TEST"

params = {
    "vnp_Amount": "5000000",        # 50,000 VND
    "vnp_Command": "pay",
    "vnp_CreateDate": create_date,
    "vnp_CurrCode": "VND",
    "vnp_ExpireDate": expire_date,
    "vnp_IpAddr": "127.0.0.1",
    "vnp_Locale": "vn",
    "vnp_OrderInfo": "KLC EV Charging topup test",
    "vnp_OrderType": "topup",
    "vnp_ReturnUrl": "http://localhost:3001/payments/result",
    "vnp_TmnCode": tmn_code,
    "vnp_TxnRef": txn_ref,
    "vnp_Version": "2.1.0",
}

sorted_params = sorted(params.items())
qs = "&".join(f"{urllib.parse.quote_plus(k)}={urllib.parse.quote_plus(v)}" for k, v in sorted_params)
h = hmac.new(hash_secret.encode(), qs.encode(), hashlib.sha512).hexdigest()

print(f"https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?{qs}&vnp_SecureHash={h}")
print(f"\nTxnRef: {txn_ref}")
EOF
```

### 6.5 Simulating IPN Callback

After completing payment in the sandbox, simulate VNPay's IPN call to your local server:

```bash
# Replace with actual values from VNPay redirect
curl "http://localhost:44305/api/v1/payments/vnpay-ipn?\
vnp_Amount=5000000&\
vnp_BankCode=NCB&\
vnp_CardType=ATM&\
vnp_OrderInfo=KLC+EV+Charging+topup+test&\
vnp_PayDate=20260323170000&\
vnp_ResponseCode=00&\
vnp_TmnCode=KLCTTE11&\
vnp_TransactionNo=14270063&\
vnp_TransactionStatus=00&\
vnp_TxnRef=KLC20260323164114TEST&\
vnp_SecureHash=<hash_from_redirect>"
```

Expected response: `{"RspCode":"00","Message":"Confirm Success"}`

---

## 7. Implementation Files

| Layer | File | Purpose |
|-------|------|---------|
| Domain | `KLC.Domain/Payments/IPaymentGatewayService.cs` | Gateway interface, DTOs |
| Service | `KLC.Application/Payments/VnPayPaymentService.cs` | VNPay logic: create URL, verify IPN, query, refund |
| App Service | `KLC.Application/Payments/PaymentAppService.cs` | Orchestration: IPN handler, amount verification |
| DTOs | `KLC.Application.Contracts/Payments/PaymentDtos.cs` | VnPayIpnResponse, ProcessPaymentDto |
| Admin API | `KLC.HttpApi/Controllers/Payments/PaymentController.cs` | IPN endpoint, query endpoint |
| BFF Endpoints | `KLC.Driver.BFF/Endpoints/WalletEndpoints.cs` | Wallet IPN endpoint |
| BFF Endpoints | `KLC.Driver.BFF/Endpoints/PaymentEndpoints.cs` | Payment processing |
| BFF Service | `KLC.Driver.BFF/Services/WalletBffService.cs` | Wallet IPN handler |
| BFF Service | `KLC.Driver.BFF/Services/PaymentBffService.cs` | Payment processing |
| Tests | `KLC.Application.Tests/Payments/VnPaySignatureTests.cs` | Signature, amount, IP tests |
| Config | `KLC.HttpApi.Host/appsettings.json` | VNPay credentials |
| Config | `KLC.Driver.BFF/appsettings.json` | VNPay credentials |

---

## 8. Security Checklist

- [x] HMAC-SHA512 signature on all payment URLs
- [x] Constant-time signature comparison (`CryptographicOperations.FixedTimeEquals`)
- [x] Amount verification on IPN callbacks (vnp_Amount / 100 vs stored amount)
- [x] Idempotent IPN handling (already completed → RspCode 02)
- [x] No raw card data stored (PCI-DSS: token references only)
- [x] vnp_ExpireDate set (15 min TTL)
- [x] Real client IP forwarded to VNPay (not hardcoded 127.0.0.1)
- [x] Sandbox credentials in appsettings.json, production in Secret Manager
- [ ] IPN URL must be publicly accessible (configure VNPay merchant portal)
- [ ] IPN URL must have valid SSL certificate
- [ ] Production HashSecret rotated after initial setup

---

## 9. Production Go-Live Checklist

- [ ] Obtain production TmnCode and HashSecret from VNPay
- [ ] Store production credentials in GCP Secret Manager
- [ ] Configure IPN URLs in VNPay merchant portal:
  - Session payments: `https://api.ev.odcall.com/api/v1/payments/vnpay-ipn`
  - Wallet top-ups: `https://bff.ev.odcall.com/api/v1/wallet/topup/vnpay-ipn`
- [ ] Update `appsettings.Production.json` with production BaseUrl (`https://pay.vnpay.vn`)
- [ ] Verify IPN endpoint is accessible from VNPay servers
- [ ] Test end-to-end with VNPay production test transaction
- [ ] Monitor IPN callback logs for first 24 hours
- [ ] Set up alerts for failed signature verifications

---

## 10. VNPay Resources

| Resource | URL |
|----------|-----|
| Sandbox portal | https://sandbox.vnpayment.vn/apis/ |
| Payment API docs | https://sandbox.vnpayment.vn/apis/docs/thanh-toan-pay/pay.html |
| Query/Refund docs | https://sandbox.vnpayment.vn/apis/docs/truy-van-hoan-tien/querydr&refund.html |
| Demo tool | http://sandbox.vnpayment.vn/tryitnow/Home/CreateOrder |
| SDK downloads | https://sandbox.vnpayment.vn/apis/downloads/ |
| Tech support | kdctt@vnpay.vn |
