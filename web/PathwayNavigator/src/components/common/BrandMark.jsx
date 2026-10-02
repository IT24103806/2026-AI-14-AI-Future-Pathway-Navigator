import React from 'react';

/**
 * PathwayNavigator brand glyph — a compass rose built from a waypoint route.
 * Inherits `currentColor` so it can sit on any glass surface.
 */
const BrandMark = ({ className = 'brand-mark', size = 22 }) => (
  <span className={className} aria-hidden="true">
    <svg
      width={size}
      height={size}
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="1.7"
      strokeLinecap="round"
      strokeLinejoin="round"
    >
      <path d="M12 2.4 4.2 11.6 12 21.6l7.8-10L12 2.4Z" opacity="0.5" />
      <path d="M12 6.3 7.5 11.6l4.5 5.3 4.5-5.3L12 6.3Z" />
      <circle cx="12" cy="11.6" r="1.5" fill="currentColor" stroke="none" />
    </svg>
  </span>
);

export default BrandMark;
