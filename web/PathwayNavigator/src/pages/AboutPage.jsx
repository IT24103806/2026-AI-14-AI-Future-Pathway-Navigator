import React from 'react';
import { Link } from 'react-router-dom';
import Reveal from '../components/common/Reveal';
import AgentWorkflow from '../components/common/AgentWorkflow';
import { AGENT_STEPS } from '../config/agentJourney';

const PRINCIPLES = [
  {
    num: '01',
    title: 'No dead ends',
    text: 'A weak result is a data point, not a verdict. Every analysis must return at least one realistic forward route.',
  },
  {
    num: '02',
    title: 'Evidence over opinion',
    text: 'Matches are scored against a curated career knowledge base and labour-market signals — and the reasoning is always shown.',
  },
  {
    num: '03',
    title: 'You keep the decision',
    text: 'The AI proposes and explains. Accepting, rejecting or revising a pathway is always yours to make.',
  },
  {
    num: '04',
    title: 'Humans review risk',
    text: 'High-impact or high-risk recommendations are paused for counsellor review before they are released to a student.',
  },
];

const AGENT_DETAILS = [
  {
    ...AGENT_STEPS[0],
    tags: ['Conversational intake', 'Slot extraction', 'Profile build'],
  },
  {
    ...AGENT_STEPS[1],
    tags: ['Knowledge base', 'Live market data', 'Ranked A/B/C paths'],
  },
  {
    ...AGENT_STEPS[2],
    tags: ['Roadmap stages', 'Progress tracking', 'Next best action'],
  },
  {
    ...AGENT_STEPS[3],
    tags: ['Feasibility score', 'Risk flags', 'Counsellor queue'],
  },
];

const AboutPage = () => (
  <div className="home-container">
    <section className="about-hero">
      <div className="about-hero__copy">
        <span className="hero-pill-badge">About PathwayNavigator</span>
        <h1 className="hero-title">
          Career guidance that shows its <span className="gradient-text">working</span>
        </h1>
        <p className="hero-lede">
          PathwayNavigator is an AI-assisted career guidance platform for students who deserve
          better than a single, unexplained recommendation. It combines conversational
          profiling, labour-market evidence, roadmap planning and human review into one
          traceable workflow.
        </p>
        <div className="hero-actions">
          <Link to="/register" className="btn btn-primary btn-lg">Create your account</Link>
          <Link to="/contact" className="btn btn-glass btn-lg">Talk to the team</Link>
        </div>
      </div>

      <Reveal className="glass-panel glass--sheen" variant="scale" style={{ borderRadius: 'var(--radius-2xl)' }}>
        <span className="eyebrow">Our mission</span>
        <h2 style={{ margin: '0.6rem 0' }}>
          Make the next step obvious, evidence-backed and reachable — for every student.
        </h2>
        <p className="text-muted">
          Academic results alone do not capture interests, practical ability or long-term
          goals. Our job is to widen the map before narrowing the choice.
        </p>
        <div className="auth-aside__stats" style={{ marginTop: '1.5rem' }}>
          <div className="auth-aside__stat">
            <strong>4</strong>
            <span>Agents</span>
          </div>
          <div className="auth-aside__stat">
            <strong>3</strong>
            <span>Ranked paths</span>
          </div>
          <div className="auth-aside__stat">
            <strong>1</strong>
            <span>Review loop</span>
          </div>
        </div>
      </Reveal>
    </section>

    <section className="section" id="principles">
      <Reveal className="section-head">
        <span className="eyebrow">Principles</span>
        <h2>Four rules we do not bend</h2>
        <p>They shape the prompts, the validation rules and what the interface is allowed to claim.</p>
      </Reveal>

      <div className="principles-grid">
        {PRINCIPLES.map((item, index) => (
          <Reveal className="principle-card" key={item.num} delay={index * 80}>
            <span className="principle-card__num">{item.num}</span>
            <h3>{item.title}</h3>
            <p>{item.text}</p>
          </Reveal>
        ))}
      </div>
    </section>

    <section className="workflow-section" id="agents">
      <Reveal className="section-head">
        <span className="eyebrow">Architecture</span>
        <h2>The four-agent workflow</h2>
        <p>
          Each agent is responsible for one stage and passes structured evidence to the next —
          which is what makes the final recommendation auditable.
        </p>
      </Reveal>

      <AgentWorkflow />

      <div className="agent-grid" style={{ marginTop: '1.25rem' }}>
        {AGENT_DETAILS.map((agent, index) => (
          <Reveal className="agent-card" key={agent.id} delay={index * 80}>
            <div className="agent-card__top">
              <span className="agent-card__icon" aria-hidden="true">{agent.icon}</span>
              <span className="agent-card__tags">
                <span className="hero-pill-badge">{agent.agent}</span>
              </span>
            </div>
            <h3>{agent.title}</h3>
            <p>{agent.text}</p>
            <div className="agent-card__tags">
              {agent.tags.map((tag) => (
                <span className="slot-pill" key={tag}>{tag}</span>
              ))}
            </div>
          </Reveal>
        ))}
      </div>
    </section>

    <Reveal className="cta-band" variant="scale">
      <span className="eyebrow">Next step</span>
      <h2>See the workflow for yourself</h2>
      <p>Create an account and let the first agent build your profile in a few minutes of conversation.</p>
      <div className="hero-actions">
        <Link to="/register" className="btn btn-primary btn-lg">Get started free</Link>
        <Link to="/login" className="btn btn-glass btn-lg">Sign in</Link>
      </div>
    </Reveal>
  </div>
);

export default AboutPage;
