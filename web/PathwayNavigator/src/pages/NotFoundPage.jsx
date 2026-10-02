import React from 'react';
import { Link } from 'react-router-dom';
import Reveal from '../components/common/Reveal';

const NotFoundPage = () => (
  <div className="not-found-container">
    <Reveal className="not-found-card" variant="scale">
      <h1 className="not-found-code">404</h1>
      <h2>This pathway doesn&rsquo;t exist</h2>
      <p>
        The page you were looking for has moved, or the route was never part of the map.
        Let&rsquo;s get you back on track.
      </p>
      <div className="hero-actions" style={{ justifyContent: 'center', marginTop: '1.5rem' }}>
        <Link to="/" className="btn btn-primary">Return home</Link>
        <Link to="/career-discovery" className="btn btn-glass">Explore pathways</Link>
      </div>
    </Reveal>
  </div>
);

export default NotFoundPage;
