/**
 * Bridges the stored student profile (camelCase, `StudentProfileDto`) and the slot format Agent 1
 * speaks (snake_case). Used by "Re-run AI onboarding" so the conversation starts from what the
 * student already saved instead of asking everything again.
 */

export const EMPTY_PROFILE_SLOTS = {
  full_name: null,
  academic_stage: null,
  core_skills: [],
  hobbies_interests: [],
  career_ambitions: null,
};

/** True when every value Agent 1 must collect is present. */
export const hasAllSlots = (slots) =>
  Boolean(slots?.academic_stage) &&
  (slots?.core_skills?.length || 0) > 0 &&
  (slots?.hobbies_interests?.length || 0) > 0 &&
  Boolean(slots?.career_ambitions);

/** Maps a StudentProfileDto (or null) to the snake_case slots Agent 1 expects. */
export const slotsFromProfile = (profile) => {
  if (!profile) return { ...EMPTY_PROFILE_SLOTS };

  return {
    full_name: profile.fullName?.trim() || null,
    academic_stage: profile.academicStage?.trim() || null,
    core_skills: [...(profile.coreSkills || [])],
    hobbies_interests: [...(profile.hobbiesInterests || [])],
    career_ambitions: profile.careerAmbitions?.trim() || null,
  };
};
