import type { ReactElement } from "react";

/** Props comunes de los iconos (SVG de línea, heredan el color del texto). */
interface IconProps {
  /** Clases utilitarias (tamaño/color); por convención `h-4 w-4`, etc. */
  readonly className?: string;
}

function LineIcon({ className, children }: IconProps & { readonly children: ReactElement | ReactElement[] }): ReactElement {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth={1.8}
      strokeLinecap="round"
      strokeLinejoin="round"
      className={className}
      aria-hidden="true"
    >
      {children}
    </svg>
  );
}

/** Signo «más» para acciones de creación. */
export function PlusIcon({ className }: IconProps): ReactElement {
  return (
    <LineIcon className={className}>
      <path d="M12 5v14M5 12h14" />
    </LineIcon>
  );
}

/** Panel lateral (contraer/expandir la barra). */
export function PanelIcon({ className }: IconProps): ReactElement {
  return (
    <LineIcon className={className}>
      <rect x="3" y="4" width="18" height="16" rx="2" />
      <path d="M9 4v16" />
    </LineIcon>
  );
}

/** Flecha de envío. */
export function SendIcon({ className }: IconProps): ReactElement {
  return (
    <LineIcon className={className}>
      <path d="M12 19V5M6 11l6-6 6 6" />
    </LineIcon>
  );
}

/** Burbuja de conversación. */
export function ChatBubbleIcon({ className }: IconProps): ReactElement {
  return (
    <LineIcon className={className}>
      <path d="M4 5h16v11H8l-4 4z" />
    </LineIcon>
  );
}

/** Casa (volver al inicio). */
export function HomeIcon({ className }: IconProps): ReactElement {
  return (
    <LineIcon className={className}>
      <path d="M3 11l9-7 9 7" />
      <path d="M5 10v10h14V10" />
    </LineIcon>
  );
}

/** Destello (marca de saludo). Relleno sólido para leerse como acento. */
export function SparkIcon({ className }: IconProps): ReactElement {
  return (
    <svg viewBox="0 0 24 24" fill="currentColor" className={className} aria-hidden="true">
      <path d="M12 2l1.7 6.8L20.5 10l-6.8 1.2L12 18l-1.7-6.8L3.5 10l6.8-1.2z" />
    </svg>
  );
}
