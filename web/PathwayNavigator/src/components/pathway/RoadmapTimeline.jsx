import React from 'react';

const PHASE_ICONS = {
  Foundation: '🌱',
  'Core Skills': '⚙️',
  Specialization: '🚀',
};

const RoadmapTimeline = ({ roadmap }) => {
  if (!roadmap || roadmap.length === 0) return null;

  return (
    <div className="roadmap-timeline">
      <h4>🗺️ Learning Roadmap</h4>
      <div className="roadmap-steps">
        {roadmap.map((step, idx) => (
          <div className="roadmap-step" key={idx} style={{ animationDelay: `${idx * 0.15}s` }}>
            <div className="roadmap-step-marker">
              <span className="roadmap-step-icon">{PHASE_ICONS[step.phase] || '📍'}</span>
              {idx < roadmap.length - 1 && <span className="roadmap-step-line" />}
            </div>
            <div className="roadmap-step-content">
              <span className="roadmap-step-phase">{step.phase}</span>
              <span className="roadmap-step-course">{step.course}</span>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

export default RoadmapTimeline;
