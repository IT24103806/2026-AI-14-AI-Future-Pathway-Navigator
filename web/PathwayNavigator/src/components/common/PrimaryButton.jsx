import React from 'react';

const PrimaryButton = ({
  children,
  type = 'button',
  onClick,
  isLoading = false,
  disabled = false,
  variant = 'primary',
  className = '',
}) => {
  return (
    <button
      type={type}
      onClick={onClick}
      disabled={disabled || isLoading}
      className={`btn btn-${variant} ${isLoading ? 'btn-loading' : ''} ${className}`}
    >
      {isLoading ? (
        <span className="btn-spinner-container">
          <span className="spinner" aria-hidden="true" />
          <span>Processing...</span>
        </span>
      ) : (
        children
      )}
    </button>
  );
};

export default PrimaryButton;
