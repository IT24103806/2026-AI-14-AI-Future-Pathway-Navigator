import React from 'react';
import ConsultantNote from '../consultation/ConsultantNote';

const PHASE_ICONS = {
  Foundation: '🌱',
  'Core Skills': '⚙️',
  Specialization: '🚀',
};

const RoadmapTimeline = ({ roadmap, guidance }) => {
  if (!roadmap || roadmap.length === 0) return null;

  // `guidance` is the consultant write-back list; entries named for a stage are pinned to that step.
  const notesFor = (step) => {
    if (!Array.isArray(guidance) || guidance.length === 0) return null;
    const stepKey = (step.stage ?? step.phase ?? '').toString().toLowerCase();
    const matching = guidance.filter((item) => item.stageKey && item.stageKey.toLowerCase() === stepKey);
    return matching.length > 0 ? matching : null;
  };

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
              {notesFor(step) && <ConsultantNote guidance={notesFor(step)} title="Consultant-recommended" />}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};

export default RoadmapTimeline;
