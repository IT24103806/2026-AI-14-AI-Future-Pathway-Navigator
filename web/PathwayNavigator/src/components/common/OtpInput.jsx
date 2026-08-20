import React, { useRef, useEffect } from 'react';

const OtpInput = ({ length = 6, value = '', onChange, onComplete, disabled = false }) => {
  const inputRefs = useRef([]);

  // Ensure value is an array of length `length`
  const digits = Array.from({ length }, (_, i) => value[i] || '');

  useEffect(() => {
    // Auto-focus first input on mount
    if (inputRefs.current[0]) {
      inputRefs.current[0].focus();
    }
  }, []);

  const handleChange = (e, index) => {
    const rawVal = e.target.value;
    const digit = rawVal.replace(/\D/g, '').slice(-1); // Only allow numeric digit

    const newDigits = [...digits];
    newDigits[index] = digit;
    const newOtp = newDigits.join('');

    onChange(newOtp);

    // Auto-advance to next input
    if (digit && index < length - 1) {
      inputRefs.current[index + 1]?.focus();
    }

    if (newOtp.length === length && !newOtp.includes('')) {
      onComplete?.(newOtp);
    }
  };

  const handleKeyDown = (e, index) => {
    if (e.key === 'Backspace') {
      if (!digits[index] && index > 0) {
        // Move to previous box if current box is already empty
        inputRefs.current[index - 1]?.focus();
      } else {
        const newDigits = [...digits];
        newDigits[index] = '';
        onChange(newDigits.join(''));
      }
    } else if (e.key === 'ArrowLeft' && index > 0) {
      inputRefs.current[index - 1]?.focus();
    } else if (e.key === 'ArrowRight' && index < length - 1) {
      inputRefs.current[index + 1]?.focus();
    }
  };

  const handlePaste = (e) => {
    e.preventDefault();
    const pastedData = e.clipboardData.getData('text').replace(/\D/g, '').slice(0, length);
    if (!pastedData) return;

    onChange(pastedData);

    // Focus the box after the pasted digits
    const nextIndex = Math.min(pastedData.length, length - 1);
    inputRefs.current[nextIndex]?.focus();

    if (pastedData.length === length) {
      onComplete?.(pastedData);
    }
  };

  return (
    <div className="otp-container" onPaste={handlePaste}>
      {Array.from({ length }).map((_, index) => (
        <input
          key={index}
          ref={(el) => (inputRefs.current[index] = el)}
          type="text"
          inputMode="numeric"
          pattern="[0-9]*"
          maxLength={1}
          value={digits[index]}
          onChange={(e) => handleChange(e, index)}
          onKeyDown={(e) => handleKeyDown(e, index)}
          disabled={disabled}
          className={`otp-box ${digits[index] ? 'has-value' : ''}`}
          aria-label={`Digit ${index + 1} of verification code`}
          autoComplete="one-time-code"
        />
      ))}
    </div>
  );
};

export default OtpInput;
