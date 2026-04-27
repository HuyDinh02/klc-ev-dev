import type { NextConfig } from "next";
import { withSentryConfig } from "@sentry/nextjs";

const isDev = process.env.NODE_ENV === "development";

const cspConnectSrc = isDev
  ? "connect-src 'self' https://localhost:44305 wss://localhost:44305 https://api.ev.klcenergy.com.vn https://bff.ev.klcenergy.com.vn wss://api.ev.klcenergy.com.vn wss://bff.ev.klcenergy.com.vn"
  : "connect-src 'self' https://api.ev.klcenergy.com.vn https://bff.ev.klcenergy.com.vn wss://api.ev.klcenergy.com.vn wss://bff.ev.klcenergy.com.vn";

const nextConfig: NextConfig = {
  output: "standalone",
  poweredByHeader: false,
  async headers() {
    return [
      {
        source: "/(.*)",
        headers: [
          {
            key: "X-Frame-Options",
            value: "DENY",
          },
          {
            key: "X-Content-Type-Options",
            value: "nosniff",
          },
          {
            key: "Referrer-Policy",
            value: "strict-origin-when-cross-origin",
          },
          {
            key: "X-DNS-Prefetch-Control",
            value: "on",
          },
          {
            key: "Strict-Transport-Security",
            value: "max-age=31536000; includeSubDomains",
          },
          {
            key: "Permissions-Policy",
            value: "camera=(), microphone=(), geolocation=()",
          },
          {
            key: "Content-Security-Policy",
            value: `default-src 'self'; script-src 'self' 'unsafe-eval' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob: https:; font-src 'self' data:; ${cspConnectSrc}; frame-ancestors 'none';`,
          },
        ],
      },
    ];
  },
};

export default withSentryConfig(nextConfig, {
  silent: true,
  disableLogger: true,
});
