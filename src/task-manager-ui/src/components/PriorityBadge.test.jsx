import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import PriorityBadge from './PriorityBadge.jsx';

describe('PriorityBadge', () => {
  it.each(['Critical', 'High', 'Medium', 'Low'])('shows the text label for %s (not colour alone)', (priority) => {
    const { container } = render(<PriorityBadge priority={priority} />);

    expect(screen.getByText(priority)).toBeInTheDocument();
    expect(container.firstChild).toHaveClass(`badge--priority-${priority.toLowerCase()}`);
  });

  it('hides the decorative icon from assistive technology', () => {
    const { container } = render(<PriorityBadge priority="Critical" />);

    expect(container.querySelector('.badge__icon')).toHaveAttribute('aria-hidden', 'true');
  });
});
