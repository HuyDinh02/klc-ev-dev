# Driver App (React Native)

## Stack
React Native (Expo), TypeScript, single codebase iOS + Android

## Key Screens
1. **Home/Map** — Station finder (Google Maps), GPS-based, availability indicators
2. **Station Detail** — Connectors, status, pricing, QR scan button
3. **QR Scanner** — Camera scan to start charging session
4. **Charging Session** — Real-time: duration, energy, estimated cost (via SignalR)
5. **Payment** — Method selection, ZaloPay/MoMo/OnePay processing
6. **History** — Sessions + payments list with details
7. **Profile** — Personal info, vehicles, payment methods, settings
8. **Notifications** — Push alerts for charge complete, fees, faults (Phase 2)

## API Integration
All calls to Driver BFF (port 5001), Redis cache-first responses.

## UI Design (Client Preferences)
- Color: Blue, White, Orange
- Smart navigation by vehicle type
- News/promotions section
- Invoice checking in profile
- PCI-DSS badge on payment screens
- Clean, modern, map-first design

## Real-time Updates
SignalR hub for live session data: energy, duration, cost, power.
Update frequency: every MeterValue (10-30 seconds).
