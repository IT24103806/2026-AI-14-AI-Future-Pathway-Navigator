import React from 'react';
import { AGENT_STEPS, WORKFLOW_LOGS } from '../../config/agentJourney';

/**
 * Animated 4-agent pipeline. The travelling pulse, node glow and rotating log
 * line are CSS-driven so the component stays a pure, testable render.
 */
const AgentWorkflow = ({ steps = AGENT_STEPS, logs = WORKFLOW_LOGS, footer = null, className = '' }) => (
  <div className={`workflow-shell ${className}`.trim()}>
    <div className="workflow-track">
      <span className="workflow-track__pulse" aria-hidden="true" />
      {steps.map((step) => (
        <div className="workflow-step" key={step.id}>
          <div className="workflow-node" aria-hidden="true">{step.icon}</div>
          <span className="workflow-step__agent">{step.agent}</span>
          <h4>{step.title}</h4>
          <p>{step.text}</p>
        </div>
      ))}
    </div>

    <div className="workflow-live-log" aria-hidden="true">
      <span className="workflow-live-log__dot" />
      <span className="workflow-live-log__stack">
        {logs.map((line, index) => (
          <span className="workflow-live-log__item" key={line} style={{ '--i': index }}>
            {line}
          </span>
        ))}
      </span>
    </div>

    {footer && <div className="workflow-footer">{footer}</div>}
  </div>
);

export default AgentWorkflow;
