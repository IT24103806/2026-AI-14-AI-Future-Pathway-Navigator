import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { dashboardPathForRole } from '../../utils/roleRoutes';

const Navbar = () => {
  const { user, isAuthenticated, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <header className="app-header">
      <div className="navbar-container">
        <Link to="/" className="navbar-brand">
          <span className="brand-logo">🧭</span>
          <span className="brand-title">PathwayNavigator</span>
        </Link>

        <nav className="navbar-links">
          {isAuthenticated ? (
            <div className="nav-user-section">
              {user?.role === 'Student' && <Link to="/onboarding" className="nav-link">
                🤖 AI Onboarding
              </Link>}
              {user?.role === 'Student' && <Link to="/career-discovery" className="nav-link">
                🧭 Career Discovery
              </Link>}
              {user?.role === 'Student' && <Link to="/student/reality-check" className="nav-link">🛡️ Reality Check</Link>}
              {(user?.role === 'Counsellor' || user?.role === 'Admin') && <Link to="/counsellor/dashboard" className="nav-link">✅ Approval Centre</Link>}
              <Link to={dashboardPathForRole(user?.role)} className="nav-link">{user?.role} Dashboard</Link>
              <div className="user-profile-badge">
                <span className="user-email">{user?.email}</span>
                <span className={`role-tag role-${user?.role?.toLowerCase()}`}>
                  {user?.role}
                </span>
              </div>
              <button onClick={handleLogout} className="btn-logout">
                Logout
              </button>
            </div>
          ) : (
            <div className="nav-auth-buttons">
              <Link to="/login" className="btn-nav btn-nav-secondary">
                Login
              </Link>
              <Link to="/register" className="btn-nav btn-nav-primary">
                Register
              </Link>
            </div>
          )}
        </nav>
      </div>
    </header>
  );
};

export default Navbar;
