import React, { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';
import FormInput from '../components/common/FormInput';
import PrimaryButton from '../components/common/PrimaryButton';
import AlertBanner from '../components/common/AlertBanner';
import GoogleSignInButton from '../components/common/GoogleSignInButton';
import AuthLayout from '../components/common/AuthLayout';
import { dashboardPathForRole } from '../utils/roleRoutes';

const LoginPage = () => {
  const [formData, setFormData] = useState({ email: '', password: '' });
  const [errors, setErrors] = useState({});
  const [apiError, setApiError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [showPassword, setShowPassword] = useState(false);

  const { login, googleLogin } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const from = location.state?.from?.pathname || '/dashboard';

  const handleChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
    if (errors[name]) {
      setErrors((prev) => ({ ...prev, [name]: '' }));
    }
    if (apiError) setApiError('');
  };

  const validateForm = () => {
    const newErrors = {};
    if (!formData.email.trim()) {
      newErrors.email = 'Email address is required.';
    } else if (!/\S+@\S+\.\S+/.test(formData.email)) {
      newErrors.email = 'Please enter a valid email address.';
    }

    if (!formData.password) {
      newErrors.password = 'Password is required.';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validateForm()) return;

    setIsSubmitting(true);
    setApiError('');

    try {
      const authData = await login({
        email: formData.email,
        password: formData.password,
      });
      navigate(from === '/dashboard' ? dashboardPathForRole(authData.role) : from, { replace: true });
    } catch (err) {
      setApiError(err.message || 'Login failed. Please verify your credentials.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleGoogleSuccess = async (idToken) => {
    setIsSubmitting(true);
    setApiError('');

    try {
      const authData = await googleLogin(idToken);
      navigate(from === '/dashboard' ? dashboardPathForRole(authData.role) : from, { replace: true });
    } catch (err) {
      setApiError(err.message || 'Google authentication failed. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleGoogleError = (error) => {
    setApiError(error.message || 'Google Sign-In was cancelled or failed.');
  };

  return (
    <AuthLayout>
      <div className="auth-header">
        <div className="auth-icon-badge">🔑</div>
        <h2>Welcome Back</h2>
        <p>Sign in to access your PathwayNavigator account</p>
      </div>

      <AlertBanner
        type="error"
        message={apiError}
        onClose={() => setApiError('')}
      />

      {/* Google Authentication */}
      <div className="google-auth-section">
        <GoogleSignInButton
          onSuccess={handleGoogleSuccess}
          onError={handleGoogleError}
          disabled={isSubmitting}
          text="signin_with"
        />
      </div>

      <div className="auth-divider">
        <span>or sign in with email</span>
      </div>

      <form onSubmit={handleSubmit} noValidate className="auth-form">
        <FormInput
          id="login-email"
          label="Email Address"
          type="email"
          name="email"
          value={formData.email}
          onChange={handleChange}
          placeholder="e.g. student@example.com"
          error={errors.email}
          required
          autoComplete="email"
        />

        <div className="form-group-with-link">
          <FormInput
            id="login-password"
            label="Password"
            type={showPassword ? 'text' : 'password'}
            name="password"
            value={formData.password}
            onChange={handleChange}
            placeholder="Enter your password"
            error={errors.password}
            required
            autoComplete="current-password"
          >
            <button
              type="button"
              className="password-toggle-btn"
              onClick={() => setShowPassword(!showPassword)}
              aria-label={showPassword ? 'Hide password' : 'Show password'}
            >
              {showPassword ? '👁️' : '🙈'}
            </button>
          </FormInput>
          <div className="forgot-password-wrapper">
            <Link to="/forgot-password" className="forgot-password-link">
              Forgot password?
            </Link>
          </div>
        </div>

        <PrimaryButton
          type="submit"
          isLoading={isSubmitting}
          disabled={isSubmitting}
          className="w-full"
        >
          Sign In
        </PrimaryButton>
      </form>

      <div className="auth-footer">
        <p>
          Don't have an account?{' '}
          <Link to="/register" className="auth-link">
            Create an Account
          </Link>
        </p>
      </div>
    </AuthLayout>
  );
};

export default LoginPage;
