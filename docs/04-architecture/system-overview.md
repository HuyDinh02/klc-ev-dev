# System Overview

> Status: APPROVED | Last Updated: 2026-03-01 | Source: Kickoff Document

---

## 1. Architecture Philosophy

EmeSoft proposes building the EV Charging Management System with a **CSMS-centric architecture**, combining modular design, cloud-based deployment, and microservices readiness. The architecture ensures stable operations, flexible scaling, and suitability for the Vietnam market.

### Design Principles
- **CSMS-centric:** CSMS is the central orchestrator for all system operations
- **Modular & Scalable:** Components are clearly separated, allowing independent scaling
- **Cloud-based:** Flexible deployment scaling with station and user growth (AWS)
- **Vietnam-market ready:** Pre-integrated payment, e-invoicing, and operations for Vietnam
- **Operational stability first:** Prioritize stable, easy-to-maintain operations in Phase 1

---

## 2. Logical Architecture

The system consists of three logical layers:

### CSMS (Core Platform)
The core platform responsible for OCPP 1.6J communication with chargers, station/session/capacity/status management, payment processing and pricing policies, and data services for all interaction channels.

### User & Operations Interaction
Users interact through the Mobile App (iOS & Android) for EV drivers and the Admin/Ops Portal for operations and technical staff. All channels communicate indirectly through CSMS for security and control.

### Enterprise Integration & Data
Built on CSMS, the system supports payment gateway integration (ZaloPay, MoMo, OnePay), e-invoice and accounting integration (MISA, Viettel, VNPT), operational and financial data collection and storage, and reporting, analytics, and future AI capabilities.

---

## 3. Dual-API Architecture

| Component | Port | Architecture | Purpose |
|-----------|------|-------------|---------|
| Admin API | 5000 | Full ABP layered (DDD) | Admin portal, full CRUD, complex operations |
| Driver BFF API | 5001 | .NET Minimal API | Mobile app, Redis cache-first, read replicas |
| Shared Domain | — | ABP Domain layer | Shared entities, domain services, business rules |

---

## 4. Technology Stack Overview

| Layer | Technology |
|-------|-----------|
| Backend & Core | .NET 10, C# 13, ABP Framework, ASP.NET Core Web API |
| OCPP Protocol | OCPP 1.6J (JSON over WebSocket) |
| Real-time | WebSocket (OCPP) + SignalR (portal/app updates) |
| Frontend & Portal | React.js, Next.js, TailwindCSS |
| Mobile App | React Native (single codebase for iOS & Android) |
| Database | PostgreSQL (EF Core + ABP), Read Replicas for BFF |
| Caching | Redis (sessions, hot data) |
| Cloud | AWS (Docker, ALB, CloudWatch) |
| CQRS | MediatR |
| Payments | ZaloPay, MoMo, OnePay |
| E-Invoice | MISA, Viettel, VNPT |
| Maps | Google Maps API |
| Push Notifications | Firebase Cloud Messaging (FCM) |

---

## 5. Future Capabilities

The architecture is designed for future expansion including operational and business data analytics, data-driven decision support, energy optimization and station scaling, and AI capabilities integrated natively into CSMS (not separate services).
