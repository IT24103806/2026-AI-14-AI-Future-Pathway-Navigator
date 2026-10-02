import React, { useEffect, useRef, useState } from 'react';

/**
 * Lightweight scroll-reveal wrapper.
 * Falls back to "always visible" when IntersectionObserver is unavailable
 * (older browsers, jsdom in unit tests, reduced-motion users).
 */
const Reveal = ({
  children,
  as: Tag = 'div',
  variant = 'up',
  delay = 0,
  className = '',
  ...rest
}) => {
  const nodeRef = useRef(null);
  const [isVisible, setIsVisible] = useState(false);

  useEffect(() => {
    const node = nodeRef.current;
    if (!node || typeof IntersectionObserver === 'undefined') {
      setIsVisible(true);
      return undefined;
    }

    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            setIsVisible(true);
            observer.disconnect();
          }
        });
      },
      { threshold: 0.12, rootMargin: '0px 0px -60px 0px' }
    );

    observer.observe(node);
    return () => observer.disconnect();
  }, []);

  const classes = ['reveal', `reveal--${variant}`, isVisible ? 'is-visible' : '', className]
    .filter(Boolean)
    .join(' ');

  return (
    <Tag ref={nodeRef} className={classes} style={{ '--reveal-delay': `${delay}ms` }} {...rest}>
      {children}
    </Tag>
  );
};

export default Reveal;
