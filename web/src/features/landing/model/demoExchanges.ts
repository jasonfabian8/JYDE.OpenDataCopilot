import type { DemoExchange } from "./DemoExchange.ts";

/**
 * Intercambios de ejemplo que ilustran el objetivo del producto: pregunta en
 * lenguaje natural → descubrimiento del dataset → consulta SoQL → respuesta citada.
 * Cifras ilustrativas; en producción provienen de la API de Socrata en vivo.
 */
export const demoExchanges: readonly DemoExchange[] = [
  {
    id: "contratos-antioquia",
    question:
      "¿Cuánto se adjudicó en contratos de obra pública en Antioquia durante 2024?",
    answer:
      "En 2024 se adjudicaron 1,87 billones de pesos en contratos de obra pública en Antioquia, distribuidos en 4.312 procesos. El mes con mayor adjudicación fue diciembre.",
    soql: "SELECT sum(valor_contrato) AS total, count(*) AS procesos\nWHERE departamento = 'Antioquia'\n  AND tipo_contrato = 'Obra'\n  AND date_extract_y(fecha_firma) = 2024",
    sources: [
      {
        dataset: "SECOP II — Contratos Electrónicos",
        entidad: "Colombia Compra Eficiente",
        url: "https://www.datos.gov.co/Gastos-Gubernamentales/SECOP-II-Contratos-Electr-nicos/jbjy-vk9h",
      },
    ],
  },
  {
    id: "calidad-aire-bogota",
    question: "¿Cómo estuvo la calidad del aire en Bogotá el último año?",
    answer:
      "El PM2.5 promedio anual en Bogotá fue de 17,4 µg/m³, por encima del umbral de la OMS (5 µg/m³). Las estaciones de Kennedy y Carvajal registraron los picos más altos.",
    soql: "SELECT estacion, avg(pm25) AS promedio\nWHERE ciudad = 'Bogotá'\n  AND date_extract_y(fecha) = 2025\nGROUP BY estacion\nORDER BY promedio DESC",
    sources: [
      {
        dataset: "Calidad del Aire — Red de Monitoreo",
        entidad: "IDEAM",
        url: "https://www.datos.gov.co/Ambiente-y-Desarrollo-Sostenible",
      },
    ],
  },
  {
    id: "matricula-educacion",
    question:
      "¿Qué municipios del Cauca tuvieron mayor deserción escolar en primaria?",
    answer:
      "Timbiquí, Guapi y López de Micay encabezan la deserción en primaria en el Cauca, con tasas entre 8,9% y 11,2%, muy por encima del promedio departamental (4,1%).",
    soql: "SELECT municipio, avg(tasa_desercion) AS desercion\nWHERE departamento = 'Cauca'\n  AND nivel = 'Primaria'\nGROUP BY municipio\nORDER BY desercion DESC\nLIMIT 5",
    sources: [
      {
        dataset: "Estadísticas en Educación — Deserción",
        entidad: "Ministerio de Educación Nacional",
        url: "https://www.datos.gov.co/Educaci-n",
      },
    ],
  },
];
