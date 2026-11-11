import type { Division, Product } from '../types/catalog'

export const divisionLabels: Record<Division, string> = {
  cheese: 'Quesería artesanal',
  goat: 'División caprina',
  cattle: 'División bovina',
}

// Precios y SKU deben coincidir con el catálogo inicial de `products` en database.sql.
// Las fotos se colocan en frontend/public/img/ con los nombres indicados; si faltan, se muestra el emblema.
export const products: Product[] = [
  {
    id: 'queso-canasta',
    division: 'cheese',
    badge: 'La Cremería San Carlos',
    subtitle: 'Moldeado tradicional en cesto',
    name: 'Queso Canasta de Cabra',
    description:
      'Nuestro producto insignia, elaborado con leche entera de cabra recién ordeñada. Se prensa a mano en molde de canasta, que graba su relieve cuadriculado en toda la corteza blanca.',
    specLabel: 'Presentaciones',
    spec: 'Rueda de 4.5 kg y de 5 kg · Conservar de 2 °C a 6 °C',
    priceLabel: '$120 MXN por kg',
    image: '/img/queso-canasta.jpg',
    imageAlt: 'Rueda de queso canasta de cabra con el relieve del cesto',
    presentations: [
      { id: 'rueda-4-5', sku: 'QSO-CAN', label: 'Rueda de 4.5 kg', price: 540 },
      { id: 'rueda-5', sku: 'QSO-CAN', label: 'Rueda de 5 kg', price: 600 },
    ],
  },
  {
    id: 'queso-panela',
    division: 'cheese',
    badge: 'La Cremería San Carlos',
    subtitle: 'Frescura suave de ordeña diaria',
    name: 'Queso Panela Artesanal',
    description:
      'Queso blanco y fresco de leche de cabra, de pasta firme pero suave al corte, sin conservadores ni sustitutos. Ideal para rebanar o asar ligeramente.',
    specLabel: 'Presentaciones',
    spec: 'Pieza de 1 kg · Conservar de 2 °C a 6 °C',
    priceLabel: '$120 MXN por pieza',
    image: '/img/queso-panela.jpg',
    imageAlt: 'Queso panela artesanal rebanado sobre tabla de madera',
    presentations: [{ id: 'pieza-1kg', sku: 'QSO-PAN', label: 'Pieza de 1 kg', price: 120 }],
  },
  {
    id: 'queso-asadero',
    division: 'cheese',
    badge: 'La Cremería San Carlos',
    subtitle: 'Hebra suave y fundido parejo',
    name: 'Queso Asadero Artesanal',
    description:
      'Elaborado en paila con leche de cabra del rancho. Su pasta elástica y brillante funde de manera uniforme en comal, parrilla o cazuela norteña.',
    specLabel: 'Presentaciones',
    spec: 'Pieza de 500 g · Empaque al vacío',
    priceLabel: '$80 MXN por pieza',
    image: '/img/queso-asadero.jpg',
    imageAlt: 'Queso asadero artesanal en pieza de 500 gramos',
    presentations: [{ id: 'pieza-500g', sku: 'QSO-ASA', label: 'Pieza de 500 g', price: 80 }],
  },
  {
    id: 'cabritos',
    division: 'goat',
    badge: 'División caprina',
    subtitle: 'Tradición del monte coahuilense',
    name: 'Cabritos',
    description:
      'Cabritos de leche criados con cuidado familiar en el monte nativo de Castaños, Coahuila. Los entregamos en pie o sacrificados y pelados, en canal lista para cocinar.',
    specLabel: 'Presentaciones',
    spec: 'En pie o en canal · Ficha sanitaria por lote',
    priceLabel: 'Precio por cotización',
    image: '/img/cabritos.jpg',
    imageAlt: 'Cabritos del hato de Rancho San Carlos',
    presentations: [
      { id: 'en-pie', sku: 'GAN-CAB', label: 'Cabrito en pie', price: null },
      { id: 'en-canal', sku: 'CAB-CAN', label: 'Cabrito en canal', price: null },
    ],
  },
  {
    id: 'becerros',
    division: 'cattle',
    badge: 'División bovina',
    subtitle: 'Genética, sanidad y fuerza del norte',
    name: 'Becerros en pie',
    description:
      'Ganado bovino criado únicamente para carne y vendido vivo. Becerros de buena conformación, con nutrición, vacunación y pesaje en báscula registrados, identificados con arete y fierro «SC».',
    specLabel: 'Venta',
    spec: 'Edad promedio de 8 meses · Peso aproximado de 150 kg',
    priceLabel: 'Cotización en pie',
    image: '/img/becerros.jpg',
    imageAlt: 'Becerros en los corrales de Rancho San Carlos',
    presentations: [{ id: 'en-pie', sku: 'GAN-BEC', label: 'Becerro en pie', price: null }],
  },
]
