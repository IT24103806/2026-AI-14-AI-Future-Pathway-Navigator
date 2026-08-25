import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { submitStandardOnboardingApi } from '../../api/onboardingApi';
import PrimaryButton from '../common/PrimaryButton';
import AlertBanner from '../common/AlertBanner';

const StandardOnboardingForm = () => {
  const [formData, setFormData] = useState({
    academicStage: 'Undergraduate',
    coreSkillsInput: '',
    coreSkills: ['Python', 'Problem Solving'],
    hobbiesInput: '',
    hobbiesInterests: ['AI', 'Gaming'],
    careerAmbitions: '',
  });

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

    try {
      await submitStandardOnboardingApi({
        academicStage: formData.academicStage,
        coreSkills: formData.coreSkills,
        hobbiesInterests: formData.hobbiesInterests,
        careerAmbitions: formData.careerAmbitions.trim(),
        onboardingMethod: 'StandardForm',
      });
      navigate('/dashboard', { replace: true });
    } catch (err) {
      setApiError(err.message || 'Failed to save onboarding details.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="standard-form-container">
      <div className="standard-form-header">
        <h3>Manual Profile Completion</h3>
        <p>Fill out your details below if you prefer a structured form over the AI chat.</p>
      </div>

      <AlertBanner
        type="error"
        message={apiError}
        onClose={() => setApiError('')}
      />

      <form onSubmit={handleSubmit} noValidate className="auth-form">
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
          Save & Access Dashboard →
        </PrimaryButton>
      </form>
    </div>
  );
};

export default StandardOnboardingForm;
