import React from 'react';
import { JOURNEY_STEPS } from '../../config/agentJourney';

/**
 * The student journey rail: Onboarding → Discovery → Reality Check.
 * Purely informational (no navigation side effects) so it can sit on any page
 * and always reflect where the student currently is.
 */
const JourneySteps = ({ current = 1, className = '' }) => (
  <ol className={`journey-steps ${className}`.trim()} aria-label="Student journey progress">
    {JOURNEY_STEPS.map((step, index) => {
      const state = step.id < current ? 'is-done' : step.id === current ? 'is-current' : 'is-upcoming';
      return (
        <li className={`journey-step ${state}`} key={step.id} aria-current={step.id === current ? 'step' : undefined}>
          <span className="journey-step__num" aria-hidden="true">
            {step.id < current ? '✓' : step.id}
          </span>
          <span className="journey-step__text">
            <strong>{step.title}</strong>
            <small>{step.label}</small>
          </span>
          {index < JOURNEY_STEPS.length - 1 && <span className="journey-connector" aria-hidden="true" />}
        </li>
      );
    })}
  </ol>
);

export default JourneySteps;
