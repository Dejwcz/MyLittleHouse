import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';

export default defineConfig({
	plugins: [sveltekit()],
	// @ts-expect-error vitest config - type mismatch between vite versions
	test: {
		globals: true,
		environment: 'jsdom',
		setupFiles: ['./src/tests/setup.ts'],
		include: ['../tests/web/unit/**/*.{test,spec}.{ts,js}']
	}
});
