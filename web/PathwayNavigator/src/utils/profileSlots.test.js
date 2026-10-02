import { describe, expect, it } from 'vitest';
import { EMPTY_PROFILE_SLOTS, hasAllSlots, slotsFromProfile } from './profileSlots';

describe('profileSlots', () => {
  it('maps a stored profile onto agent slots', () => {
    const slots = slotsFromProfile({
      fullName: ' Nimal Perera ',
      academicStage: 'Undergraduate',
      coreSkills: ['Python', 'SQL'],
      hobbiesInterests: ['AI'],
      careerAmbitions: 'AI Engineer',
    });

    expect(slots).toEqual({
      full_name: 'Nimal Perera',
      academic_stage: 'Undergraduate',
      core_skills: ['Python', 'SQL'],
      hobbies_interests: ['AI'],
      career_ambitions: 'AI Engineer',
    });
  });

  it('returns blank slots when there is no profile yet', () => {
    expect(slotsFromProfile(null)).toEqual(EMPTY_PROFILE_SLOTS);
    expect(slotsFromProfile(undefined)).toEqual(EMPTY_PROFILE_SLOTS);
  });

  it('does not leak the profile arrays into the slot copy', () => {
    const profile = { coreSkills: ['Python'], hobbiesInterests: [] };
    const slots = slotsFromProfile(profile);
    slots.core_skills.push('Rust');
    expect(profile.coreSkills).toEqual(['Python']);
  });

  it('detects whether a profile carries everything Agent 1 collects', () => {
    expect(hasAllSlots(slotsFromProfile(null))).toBe(false);
    expect(
      hasAllSlots(
        slotsFromProfile({
          academicStage: 'Undergraduate',
          coreSkills: ['Python'],
          hobbiesInterests: ['AI'],
          careerAmbitions: 'AI Engineer',
        })
      )
    ).toBe(true);
    expect(
      hasAllSlots({
        academic_stage: 'Undergraduate',
        core_skills: [],
        hobbies_interests: ['AI'],
        career_ambitions: 'AI Engineer',
      })
    ).toBe(false);
  });
});
