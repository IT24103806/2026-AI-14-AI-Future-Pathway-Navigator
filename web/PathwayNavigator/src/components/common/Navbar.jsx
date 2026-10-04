import React, { useEffect, useState } from 'react';
import { Link, NavLink, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { canAccessConsultantDesk, dashboardPathForRole } from '../../utils/roleRoutes';
import BrandMark from './BrandMark';
import ThemeToggle from './ThemeToggle';
import NotificationBell from '../consultation/NotificationBell';

const initialsFor = (user) => {
  const source = user?.fullName || user?.email || 'U';
  return source.trim().charAt(0).toUpperCase();
};

/** Compact door-and-arrow logout glyph; inherits currentColor so it works on any glass surface. */
const LogoutIcon = () => (
  <svg
    viewBox="0 0 24 24"
    width="17"
    height="17"
    fill="none"
    stroke="currentColor"
    strokeWidth="1.9"
    strokeLinecap="round"
    strokeLinejoin="round"
    aria-hidden="true"
  >
    <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
    <path d="m16 17 5-5-5-5" />
    <path d="M21 12H9" />
  </svg>
);

const Navbar = () => {
  const { user, isAuthenticated, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [isScrolled, setIsScrolled] = useState(false);

  const role = user?.role;

  const primaryLinks = isAuthenticated
    ? [
        ...(role === 'Student'
          ? [
              { to: '/onboarding', label: '🤖 AI Onboarding' },
              { to: '/career-discovery', label: '🧭 Career Discovery' },
              { to: '/student/reality-check', label: '🛡️ Reality Check' },
            ]
          : []),
        ...(role === 'Counsellor' || role === 'Admin'
          ? [{ to: '/counsellor/dashboard', label: '✅ Approval Centre' }]
          : []),
        // Guidance (Consultant) and authority (Counsellor) are separate desks on purpose.
        ...(canAccessConsultantDesk(role)
          ? [{ to: '/consultant/dashboard', label: '🤝 Consultant Desk' }]
          : []),
        ...(role === 'Student'
          ? [{ to: '/student/support', label: '🙋 My Questions' }]
          : []),
        { to: dashboardPathForRole(role), label: '📊 Dashboard' },
      ]
    : [
        { to: '/', label: 'Home' },
        { to: '/about', label: 'About' },
        { to: '/contact', label: 'Contact' },
      ];

  useEffect(() => {
    const onScroll = () => setIsScrolled(window.scrollY > 12);
    onScroll();
    window.addEventListener('scroll', onScroll, { passive: true });
    return () => window.removeEventListener('scroll', onScroll);
  }, []);

  // Close the drawer whenever the route changes.
  useEffect(() => {
    setIsDrawerOpen(false);
  }, [location.pathname]);

  // Escape closes the drawer and body scroll is locked while it is open.
  useEffect(() => {
    if (!isDrawerOpen) return undefined;
    const onKeyDown = (event) => {
      if (event.key === 'Escape') setIsDrawerOpen(false);
    };
    window.addEventListener('keydown', onKeyDown);
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    return () => {
      window.removeEventListener('keydown', onKeyDown);
      document.body.style.overflow = previousOverflow;
    };
  }, [isDrawerOpen]);

  const handleLogout = () => {
    setIsDrawerOpen(false);
    logout();
    navigate('/login');
  };

  const renderLinks = (onNavigate) =>
    primaryLinks.map((link) => (
      <NavLink
        key={link.to}
        to={link.to}
        end={link.to === '/'}
        className={({ isActive }) => `nav-link ${isActive ? 'active' : ''}`}
        onClick={onNavigate}
      >
        {link.label}
      </NavLink>
    ));

  return (
    <>
      <a className="skip-link" href="#main-content">
        Skip to content
      </a>
      <header
        className={[
          'site-header',
          'app-header',
          isAuthenticated ? 'site-header--auth' : 'site-header--guest',
          isScrolled ? 'site-header--scrolled' : '',
        ]
          .filter(Boolean)
          .join(' ')}
      >
        <div className="site-header__inner">
          <Link to="/" className="navbar-brand brand" aria-label="PathwayNavigator home">
            <BrandMark />
            <span className="brand-text">
              <strong>PathwayNavigator</strong>
              <small>AI Future Pathway</small>
            </span>
          </Link>

          <nav className="site-nav navbar-links" aria-label="Primary navigation">
            {renderLinks()}
          </nav>

          <div className="nav-actions">
            <ThemeToggle />
            {isAuthenticated && <NotificationBell />}

            {isAuthenticated ? (
              <div className="nav-user-section">
                <Link
                  to={dashboardPathForRole(role)}
                  className="user-profile-badge nav-link--desktop"
                  title={user?.email}
                >
                  <span className="user-email">{user?.email}</span>
                  <span className={`role-tag role-${role?.toLowerCase()}`}>{role}</span>
                </Link>
                <span className="user-avatar nav-user-section__avatar" aria-hidden="true">
                  {initialsFor(user)}
                </span>
                <button
                  type="button"
                  onClick={handleLogout}
                  className="btn-logout"
                  aria-label="Log out"
                  title="Log out"
                >
                  <LogoutIcon />
                  <span className="btn-logout__label">Log out</span>
                </button>
              </div>
            ) : (
              <div className="nav-auth-buttons">
                <Link to="/login" className="btn-nav btn-nav-secondary">Login</Link>
                <Link to="/register" className="btn-nav btn-nav-primary">Register</Link>
              </div>
            )}

            <button
              type="button"
              className="nav-burger"
              aria-expanded={isDrawerOpen}
              aria-controls="mobile-navigation"
              aria-label={isDrawerOpen ? 'Close menu' : 'Open menu'}
              onClick={() => setIsDrawerOpen((open) => !open)}
            >
              <span className="nav-burger__bars" aria-hidden="true" />
            </button>
          </div>
        </div>
      </header>

      <button
        type="button"
        className={`drawer-scrim ${isDrawerOpen ? 'is-open' : ''}`}
        aria-label="Close menu"
        tabIndex={isDrawerOpen ? 0 : -1}
        onClick={() => setIsDrawerOpen(false)}
      />

      <div id="mobile-navigation" className={`mobile-drawer ${isDrawerOpen ? 'is-open' : ''}`}>
        <nav className="mobile-drawer__section" aria-label="Mobile navigation">
          {renderLinks(() => setIsDrawerOpen(false))}
        </nav>

        {isAuthenticated && (
          <div className="mobile-drawer__section">
            <div className="nav-user-section nav-user-section--drawer">
              <Link
                to={dashboardPathForRole(role)}
                className="user-profile-badge"
                title={user?.email}
                onClick={() => setIsDrawerOpen(false)}
              >
                <span className="user-avatar" aria-hidden="true">
                  {initialsFor(user)}
                </span>
                <span className="drawer-user-meta">
                  <span className="user-email">{user?.email}</span>
                  <span className={`role-tag role-${role?.toLowerCase()}`}>{role}</span>
                </span>
              </Link>
              <button type="button" onClick={handleLogout} className="btn-logout btn-logout--block">
                <LogoutIcon />
                <span>Log out</span>
              </button>
            </div>
          </div>
        )}

        {!isAuthenticated && (
          <div className="mobile-drawer__footer">
            <Link to="/login" className="btn-nav btn-nav-secondary" onClick={() => setIsDrawerOpen(false)}>
              Login
            </Link>
            <Link to="/register" className="btn-nav btn-nav-primary" onClick={() => setIsDrawerOpen(false)}>
              Register
            </Link>
          </div>
        )}
      </div>
    </>
  );
};

export default Navbar;
