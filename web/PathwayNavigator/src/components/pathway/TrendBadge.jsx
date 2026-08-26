import React from 'react';

const TREND_CONFIG = {
  rising: { icon: '📈', label: 'Rising demand', className: 'trend-rising' },
  stable: { icon: '➡️', label: 'Stable demand', className: 'trend-stable' },
  declining: { icon: '📉', label: 'Declining demand', className: 'trend-declining' },
};

const TrendBadge = ({ trend }) => {
  const config = TREND_CONFIG[trend] || TREND_CONFIG.stable;

  return (
    <span className={`trend-badge ${config.className}`}>
      {trend === 'rising' && <span className="trend-pulse-dot" />}
      <span>{config.icon} {config.label}</span>
    </span>
  );
};

export default TrendBadge;
