import React, { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import FormInput from '../components/common/FormInput';
import PrimaryButton from '../components/common/PrimaryButton';
import AlertBanner from '../components/common/AlertBanner';
import OtpInput from '../components/common/OtpInput';
import { forgotPasswordApi, verifyResetCodeApi, resetPasswordApi } from '../api/authApi';
import AuthLayout from '../components/common/AuthLayout';

const RESET_HIGHLIGHTS = [
  {
    icon: '🔐',
    title: '6-digit verification',
    text: 'We e-mail a single-use code to confirm the request really came from you.',
  },
  {
    icon: '⏱️',
    title: 'Codes expire quickly',
    text: 'If the timer runs out you can request a fresh code from the verification step.',
  },
  {
    icon: '🛡️',
    title: 'Your data stays yours',
    text: 'Resetting a password never touches your profile, pathways or review history.',
  },
];

const STEPS = {
  REQUEST_CODE: 1,
  VERIFY_CODE: 2,
  RESET_PASSWORD: 3,
  SUCCESS: 4,
};

const ForgotPasswordPage = () => {
  const [currentStep, setCurrentStep] = useState(STEPS.REQUEST_CODE);
  const [email, setEmail] = useState('');
  const [code, setCode] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);

  const [errors, setErrors] = useState({});
  const [apiError, setApiError] = useState('');
  const [apiSuccess, setApiSuccess] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Resend OTP countdown timer
  const [resendTimer, setResendTimer] = useState(60);
  const [canResend, setCanResend] = useState(false);

  const navigate = useNavigate();

  useEffect(() => {
    let interval = null;
    if (currentStep === STEPS.VERIFY_CODE && resendTimer > 0) {
      interval = setInterval(() => {
        setResendTimer((prev) => prev - 1);
      }, 1000);
    } else if (resendTimer === 0) {
      setCanResend(true);
      clearInterval(interval);
    }
    return () => clearInterval(interval);
  }, [currentStep, resendTimer]);

  // Step 1: Submit email to request 6-digit OTP
  const handleRequestCode = async (e) => {
    e.preventDefault();
    setApiError('');
    setApiSuccess('');

    if (!email.trim()) {
      setErrors({ email: 'Email address is required.' });
      return;
    }
    if (!/\S+@\S+\.\S+/.test(email)) {
      setErrors({ email: 'Please enter a valid email address.' });
      return;
    }
    setErrors({});

    setIsSubmitting(true);
    try {
      const response = await forgotPasswordApi(email.trim());
      setApiSuccess(response.message || 'Verification code sent to your email.');
      setCurrentStep(STEPS.VERIFY_CODE);
      setResendTimer(60);
      setCanResend(false);
    } catch (err) {
      setApiError(err.message || 'Failed to send verification code. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  // Resend OTP handler
  const handleResendCode = async () => {
    if (!canResend || isSubmitting) return;

    setIsSubmitting(true);
    setApiError('');
    setApiSuccess('');

    try {
      const response = await forgotPasswordApi(email.trim());
      setApiSuccess(response.message || 'A new verification code has been sent.');
      setResendTimer(60);
      setCanResend(false);
    } catch (err) {
      setApiError(err.message || 'Failed to resend verification code.');
    } finally {
      setIsSubmitting(false);
    }
  };

  // Step 2: Validate 6-digit OTP
  const handleVerifyCode = async (e) => {
    if (e) e.preventDefault();
    setApiError('');
    setApiSuccess('');

    if (!code || code.length !== 6) {
      setErrors({ code: 'Please enter the complete 6-digit verification code.' });
      return;
    }
    setErrors({});

    setIsSubmitting(true);
    try {
      const response = await verifyResetCodeApi(email.trim(), code.trim());
      setApiSuccess(response.message || 'Verification code confirmed.');
      setCurrentStep(STEPS.RESET_PASSWORD);
    } catch (err) {
      setApiError(err.message || 'Invalid or expired verification code.');
    } finally {
      setIsSubmitting(false);
    }
  };

  // Step 3: Set new password
  const handleResetPassword = async (e) => {
    e.preventDefault();
    setApiError('');
    setApiSuccess('');

    const newErrors = {};
    if (!newPassword) {
      newErrors.newPassword = 'New password is required.';
    } else if (newPassword.length < 8) {
      newErrors.newPassword = 'Password must be at least 8 characters long.';
    }

    if (!confirmPassword) {
      newErrors.confirmPassword = 'Please confirm your new password.';
    } else if (newPassword !== confirmPassword) {
      newErrors.confirmPassword = 'Passwords do not match.';
    }

    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }
    setErrors({});

    setIsSubmitting(true);
    try {
      const response = await resetPasswordApi({
        email: email.trim(),
        code: code.trim(),
        newPassword,
        confirmPassword,
      });
      setApiSuccess(response.message || 'Password has been reset successfully!');
      setCurrentStep(STEPS.SUCCESS);
    } catch (err) {
      setApiError(err.message || 'Failed to reset password. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <AuthLayout highlights={RESET_HIGHLIGHTS}>
      {/* Step Progression Indicators */}
      {currentStep !== STEPS.SUCCESS && (
        <div className="step-wizard">
          <div className={`step-item ${currentStep >= STEPS.REQUEST_CODE ? 'active' : ''} ${currentStep > STEPS.REQUEST_CODE ? 'completed' : ''}`}>
            <div className="step-number">{currentStep > STEPS.REQUEST_CODE ? '✓' : '1'}</div>
            <span className="step-label">Email</span>
          </div>
          <div className="step-divider" />
          <div className={`step-item ${currentStep >= STEPS.VERIFY_CODE ? 'active' : ''} ${currentStep > STEPS.VERIFY_CODE ? 'completed' : ''}`}>
            <div className="step-number">{currentStep > STEPS.VERIFY_CODE ? '✓' : '2'}</div>
            <span className="step-label">Verify</span>
          </div>
          <div className="step-divider" />
          <div className={`step-item ${currentStep >= STEPS.RESET_PASSWORD ? 'active' : ''}`}>
            <div className="step-number">3</div>
            <span className="step-label">New Password</span>
          </div>
        </div>
      )}

      {/* Alerts */}
      <AlertBanner
        type="error"
        message={apiError}
        onClose={() => setApiError('')}
      />
      <AlertBanner
        type="success"
        message={apiSuccess}
        onClose={() => setApiSuccess('')}
      />

      {/* STEP 1: Enter Email */}
      {currentStep === STEPS.REQUEST_CODE && (
        <div>
          <div className="auth-header">
            <div className="auth-icon-badge">🔐</div>
            <h2>Reset Password</h2>
            <p>Enter your email address and we will send you a 6-digit verification code</p>
          </div>

          <form onSubmit={handleRequestCode} noValidate className="auth-form">
            <FormInput
              id="reset-email"
              label="Email Address"
              type="email"
              name="email"
              value={email}
              onChange={(e) => {
                setEmail(e.target.value);
                if (errors.email) setErrors({});
                if (apiError) setApiError('');
              }}
              placeholder="e.g. student@example.com"
              error={errors.email}
              required
              autoComplete="email"
            />

            <PrimaryButton
              type="submit"
              isLoading={isSubmitting}
              disabled={isSubmitting}
              className="w-full"
            >
              Send Verification Code
            </PrimaryButton>
          </form>
        </div>
      )}

      {/* STEP 2: Enter 6-digit OTP Code */}
      {currentStep === STEPS.VERIFY_CODE && (
        <div>
          <div className="auth-header">
            <div className="auth-icon-badge">📬</div>
            <h2>Enter Verification Code</h2>
            <p>
              We sent a 6-digit code to <strong style={{ color: 'var(--text-main)' }}>{email}</strong>.{' '}
              <button
                type="button"
                className="inline-link-btn"
                onClick={() => {
                  setCurrentStep(STEPS.REQUEST_CODE);
                  setCode('');
                  setApiError('');
                }}
              >
                Change email
              </button>
            </p>
          </div>

          <form onSubmit={handleVerifyCode} className="auth-form">
            <div className="form-group" style={{ textAlign: 'center' }}>
              <label className="form-label" style={{ marginBottom: '0.75rem', display: 'block' }}>
                6-Digit OTP Code
              </label>
              <OtpInput
                length={6}
                value={code}
                onChange={(val) => {
                  setCode(val);
                  if (errors.code) setErrors({});
                  if (apiError) setApiError('');
                }}
                onComplete={(completedCode) => {
                  // Optional auto-submit when all 6 digits entered
                  setCode(completedCode);
                }}
                disabled={isSubmitting}
              />
              {errors.code && <p className="form-error" style={{ textAlign: 'center', marginTop: '0.5rem' }}>{errors.code}</p>}
            </div>

            <div className="resend-section">
              {canResend ? (
                <button
                  type="button"
                  className="resend-code-btn"
                  onClick={handleResendCode}
                  disabled={isSubmitting}
                >
                  Resend Code
                </button>
              ) : (
                <p className="resend-timer-text">
                  Resend code in <span>{resendTimer}s</span>
                </p>
              )}
            </div>

            <PrimaryButton
              type="submit"
              isLoading={isSubmitting}
              disabled={isSubmitting || code.length !== 6}
              className="w-full"
            >
              Verify Code
            </PrimaryButton>
          </form>
        </div>
      )}

      {/* STEP 3: Set New Password */}
      {currentStep === STEPS.RESET_PASSWORD && (
        <div>
          <div className="auth-header">
            <div className="auth-icon-badge">✨</div>
            <h2>Create New Password</h2>
            <p>Enter your new password to secure your account</p>
          </div>

          <form onSubmit={handleResetPassword} noValidate className="auth-form">
            <FormInput
              id="new-password"
              label="New Password"
              type={showPassword ? 'text' : 'password'}
              name="newPassword"
              value={newPassword}
              onChange={(e) => {
                setNewPassword(e.target.value);
                if (errors.newPassword) setErrors((prev) => ({ ...prev, newPassword: '' }));
                if (apiError) setApiError('');
              }}
              placeholder="Min. 8 characters"
              error={errors.newPassword}
              required
              autoComplete="new-password"
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

            <FormInput
              id="confirm-password"
              label="Confirm New Password"
              type={showPassword ? 'text' : 'password'}
              name="confirmPassword"
              value={confirmPassword}
              onChange={(e) => {
                setConfirmPassword(e.target.value);
                if (errors.confirmPassword) setErrors((prev) => ({ ...prev, confirmPassword: '' }));
                if (apiError) setApiError('');
              }}
              placeholder="Re-enter your new password"
              error={errors.confirmPassword}
              required
              autoComplete="new-password"
            />

            <PrimaryButton
              type="submit"
              isLoading={isSubmitting}
              disabled={isSubmitting}
              className="w-full"
            >
              Reset Password
            </PrimaryButton>
          </form>
        </div>
      )}

      {/* SUCCESS VIEW */}
      {currentStep === STEPS.SUCCESS && (
        <div className="auth-success-card" style={{ textAlign: 'center', padding: '1rem 0' }}>
          <div className="auth-icon-badge success-glow" style={{ fontSize: '2.5rem', marginBottom: '1.25rem' }}>
            🎉
          </div>
          <h2>Password Reset Successful!</h2>
          <p style={{ color: 'var(--text-muted)', margin: '0.75rem 0 2rem' }}>
            Your password has been successfully updated. You can now sign in with your new credentials.
          </p>
          <PrimaryButton
            type="button"
            onClick={() => navigate('/login')}
            className="w-full"
          >
            Sign In with New Password
          </PrimaryButton>
        </div>
      )}

      <div className="auth-footer">
        <p>
          Remember your password?{' '}
          <Link to="/login" className="auth-link">
            Back to Sign In
          </Link>
        </p>
      </div>
    </AuthLayout>
  );
};

export default ForgotPasswordPage;
