import React, { useEffect, useRef, useState } from 'react';

const GoogleSignInButton = ({ onSuccess, onError, disabled = false, text = 'signin_with' }) => {
  const buttonRef = useRef(null);
  const [isScriptLoaded, setIsScriptLoaded] = useState(false);
  const [scriptError, setScriptError] = useState(false);

  const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID;

  useEffect(() => {
    // Check if client ID is configured
    if (!clientId) {
      console.warn('VITE_GOOGLE_CLIENT_ID is not configured in .env file.');
      return;
    }

    // Check if Google script is already loaded
    if (window.google?.accounts?.id) {
      setIsScriptLoaded(true);
      return;
    }

    // Check if script tag is already in DOM
    const existingScript = document.getElementById('google-gsi-client');
    if (existingScript) {
      existingScript.addEventListener('load', () => setIsScriptLoaded(true));
      existingScript.addEventListener('error', () => setScriptError(true));
      return;
    }

    // Dynamically insert Google GIS script
    const script = document.createElement('script');
    script.id = 'google-gsi-client';
    script.src = 'https://accounts.google.com/gsi/client';
    script.async = true;
    script.defer = true;
    script.onload = () => setIsScriptLoaded(true);
    script.onerror = () => {
      console.error('Failed to load Google Identity Services script.');
      setScriptError(true);
    };
    document.body.appendChild(script);
  }, [clientId]);

  useEffect(() => {
    if (!isScriptLoaded || !window.google?.accounts?.id || !buttonRef.current || !clientId) {
      return;
    }

    try {
      window.google.accounts.id.initialize({
        client_id: clientId,
        callback: (response) => {
          if (response?.credential) {
            onSuccess(response.credential);
          } else {
            onError?.(new Error('No credentials received from Google.'));
          }
        },
        auto_select: false,
        cancel_on_tap_outside: true,
      });

      // Render official Google button
      window.google.accounts.id.renderButton(buttonRef.current, {
        type: 'standard',
        theme: 'filled_black',
        size: 'large',
        text: text, // 'signin_with' or 'signup_with' or 'continue_with'
        shape: 'rectangular',
        logo_alignment: 'left',
        width: buttonRef.current.parentElement?.clientWidth || 360,
      });
    } catch (err) {
      console.error('Error initializing Google Sign-In button:', err);
    }
  }, [isScriptLoaded, clientId, text, onSuccess, onError]);

  if (!clientId || scriptError) {
    return null;
  }

  return (
    <div className="google-btn-wrapper" style={{ opacity: disabled ? 0.6 : 1, pointerEvents: disabled ? 'none' : 'auto' }}>
      <div ref={buttonRef} className="google-btn-container" />
    </div>
  );
};

export default GoogleSignInButton;
