import apiClient from './apiClient';
import { API_ROUTES } from '../config/constants';

/**
 * Sends a message turn to Agent 1 via the backend API gateway.
 * @param {Object} data - { message, history, current_slots }
 * @returns {Promise<Object>} AgentChatResponseDto ({ reply_message, extracted_slots, missing_slots, is_complete, is_saved })
 */
export const sendAgentChatMessageApi = async (data) => {
  try {
    const response = await apiClient.post(API_ROUTES.ONBOARDING_CHAT, data);
    return response.data;
  } catch (error) {
    if (error.response) {
      throw new Error(error.response.data?.message || 'Failed to communicate with onboarding agent.');
    }
    throw new Error('Network error. Unable to reach authentication server.');
  }
};

/**
 * Completes onboarding via the standard form.
 * @param {Object} profileData - { academicStage, coreSkills, hobbiesInterests, careerAmbitions }
 * @returns {Promise<Object>} StudentProfileDto
 */
export const submitStandardOnboardingApi = async (profileData) => {
  try {
    const response = await apiClient.post(API_ROUTES.ONBOARDING_STANDARD, profileData);
    return response.data;
  } catch (error) {
    if (error.response) {
      throw new Error(error.response.data?.message || 'Failed to save student profile.');
    }
    throw new Error('Network error. Unable to reach authentication server.');
  }
};
