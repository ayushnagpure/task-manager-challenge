import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useState } from 'react';
import { describe, expect, it } from 'vitest';
import Modal from './Modal.jsx';

function Harness({ busy = false }) {
  const [open, setOpen] = useState(false);
  return (
    <>
      <button type="button" onClick={() => setOpen(true)}>
        Open
      </button>
      {open && (
        <Modal title="Example dialog" onClose={() => setOpen(false)} busy={busy}>
          <label htmlFor="first">First</label>
          <input id="first" />
          <button type="button">Last</button>
        </Modal>
      )}
    </>
  );
}

/** Renders the harness and opens the dialog with a click on "Open". */
async function renderOpen(props) {
  const result = render(<Harness {...props} />);
  await userEvent.click(screen.getByRole('button', { name: 'Open' }));
  return { ...result, dialog: screen.getByRole('dialog', { name: 'Example dialog' }) };
}

describe('Modal', () => {
  it('is a modal dialog labelled by its heading', async () => {
    const { dialog } = await renderOpen();

    expect(dialog).toHaveAttribute('aria-modal', 'true');
  });

  it('moves focus to the first control in the body when it opens', async () => {
    await renderOpen();

    expect(screen.getByLabelText('First')).toHaveFocus();
  });

  it('traps Tab and Shift+Tab inside the dialog', async () => {
    await renderOpen();
    const close = screen.getByRole('button', { name: 'Close dialog' });
    const last = screen.getByRole('button', { name: 'Last' });

    await userEvent.tab(); // First -> Last
    expect(last).toHaveFocus();
    await userEvent.tab(); // Last -> wraps to the first focusable (Close)
    expect(close).toHaveFocus();
    await userEvent.tab({ shift: true }); // Close -> wraps back to Last
    expect(last).toHaveFocus();
  });

  it('closes on Escape and returns focus to the trigger', async () => {
    await renderOpen();

    await userEvent.keyboard('{Escape}');

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Open' })).toHaveFocus();
  });

  it('closes with the Close button', async () => {
    await renderOpen();

    await userEvent.click(screen.getByRole('button', { name: 'Close dialog' }));

    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('makes the rest of the page inert while open, and restores it on close', async () => {
    const { container } = await renderOpen();

    expect(container).toHaveAttribute('inert');

    await userEvent.keyboard('{Escape}');
    expect(container).not.toHaveAttribute('inert');
  });

  it('ignores Escape and disables Close while busy', async () => {
    await renderOpen({ busy: true });

    await userEvent.keyboard('{Escape}');

    expect(screen.getByRole('dialog')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Close dialog' })).toBeDisabled();
  });
});
