import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig, type Plugin } from 'vite';

const ensureViteEnvironments = (): Plugin => ({
  name: 'ensure-vite-environments',
  configureServer(server) {
    const serverWithEnv = server as typeof server & { environments?: Record<string, { config?: { consumer?: string } }> };
    if (!serverWithEnv.environments) {
      serverWithEnv.environments = { client: { config: { consumer: 'client' } } };
    }
  }
});

export default defineConfig({
  plugins: [ensureViteEnvironments(), sveltekit()],
  // @ts-expect-error vitest config - type mismatch between vite versions
  test: {
		globals: true,
		environment: 'jsdom',
		setupFiles: ['./src/tests/setup.ts'],
		include: ['../tests/web/unit/**/*.{test,spec}.{ts,js}']
	}
});
