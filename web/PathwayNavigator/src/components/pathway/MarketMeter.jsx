import React, { useEffect, useState } from 'react';

const MarketMeter = ({ icon, label, value, variant }) => {
  const [filled, setFilled] = useState(false);

  useEffect(() => {
    const timer = setTimeout(() => setFilled(true), 80);
    return () => clearTimeout(timer);
  }, []);

  return (
    <div className="market-meter">
      <div className="market-meter-header">
        <span>{icon} {label}</span>
        <span className="market-meter-value">{value}/100</span>
      </div>
      <div className="market-meter-track">
        <div
          className={`market-meter-fill market-meter-${variant}`}
          style={{ width: filled ? `${value}%` : '0%' }}
        />
      </div>
    </div>
  );
};

export default MarketMeter;
