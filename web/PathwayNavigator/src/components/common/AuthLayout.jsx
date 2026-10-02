import React from 'react';
import BrandMark from './BrandMark';

/**
 * Shared split layout for Login / Register / Forgot password.
 * Left: marketing showcase. Right: the actual glass form card (children).
 * On tablet and below the showcase collapses away so the form is never
 * competing for space with decoration.
 */
const DEFAULT_HIGHLIGHTS = [
  {
    icon: '🧭',
    title: 'Ranked, explainable pathways',
    text: 'Three realistic options — not a single verdict — each with market evidence.',
  },
  {
    icon: '🛡️',
    title: 'A counsellor stays in the loop',
    text: 'High-impact decisions are reviewed by a human before they are released.',
  },
  {
    icon: '🗺️',
    title: 'Roadmaps you can actually track',
    text: 'Milestones, dates and next actions you can tick off as you progress.',
  },
];

const AuthLayout = ({ children, highlights = DEFAULT_HIGHLIGHTS }) => (
  <div className="auth-page-container">
    <aside className="auth-aside">
      <div className="orb-field" aria-hidden="true">
        <span className="orb orb--cyan" style={{ width: 240, height: 240, top: -60, left: -40 }} />
        <span className="orb orb--violet" style={{ width: 260, height: 260, bottom: -80, right: -60 }} />
      </div>

      <span className="hero-pill-badge" style={{ alignSelf: 'flex-start' }}>
        <BrandMark size={16} /> PathwayNavigator
      </span>

      <h2>
        Your future, <span className="gradient-text">clearly mapped.</span>
      </h2>
      <p>
        Join students, parents and counsellors using AI-assisted, evidence-backed
        pathway planning instead of guesswork.
      </p>

      <ul className="auth-aside__list">
        {highlights.map((item) => (
          <li className="auth-aside__item" key={item.title}>
            <span className="auth-aside__icon" aria-hidden="true">{item.icon}</span>
            <span>
              <strong>{item.title}</strong>
              {item.text}
            </span>
          </li>
        ))}
      </ul>

      <div className="auth-aside__stats">
        <div className="auth-aside__stat">
          <strong>4</strong>
          <span>AI agents</span>
        </div>
        <div className="auth-aside__stat">
          <strong>3</strong>
          <span>Ranked paths</span>
        </div>
        <div className="auth-aside__stat">
          <strong>100%</strong>
          <span>Human reviewed</span>
        </div>
      </div>
    </aside>

    <div className="auth-card">{children}</div>
  </div>
);

export default AuthLayout;
