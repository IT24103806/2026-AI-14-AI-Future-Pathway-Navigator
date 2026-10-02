import apiClient from './apiClient';
import { getApiErrorMessage } from '../utils/apiError';
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
    throw new Error(getApiErrorMessage(error, 'Failed to check profile status.'));
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
    throw new Error(getApiErrorMessage(error, 'Failed to fetch student profile.'));
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
    throw new Error(getApiErrorMessage(error, 'Failed to update student profile.'));
  }
};
