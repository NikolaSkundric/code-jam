import { useEffect, useRef } from 'react'
import type { ReactNode } from 'react'
import './modal.css'

interface ModalProps {
  title: string
  onClose: () => void
  children: ReactNode
  /** Wider dialog for tabbed content. */
  wide?: boolean
}

/** Modal dialog (native <dialog>: focus trap, Escape to close, backdrop click to close). */
export default function Modal({ title, onClose, children, wide = false }: ModalProps) {
  const ref = useRef<HTMLDialogElement>(null)

  useEffect(() => {
    const dialog = ref.current
    if (dialog && !dialog.open) dialog.showModal()
    return () => dialog?.close()
  }, [])

  return (
    <dialog
      ref={ref}
      className={`modal${wide ? ' modal-wide' : ''}`}
      aria-label={title}
      onCancel={(event) => {
        event.preventDefault()
        onClose()
      }}
      onClick={(event) => {
        if (event.target === ref.current) onClose() // click on the backdrop
      }}
    >
      <div className="modal-body">
        <header className="modal-header">
          <h2>{title}</h2>
          <button type="button" className="link-button modal-close" onClick={onClose} aria-label="Close">
            ×
          </button>
        </header>
        {children}
      </div>
    </dialog>
  )
}
