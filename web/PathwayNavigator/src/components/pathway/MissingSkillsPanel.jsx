import React from 'react';

const MissingSkillsPanel = ({ skills }) => {
  if (!skills || skills.length === 0) {
    return (
      <div className="missing-skills-panel missing-skills-empty">
        <h4>✅ Skill Gap</h4>
        <p>You already have every core skill this pathway needs!</p>
      </div>
    );
  }

  return (
    <div className="missing-skills-panel">
      <h4>🎯 Skills to Learn</h4>
      <div className="slot-tags-container">
        {skills.map((skill, idx) => (
          <span key={idx} className="skill-gap-pill" style={{ animationDelay: `${idx * 0.06}s` }}>
            {skill}
          </span>
        ))}
      </div>
    </div>
  );
};

export default MissingSkillsPanel;
