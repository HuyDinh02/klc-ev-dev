# Risk Register

> Status: ACTIVE | Last Updated: 2026-03-01

---

| ID | Risk | Probability | Impact | Mitigation |
|----|------|-------------|--------|------------|
| R01 | OCPP charger compatibility issues with different hardware vendors | Medium | High | Test with multiple charger brands early; use OCPP simulator for development |
| R02 | Payment gateway integration delays (ZaloPay, MoMo, OnePay) | Medium | High | Start integration in parallel with core development; have fallback payment flow |
| R03 | Tight timeline (4 months to MVP) | High | High | Strict Phase 1 scope; no scope creep; confirmed feature list with client |
| R04 | Performance under high concurrent charger connections | Medium | Medium | Early load testing; Redis caching layer; read replicas for BFF |
| R05 | Vietnam regulatory changes affecting e-invoicing or payment | Low | Medium | Monitor regulations; flexible integration architecture |
| R06 | Network quality at station locations affecting real-time data | Medium | Medium | Offline data buffering; sync on reconnection; graceful degradation |
| R07 | Charger firmware variations affecting OCPP message handling | Medium | Medium | Strongly-typed models; comprehensive OCPP test scenarios; handle edge cases |
| R08 | Client requirement changes during development | Medium | High | Change request process; weekly alignment meetings; documented Phase 1/2 split |
