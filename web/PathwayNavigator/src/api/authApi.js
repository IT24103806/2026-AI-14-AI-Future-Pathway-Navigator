import apiClient from './apiClient';
import { getApiErrorMessage } from '../utils/apiError';
import { API_ROUTES } from '../config/constants';

/**
 * Registers a new user.
 * @param {Object} userData - { email, password, roleName }
 * @returns {Promise<Object>} AuthResponseDto ({ userId, email, role, token, expiresAt })
 */
export const registerApi = async (userData) => {
  try {
    const response = await apiClient.post(API_ROUTES.REGISTER, {
      email: userData.email,
      password: userData.password,
      roleName: userData.roleName || 'Student',
    });
    return response.data;
  } catch (error) {
    throw new Error(getApiErrorMessage(error, 'Registration failed. Please try again.'));
  }
};

/**
 * Authenticates user credentials.
 * @param {Object} credentials - { email, password }
 * @returns {Promise<Object>} AuthResponseDto ({ userId, email, role, token, expiresAt })
 */
export const loginApi = async (credentials) => {
  try {
    const response = await apiClient.post(
      API_ROUTES.LOGIN,
      {
        email: credentials.email,
        password: credentials.password,
      },
      // Sign-in changes nothing on the server, so it is safe to repeat if the API hiccups.
      { retryOnTransient: true }
    );
    return response.data;
  } catch (error) {
    throw new Error(getApiErrorMessage(error, 'Invalid email or password.'));
  }
};

/**
 * Authenticates or registers a user via Google ID Token.
 * @param {string} idToken - The Google ID token (JWT)
 * @returns {Promise<Object>} AuthResponseDto ({ userId, email, role, token, expiresAt })
 */
export const googleLoginApi = async (idToken) => {
  try {
    const response = await apiClient.post(
      API_ROUTES.GOOGLE,
      {
        idToken,
      },
      { retryOnTransient: true }
    );
    return response.data;
  } catch (error) {
    throw new Error(getApiErrorMessage(error, 'Google authentication failed. Please try again.'));
  }
};

/**
 * Initiates the password reset process by requesting a 6-digit OTP code.
 * @param {string} email - The user's registered email address
 * @returns {Promise<Object>} { message }
 */
export const forgotPasswordApi = async (email) => {
  try {
    const response = await apiClient.post(API_ROUTES.FORGOT_PASSWORD, {
      email,
    });
    return response.data;
  } catch (error) {
    throw new Error(getApiErrorMessage(error, 'Failed to request password reset code.'));
  }
};

/**
 * Pre-validates the 6-digit OTP code before displaying new password input.
 * @param {string} email - The user's email address
 * @param {string} code - The 6-digit verification code
 * @returns {Promise<Object>} { message }
 */
export const verifyResetCodeApi = async (email, code) => {
  try {
    const response = await apiClient.post(
      API_ROUTES.VERIFY_RESET_CODE,
      {
        email,
        code,
      },
      // Pure validation: repeating it cannot consume the code or send anything.
      { retryOnTransient: true }
    );
    return response.data;
  } catch (error) {
    throw new Error(getApiErrorMessage(error, 'Invalid or expired verification code.'));
  }
};

/**
 * Resets the user's password using the 6-digit verification code.
 * @param {Object} resetData - { email, code, newPassword, confirmPassword }
 * @returns {Promise<Object>} { message }
 */
export const resetPasswordApi = async (resetData) => {
  try {
    const response = await apiClient.post(API_ROUTES.RESET_PASSWORD, {
      email: resetData.email,
      code: resetData.code,
      newPassword: resetData.newPassword,
      confirmPassword: resetData.confirmPassword,
    });
    return response.data;
  } catch (error) {
    throw new Error(getApiErrorMessage(error, 'Failed to reset password. Please check your inputs.'));
  }
};

