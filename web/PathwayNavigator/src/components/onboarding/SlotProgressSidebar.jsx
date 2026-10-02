import React from 'react';

const SlotProgressSidebar = ({ slots, isUpdateMode = false }) => {
  const fullName = slots?.full_name;
  const academicStage = slots?.academic_stage;
  const coreSkills = slots?.core_skills || [];
  const hobbiesInterests = slots?.hobbies_interests || [];
  const careerAmbitions = slots?.career_ambitions;

  // Calculate completed count out of 4
  const completedCount = [
    Boolean(academicStage),
    coreSkills.length > 0,
    hobbiesInterests.length > 0,
    Boolean(careerAmbitions && careerAmbitions.trim().length > 0),
  ].filter(Boolean).length;

  const percentage = Math.round((completedCount / 4) * 100);

  return (
    <aside className="slot-sidebar">
      <div className="slot-sidebar-header">
        <h3>Profile Progress</h3>
        <span className="slot-percentage">{percentage}%</span>
      </div>

      <div className="progress-bar-container">
        <div className="progress-bar-fill" style={{ width: `${percentage}%` }} />
      </div>

      <p className="slot-sidebar-subtitle">
        {isUpdateMode
          ? 'Your saved details are loaded. Agent 1 updates them here as you chat.'
          : 'Agent 1 is dynamically extracting your profile details as you chat.'}
      </p>

      <div className="slot-items-list">
        {/* Name (only shown once Agent 1 knows it) */}
        {fullName && (
          <div className="slot-card slot-completed">
            <div className="slot-card-header">
              <span className="slot-icon">🙋</span>
              <span className="slot-title">Name</span>
              <span className="slot-status-icon">✓</span>
            </div>
            <div className="slot-tag-badge">{fullName}</div>
          </div>
        )}

        {/* Slot 1: Academic Stage */}
        <div className={`slot-card ${academicStage ? 'slot-completed' : 'slot-pending'}`}>
          <div className="slot-card-header">
            <span className="slot-icon">🎓</span>
            <span className="slot-title">Academic Stage</span>
            <span className="slot-status-icon">{academicStage ? '✓' : '○'}</span>
          </div>
          {academicStage ? (
            <div className="slot-tag-badge">{academicStage}</div>
          ) : (
            <span className="slot-empty-hint">Waiting for response...</span>
          )}
        </div>

        {/* Slot 2: Core Skills */}
        <div className={`slot-card ${coreSkills.length > 0 ? 'slot-completed' : 'slot-pending'}`}>
          <div className="slot-card-header">
            <span className="slot-icon">💡</span>
            <span className="slot-title">Core Skills</span>
            <span className="slot-status-icon">{coreSkills.length > 0 ? '✓' : '○'}</span>
          </div>
          {coreSkills.length > 0 ? (
            <div className="slot-tags-container">
              {coreSkills.map((skill, index) => (
                <span key={index} className="slot-pill">{skill}</span>
              ))}
            </div>
          ) : (
            <span className="slot-empty-hint">e.g. Python, Problem Solving</span>
          )}
        </div>

        {/* Slot 3: Hobbies & Interests */}
        <div className={`slot-card ${hobbiesInterests.length > 0 ? 'slot-completed' : 'slot-pending'}`}>
          <div className="slot-card-header">
            <span className="slot-icon">🎨</span>
            <span className="slot-title">Hobbies & Interests</span>
            <span className="slot-status-icon">{hobbiesInterests.length > 0 ? '✓' : '○'}</span>
          </div>
          {hobbiesInterests.length > 0 ? (
            <div className="slot-tags-container">
              {hobbiesInterests.map((hobby, index) => (
                <span key={index} className="slot-pill hobby-pill">{hobby}</span>
              ))}
            </div>
          ) : (
            <span className="slot-empty-hint">e.g. Robotics, Gaming, AI</span>
          )}
        </div>

        {/* Slot 4: Career Ambitions */}
        <div className={`slot-card ${careerAmbitions ? 'slot-completed' : 'slot-pending'}`}>
          <div className="slot-card-header">
            <span className="slot-icon">🚀</span>
            <span className="slot-title">Career Ambitions</span>
            <span className="slot-status-icon">{careerAmbitions ? '✓' : '○'}</span>
          </div>
          {careerAmbitions ? (
            <div className="slot-tag-badge ambition-badge">{careerAmbitions}</div>
          ) : (
            <span className="slot-empty-hint">e.g. AI Engineer, Software Architect</span>
          )}
        </div>
      </div>
    </aside>
  );
};

export default SlotProgressSidebar;
