import '@testing-library/jest-dom/vitest';

// Polyfill window.matchMedia for jsdom (used by useIsMobile hook in sidebar)
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: (query: string) => ({
    matches: false, // default: desktop
    media: query,
    onchange: null,
    addListener: () => {},
    removeListener: () => {},
    addEventListener: () => {},
    removeEventListener: () => {},
    dispatchEvent: () => false,
  }),
});
