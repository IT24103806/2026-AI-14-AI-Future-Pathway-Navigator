import apiClient from './apiClient';
import { API_ROUTES } from '../config/constants';

/**
 * Checks whether the current user has completed onboarding profile.
 * @returns {Promise<Object>} ProfileStatusDto ({ hasProfile, isOnboardingCompleted, academicStage })
 */
export const getProfileStatusApi = async () => {
  try {
    const response = await apiClient.get(API_ROUTES.PROFILE_STATUS);
    return response.data;
  } catch (error) {
    if (error.response) {
      throw new Error(error.response.data?.message || 'Failed to check profile status.');
    }
    throw new Error('Network error. Unable to connect to server.');
  }
};

/**
 * Retrieves the full student profile.
 * @returns {Promise<Object>} StudentProfileDto
 */
export const getStudentProfileApi = async () => {
  try {
    const response = await apiClient.get(API_ROUTES.PROFILE);
    return response.data;
  } catch (error) {
    if (error.response) {
      throw new Error(error.response.data?.message || 'Failed to fetch student profile.');
    }
    throw new Error('Network error. Unable to connect to server.');
  }
};

/**
 * Updates the student profile.
 * @param {Object} profileData
 * @returns {Promise<Object>} StudentProfileDto
 */
export const updateStudentProfileApi = async (profileData) => {
  try {
    const response = await apiClient.put(API_ROUTES.PROFILE, profileData);
    return response.data;
  } catch (error) {
    if (error.response) {
      throw new Error(error.response.data?.message || 'Failed to update student profile.');
    }
    throw new Error('Network error. Unable to connect to server.');
  }
};
