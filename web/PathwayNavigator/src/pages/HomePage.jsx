import React from 'react';
import { Link } from 'react-router-dom';

const HomePage = () => {
  return (
    <div className="home-container">
      {/* Hero Section */}
      <section className="hero-section" style={{ textAlign: 'center', padding: '5rem 1.5rem' }}>
        <h1 style={{ fontSize: '3rem', fontWeight: '800', marginBottom: '1rem' }}>
          Navigate Your Future with Confidence
        </h1>
        <p style={{ fontSize: '1.25rem', color: 'var(--text-muted)', maxWidth: '800px', margin: '0 auto 2.5rem' }}>
          From "What should I do next?" to "Here are realistic pathways I can understand, compare, and act on."
        </p>
        <div style={{ display: 'flex', gap: '1rem', justifyContent: 'center' }}>
          <Link to="/register" className="btn btn-primary" style={{ padding: '1rem 2rem', fontSize: '1.1rem' }}>
            Start Your Journey
          </Link>
          <Link to="/login" className="btn btn-nav-secondary" style={{ padding: '1rem 2rem', fontSize: '1.1rem' }}>
            I Already Have an Account
          </Link>
        </div>
      </section>

      {/* Why Use This Platform (Problem/Solution) */}
      <section className="dashboard-container">
        <div className="dashboard-header-card" style={{ justifyContent: 'center', textAlign: 'center', flexDirection: 'column' }}>
          <h2 style={{ fontSize: '2rem', marginBottom: '1rem' }}>Why AI Future Pathway Navigator?</h2>
          <p style={{ color: 'var(--text-muted)', maxWidth: '700px' }}>
            Academic results alone do not capture your interests, practical abilities, or long-term goals[cite: 1]. We look beyond the grades to find where you truly belong.
          </p>
        </div>

        <div className="dashboard-grid">
          {/* Card 1: No Dead Ends */}
          <div className="dash-card">
            <div className="feature-icon">🚀</div>
            <h3>The No-Dead-End Philosophy</h3>
            <p>
              Weak examination results do not terminate your future[cite: 1]. We explore all realistic options, including repeating exams, vocational training, and skilled trades, to keep you moving forward[cite: 1].
            </p>
          </div>

          {/* Card 2: Labor Market Reality */}
          <div className="dash-card">
            <div className="feature-icon">📊</div>
            <h3>Labor-Market Intelligence</h3>
            <p>
              Don't guess what the market wants. We use trustworthy data to identify demand, competition, and emerging opportunities so you can make evidence-backed choices[cite: 1].
            </p>
          </div>

          {/* Card 3: You Are In Control */}
          <div className="dash-card">
            <div className="feature-icon">🤝</div>
            <h3>Human-in-the-Loop Decisions</h3>
            <p>
              The AI proposes and explains the pathways, but you remain fully responsible for accepting, rejecting, or modifying the plan[cite: 1]. You are in the driver's seat.
            </p>
          </div>
        </div>
      </section>

      {/* How It Works (The 4 Agents Simplified) */}
      <section className="dashboard-container" style={{ paddingTop: '1rem' }}>
        <h2 style={{ fontSize: '2rem', textAlign: 'center', marginBottom: '2.5rem' }}>How It Works</h2>
        <div className="dashboard-grid">
          <div className="dash-card" style={{ borderTop: '4px solid var(--primary)' }}>
            <h3>1. Profile Analysis</h3>
            <p>We analyze your academic results, skills, constraints, and personality to build a structured profile[cite: 1].</p>
          </div>
          <div className="dash-card" style={{ borderTop: '4px solid var(--secondary)' }}>
            <h3>2. Career Discovery</h3>
            <p>Discover adjacent or alternative careers that preserve your interests and transferable strengths[cite: 1].</p>
          </div>
          <div className="dash-card" style={{ borderTop: '4px solid var(--success)' }}>
            <h3>3. Market Evaluation</h3>
            <p>We evaluate your candidate careers against real-world labor market signals and skill gaps[cite: 1].</p>
          </div>
          <div className="dash-card" style={{ borderTop: '4px solid var(--danger)' }}>
            <h3>4. Actionable Roadmaps</h3>
            <p>Generate step-by-step pathways from your current position toward your selected goals[cite: 1].</p>
          </div>
        </div>
      </section>

      {/* Target Audience Section */}
      <section className="dashboard-container" style={{ paddingBottom: '5rem' }}>
        <div className="auth-card" style={{ maxWidth: '100%', display: 'flex', flexDirection: 'column', alignItems: 'center', textAlign: 'center' }}>
          <h2 style={{ marginBottom: '1.5rem' }}>Who Is This For?</h2>
          <div className="info-list" style={{ width: '100%', maxWidth: '600px', textAlign: 'left' }}>
            <div className="info-item">
              <span className="info-label">O/L & A/L Students</span>
              <span className="info-value">Explore streams, higher education, or alternative routes[cite: 1].</span>
            </div>
            <div className="info-item">
              <span className="info-label">University Students</span>
              <span className="info-value">Explore specializations and manage career transitions[cite: 1].</span>
            </div>
            <div className="info-item">
              <span className="info-label">Parents & Guardians</span>
              <span className="info-value">Support decisions with evidence-based alternatives[cite: 1].</span>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
};

export default HomePage;