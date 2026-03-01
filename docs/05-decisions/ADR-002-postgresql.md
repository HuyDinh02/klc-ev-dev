# ADR-002: PostgreSQL as Primary Relational Database

> Status: ACCEPTED | Date: 2026-03-01

## Context

Designing the data layer for an EV Charging CSMS requires:
- **Multi-tenancy** — Isolate data between operators; support shared and isolated database strategies
- **Complex queries** — Real-time reporting, charging session analysis, user transaction history
- **JSON flexibility** — Store OCPP protocol payloads, device metadata, configuration without schema migration
- **Read replicas** — Driver BFF (read-heavy) separate from Admin API; scale read operations independently
- **Vietnamese market** — Cost-effectiveness for startup; strong hosting ecosystem in Vietnam
- **ACID compliance** — Financial transactions (billing, refunds) must be consistent
- **Ecosystem** — Integration with existing .NET tools (EF Core, Dapper, Npgsql)

**Database candidates:**
1. PostgreSQL (open-source, advanced features)
2. SQL Server (enterprise, Microsoft ecosystem)
3. MySQL (lightweight, WordPress ecosystem)
4. MongoDB (document store, schema-less)
5. MariaDB (MySQL fork, open-source)

## Decision

**Use PostgreSQL 15+ as the primary transactional database with read replicas.**

### Rationale

PostgreSQL is the optimal balance of **power, cost, and Vietnam-market fit**:

1. **JSON/JSONB Support**
   - Store OCPP 1.6J messages (`message.payload` as JSONB)
   - Query JSON fields with `@>`, `->>` operators
   - Index JSON columns for fast retrieval
   - Flexible schema for device metadata without migrations

2. **Read Replicas Architecture**
   - **Primary (Write)**: Admin API writes to primary
   - **Read Replica 1**: Driver BFF for real-time station queries
   - **Read Replica 2**: Async reporting, analytics jobs
   - **Streaming replication** via WAL (Write-Ahead Logs); ~milliseconds lag
   - Cost-effective scaling without message queues

3. **ACID & Multi-Tenancy**
   - Full ACID compliance for billing transactions
   - Row-level security (RLS) for tenant isolation (if using shared database)
   - Serializable isolation levels prevent race conditions in concurrent sessions

4. **Advanced Features**
   - **Window Functions**: Cumulative charging costs, session rankings
   - **Common Table Expressions (CTEs)**: Complex billing queries
   - **Full-Text Search**: Search charging stations, user transactions
   - **Arrays & Ranges**: Store station operating hours, availability slots
   - **Trigger Support**: Auto-update `UpdatedAt`, enforce business logic at DB layer

5. **Open Source & Cost**
   - **No licensing costs** — reduces startup infrastructure spend
   - **AWS RDS** — Native PostgreSQL support with automated backups, failover
   - **Vietnam hosting** — Cheap managed PostgreSQL from FPT Cloud, VCCORP
   - **Community** — Large, active community; many Vietnamese PostgreSQL specialists

6. **.NET Ecosystem**
   - **Entity Framework Core**: Excellent PostgreSQL provider (Npgsql)
   - **Dapper**: High-performance queries using PostgreSQL native functions
   - **MigraDoc/FluentMigrator**: Schema versioning and rollback
   - **EF Core 8** with JSON support (EF.Property<T>("json_column"))

7. **Performance**
   - **B-tree indexes** for primary/foreign key lookups
   - **GiST/BRIN indexes** for geospatial queries (station location)
   - **Partial indexes** for active sessions only
   - **Query planner** — EXPLAIN ANALYZE for optimization
   - **Connection pooling** — pgBouncer reduces overhead

8. **Operations**
   - **Point-in-time recovery**: Backup snapshots; restore to any moment
   - **Logical replication**: Upgrade PostgreSQL without downtime
   - **Monitoring**: pgAdmin, Grafana integration for metrics
   - **Vacuuming**: Automatic cleanup; minimal manual maintenance

## Consequences

### Positive

- ✅ **Cost-Effective**: No license fees; cheap managed hosting in Vietnam (FPT Cloud, VCCORP)
- ✅ **Scalable Read Traffic**: Read replicas enable Driver BFF to handle high query volume (millions of station lookups/day)
- ✅ **JSON Flexibility**: OCPP payloads, device metadata stored natively without ETL
- ✅ **ACID Guarantee**: Billing transactions are reliable; no data loss on crashes
- ✅ **Mature & Stable**: PostgreSQL 15 is production-hardened; minimal CVEs
- ✅ **.NET Integration**: Npgsql provider is excellent; EF Core support is full-featured
- ✅ **Multi-Tenancy**: Row-level security (RLS) supports both shared & isolated databases
- ✅ **Reporting**: Window functions, CTEs, arrays enable complex analytics without application-layer logic
- ✅ **Minimal Operational Overhead**: Auto-vacuuming, connection pooling, query optimization built-in

### Negative

- ❌ **PostgreSQL Expertise Rare in Vietnam**: Fewer senior developers vs. MySQL/SQL Server
- ❌ **RAM Usage**: PostgreSQL uses more memory than MySQL for same data; plan infra accordingly
- ❌ **Replication Lag**: Read replicas may lag primary by milliseconds; eventual consistency model
- ❌ **JSONB Queries Are Slower**: Complex JSON queries slower than relational queries; denormalization sometimes needed
- ❌ **No Native Sharding**: PostgreSQL doesn't auto-shard; manual sharding required at app layer (Phase 2+)
- ❌ **Vacuum Bloat**: Under-vacuuming causes table bloat; requires monitoring and tuning
- ❌ **Migration Challenges**: Migrating away from PostgreSQL to another DB is costly (schema, queries specific to PG)
- ❌ **Scaling Vertical Only (Phase 1)**: Read replicas help, but still single-write primary; horizontal write scaling requires Citus or sharding (Phase 2+)

### Risks

| Risk | Mitigation |
|------|-----------|
| **Read replica lag causes stale data in Driver BFF** | Accept ~100ms lag; critical queries use primary; implement Redis cache for consistent reads |
| **JSONB queries become slow as data grows** | Use JSONB indices; denormalize frequently-queried fields; use columnar format for analytics |
| **Storage bloat from vacuuming issues** | Set up automated vacuum schedules; monitor dead rows with `pg_stat_user_tables`; use pg_repack if needed |
| **Backup/restore takes hours for large DB** | Implement incremental backups with WAL archiving; test restore procedure quarterly |
| **OCPP messages in JSONB grow unbounded** | Archive old messages to cold storage (S3); keep primary JSONB lean (< 1GB/table) |
| **Operator accidentally breaks isolation with RLS** | Enforce RLS policies in EF Core configuration; code review for cross-tenant queries |

## Alternatives Considered

### 1. **Microsoft SQL Server**
**Pros:**
- Enterprise-grade; tight .NET integration
- Excellent analysis services (SSAS) for reporting
- SQL Agent for job scheduling
- Native replication to Azure

**Cons:**
- **Licensing costs**: $15K+/year per core in Vietnam
- **Vertical scaling only**: Horizontal scaling requires expensive Availability Groups
- Not open-source; vendor lock-in
- Overkill features for startup CSMS

**Verdict:** Too expensive for Vietnamese startup; PostgreSQL provides 95% of features at 5% cost.

---

### 2. **MySQL 8.0+**
**Pros:**
- Lightweight; lower memory usage than PostgreSQL
- Faster writes; simpler replication (Row-Based Replication)
- Common in Vietnam; more developers available
- Built-in JSON support

**Cons:**
- **Weak JSON support**: Cannot query nested JSON efficiently; no JSONB equivalent
- **Limited window functions**: Slower analytics queries
- **ACID limitations**: Default MyISAM storage engine not ACID
- **Replication lag**: Harder to implement multi-replica setup reliably
- **Row-level security**: No built-in RLS; must enforce in app code

**Verdict:** Good for web apps; insufficient for CSMS financial transactions and OCPP flexibility.

---

### 3. **MongoDB (Document Store)**
**Pros:**
- Flexible schema; great for JSON data
- Horizontal scaling (sharding) built-in
- High write throughput

**Cons:**
- **No ACID across documents**: Transactions are weak; billing data vulnerable
- **Expensive storage**: JSON duplication across shards
- **Complex queries**: Joins not supported; must denormalize heavily
- **Operational complexity**: No referential integrity; manual cleanup needed
- **Not ideal for relational data**: Operator-Station-Session relationships are relational

**Verdict:** Better for caching (via Redis); wrong choice for transactional charging billing.

---

### 4. **CockroachDB (NewSQL)**
**Pros:**
- Horizontal scaling with ACID
- PostgreSQL wire protocol compatible (drop-in replacement)
- Multi-region replication

**Cons:**
- **Managed hosting expensive**: $6K+/month; beyond startup budget
- **Overkill for Phase 1**: Horizontal write scaling not needed yet
- **Smaller ecosystem**: Fewer integrations, fewer Vietnamese specialists

**Verdict:** Good future option for Phase 3 (global scale); premature for Phase 1.

---

### 5. **MariaDB (MySQL Fork)**
**Pros:**
- Open-source; no licensing
- Better ACID than MySQL

**Cons:**
- Same weaknesses as MySQL
- Smaller community than PostgreSQL/MySQL
- Still lacks strong JSON querying

**Verdict:** MySQL alternative; same limitations apply.

---

## Data Architecture (Read Replicas)

```
┌─────────────────────────────────────────────────────────┐
│                    CSMS Data Layer                       │
├─────────────────────────────────────────────────────────┤
│                                                           │
│  Admin API (Write)         Driver BFF (Read-Heavy)       │
│       ↓                              ↓                    │
│  PostgreSQL Primary ←─ Streaming Replication ← Read1     │
│  (Writes, Writes)          (100ms lag)          (Reads)  │
│                                         ↓                 │
│                            Read Replica 2 (Reads)        │
│                            (Async Reports)               │
│                                                           │
│  Cache Layer: Redis (Session < 5min, warm queries)       │
│                                                           │
└─────────────────────────────────────────────────────────┘
```

- **Primary**: Handles all writes (billing transactions, session updates)
- **Read Replica 1**: Driver BFF queries (stations, availability, pricing)
- **Read Replica 2**: Async jobs (daily revenue reports, usage analytics)

## Related Decisions

- **ADR-001**: ABP Framework uses Entity Framework Core → Npgsql provider
- **ADR-004**: Modular Monolith can scale read traffic via replicas before microservices
- **ADR-005**: CQRS separates read (replicas) and write (primary) paths

## References

- [PostgreSQL Official Documentation](https://www.postgresql.org/docs/)
- [Entity Framework Core PostgreSQL Provider (Npgsql)](https://www.npgsql.org/efcore/)
- [PostgreSQL Replication & High Availability](https://www.postgresql.org/docs/current/warm-standby.html)
- [AWS RDS for PostgreSQL Best Practices](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/CHAP_PostgreSQL.html)
- [pgAdmin - PostgreSQL Management Tool](https://www.pgadmin.org/)
- [Use The Index, Luke! (Query Optimization)](https://use-the-index-luke.com/)
