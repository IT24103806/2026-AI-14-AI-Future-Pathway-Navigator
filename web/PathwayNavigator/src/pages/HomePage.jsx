import React, { useEffect } from 'react';
import { Link, useLocation } from 'react-router-dom';
import Reveal from '../components/common/Reveal';
import AgentWorkflow from '../components/common/AgentWorkflow';
import MarketMeter from '../components/pathway/MarketMeter';

const STATS = [
  { value: '4', label: 'Specialised AI agents', hint: 'Discovery → roadmap → reality check' },
  { value: '3', label: 'Ranked pathways', hint: 'Path A, B and C — never a single verdict' },
  { value: '100%', label: 'Explainable', hint: 'Evidence shown for every match' },
  { value: '1', label: 'Human counsellor', hint: 'Approves high-impact decisions' },
];

const PILLARS = [
  {
    icon: '🚀',
    title: 'The No-Dead-End Philosophy',
    text: 'Weak examination results do not terminate your future. We surface every realistic route — repeat exams, vocational training, NVQ skills and skilled trades — so a setback never becomes a full stop.',
    tone: 'cyan',
    wide: true,
  },
  {
    icon: '📊',
    title: 'Labor-Market Intelligence',
    text: 'Demand, competition and trend signals for every candidate pathway, so choices are evidence-backed instead of guesswork.',
    tone: 'violet',
  },
  {
    icon: '🤝',
    title: 'Human-in-the-Loop',
    text: 'The AI proposes and explains. You decide. Counsellors review any high-impact or high-risk recommendation before it is released.',
    tone: 'mint',
  },
  {
    icon: '🧠',
    title: 'Beyond Grades',
    text: 'Academic results alone never capture aptitude. Skills, interests, budget and constraints are all part of the model.',
    tone: 'cyan',
  },
  {
    icon: '🗺️',
    title: 'Roadmaps, Not Reports',
    text: 'Every pathway ends in concrete milestones, courses and next actions you can tick off as you go.',
    tone: 'violet',
    wide: true,
  },
];

const AUDIENCE = [
  {
    icon: '🎓',
    title: 'O/L & A/L Students',
    text: 'Explore streams, higher education, vocational routes and alternative entries — before results close doors.',
  },
  {
    icon: '🏛️',
    title: 'University Students',
    text: 'Compare specialisations, adjacent careers and skill stacks so your degree connects to a real job market.',
  },
  {
    icon: '👨‍👩‍👧',
    title: 'Parents & Guardians',
    text: 'See the evidence behind each recommendation and support decisions with realistic alternatives.',
  },
  {
    icon: '🧑‍🏫',
    title: 'Counsellors & Schools',
    text: 'A review queue with the full agent evidence trail — validation, tool calls and risk flags included.',
  },
];

const QUOTES = [
  {
    quote:
      'I got three clear options with the market evidence behind each one — and a roadmap I could actually start on Monday.',
    name: 'A/L leaver',
    role: 'Physical Science stream',
  },
  {
    quote:
      'The risk flag and counsellor review are what convinced me. It is not just a chatbot telling my son what to do.',
    name: 'Parent',
    role: 'Colombo',
  },
  {
    quote:
      'I can inspect the validation results, tool calls and execution trace before approving a high-impact pathway.',
    name: 'School counsellor',
    role: 'Approval Centre user',
  },
];

const FAQS = [
  {
    q: 'Do I need perfect exam results to use PathwayNavigator?',
    a: 'No — the opposite. The platform is built for students whose results do not tell the whole story. If a target pathway is not directly reachable, you get the shortest gap-closing plan instead of a rejection.',
  },
  {
    q: 'How does the AI decide what to recommend?',
    a: 'Agent 1 builds your profile conversationally. Agent 2 scores it against a curated career knowledge base and live labour-market signals, then explains every match in plain language. Agent 3 turns the pathway you choose into a tracked roadmap.',
  },
  {
    q: 'Where does a human come in?',
    a: 'Agent 4 runs a feasibility and risk check against your actual results, subjects, budget and skills. Anything high-risk goes to a counsellor approval queue, and their feedback appears on your Reality Check page.',
  },
  {
    q: 'Can parents and counsellors use it too?',
    a: 'Yes. Students get the discovery and roadmap experience; counsellors and admins get a review centre with the complete evidence trail for every high-impact decision.',
  },
];

const HomePage = () => {
  const { hash } = useLocation();

  // Deep links such as /#how-it-works should land on the right section.
  useEffect(() => {
    if (!hash) return;
    const target = document.querySelector(hash);
    if (target) {
      window.requestAnimationFrame(() => target.scrollIntoView({ behavior: 'smooth', block: 'start' }));
    }
  }, [hash]);

  return (
    <div className="home-container">
      {/* ---------------------------------------------------------------- Hero */}
      <section className="hero-section">
        <div className="hero-copy">
          <span className="hero-badge">
            <span className="hero-badge__dot" aria-hidden="true">✨</span>
            AI-assisted career navigation · Sri Lanka focused
          </span>

          <h1 className="hero-title">
            Navigate your future with <span className="gradient-text">confidence</span>
          </h1>

          <p className="hero-lede">
            From &ldquo;What should I do next?&rdquo; to realistic pathways you can understand,
            compare and act on — with labour-market evidence, a trackable roadmap and a
            counsellor in the loop.
          </p>

          <div className="hero-actions">
            <Link to="/register" className="btn btn-primary btn-lg">
              Start your journey →
            </Link>
            <Link to="/login" className="btn btn-glass btn-lg">
              I already have an account
            </Link>
          </div>

          <ul className="hero-points">
            <li>No dead ends</li>
            <li>Explainable matches</li>
            <li>Human reviewed</li>
          </ul>
        </div>

        <div className="hero-visual">
          <div className="hero-showcase glass--top-line">
            <div className="orb-field" aria-hidden="true">
              <span className="orb orb--cyan" style={{ width: 220, height: 220, top: -70, right: -50 }} />
              <span className="orb orb--violet" style={{ width: 240, height: 240, bottom: -90, left: -60 }} />
            </div>

            <div className="hero-showcase__top">
              <span className="hero-showcase__title">Agent 2 · Live match preview</span>
              <span className="live-chip">
                <span className="live-chip__dot" /> analysing
              </span>
            </div>

            <div className="hero-profile-card">
              <span className="hero-profile-card__avatar">A</span>
              <div>
                <strong>After A/L · Physical Science</strong>
                <span>Python · Problem Solving · Robotics</span>
              </div>
            </div>

            <div className="hero-pathways">
              {[
                { rank: 'A', name: 'AI / Machine Learning Engineer', score: '88%', delay: 0 },
                { rank: 'B', name: 'Data Analyst', score: '81%', delay: 120 },
                { rank: 'C', name: 'Software Quality Engineer', score: '74%', delay: 240 },
              ].map((path) => (
                <div className="hero-pathway" key={path.rank} style={{ animationDelay: `${path.delay}ms` }}>
                  <span className="hero-pathway__rank">{path.rank}</span>
                  <span>
                    <strong>{path.name}</strong>
                    <span>Market-checked · explained</span>
                  </span>
                  <span className="hero-pathway__score gradient-text">{path.score}</span>
                </div>
              ))}
            </div>

            <div className="hero-meter-list">
              <MarketMeter icon="📈" label="Demand" value={82} variant="demand" />
              <MarketMeter icon="⚔️" label="Competition" value={60} variant="competition" />
            </div>
          </div>

          <span className="hero-floating-card hero-floating-card--one">🛡️ Counsellor reviewed</span>
          <span className="hero-floating-card hero-floating-card--two">🗺️ Roadmap ready</span>
        </div>
      </section>

      {/* --------------------------------------------------------------- Stats */}
      <section className="stats-strip" aria-label="Platform at a glance">
        <Reveal className="stats-grid" variant="scale">
          {STATS.map((stat) => (
            <div className="stat" key={stat.label}>
              <span className="stat__value">{stat.value}</span>
              <span className="stat__label">{stat.label}</span>
              <span className="stat__hint">{stat.hint}</span>
            </div>
          ))}
        </Reveal>
      </section>

      {/* ------------------------------------------------------------- Pillars */}
      <section className="section" id="pathways">
        <Reveal className="section-head">
          <span className="eyebrow">Why PathwayNavigator</span>
          <h2>Guidance that survives real life</h2>
          <p>
            Grades are one signal. Budget, subjects, aptitude, geography and the job market
            are the others — and they all belong in the decision.
          </p>
        </Reveal>

        <div className="bento-grid">
          {PILLARS.map((pillar, index) => (
            <Reveal
              key={pillar.title}
              delay={index * 70}
              className={[
                'bento-card',
                pillar.wide ? 'bento-card--wide' : '',
                pillar.tone === 'violet' ? 'bento-card--violet' : '',
                pillar.tone === 'mint' ? 'bento-card--mint' : '',
              ]
                .filter(Boolean)
                .join(' ')}
            >
              <span className="bento-card__glow" aria-hidden="true" />
              <span className="feature-icon" aria-hidden="true">{pillar.icon}</span>
              <h3>{pillar.title}</h3>
              <p>{pillar.text}</p>
            </Reveal>
          ))}
        </div>
      </section>

      {/* ------------------------------------------------------------ Workflow */}
      <section className="workflow-section" id="how-it-works">
        <Reveal className="section-head">
          <span className="eyebrow">How it works</span>
          <h2>Four agents, one clear pathway</h2>
          <p>
            Each agent has a single job — and each one hands its evidence to the next, so the
            final recommendation can always be traced back to the data behind it.
          </p>
        </Reveal>

        <AgentWorkflow />

        <div className="how-it-works-grid" style={{ marginTop: '1.25rem' }}>
          {[
            { step: '01', title: 'Talk, don’t type forms', text: 'The AI guide asks, listens and extracts your profile slot by slot — with a standard form alternative if you prefer.' },
            { step: '02', title: 'Compare, don’t accept blindly', text: 'Every pathway ships with demand, competition and trend signals plus a plain-language explanation.' },
            { step: '03', title: 'Track, don’t forget', text: 'Roadmap milestones auto-save to your account so progress survives across sessions.' },
          ].map((item, index) => (
            <Reveal className="how-it-works-card" key={item.step} delay={index * 90}>
              <div className="how-it-works-icon" aria-hidden="true">{item.step}</div>
              <h4>{item.title}</h4>
              <p>{item.text}</p>
            </Reveal>
          ))}
        </div>
      </section>

      {/* ------------------------------------------------------------ Audience */}
      <section className="section" id="audience">
        <Reveal className="section-head">
          <span className="eyebrow">Who is this for</span>
          <h2>Built for everyone around the decision</h2>
          <p>A student decision is rarely a solo decision — so each role sees the right view.</p>
        </Reveal>

        <div className="audience-grid">
          {AUDIENCE.map((item, index) => (
            <Reveal className="audience-card" key={item.title} delay={index * 80}>
              <div className="audience-card__icon" aria-hidden="true">{item.icon}</div>
              <h3>{item.title}</h3>
              <p>{item.text}</p>
            </Reveal>
          ))}
        </div>
      </section>

      {/* -------------------------------------------------------------- Quotes */}
      <section className="section section-tight">
        <Reveal className="section-head">
          <span className="eyebrow">Signals</span>
          <h2>Why people trust the result</h2>
        </Reveal>

        <div className="quote-grid">
          {QUOTES.map((item, index) => (
            <Reveal className="quote-card" key={item.name + index} delay={index * 90} variant="scale">
              <span aria-hidden="true">⭐️⭐️⭐️⭐️⭐️</span>
              <p>{item.quote}</p>
              <footer>
                <span className="user-avatar" aria-hidden="true">
                  {item.name.charAt(0)}
                </span>
                <span>
                  <strong>{item.name}</strong>
                  <span>{item.role}</span>
                </span>
              </footer>
            </Reveal>
          ))}
        </div>
      </section>

      {/* ----------------------------------------------------------------- FAQ */}
      <section className="section" id="faq">
        <Reveal className="section-head">
          <span className="eyebrow">Questions</span>
          <h2>Everything you might be wondering</h2>
        </Reveal>

        <div className="faq-list">
          {FAQS.map((item, index) => (
            <Reveal as="details" className="faq-item" key={item.q} delay={index * 60} variant="up">
              <summary>{item.q}</summary>
              <p>{item.a}</p>
            </Reveal>
          ))}
        </div>
      </section>

      {/* ----------------------------------------------------------------- CTA */}
      <Reveal className="cta-band" variant="scale">
        <span className="eyebrow">Ready when you are</span>
        <h2>Turn your next step into a plan</h2>
        <p>
          Create a free account, talk to the AI guide for a few minutes, and walk away with
          ranked pathways, market evidence and a roadmap you can start today.
        </p>
        <div className="hero-actions">
          <Link to="/register" className="btn btn-primary btn-lg">Create free account</Link>
          <Link to="/about" className="btn btn-glass btn-lg">Learn more about us</Link>
        </div>
      </Reveal>
    </div>
  );
};

export default HomePage;
