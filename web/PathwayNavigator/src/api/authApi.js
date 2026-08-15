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
