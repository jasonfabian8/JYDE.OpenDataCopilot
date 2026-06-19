/** Fuente oficial citada en una respuesta: el dataset de datos.gov.co que la respalda. */
export interface DatasetSource {
  /** Nombre del dataset tal como aparece en el catálogo. */
  readonly dataset: string;
  /** Entidad pública que publica el dataset. */
  readonly entidad: string;
  /** Enlace al dataset en datos.gov.co. */
  readonly url: string;
}
