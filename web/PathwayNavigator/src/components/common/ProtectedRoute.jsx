import React from 'react';
import { Link, Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';

const ProtectedRoute = ({ children, allowedRoles = [] }) => {
  const { isAuthenticated, isLoading, user } = useAuth();
  const location = useLocation();

  if (isLoading) {
    return (
      <div className="full-page-loader">
        <div className="spinner large-spinner"></div>
        <p>Verifying authentication…</p>
      </div>
    );
  }

  if (!isAuthenticated) {
    // Redirect to login, retaining target route in state for post-login redirect
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  if (allowedRoles.length > 0 && !allowedRoles.includes(user?.role)) {
    return (
      <div className="unauthorized-container">
        <div className="not-found-card">
          <span className="auth-icon-badge" aria-hidden="true">🔒</span>
          <h2>Access Denied</h2>
          <p>You don&rsquo;t have permission to view this page with your current role.</p>
          <div className="hero-actions" style={{ justifyContent: 'center' }}>
            <Link to="/dashboard" className="btn btn-primary">Go to my dashboard</Link>
            <Link to="/" className="btn btn-glass">Return home</Link>
          </div>
        </div>
      </div>
    );
  }

  return children;
};

export default ProtectedRoute;
