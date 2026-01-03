import '@testing-library/jest-dom/vitest';
import { vi } from 'vitest';

vi.mock('$app/environment', () => ({ browser: false }));
vi.mock('$app/navigation', () => ({ goto: vi.fn() }));
