import { fileURLToPath } from 'node:url';
import path from 'node:path';
import { sveltekit } from '@sveltejs/kit/vite';
import { svelte } from '@sveltejs/vite-plugin-svelte';
import { defineConfig } from 'vite';

const configDir = fileURLToPath(new URL('.', import.meta.url));
const testGlob = path.join(configDir, 'tests/unit/**/*.{test,spec}.{ts,js}').replace(/\\/g, '/');
const setupFile = path.join(configDir, 'src/tests/setup.ts').replace(/\\/g, '/');
export default defineConfig(({ mode }) => {
  const isTest = mode === 'test';
  const testAliases = isTest
    ? {
        '$lib': path.join(configDir, 'src/lib').replace(/\\/g, '/'),
        '$app/navigation': path.join(configDir, 'src/tests/mocks/app-navigation.ts').replace(/\\/g, '/'),
        '$app/environment': path.join(configDir, 'src/tests/mocks/app-environment.ts').replace(/\\/g, '/'),
        '$app/stores': path.join(configDir, 'src/tests/mocks/app-stores.ts').replace(/\\/g, '/')
      }
    : {};

  return {
    plugins: isTest ? [svelte()] : [sveltekit()],
    resolve: {
      alias: testAliases,
      conditions: isTest ? ['browser', 'development'] : undefined
    },
    // @ts-expect-error vitest config - type mismatch between vite versions
    test: {
      globals: true,
      environment: 'jsdom',
      setupFiles: [setupFile],
      include: [testGlob]
    }
  };
});
