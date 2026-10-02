/**
 * Shared content for the student journey and the four-agent workflow.
 * Kept in the config layer so components stay hot-reload friendly and the copy
 * can be reused by the home page, the about page and the in-app rails.
 */

export const AGENT_STEPS = [
  {
    id: 'agent-1',
    icon: '🎓',
    agent: 'Agent 1',
    title: 'Profile Discovery',
    text: 'A conversational AI guide captures your academic stage, skills, interests and ambitions.',
  },
  {
    id: 'agent-2',
    icon: '🧭',
    agent: 'Agent 2',
    title: 'Career Matching',
    text: 'Your profile is scored against a curated career knowledge base to surface ranked pathways.',
  },
  {
    id: 'agent-3',
    icon: '📊',
    agent: 'Agent 3',
    title: 'Market & Roadmap',
    text: 'Live labour-market signals, gap analysis and a step-by-step roadmap you can track.',
  },
  {
    id: 'agent-4',
    icon: '🛡️',
    agent: 'Agent 4',
    title: 'Reality Check',
    text: 'Feasibility is validated against your results and budget, then reviewed by a counsellor.',
  },
];

export const WORKFLOW_LOGS = [
  'agent_1 → parsing academic_stage, core_skills …',
  'agent_2 → matching 3 pathways against market data …',
  'agent_3 → building roadmap, evaluating skill gaps …',
  'agent_4 → reality check queued for counsellor review …',
];

export const JOURNEY_STEPS = [
  { id: 1, label: 'Profile', title: 'AI Onboarding' },
  { id: 2, label: 'Discover', title: 'Career Discovery' },
  { id: 3, label: 'Validate', title: 'Reality Check' },
];

export default AGENT_STEPS;
