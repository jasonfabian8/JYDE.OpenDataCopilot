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

/** Rueda dentada (configuración). */
export function GearIcon({ className }: IconProps): ReactElement {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="currentColor"
      fillRule="evenodd"
      clipRule="evenodd"
      className={className}
      aria-hidden="true"
    >
      <path d="M19.43 12.98c.04-.32.07-.64.07-.98 0-.34-.03-.66-.07-.98l2.11-1.65c.19-.15.24-.42.12-.64l-2-3.46c-.12-.22-.39-.3-.61-.22l-2.49 1c-.52-.4-1.08-.73-1.69-.98l-.38-2.65C14.46 2.18 14.25 2 14 2h-4c-.25 0-.46.18-.49.42l-.38 2.65c-.61.25-1.17.59-1.69.98l-2.49-1c-.23-.09-.49 0-.61.22l-2 3.46c-.13.22-.07.49.12.64l2.11 1.65c-.04.32-.07.65-.07.98 0 .33.03.66.07.98l-2.11 1.65c-.19.15-.24.42-.12.64l2 3.46c.12.22.39.3.61.22l2.49-1c.52.4 1.08.73 1.69.98l.38 2.65c.03.24.24.42.49.42h4c.25 0 .46-.18.49-.42l.38-2.65c.61-.25 1.17-.59 1.69-.98l2.49 1c.23.09.49 0 .61-.22l2-3.46c.12-.22.07-.49-.12-.64l-2.11-1.65zM12 15.5c-1.93 0-3.5-1.57-3.5-3.5s1.57-3.5 3.5-3.5 3.5 1.57 3.5 3.5-1.57 3.5-3.5 3.5z" />
    </svg>
  );
}

/** Cerrar (aspa). */
export function CloseIcon({ className }: IconProps): ReactElement {
  return (
    <LineIcon className={className}>
      <path d="M6 6l12 12M18 6L6 18" />
    </LineIcon>
  );
}

/** Marcador (memoria/objetivo de la conversación). */
export function MemoryIcon({ className }: IconProps): ReactElement {
  return (
    <LineIcon className={className}>
      <path d="M6 3h12v18l-6-4-6 4z" />
    </LineIcon>
  );
}

/** Barras (artefactos: tablas/gráficos). */
export function ChartBarIcon({ className }: IconProps): ReactElement {
  return (
    <LineIcon className={className}>
      <path d="M4 20V10M10 20V4M16 20v-8M22 20H2" />
    </LineIcon>
  );
}

/** Portapapeles con lista (auditoría). */
export function AuditIcon({ className }: IconProps): ReactElement {
  return (
    <LineIcon className={className}>
      <path d="M9 4h6v3H9zM7 5H5v16h14V5h-2M8 11h8M8 15h8" />
    </LineIcon>
  );
}

/** Disquete (guardar en la base de datos). */
export function SaveIcon({ className }: IconProps): ReactElement {
  return (
    <LineIcon className={className}>
      <path d="M5 3h11l3 3v15H5zM8 3v5h7V3M8 14h8v7H8z" />
    </LineIcon>
  );
}

/** Papelera (eliminar). */
export function TrashIcon({ className }: IconProps): ReactElement {
  return (
    <LineIcon className={className}>
      <path d="M4 7h16M9 7V4h6v3M6 7l1 14h10l1-14M10 11v6M14 11v6" />
    </LineIcon>
  );
}

/** Tres puntos verticales (menú de acciones). */
export function KebabIcon({ className }: IconProps): ReactElement {
  return (
    <svg viewBox="0 0 24 24" fill="currentColor" className={className} aria-hidden="true">
      <circle cx="12" cy="5" r="1.6" />
      <circle cx="12" cy="12" r="1.6" />
      <circle cx="12" cy="19" r="1.6" />
    </svg>
  );
}
