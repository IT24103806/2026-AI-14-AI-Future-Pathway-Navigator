import apiClient from './apiClient';
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
    if (error.response) {
      throw new Error(error.response.data?.message || 'Registration failed. Please try again.');
    }
    throw new Error('Network error. Unable to connect to authentication server.');
  }
};

/**
 * Authenticates user credentials.
 * @param {Object} credentials - { email, password }
 * @returns {Promise<Object>} AuthResponseDto ({ userId, email, role, token, expiresAt })
 */
export const loginApi = async (credentials) => {
  try {
    const response = await apiClient.post(API_ROUTES.LOGIN, {
      email: credentials.email,
      password: credentials.password,
    });
    return response.data;
  } catch (error) {
    if (error.response) {
      throw new Error(error.response.data?.message || 'Invalid email or password.');
    }
    throw new Error('Network error. Unable to connect to authentication server.');
  }
};

/**
 * Authenticates or registers a user via Google ID Token.
 * @param {string} idToken - The Google ID token (JWT)
 * @returns {Promise<Object>} AuthResponseDto ({ userId, email, role, token, expiresAt })
 */
export const googleLoginApi = async (idToken) => {
  try {
    const response = await apiClient.post(API_ROUTES.GOOGLE, {
      idToken,
    });
    return response.data;
  } catch (error) {
    if (error.response) {
      throw new Error(error.response.data?.message || 'Google authentication failed. Please try again.');
    }
    throw new Error('Network error. Unable to connect to authentication server.');
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
    if (error.response) {
      throw new Error(error.response.data?.message || 'Failed to request password reset code.');
    }
    throw new Error('Network error. Unable to connect to authentication server.');
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
    const response = await apiClient.post(API_ROUTES.VERIFY_RESET_CODE, {
      email,
      code,
    });
    return response.data;
  } catch (error) {
    if (error.response) {
      throw new Error(error.response.data?.message || 'Invalid or expired verification code.');
    }
    throw new Error('Network error. Unable to connect to authentication server.');
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
    if (error.response) {
      throw new Error(error.response.data?.message || 'Failed to reset password. Please check your inputs.');
    }
    throw new Error('Network error. Unable to connect to authentication server.');
  }
};

