import type { TraceRecord } from '../types/catalog'

// Registros de demostración. Se sustituyen por la consulta a la API de trazabilidad
// (vista v_cheese_batch_traceability y fichas de animales) cuando exista el módulo.
export const traceSamples: TraceRecord[] = [
  {
    code: 'QC-104',
    division: 'cheese',
    sampleLabel: 'Lote de queso',
    badge: 'Bitácora de ordeña y lote de quesería',
    title: 'Lote QC-104 · Queso Canasta de Cabra',
    fields: [
      { label: 'Producto', value: 'Queso Canasta de Cabra, rueda de 4.5 kg' },
      { label: 'Origen de la leche', value: 'Ordeña propia de cabra · Rancho San Carlos, Castaños' },
      { label: 'Ingredientes', value: 'Leche entera de cabra, cuajo y sal' },
      { label: 'Proceso', value: 'Cuajado lento y prensado a mano en molde de canasta' },
      { label: 'Conservación', value: 'Refrigeración de 2 °C a 6 °C' },
      { label: 'Elaboración y caducidad', value: '06/11/2026 · 13/11/2026' },
    ],
  },
  {
    code: 'SC-CAB-089',
    division: 'goat',
    sampleLabel: 'Lote de cabritos',
    badge: 'Ficha sanitaria caprina',
    title: 'Registro SC-CAB-089 · Lote de cabritos',
    fields: [
      { label: 'Producto', value: 'Cabritos de leche, en pie o en canal' },
      { label: 'Alimentación', value: 'Leche materna y monte nativo de Coahuila' },
      { label: 'Identificación', value: 'Control por lote con marca interna «SC»' },
      { label: 'Ubicación', value: 'Área caprina · Rancho San Carlos, Castaños' },
      { label: 'Matanza', value: 'Solo después del retiro sanitario de carne' },
      { label: 'Documentación', value: 'Nota de remisión y guía sanitaria de salida' },
    ],
  },
  {
    code: 'SC-BEC-214',
    division: 'cattle',
    sampleLabel: 'Arete de becerro',
    badge: 'Ficha genealógica y sanitaria bovina',
    title: 'Arete SC-BEC-214 · Becerro en pie',
    fields: [
      { label: 'Producto', value: 'Becerro en pie para carne' },
      { label: 'Edad y peso de venta', value: '8 meses · 152 kg en la última pesada' },
      { label: 'Marca de herrar', value: 'Fierro con monograma «SC»' },
      { label: 'Pesaje', value: 'Historial de pesos en la báscula del rancho' },
      { label: 'Sanidad', value: 'Esquema de vacunación registrado' },
      { label: 'Ubicación', value: 'Corrales bovinos · Rancho San Carlos, Castaños' },
    ],
  },
]
