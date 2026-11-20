import { useEffect, useId, useLayoutEffect, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import './Modal.css';

const FOCUSABLE = [
  'a[href]',
  'button:not([disabled])',
  'input:not([disabled])',
  'select:not([disabled])',
  'textarea:not([disabled])',
  '[tabindex]:not([tabindex="-1"])',
].join(',');

/**
 * Accessible modal dialog (no library; jsdom has no <dialog>.showModal, so the behaviour lives here
 * where tests can exercise it):
 * - role="dialog", aria-modal, labelled by its heading;
 * - everything else on the page is made `inert` while it is open;
 * - focus moves inside on open (to [data-autofocus], else the first control in the body) and is
 *   trapped there with Tab / Shift+Tab;
 * - Escape, the close button and a backdrop click call onClose, unless `busy`;
 * - on close, focus returns to the element that opened it.
 *
 * @param {{ title: string, onClose: () => void, busy?: boolean, children: import('react').ReactNode }} props
 */
export default function Modal({ title, onClose, busy = false, children }) {
  const titleId = useId();
  const dialogRef = useRef(null);
  const [host] = useState(() => document.createElement('div'));
  // Captured during the first render, before any child moves focus.
  const [returnFocusTo] = useState(() => document.activeElement);

  const onCloseRef = useRef(onClose);
  const busyRef = useRef(busy);
  useLayoutEffect(() => {
    onCloseRef.current = onClose;
    busyRef.current = busy;
  });

  // Attach the portal host and make the rest of the page inert.
  useLayoutEffect(() => {
    document.body.appendChild(host);
    const madeInert = Array.from(document.body.children).filter((el) => el !== host && !el.hasAttribute('inert'));
    madeInert.forEach((el) => el.setAttribute('inert', ''));

    return () => {
      madeInert.forEach((el) => el.removeAttribute('inert'));
      host.remove();
      if (returnFocusTo instanceof HTMLElement && returnFocusTo.isConnected) {
        returnFocusTo.focus();
      }
    };
  }, [host, returnFocusTo]);

  // Initial focus (children may already have focused something inside, e.g. the form's first field).
  useEffect(() => {
    const dialog = dialogRef.current;
    if (dialog && !dialog.contains(document.activeElement)) {
      const target =
        dialog.querySelector('[data-autofocus]') ?? dialog.querySelector(`.modal__body :is(${FOCUSABLE})`) ?? dialog;
      target.focus();
    }
  }, []);

  // Escape to close; Tab / Shift+Tab stay inside the dialog.
  useEffect(() => {
    const handleKeyDown = (event) => {
      const dialog = dialogRef.current;
      if (!dialog) {
        return;
      }

      if (event.key === 'Escape') {
        event.preventDefault();
        if (!busyRef.current) {
          onCloseRef.current();
        }
        return;
      }

      if (event.key !== 'Tab') {
        return;
      }

      const focusable = Array.from(dialog.querySelectorAll(FOCUSABLE));
      if (focusable.length === 0) {
        event.preventDefault();
        dialog.focus();
        return;
      }

      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      const active = document.activeElement;
      const outside = !dialog.contains(active);

      if (event.shiftKey && (active === first || active === dialog || outside)) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && (active === last || outside)) {
        event.preventDefault();
        first.focus();
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, []);

  const handleBackdropMouseDown = (event) => {
    if (event.target === event.currentTarget && !busy) {
      onClose();
    }
  };

  return createPortal(
    <div className="modal-backdrop" onMouseDown={handleBackdropMouseDown}>
      <div
        ref={dialogRef}
        className="modal"
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        tabIndex={-1}
      >
        <div className="modal__header">
          <h2 id={titleId} className="modal__title">
            {title}
          </h2>
          <button type="button" className="modal__close" aria-label="Close dialog" onClick={onClose} disabled={busy}>
            ×
          </button>
        </div>
        <div className="modal__body">{children}</div>
      </div>
    </div>,
    host,
  );
}
