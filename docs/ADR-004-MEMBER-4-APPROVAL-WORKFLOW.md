# ADR-004: Deterministic Reality Check with Human Approval

**Status:** Accepted  
**Owner:** Member 4 - IT24510004

## Context

Career recommendations can be incorrect, unaffordable or incompatible with entry requirements. The specification requires a distinct validation agent, controlled tools, durable shared state, deterministic checks, observability and an authorized human approval pause.

## Options considered

1. Let the language model approve pathways automatically. This is flexible but unsafe, difficult to reproduce and hard to test.
2. Use only controller `if` statements. This is deterministic but is not an identifiable agent and provides weak execution evidence.
3. Use a dedicated Reality Check Agent with a structured contract, allow-listed catalogue tool, deterministic rules and a persisted approval workflow.

## Decision

Use option 3. Python Agent 4 produces a Pydantic-validated result and an execution summary. ASP.NET Core is the public security boundary, verifies ownership, calls Agent 4 internally and persists the review plus audit history in PostgreSQL. High-risk results are held as `Pending`; only Counsellor/Admin JWT roles may make a decision. Flutter reads status using `/me` endpoints, while React provides the staff approval interface.

## Consequences

- Results are reproducible and can be evaluated with rule-based golden tests.
- High-impact actions cannot bypass human review.
- Tool permissions and stored evidence are clear during the viva.
- The curated prerequisite catalogue must be maintained and expanded.
- The Python service is an internal runtime dependency, so timeout and safe-failure behaviour are necessary.
