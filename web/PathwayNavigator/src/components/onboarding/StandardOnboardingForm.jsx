import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { submitStandardOnboardingApi } from '../../api/onboardingApi';
import { updateStudentProfileApi } from '../../api/profileApi';
import PrimaryButton from '../common/PrimaryButton';
import AlertBanner from '../common/AlertBanner';

const DEFAULT_SKILLS = ['Python', 'Problem Solving'];
const DEFAULT_INTERESTS = ['AI', 'Gaming'];

const buildInitialFormData = (mode, initialProfile) => {
  const isUpdateMode = mode === 'update';

  return {
    fullName: initialProfile?.fullName || '',
    academicStage: initialProfile?.academicStage || 'Undergraduate',
    coreSkillsInput: '',
    coreSkills: initialProfile?.coreSkills?.length
      ? [...initialProfile.coreSkills]
      : isUpdateMode
        ? []
        : DEFAULT_SKILLS,
    hobbiesInput: '',
    hobbiesInterests: initialProfile?.hobbiesInterests?.length
      ? [...initialProfile.hobbiesInterests]
      : isUpdateMode
        ? []
        : DEFAULT_INTERESTS,
    careerAmbitions: initialProfile?.careerAmbitions || '',
  };
};

/**
 * Manual profile form. In `create` mode it completes onboarding; in `update` mode (reached from
 * the dashboard's "Re-run AI onboarding") it is pre-filled with the saved profile and writes the
 * changes back through PUT /api/profile, which only touches the fields that were sent.
 */
const StandardOnboardingForm = ({ mode = 'create', initialProfile = null }) => {
  const isUpdateMode = mode === 'update';

  const [formData, setFormData] = useState(() => buildInitialFormData(mode, initialProfile));
  const [errors, setErrors] = useState({});
  const [apiError, setApiError] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const navigate = useNavigate();

  const handleAddSkill = (e) => {
    if (e.key === 'Enter' || e.type === 'click') {
      e.preventDefault();
      const val = formData.coreSkillsInput.trim();
      if (val && !formData.coreSkills.includes(val)) {
        setFormData((prev) => ({
          ...prev,
          coreSkills: [...prev.coreSkills, val],
          coreSkillsInput: '',
        }));
      }
    }
  };

  const handleRemoveSkill = (skillToRemove) => {
    setFormData((prev) => ({
      ...prev,
      coreSkills: prev.coreSkills.filter((s) => s !== skillToRemove),
    }));
  };

  const handleAddHobby = (e) => {
    if (e.key === 'Enter' || e.type === 'click') {
      e.preventDefault();
      const val = formData.hobbiesInput.trim();
      if (val && !formData.hobbiesInterests.includes(val)) {
        setFormData((prev) => ({
          ...prev,
          hobbiesInterests: [...prev.hobbiesInterests, val],
          hobbiesInput: '',
        }));
      }
    }
  };

  const handleRemoveHobby = (hobbyToRemove) => {
    setFormData((prev) => ({
      ...prev,
      hobbiesInterests: prev.hobbiesInterests.filter((h) => h !== hobbyToRemove),
    }));
  };

  const validateForm = () => {
    const newErrors = {};
    const fullName = formData.fullName.trim();

    if (!fullName) {
      newErrors.fullName = 'Please tell us your full name so we can personalise your dashboard.';
    } else if (fullName.length < 2) {
      newErrors.fullName = 'Your full name must be at least 2 characters long.';
    }

    if (!formData.academicStage) {
      newErrors.academicStage = 'Please select your academic stage.';
    }
    if (formData.coreSkills.length === 0) {
      newErrors.coreSkills = 'Please add at least one core skill.';
    }
    if (formData.hobbiesInterests.length === 0) {
      newErrors.hobbiesInterests = 'Please add at least one interest or hobby.';
    }
    if (!formData.careerAmbitions.trim()) {
      newErrors.careerAmbitions = 'Please specify your career ambition or dream role.';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!validateForm()) return;

    setIsSubmitting(true);
    setApiError('');

    const payload = {
      fullName: formData.fullName.trim(),
      academicStage: formData.academicStage,
      coreSkills: formData.coreSkills,
      hobbiesInterests: formData.hobbiesInterests,
      careerAmbitions: formData.careerAmbitions.trim(),
    };

    try {
      if (isUpdateMode) {
        // Partial update: the student's saved reality-check context (A/L stream, results, budget)
        // is left untouched because those fields are simply not sent.
        await updateStudentProfileApi(payload);
      } else {
        await submitStandardOnboardingApi({ ...payload, onboardingMethod: 'StandardForm' });
      }
      navigate('/dashboard', { replace: true });
    } catch (err) {
      setApiError(err.message || 'Failed to save your profile. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="standard-form-container">
      <div className="standard-form-header">
        <h3>{isUpdateMode ? 'Update your profile details' : 'Manual Profile Completion'}</h3>
        <p>
          {isUpdateMode
            ? 'Your saved details are pre-filled. Change anything that has moved on and save — your name, academic stage, skills, interests and career ambition all live in your student profile.'
            : 'Fill out your details below if you prefer a structured form over the AI chat.'}
        </p>
      </div>

      <AlertBanner
        type="error"
        message={apiError}
        onClose={() => setApiError('')}
      />

      <form onSubmit={handleSubmit} noValidate className="auth-form">
        <div className="form-group">
          <label htmlFor="fullName" className="form-label">
            Full Name <span className="required-star">*</span>
          </label>
          <input
            id="fullName"
            name="fullName"
            type="text"
            autoComplete="name"
            placeholder="e.g. Nimal Perera"
            value={formData.fullName}
            onChange={(e) => {
              setFormData({ ...formData, fullName: e.target.value });
              if (errors.fullName) setErrors({ ...errors, fullName: '' });
            }}
            className="form-input"
          />
          {errors.fullName && <p className="form-error">{errors.fullName}</p>}
        </div>

        <div className="form-group">
          <label htmlFor="academicStage" className="form-label">
            Academic Stage <span className="required-star">*</span>
          </label>
          <select
            id="academicStage"
            name="academicStage"
            value={formData.academicStage}
            onChange={(e) => setFormData({ ...formData, academicStage: e.target.value })}
            className="form-select"
          >
            <option value="After O/L">After O/L (Ordinary Level)</option>
            <option value="After A/L">After A/L (Advanced Level)</option>
            <option value="Undergraduate">Undergraduate / Bachelor Student</option>
            <option value="Graduated">Graduated / Postgraduate</option>
            <option value="Other">Other</option>
          </select>
          {errors.academicStage && <p className="form-error">{errors.academicStage}</p>}
        </div>

        {/* Core Skills Input */}
        <div className="form-group">
          <label className="form-label">
            Core Skills <span className="required-star">*</span>
          </label>
          <div className="tag-input-wrapper">
            <input
              type="text"
              placeholder="e.g. Python, SQL, Communication (Press Enter to add)"
              value={formData.coreSkillsInput}
              onChange={(e) => setFormData({ ...formData, coreSkillsInput: e.target.value })}
              onKeyDown={handleAddSkill}
              className="form-input"
            />
            <button type="button" onClick={handleAddSkill} className="btn-add-tag">
              + Add
            </button>
          </div>
          <div className="tags-display-container">
            {formData.coreSkills.map((skill, index) => (
              <span key={index} className="form-tag">
                {skill}
                <button type="button" onClick={() => handleRemoveSkill(skill)}>×</button>
              </span>
            ))}
          </div>
          {errors.coreSkills && <p className="form-error">{errors.coreSkills}</p>}
        </div>

        {/* Hobbies & Interests Input */}
        <div className="form-group">
          <label className="form-label">
            Hobbies & Interests <span className="required-star">*</span>
          </label>
          <div className="tag-input-wrapper">
            <input
              type="text"
              placeholder="e.g. Robotics, Gaming, AI, Music (Press Enter to add)"
              value={formData.hobbiesInput}
              onChange={(e) => setFormData({ ...formData, hobbiesInput: e.target.value })}
              onKeyDown={handleAddHobby}
              className="form-input"
            />
            <button type="button" onClick={handleAddHobby} className="btn-add-tag">
              + Add
            </button>
          </div>
          <div className="tags-display-container">
            {formData.hobbiesInterests.map((hobby, index) => (
              <span key={index} className="form-tag hobby-tag">
                {hobby}
                <button type="button" onClick={() => handleRemoveHobby(hobby)}>×</button>
              </span>
            ))}
          </div>
          {errors.hobbiesInterests && <p className="form-error">{errors.hobbiesInterests}</p>}
        </div>

        {/* Career Ambitions */}
        <div className="form-group">
          <label htmlFor="careerAmbitions" className="form-label">
            Career Ambitions <span className="required-star">*</span>
          </label>
          <textarea
            id="careerAmbitions"
            name="careerAmbitions"
            rows={3}
            placeholder="e.g. I aspire to become a Cloud AI Architect and lead enterprise machine learning initiatives."
            value={formData.careerAmbitions}
            onChange={(e) => {
              setFormData({ ...formData, careerAmbitions: e.target.value });
              if (errors.careerAmbitions) setErrors({ ...errors, careerAmbitions: '' });
            }}
            className="form-input"
          />
          {errors.careerAmbitions && <p className="form-error">{errors.careerAmbitions}</p>}
        </div>

        <PrimaryButton
          type="submit"
          isLoading={isSubmitting}
          disabled={isSubmitting}
          className="w-full"
        >
          {isUpdateMode ? 'Save Profile Changes' : 'Save & Access Dashboard →'}
        </PrimaryButton>
      </form>
    </div>
  );
};

export default StandardOnboardingForm;
