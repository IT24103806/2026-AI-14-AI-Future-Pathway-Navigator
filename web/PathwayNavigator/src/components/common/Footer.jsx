import React from 'react';
import { Link } from 'react-router-dom';
import BrandMark from './BrandMark';

const PRODUCT_LINKS = [
  { to: '/career-discovery', label: 'Career Discovery' },
  { to: '/onboarding', label: 'AI Onboarding' },
  { to: '/student/reality-check', label: 'Reality Check' },
  { to: '/dashboard', label: 'Student Dashboard' },
];

const COMPANY_LINKS = [
  { to: '/about', label: 'About Us' },
  { to: '/contact', label: 'Contact Us' },
  { to: '/register', label: 'Create Account' },
  { to: '/login', label: 'Sign In' },
];

const EXPLORE_LINKS = [
  { to: '/#how-it-works', label: 'How It Works' },
  { to: '/#pathways', label: 'The 4 AI Agents' },
  { to: '/#audience', label: 'Who It Is For' },
  { to: '/#faq', label: 'FAQ' },
];

const Footer = () => (
  <footer className="site-footer app-footer">
    <div className="footer-grid">
      <div className="footer-brand">
        <Link to="/" className="brand">
          <BrandMark />
          <span className="brand-text">
            <strong>PathwayNavigator</strong>
            <small>AI Future Pathway</small>
          </span>
        </Link>
        <p>
          From &ldquo;What should I do next?&rdquo; to realistic, explainable pathways you can
          compare, question and act on — with a counsellor in the loop.
        </p>
        <div className="footer-badges">
          <span className="footer-badge">🧠 4 AI Agents</span>
          <span className="footer-badge">🔐 JWT Secured</span>
          <span className="footer-badge">🇱🇰 Sri Lanka Focused</span>
        </div>
      </div>

      <nav className="footer-col" aria-label="Product">
        <h4>Platform</h4>
        <ul>
          {PRODUCT_LINKS.map((link) => (
            <li key={link.to}>
              <Link to={link.to}>{link.label}</Link>
            </li>
          ))}
        </ul>
      </nav>

      <nav className="footer-col" aria-label="Explore">
        <h4>Explore</h4>
        <ul>
          {EXPLORE_LINKS.map((link) => (
            <li key={link.to}>
              <Link to={link.to}>{link.label}</Link>
            </li>
          ))}
        </ul>
      </nav>

      <nav className="footer-col" aria-label="Company">
        <h4>Company</h4>
        <ul>
          {COMPANY_LINKS.map((link) => (
            <li key={link.to}>
              <Link to={link.to}>{link.label}</Link>
            </li>
          ))}
        </ul>
      </nav>
    </div>

    <div className="footer-bottom">
      <p>&copy; {new Date().getFullYear()} PathwayNavigator. All rights reserved.</p>
      <p className="footer-note">Built as an AI-assisted career guidance platform for students, parents and counsellors.</p>
    </div>
  </footer>
);

export default Footer;
