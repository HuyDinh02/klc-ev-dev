# Mobile App Architecture (React Native)

> Status: APPROVED | Last Updated: 2026-03-01

---

## 1. Overview

React Native with a single codebase for iOS and Android. The app serves EV drivers to find stations, start/stop charging, pay, and track history.

## 2. Key Screens & Features

| Screen | Features |
|--------|----------|
| Home / Map | Station finder with GPS, availability indicators, station details |
| Station Detail | Connector list, status, pricing, QR scan button |
| Charging Session | Real-time progress: duration, energy, estimated cost |
| Payment | Payment method selection, transaction processing |
| History | Charging sessions list, payment transactions, invoice access |
| Profile | Personal info, vehicle management, account settings, security |
| Notifications | Charge complete, fee alerts, system notifications |

## 3. Integration Points

- **Driver BFF API (port 5001):** All data flows through the BFF
- **Google Maps:** Station locations and directions
- **QR Scanner:** Camera-based QR code scanning for session initiation
- **Payment SDKs:** ZaloPay, MoMo, OnePay native SDKs
- **FCM:** Firebase push notifications
- **SignalR:** Real-time charging status updates

## 4. UI Design Preferences (from Client)

- Color tone: Blue, White, Orange
- Smart navigation by vehicle type (xe may, o to)
- News and promotional program sections
- Invoice checking in profile
- PCI-DSS certification display for payments
- Clean, modern design emphasizing map and availability
