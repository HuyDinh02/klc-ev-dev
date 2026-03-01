# Tech Stack

## Backend
- .NET 10, C# 13, ABP Framework (DDD layered architecture)
- ASP.NET Core Web API (Admin: port 5000, Driver BFF: port 5001)
- CQRS with MediatR, AutoMapper for DTOs
- ABP built-ins: Identity, OpenIddict, AuditLog, Permissions, Localization

## Database
- PostgreSQL (EF Core + ABP), Read Replicas for BFF
- Redis caching (ElastiCache)
- Code-first migrations via ABP DbMigrator

## Protocol
- OCPP 1.6J (JSON over WebSocket)
- OCPP.Core library for message handling

## Frontend
- React.js + Next.js + TailwindCSS (Admin Portal)
- React Native / Expo (Mobile App, iOS + Android)

## Cloud
- AWS: Docker, ALB, RDS PostgreSQL, ElastiCache Redis, CloudWatch
- CI/CD: GitHub Actions
- Monitoring: Serilog structured logging

## Integrations
- Payment: ZaloPay, MoMo, OnePay
- E-Invoice: MISA, Viettel, VNPT
- Maps: Google Maps API
- Push: Firebase Cloud Messaging (FCM)
- Real-time: SignalR (app/portal updates)
