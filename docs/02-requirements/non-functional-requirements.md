# Non-Functional Requirements

> Status: APPROVED | Last Updated: 2026-03-01 | Source: BRD v0.1

---

## Performance

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-P01 | System response time for user actions | < 5 seconds under normal conditions |
| NFR-P02 | Payment processing confirmation | < 3 seconds after gateway response |
| NFR-P03 | Charger status update latency | < 10 seconds from actual change |
| NFR-P04 | API response time (p95) | < 200ms |
| NFR-P05 | WebSocket message latency | < 100ms |
| NFR-P06 | Mobile app startup time | < 3 seconds |

## Availability

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-A01 | System uptime | Minimum 99.5% per month |
| NFR-A02 | Planned maintenance | Must be communicated in advance |
| NFR-A03 | Graceful degradation | Handle charger offline scenarios |

## Scalability

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-SC01 | Concurrent charger connections | Support 1000+ |
| NFR-SC02 | Horizontal scaling | API servers scale independently |
| NFR-SC03 | Database read replicas | PostgreSQL read replicas for BFF |
| NFR-SC04 | Caching layer | Redis for hot data |

## Security

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-S01 | Data in transit | HTTPS/TLS encryption |
| NFR-S02 | Data at rest | Encryption for personal and payment data |
| NFR-S03 | Payment compliance | PCI-DSS standards |
| NFR-S04 | Access control | RBAC for Web Admin Portal |
| NFR-S05 | Authentication | OAuth2/OpenID Connect, secure password policies |
| NFR-S06 | Communication | HTTPS/WSS for all connections |

## Reliability

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-R01 | Data integrity | 100% transactional integrity for sessions |
| NFR-R02 | Duplicate prevention | No duplicate sessions or billing records |
| NFR-R03 | Offline resilience | Buffer and sync session data on network restoration |

## Audit & Logging

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-AL01 | Admin action logging | All create, update, delete, pricing changes logged |
| NFR-AL02 | Log retention | Minimum 12 months |
| NFR-AL03 | Compliance | Vietnamese data protection and tax regulations |
