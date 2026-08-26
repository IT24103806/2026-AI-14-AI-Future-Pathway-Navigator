import React, { useEffect, useState } from 'react';

const RADIUS = 34;
const CIRCUMFERENCE = 2 * Math.PI * RADIUS;

const getRingColor = (score) => {
  if (score >= 70) return '#10b981';
  if (score >= 40) return '#f59e0b';
  return '#ef4444';
};

const ScoreRing = ({ score, label }) => {
  const [animatedScore, setAnimatedScore] = useState(0);
  const color = getRingColor(score);

  useEffect(() => {
    const timer = setTimeout(() => setAnimatedScore(score), 150);
    return () => clearTimeout(timer);
  }, [score]);

  const offset = CIRCUMFERENCE - (animatedScore / 100) * CIRCUMFERENCE;

  return (
    <div className="score-ring-wrapper">
      <div className="score-ring">
        <svg viewBox="0 0 80 80" className="score-ring-svg">
          <circle cx="40" cy="40" r={RADIUS} className="score-ring-track" />
          <circle
            cx="40"
            cy="40"
            r={RADIUS}
            className="score-ring-progress"
            style={{
              stroke: color,
              strokeDasharray: CIRCUMFERENCE,
              strokeDashoffset: offset,
            }}
          />
        </svg>
        <div className="score-ring-inner">
          <span className="score-ring-value">{score}</span>
          <span className="score-ring-percent">%</span>
        </div>
      </div>
      <span className="score-ring-label">{label}</span>
    </div>
  );
};

export default ScoreRing;
