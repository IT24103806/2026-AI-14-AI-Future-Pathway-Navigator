# Agent 3 Pathway Planner

## Purpose

Agent 3 builds a structured, phase-based career roadmap for a student who has already chosen a general pathway from the curated career knowledge base.

## Inputs

- selected_pathway: the one pathway the student chooses
- profile: academic stage, core skills, and career ambitions
- completed_phases: previously completed roadmap stages

## Output

The planner returns:

- workflow_id
- status
- selected_pathway
- roadmap
- missing_skills
- next_action
- validation_errors
- execution_trace

## Validation rules

- the selected pathway must exist in the curated knowledge base
- unknown or fabricated pathway names are rejected
- roadmap stages are generated in a deterministic order
- the next action is always selected from the first incomplete stage

## API route

- POST /api/v1/agent-3/plan

## Backend integration

The ASP.NET backend authenticates the student, loads the saved profile, and forwards the planner request to the Python AI service. This prevents clients from submitting a profile that does not match the signed-in user.

## Notes

This feature is intentionally deterministic and explainable. It does not invent new careers or fabricate course sequences.
