import { render, screen } from '@testing-library/svelte';
import Home from '../../src/routes/+page.svelte';

test('renders hero headline', () => {
  render(Home);
  expect(screen.getByRole('heading', { level: 1 })).toBeInTheDocument();
});
