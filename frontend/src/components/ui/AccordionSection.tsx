import type { ReactNode } from 'react'
import './accordion.css'

interface AccordionSectionProps {
  title: string
  /** Optional counter shown next to the title (e.g. unread messages). */
  badge?: number
  open: boolean
  onToggle: () => void
  children: ReactNode
}

/** Collapsible sidebar section (accordion style). */
export default function AccordionSection({ title, badge = 0, open, onToggle, children }: AccordionSectionProps) {
  return (
    <section className="accordion">
      <h2>
        <button type="button" className="accordion-header" aria-expanded={open} onClick={onToggle}>
          <span className="accordion-caret" aria-hidden="true">
            {open ? '▾' : '▸'}
          </span>
          <span className="accordion-title">{title}</span>
          {badge > 0 && (
            <span className="nav-badge" aria-label={`${badge} unread`}>
              {badge}
            </span>
          )}
        </button>
      </h2>
      {open && <div className="accordion-body">{children}</div>}
    </section>
  )
}
