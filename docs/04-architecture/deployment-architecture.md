# Deployment Architecture (AWS)

> Status: APPROVED | Last Updated: 2026-03-01

---

## 1. Infrastructure

| Component | Technology |
|-----------|-----------|
| Cloud Provider | AWS |
| Containers | Docker |
| Orchestration | Docker Compose (Phase 1) → ECS/EKS (future) |
| Load Balancing | AWS ALB |
| Monitoring | CloudWatch + Serilog structured logging |
| CI/CD | GitHub Actions or AWS CodePipeline |
| Database | AWS RDS PostgreSQL + Read Replicas |
| Cache | AWS ElastiCache (Redis) |
| Backup | Automated periodic backups |

## 2. Phase 1 Deployment

```
ALB → Admin API Container (port 5000)
    → Driver BFF Container (port 5001)
    → OCPP WebSocket Container

RDS PostgreSQL (Primary + Read Replica)
ElastiCache Redis
```

## 3. Future Scaling (Phase 2+)

- Container orchestration via ECS or EKS
- Auto-scaling based on charger connection count and API load
- Multi-AZ deployment for high availability
- Data synchronization to head office systems
