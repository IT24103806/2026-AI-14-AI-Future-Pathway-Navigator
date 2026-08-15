import React from 'react';
import { Link } from 'react-router-dom';

const NotFoundPage = () => {
  return (
    <div className="not-found-container">
      <div className="not-found-card">
        <h1 className="not-found-code">404</h1>
        <h2>Page Not Found</h2>
        <p>The page or pathway you are looking for does not exist.</p>
        <Link to="/" className="btn btn-primary mt-4">
          Return to Home
        </Link>
      </div>
    </div>
  );
};

export default NotFoundPage;
