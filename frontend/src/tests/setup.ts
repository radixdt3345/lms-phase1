import '@testing-library/jest-dom/vitest';
import React from 'react';
import { useSyncExternalStore } from 'use-sync-external-store/shim';

// Polyfill useSyncExternalStore for React 17 (needed by react-redux v9+)
if (!(React as unknown as Record<string, unknown>)['useSyncExternalStore']) {
  (React as unknown as Record<string, unknown>)['useSyncExternalStore'] = useSyncExternalStore;
}
