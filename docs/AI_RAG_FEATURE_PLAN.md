# AI/RAG Feature Implementation Plan
## Natural Language Database Query System

**Feature Branch:** `feature/add-ai-rag`
**Created:** 2025-10-13
**Status:** Planning Phase

---

## 1. EXECUTIVE SUMMARY

### Vision
Enable users to ask natural language questions about their project scheduling data and receive accurate, contextual answers without needing to understand the database schema or write SQL queries.

### Target Users
- Project Managers: "Which squads can take on a new 80-hour project next month?"
- Squad Leads: "Show me all projects assigned to my squad in Q1"
- Resource Planners: "Which team members are overallocated this week?"
- Executives: "What's our overall capacity utilization for November?"

### Success Criteria
- Users can ask questions in plain English and get accurate responses
- Response time < 5 seconds for most queries
- 90%+ accuracy on common query patterns
- Graceful handling of ambiguous or out-of-scope questions

---

## 2. USE CASE ANALYSIS

### Primary Use Cases (Must Have)

#### UC-1: Squad Availability Queries
**Examples:**
- "Which squads have availability next week?"
- "Show me squads with more than 20 hours free capacity in December"
- "List all active squads sorted by available capacity"

**Data Required:** Squads, TeamMembers, ProjectAllocations
**Complexity:** Medium (requires aggregation across time periods)

#### UC-2: Project Status Queries
**Examples:**
- "What projects are currently assigned to Squad Alpha?"
- "Show all projects scheduled for Q1 2025"
- "Which projects are going live this month?"

**Data Required:** Projects, ProjectAllocations, Squads
**Complexity:** Low (mostly direct queries)

#### UC-3: Team Member Queries
**Examples:**
- "Who are the members of Squad Bravo?"
- "List all inactive team members"
- "Which developers have daily capacity over 6 hours?"

**Data Required:** TeamMembers, Squads
**Complexity:** Low (straightforward queries)

#### UC-4: Capacity Utilization Analysis
**Examples:**
- "What's the utilization rate for Squad Charlie this month?"
- "Show me overallocated squads for the next 2 weeks"
- "Calculate total allocated hours vs capacity for November"

**Data Required:** All tables (complex aggregations)
**Complexity:** High (requires business logic from CapacityService)

#### UC-5: Allocation History
**Examples:**
- "Show allocation history for Project P-2024-001"
- "What was Squad Delta working on in September?"
- "List all development vs onsite allocations for last quarter"

**Data Required:** ProjectAllocations, Projects, Squads
**Complexity:** Medium (time-based filtering)

### Secondary Use Cases (Nice to Have)

#### UC-6: Predictive Queries
**Examples:**
- "When is the next available slot for a 40-hour project?"
- "Can Squad Echo handle a new 60-hour project starting next Monday?"
- "Suggest best squad for a 3-week project in January"

**Data Required:** All tables + scheduling algorithms
**Complexity:** Very High (requires integration with AllocationService)

#### UC-7: Comparative Analysis
**Examples:**
- "Compare capacity utilization across all squads for Q4"
- "Which squad completed the most projects last month?"
- "Show average project duration by squad"

**Data Required:** Historical data + aggregations
**Complexity:** High (complex analytics)

### Out of Scope (V1)
- Modification queries ("Add a new squad", "Delete project")
- Multi-turn conversations with context
- Chart/visualization generation
- Real-time streaming results
- Voice input/output

---

## 3. TECHNICAL ARCHITECTURE

### Architecture Decision: Hybrid Text-to-SQL + RAG Approach

**Rationale:**
1. **Text-to-SQL** is ideal for structured queries against relational data
2. **RAG** provides context about business rules, schema, and example queries
3. **Hybrid** gives us accuracy + flexibility

### System Components

```
┌─────────────────────────────────────────────────────────────┐
│                      Frontend (React)                        │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  New Component: NaturalLanguageQueryPanel.jsx         │  │
│  │  - Text input for questions                            │  │
│  │  - Loading state                                       │  │
│  │  - Results display (table/cards)                       │  │
│  │  - Query history                                       │  │
│  └───────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
                              │ HTTP POST /api/ai/query
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                 ASP.NET Core Web API                         │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  New Controller: AiQueryController.cs                  │  │
│  │  - Endpoint: POST /api/ai/query                        │  │
│  │  - Input validation & rate limiting                    │  │
│  │  - Response formatting                                 │  │
│  └───────────────────────────────────────────────────────┘  │
│                              │                               │
│                              ▼                               │
│  ┌───────────────────────────────────────────────────────┐  │
│  │  New Service: AiQueryService.cs                        │  │
│  │  - Orchestrates query processing                       │  │
│  │  - Builds context for LLM                              │  │
│  │  - SQL generation & validation                         │  │
│  │  - Query execution                                     │  │
│  │  - Result transformation                               │  │
│  └───────────────────────────────────────────────────────┘  │
│                              │                               │
│          ┌───────────────────┴───────────────────┐          │
│          ▼                                        ▼          │
│  ┌──────────────────┐                  ┌─────────────────┐  │
│  │  LLM Service     │                  │  SQL Validator  │  │
│  │  (OpenAI SDK)    │                  │  & Safety       │  │
│  └──────────────────┘                  └─────────────────┘  │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                   SQL Server Database                        │
│  - Squads, TeamMembers, Projects                             │
│  - ProjectAllocations, OnsiteSchedules                       │
│  - New: QueryHistory table (audit log)                       │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    OpenAI API (GPT-4)                        │
│  - Text-to-SQL translation                                   │
│  - Natural language result formatting                        │
└─────────────────────────────────────────────────────────────┘
```

---

## 4. AI SERVICE PROVIDER EVALUATION

### Option 1: OpenAI API ⭐ RECOMMENDED
**Pros:**
- Best-in-class text-to-SQL capabilities (GPT-4)
- Excellent documentation & community support
- Fast iteration (no model training needed)
- Reliable, enterprise-grade uptime
- Official .NET SDK available

**Cons:**
- Cost: ~$0.01-0.03 per query (GPT-4)
- Requires internet connection
- Data leaves your infrastructure (privacy consideration)
- API key management required

**Cost Estimate:**
- 1000 queries/month × $0.02/query = $20/month
- Acceptable for MVP/small teams

**Recommendation:** Use GPT-4-turbo for balance of cost/performance

### Option 2: Azure OpenAI Service
**Pros:**
- Same models as OpenAI, but hosted on Azure
- Better for enterprise compliance (data residency)
- Integration with Azure AD for auth
- SLA guarantees

**Cons:**
- Requires Azure subscription
- More complex setup
- Similar costs to OpenAI
- Geographic restrictions

**When to use:** If already on Azure or have compliance requirements

### Option 3: Local LLM (Ollama + CodeLlama/Mistral)
**Pros:**
- No per-query costs
- No data leaves premises
- No API key management
- Works offline

**Cons:**
- Requires powerful hardware (GPU recommended)
- Lower accuracy than GPT-4 for complex queries
- More maintenance overhead
- Slower response times

**When to use:** Budget constraints or strict data privacy requirements

### Option 4: Anthropic Claude API
**Pros:**
- Excellent instruction following
- Long context window (100K+ tokens)
- Strong safety features

**Cons:**
- Higher cost than OpenAI
- Less mature text-to-SQL capabilities
- Smaller community

**When to use:** If OpenAI is not available

### DECISION: Start with OpenAI GPT-4-turbo
- Fastest time to value
- Best accuracy for MVP
- Can switch to Azure OpenAI later if needed
- Fallback plan: Local LLM for offline mode

---

## 5. RAG IMPLEMENTATION STRATEGY

### What Goes Into RAG Context?

#### 1. Database Schema Documentation
```markdown
## Database Schema

### Squads Table
- SquadId (int, PK): Unique identifier
- SquadName (string): Name of the squad
- SquadLeadName (string): Name of squad leader
- IsActive (bool): Whether squad is currently active

Relationships:
- Has many TeamMembers
- Has many ProjectAllocations

[... similar for all tables ...]
```

#### 2. Business Rules & Calculations
```markdown
## Business Rules

### Capacity Calculation
Daily Squad Capacity = SUM(TeamMember.DailyCapacityHours WHERE IsActive = true)
Utilization % = (Allocated Hours / Total Capacity) × 100

### Date Calculations
- CRP Date = 3 working days before UAT
- Go-Live Date = UAT Date + 10 working days
- Weekends (Sat/Sun) are excluded from calculations
```

#### 3. Example Queries (Few-Shot Learning)
```markdown
## Example Queries

Q: "Show all active squads"
SQL: SELECT SquadId, SquadName, SquadLeadName FROM Squads WHERE IsActive = 1

Q: "Which squads have projects in November 2024?"
SQL: SELECT DISTINCT s.SquadName
     FROM Squads s
     JOIN ProjectAllocations pa ON s.SquadId = pa.SquadId
     WHERE MONTH(pa.AllocationDate) = 11 AND YEAR(pa.AllocationDate) = 2024
```

#### 4. Common Patterns & Gotchas
```markdown
## Important Notes

- Always filter ProjectAllocations by date range to avoid slow queries
- Use DISTINCT when joining ProjectAllocations (many rows per project)
- AllocationType can be 'Development' or 'Onsite'
- Dates are stored as DateOnly in ProjectAllocations
- Team capacity only counts IsActive = true members
```

### RAG Storage Options

#### Option A: Embedded in Prompt (Simple) ⭐ RECOMMENDED FOR V1
- Store schema docs as markdown files in `/docs/rag-context/`
- Load and inject into LLM prompt on each request
- Pros: Simple, no extra infrastructure, easy to update
- Cons: Consumes tokens, not scalable to huge schemas
- Best for: MVP with 5-10 tables

#### Option B: Vector Database (Advanced)
- Use Pinecone, Weaviate, or Qdrant
- Embed schema chunks + examples
- Semantic search for relevant context
- Pros: Scalable, efficient token usage
- Cons: Additional infrastructure, complexity
- Best for: Large schemas (50+ tables) or many examples

#### Option C: Hybrid (Future)
- Core schema in prompt
- Examples in vector DB
- Retrieve top 5 relevant examples per query

### DECISION: Start with embedded-in-prompt (Option A)
- Sufficient for 5 tables
- Can migrate to vector DB if needed
- Keeps architecture simple for MVP

---

## 6. SECURITY & SAFETY CONSIDERATIONS

### SQL Injection Prevention
1. **Never execute raw LLM-generated SQL directly**
2. **Multi-layer validation:**
   - Whitelist allowed SQL keywords (SELECT only, no INSERT/UPDATE/DELETE)
   - Regex validation for dangerous patterns (`;`, `DROP`, `EXEC`, etc.)
   - Parse SQL AST and verify structure
   - Use parameterized queries where possible

### SQL Validation Rules
```csharp
✅ ALLOWED:
- SELECT statements only
- JOINs on known tables
- WHERE, GROUP BY, ORDER BY, HAVING
- Aggregate functions (SUM, COUNT, AVG)
- Date functions (YEAR, MONTH, DAY)

❌ BLOCKED:
- INSERT, UPDATE, DELETE, DROP, ALTER, TRUNCATE
- Multiple statements (semicolon detection)
- Stored procedure calls (EXEC, CALL)
- System tables/views
- xp_ commands
- UNION (can be used for injection)
- Comments (-- or /* */)
```

### Rate Limiting
- 10 queries per user per minute
- 100 queries per user per day
- Prevents abuse & controls API costs

### Data Privacy
- Query logs stored in database for audit
- Include: timestamp, user, question, SQL, success/failure
- Do NOT log actual result data (may contain sensitive info)
- Anonymize logs after 90 days

### Error Handling
- Never expose raw SQL errors to user
- Generic error messages: "I couldn't process that query. Please try rephrasing."
- Log detailed errors server-side for debugging

---

## 7. DATABASE SCHEMA CHANGES

### New Table: QueryHistory

```sql
CREATE TABLE QueryHistory (
    QueryHistoryId INT PRIMARY KEY IDENTITY(1,1),
    UserId NVARCHAR(256) NULL,              -- Future: if user auth added
    Question NVARCHAR(MAX) NOT NULL,         -- User's natural language question
    GeneratedSql NVARCHAR(MAX) NULL,         -- SQL generated by AI
    IsSuccessful BIT NOT NULL,               -- Did query execute successfully?
    ErrorMessage NVARCHAR(MAX) NULL,         -- Error if failed
    ExecutionTimeMs INT NULL,                -- Query execution time
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),

    -- Indexes
    INDEX IX_QueryHistory_CreatedDate (CreatedDate DESC),
    INDEX IX_QueryHistory_IsSuccessful (IsSuccessful)
);
```

**Purpose:**
- Audit trail for compliance
- Analytics on query patterns (improve RAG examples)
- Debugging failed queries
- Cost tracking (API calls)

---

## 8. API DESIGN

### Endpoint: POST /api/ai/query

#### Request Schema
```json
{
  "question": "Which squads have availability next week?",
  "options": {
    "maxResults": 100,              // Optional: limit result rows
    "includeExplanation": true,      // Optional: include SQL explanation
    "useCache": true                 // Optional: return cached result if available
  }
}
```

#### Response Schema (Success)
```json
{
  "success": true,
  "question": "Which squads have availability next week?",
  "sql": "SELECT s.SquadName, SUM(tm.DailyCapacityHours) as TotalCapacity...",
  "explanation": "This query calculates total capacity by summing active team members...",
  "results": [
    {
      "squadName": "Alpha Squad",
      "totalCapacity": 32,
      "allocatedHours": 20,
      "availableHours": 12
    },
    {
      "squadName": "Bravo Squad",
      "totalCapacity": 40,
      "allocatedHours": 5,
      "availableHours": 35
    }
  ],
  "resultCount": 2,
  "executionTimeMs": 1250,
  "timestamp": "2025-10-13T10:30:00Z"
}
```

#### Response Schema (Error)
```json
{
  "success": false,
  "question": "Delete all squads",
  "error": "I can only answer questions about your data, not modify it. Please rephrase your question.",
  "errorCode": "UNSAFE_QUERY",
  "timestamp": "2025-10-13T10:30:00Z"
}
```

### Error Codes
- `UNSAFE_QUERY`: Query contains blocked operations
- `INVALID_SQL`: Generated SQL is malformed
- `QUERY_TIMEOUT`: Database query took too long (>30s)
- `AI_SERVICE_ERROR`: OpenAI API failure
- `RATE_LIMIT_EXCEEDED`: User exceeded query quota
- `AMBIGUOUS_QUESTION`: Cannot determine user intent

---

## 9. FRONTEND UX DESIGN

### Component: NaturalLanguageQueryPanel.jsx

#### Layout Mockup
```
┌─────────────────────────────────────────────────────────────┐
│  Ask a Question About Your Projects                  [?]    │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  ┌────────────────────────────────────────────────────────┐ │
│  │ Which squads have availability next week?          [🔍]│ │
│  └────────────────────────────────────────────────────────┘ │
│                                                              │
│  💡 Try asking:                                              │
│  • "Show all active squads"                                  │
│  • "Which projects are going live this month?"               │
│  • "What's the utilization for Squad Alpha?"                 │
│  • "List team members with capacity over 6 hours"            │
│                                                              │
├─────────────────────────────────────────────────────────────┤
│  Results (2 squads found)                         Export ▾   │
├─────────────────────────────────────────────────────────────┤
│  Squad Name      Total Capacity   Allocated   Available     │
│  ─────────────   ──────────────   ─────────   ─────────     │
│  Alpha Squad           32h            20h        12h         │
│  Bravo Squad           40h             5h        35h         │
│                                                              │
│  ℹ️ Generated SQL: SELECT s.SquadName, SUM... [View]        │
│  ⏱️ Query took 1.25 seconds                                  │
└─────────────────────────────────────────────────────────────┘
```

#### Key Features
1. **Search input** with autocomplete (suggest common questions)
2. **Loading state** with spinner & "Thinking..." message
3. **Results table** (dynamic columns based on query)
4. **Export button** (CSV/Excel download)
5. **SQL view toggle** (collapsible, for power users)
6. **Query history dropdown** (last 10 queries)
7. **Help tooltip** with query examples

#### Where to Place It?
**Option A:** New dedicated page `/ai-query`
- Pros: Focused experience, more room for features
- Cons: Requires navigation, less discoverable

**Option B:** Modal/panel on Capacity page
- Pros: Contextual, quick access
- Cons: Limited space

**DECISION:** New page `/ai-query` with navigation link in main menu

---

## 10. IMPLEMENTATION PHASES

### Phase 1: Foundation (Week 1) 🏗️
**Goal:** Basic text-to-SQL working with simple queries

**Tasks:**
1. Add OpenAI SDK NuGet package
2. Create `AiQueryService.cs` with basic text-to-SQL
3. Create RAG context markdown files (`/docs/rag-context/schema.md`)
4. Implement SQL validation & safety checks
5. Add `QueryHistory` table migration
6. Create `AiQueryController.cs` with POST endpoint
7. Write unit tests for SQL validator

**Deliverable:** API endpoint that answers simple questions like "Show all squads"

### Phase 2: Enhanced Queries (Week 2) 🚀
**Goal:** Support complex aggregations & date-based queries

**Tasks:**
1. Expand RAG context with business rules
2. Add 20+ example queries to context
3. Implement query result caching (in-memory)
4. Add rate limiting middleware
5. Improve error handling & user-friendly messages
6. Test with all use cases from Section 2

**Deliverable:** API handles 90% of primary use cases accurately

### Phase 3: Frontend Integration (Week 3) 🎨
**Goal:** User-facing UI for natural language queries

**Tasks:**
1. Create `NaturalLanguageQueryPage.jsx`
2. Build search input component
3. Implement results table with dynamic columns
4. Add loading states & error handling
5. Create query history dropdown
6. Add CSV export functionality
7. Update navigation menu

**Deliverable:** Fully functional AI query page in production

### Phase 4: Polish & Optimization (Week 4) ✨
**Goal:** Production-ready with monitoring & docs

**Tasks:**
1. Add query analytics dashboard (admin view)
2. Implement query result caching (Redis optional)
3. Optimize slow queries (database indexes)
4. Write user documentation
5. Add telemetry/logging
6. Performance testing (load test with 100 concurrent users)
7. Security audit

**Deliverable:** Feature ready for production release

---

## 11. COST ANALYSIS

### OpenAI API Costs

**Assumptions:**
- 1000 queries per month
- Average query: 2000 input tokens + 500 output tokens
- Model: GPT-4-turbo

**Calculation:**
- Input: 1000 queries × 2000 tokens × $0.01/1K tokens = $20
- Output: 1000 queries × 500 tokens × $0.03/1K tokens = $15
- **Total: $35/month**

**Optimization Strategies:**
1. **Caching:** Cache identical questions for 5 minutes → save 30%
2. **Context pruning:** Only include relevant schema sections → save 20%
3. **GPT-3.5-turbo fallback:** Use for simple queries → save 50% on those
4. **Rate limiting:** Prevent abuse

**Estimated Production Cost:** $20-40/month for small team (<50 users)

### Infrastructure Costs
- No additional database costs (use existing SQL Server)
- Redis cache (optional): $10/month (Azure Cache)
- Monitoring (optional): Free tier sufficient

**Total Monthly Cost:** ~$30-50 for MVP

---

## 12. RISKS & MITIGATIONS

### Risk 1: Inaccurate SQL Generation
**Impact:** High (wrong answers erode trust)
**Probability:** Medium
**Mitigation:**
- Extensive RAG examples for common patterns
- Automated test suite with 100+ question/SQL pairs
- Confidence score threshold (reject low-confidence queries)
- User feedback loop ("Was this answer helpful?")

### Risk 2: Performance Issues
**Impact:** Medium (slow queries frustrate users)
**Probability:** Medium
**Mitigation:**
- 30-second query timeout
- Database indexes on frequently queried columns
- Result pagination (max 1000 rows)
- Caching for repeated queries

### Risk 3: API Cost Overruns
**Impact:** Low (budget concerns)
**Probability:** Low
**Mitigation:**
- Strict rate limiting per user
- Alert when approaching monthly budget
- Automatic failover to GPT-3.5-turbo if budget exceeded

### Risk 4: Security Breach
**Impact:** Critical (data exposure, system damage)
**Probability:** Low (with proper validation)
**Mitigation:**
- Multi-layer SQL validation (see Section 6)
- Read-only database user for AI queries
- Penetration testing before launch
- Query audit logs

### Risk 5: Poor User Adoption
**Impact:** Medium (wasted effort)
**Probability:** Medium
**Mitigation:**
- User testing with 3-5 target users during Phase 3
- In-app tutorial/examples
- Gather feedback early and iterate
- Make feature optional, not mandatory

---

## 13. SUCCESS METRICS (KPIs)

### Technical Metrics
- **Query Success Rate:** >90% of queries execute successfully
- **Response Time:** P95 < 5 seconds
- **SQL Accuracy:** >85% of results marked helpful by users
- **System Uptime:** 99.5%

### User Metrics
- **Adoption Rate:** 50% of active users try feature in first month
- **Retention:** 30% of users return to use feature weekly
- **Queries per User:** Average 5-10 queries/week for active users

### Business Metrics
- **Time Saved:** Estimate hours saved vs manual SQL queries
- **User Satisfaction:** NPS score >40 for AI feature
- **Cost Efficiency:** Stay within $50/month budget

---

## 14. TESTING STRATEGY

### Unit Tests
- SQL validator with 50+ malicious input cases
- RAG context builder
- Query result formatter

### Integration Tests
- End-to-end: Question → SQL → Results
- Test all primary use cases (Section 2)
- OpenAI API mocking for CI/CD

### User Acceptance Testing
- 5 target users test feature in staging
- Provide 20 realistic questions
- Measure accuracy & satisfaction

### Security Testing
- Automated SQL injection test suite
- Penetration testing (external)
- Code review by security team

### Performance Testing
- Load test: 100 concurrent queries
- Stress test: 10,000 queries in 1 hour
- Measure database impact

---

## 15. DOCUMENTATION PLAN

### Developer Documentation
- Architecture overview
- API reference (OpenAPI/Swagger)
- RAG context update guide
- Deployment instructions

### User Documentation
- "How to ask questions" tutorial
- Example queries by use case
- Troubleshooting guide
- FAQ

### Operational Documentation
- Monitoring & alerting setup
- Incident response playbook
- Cost tracking dashboard
- Backup & recovery

---

## 16. FUTURE ENHANCEMENTS (Post-V1)

### V2 Features
1. **Multi-turn conversations:** "Show Squad Alpha projects" → "Now show Bravo"
2. **Chart generation:** Auto-create bar charts for numeric results
3. **Scheduled queries:** Email daily capacity reports
4. **Query sharing:** Save & share queries with team
5. **Voice input:** Ask questions via microphone

### Advanced Features
1. **Predictive analytics:** ML model for capacity forecasting
2. **Anomaly detection:** Alert on unusual allocation patterns
3. **Natural language modifications:** "Add 5 hours to Squad Alpha for Project X"
4. **Integration with scheduling algorithms:** "Suggest optimal squad for Project Y"

---

## 17. DECISION LOG

| Date       | Decision                        | Rationale                                      |
|------------|---------------------------------|------------------------------------------------|
| 2025-10-13 | Use OpenAI GPT-4-turbo          | Best accuracy, fast iteration                  |
| 2025-10-13 | Hybrid Text-to-SQL + RAG        | Combines structured query power with context   |
| 2025-10-13 | Embedded RAG context (not vector DB) | Sufficient for 5-table schema, simpler MVP |
| 2025-10-13 | Read-only SQL access            | Security: prevent data modification            |
| 2025-10-13 | New page /ai-query              | Better UX than modal, dedicated experience     |
| 2025-10-13 | 4-week implementation timeline  | Balanced scope for MVP                         |

---

## 18. NEXT STEPS

### Immediate Actions
1. ✅ Review this plan with stakeholders
2. ⏳ Obtain OpenAI API key
3. ⏳ Create `/docs/rag-context/` directory
4. ⏳ Set up development branch
5. ⏳ Begin Phase 1 implementation

### Questions to Resolve Before Coding
1. **OpenAI API Key:** Do you have one, or should we create a new account?
2. **User Authentication:** Is there user auth in the app, or is it single-user?
3. **Budget Approval:** Is $30-50/month acceptable for OpenAI costs?
4. **Data Privacy:** Any concerns about sending schema (not data) to OpenAI?
5. **Timeline:** Is 4-week estimate acceptable, or need faster/slower?

---

## 19. APPENDIX

### A. Technology Stack Summary
- **Backend:** ASP.NET Core 8.0, C# 12
- **AI Service:** OpenAI GPT-4-turbo via official SDK
- **Database:** SQL Server LocalDB (dev), SQL Server (prod)
- **Frontend:** React 18, Vite, Axios
- **New Dependencies:**
  - `OpenAI` NuGet package (official SDK)
  - `Microsoft.Extensions.Caching.Memory` (result caching)

### B. Estimated LOC (Lines of Code)
- Backend: ~800 lines
  - `AiQueryService.cs`: 300 lines
  - `AiQueryController.cs`: 150 lines
  - `SqlValidator.cs`: 200 lines
  - Tests: 150 lines
- Frontend: ~500 lines
  - `NaturalLanguageQueryPage.jsx`: 300 lines
  - API integration: 100 lines
  - Tests: 100 lines
- Documentation: 500 lines (RAG context)
- **Total: ~1800 lines**

### C. Database Schema Reference
```
Squads (4 columns, ~10-50 rows)
├── TeamMembers (6 columns, ~50-200 rows)
Projects (12 columns, ~100-500 rows)
├── ProjectAllocations (7 columns, ~1000-10000 rows) [Largest table]
└── OnsiteSchedules (8 columns, ~200-1000 rows)
```

---

**End of Plan**
