import React from 'react';

const AlertBanner = ({ type = 'error', message, onClose }) => {
  if (!message) return null;

  return (
    <div className={`alert-banner alert-${type}`} role="alert">
      <div className="alert-content">
        <span className="alert-icon">
          {type === 'error' ? '⚠️' : type === 'success' ? '✅' : 'ℹ️'}
        </span>
        <span className="alert-message">{message}</span>
      </div>
      {onClose && (
        <button
          type="button"
          className="alert-close-btn"
          onClick={onClose}
          aria-label="Close alert"
        >
          &times;
        </button>
      )}
    </div>
  );
};

export default AlertBanner;
