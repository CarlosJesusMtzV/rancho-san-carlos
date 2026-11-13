<script setup lang="ts">
import { ref } from 'vue'
import { RouterLink } from 'vue-router'

import { UiToggle } from '../../../shared/components'
import { useTheme } from '../../../shared/composables/useTheme'
import BrandLogo from '../components/BrandLogo.vue'
import BrandSeal from '../components/BrandSeal.vue'
import OrderDialog from '../components/OrderDialog.vue'
import ProductCatalog, { type CatalogFilter } from '../components/ProductCatalog.vue'
import TraceLookup from '../components/TraceLookup.vue'
import { useOrder } from '../composables/useOrder'
import { craftFeatures, pillars } from '../data/brand'
import { products } from '../data/catalog'
import type { Presentation, Product } from '../types/catalog'
import '../styles/landing.css'

const { dark } = useTheme()
const order = useOrder()
const orderOpen = ref(false)
const catalogFilter = ref<CatalogFilter>('all')

function addToOrder(product: Product, presentation: Presentation): void {
  order.add(product, presentation)
  orderOpen.value = true
}

const year = new Date().getFullYear()
</script>

<template>
  <div class="lp-root">
    <a class="lp-skip" href="#contenido">Saltar al contenido</a>

    <div class="lp-strip">
      <p><span>Rancho San Carlos y La Cremería San Carlos</span> · Castaños, Coahuila</p>
      <div class="lp-strip-actions">
        <UiToggle v-model="dark" type="switch" label="Modo oscuro" class="theme" />
        <RouterLink to="/panel">Sistema</RouterLink>
      </div>
    </div>

    <header class="lp-header">
      <div class="lp-header-inner">
        <nav class="lp-nav" aria-label="Secciones del sitio">
          <RouterLink :to="{ hash: '#proposito' }">Nuestro origen</RouterLink>
          <RouterLink :to="{ hash: '#productos' }">Quesos y ganado</RouterLink>
          <RouterLink :to="{ hash: '#oficio' }">Oficio artesanal</RouterLink>
        </nav>
        <RouterLink class="lp-logo-link" to="/" aria-label="Rancho San Carlos, inicio"><BrandLogo /></RouterLink>
        <nav class="lp-nav lp-nav--end" aria-label="Más secciones">
          <RouterLink :to="{ hash: '#pilares' }">Pilares</RouterLink>
          <RouterLink :to="{ hash: '#trazabilidad' }">Trazabilidad</RouterLink>
          <button type="button" class="lp-btn lp-btn--ink lp-btn--small" @click="orderOpen = true">
            Pedido ({{ order.itemCount.value }})
          </button>
        </nav>
      </div>
    </header>

    <main id="contenido">
      <section class="lp-hero">
        <div class="lp-hero-inner">
          <span class="lp-accent-text">Región Centro-Desierto · Castaños, Coahuila</span>
          <h1>Nuestra raíz es el monte y el molde.</h1>
          <p>
            Una ganadería y una quesería que nacen del carácter del semidesierto coahuilense: vacas criadas para
            carne y chivas que dan cabritos y la leche con la que hacemos queso canasta y panela.
          </p>
          <div class="lp-hero-actions">
            <RouterLink class="lp-btn lp-btn--ocre" :to="{ hash: '#productos' }">Ver quesos, cabritos y becerros</RouterLink>
            <RouterLink class="lp-btn lp-btn--outline" :to="{ hash: '#oficio' }">Conocer el proceso artesanal</RouterLink>
          </div>
        </div>
      </section>

      <section id="proposito" class="lp-section lp-purpose lp-pattern-basket">
        <div class="lp-narrow">
          <BrandSeal />
          <p class="lp-eyebrow lp-eyebrow--terracota">Nuestro propósito</p>
          <h2>Tradición ganadera y pureza de origen: del rancho a tu mesa.</h2>
          <p class="lp-lead">
            En <strong>Rancho San Carlos</strong> criamos <strong>becerros</strong> para carne, que vendemos vivos, y
            <strong>cabritos</strong>, en pie o en canal, en el semidesierto de Castaños, Coahuila. Solo con la leche de
            nuestras cabras, en
            <strong>La Cremería San Carlos</strong> elaboramos <strong>queso canasta</strong> y
            <strong>queso panela</strong>. También vendemos <strong>queso asadero</strong> de leche de vaca, que
            compramos a un proveedor.
          </p>
          <p class="lp-quote">«El sabor auténtico del rancho en cada canasta.»</p>
        </div>
      </section>

      <section id="productos" class="lp-section lp-wide">
        <div class="lp-section-head">
          <h2>Nuestros productos</h2>
          <p>Queso canasta y panela de nuestra cremería, queso asadero de proveedor y ganado criado con control sanitario en los corrales y agostaderos del rancho.</p>
        </div>
        <ProductCatalog v-model="catalogFilter" @add="addToOrder" />
      </section>

      <section id="oficio" class="lp-section lp-spotlight lp-pattern-basket">
        <div class="lp-spotlight-grid">
          <div>
            <p class="lp-eyebrow lp-eyebrow--ocre">Tradición sincera · La Cremería San Carlos</p>
            <h2>El arte del molde y la paila</h2>
            <p class="lp-accent-text lp-accent-text--terracota">«Moldeado a mano cada mañana con leche de nuestro propio hato»</p>
            <p>
              Respetamos los tiempos naturales de la leche de cabra recién ordeñada. De ese proceso sale nuestro
              queso fresco, que toma dos formas según el molde: el queso canasta y el panela.
            </p>
            <p>
              Cada rueda de queso canasta se prensa a mano dentro de cestos tradicionales, que dejan grabada en la
              corteza blanca la huella geométrica de su origen artesanal, sin conservadores ni sustitutos.
            </p>
            <div class="lp-craft">
              <div v-for="feature in craftFeatures" :key="feature.title" class="lp-craft-item">
                <h3>{{ feature.title }}</h3>
                <p>{{ feature.text }}</p>
              </div>
            </div>
          </div>

          <figure class="lp-label">
            <svg viewBox="0 0 360 360" role="img" aria-label="Etiqueta circular del queso canasta de cabra, rueda de 4.5 kilogramos">
              <circle cx="180" cy="180" r="168" fill="#F7F4EC" stroke="#D19A36" stroke-width="2" />
              <circle cx="180" cy="180" r="156" fill="none" stroke="#1A1918" stroke-opacity="0.12" stroke-width="8" stroke-dasharray="10 6" />
              <circle cx="180" cy="180" r="140" fill="none" stroke="#1A1918" stroke-opacity="0.12" stroke-width="8" stroke-dasharray="8 8" />
              <circle cx="180" cy="180" r="124" fill="none" stroke="#1A1918" stroke-opacity="0.12" stroke-width="6" stroke-dasharray="12 5" />
              <circle cx="180" cy="180" r="102" fill="#F7F4EC" stroke="#1A1918" stroke-width="2.5" />
              <circle cx="180" cy="180" r="95" fill="none" stroke="#D19A36" stroke-width="1.5" stroke-dasharray="4 3" />
              <text x="180" y="114" text-anchor="middle" textLength="126" lengthAdjust="spacingAndGlyphs" font-family="Cinzel, Georgia, serif" font-size="9" font-weight="700" fill="#1A1918">LA CREMERÍA SAN CARLOS</text>
              <text x="180" y="138" text-anchor="middle" font-family="Cinzel, Georgia, serif" font-size="22" font-weight="700" fill="#1A1918">SC</text>
              <text x="180" y="152" text-anchor="middle" font-family="Plus Jakarta Sans, sans-serif" font-size="7.5" font-weight="600" fill="#5E6B3E" letter-spacing="1.2">CASTAÑOS · COAHUILA</text>
              <rect x="82" y="162" width="196" height="28" rx="2" fill="#D19A36" />
              <text x="180" y="180" text-anchor="middle" textLength="180" lengthAdjust="spacingAndGlyphs" font-family="Plus Jakarta Sans, sans-serif" font-size="8.5" font-weight="700" fill="#1A1918">QUESO CANASTA · 100% LECHE DE CABRA</text>
              <rect x="112" y="198" width="136" height="42" fill="#FFFFFF" stroke="#1A1918" stroke-width="1" />
              <text x="118" y="214" textLength="66" lengthAdjust="spacingAndGlyphs" font-family="Plus Jakarta Sans, sans-serif" font-size="7" font-weight="700" fill="#1A1918">PESO NETO: 4.50 kg</text>
              <text x="242" y="214" text-anchor="end" textLength="48" lengthAdjust="spacingAndGlyphs" font-family="Plus Jakarta Sans, sans-serif" font-size="7" font-weight="700" fill="#B05A3E">LOTE: QC-104</text>
              <text x="118" y="230" textLength="58" lengthAdjust="spacingAndGlyphs" font-family="Plus Jakarta Sans, sans-serif" font-size="7" fill="#444B50">ELAB: 14/10/2026</text>
              <text x="242" y="230" text-anchor="end" textLength="56" lengthAdjust="spacingAndGlyphs" font-family="Plus Jakarta Sans, sans-serif" font-size="7" fill="#444B50">CAD: 14/11/2026</text>
              <text x="180" y="256" text-anchor="middle" textLength="112" lengthAdjust="spacingAndGlyphs" font-family="Cormorant Garamond, Georgia, serif" font-style="italic" font-size="9" fill="#1A1918">Conservar de 2 °C a 6 °C</text>
            </svg>
            <figcaption>Etiqueta de ejemplo. Deja a la vista la corteza grabada por la canasta.</figcaption>
          </figure>
        </div>
      </section>

      <section id="pilares" class="lp-section lp-pillars lp-pattern-desert">
        <div class="lp-wide">
          <div class="lp-section-head lp-section-head--night">
            <p class="lp-eyebrow lp-eyebrow--ocre">Valores de marca</p>
            <h2>Los seis pilares de nuestra identidad</h2>
          </div>
          <ol class="lp-pillar-grid">
            <li v-for="(pillar, index) in pillars" :key="pillar.title" class="lp-pillar">
              <span class="lp-pillar-num">Pilar {{ String(index + 1).padStart(2, '0') }}</span>
              <h3>{{ pillar.title }}</h3>
              <p>{{ pillar.text }}</p>
            </li>
          </ol>
        </div>
      </section>

      <section id="trazabilidad" class="lp-section lp-wide lp-wide--trace">
        <div class="lp-section-head">
          <h2>Consulta de trazabilidad y origen</h2>
          <p>Conoce de dónde viene tu lote de queso o la ficha de nuestros cabritos y becerros identificados con el monograma «SC».</p>
        </div>
        <TraceLookup />
      </section>
    </main>

    <footer class="lp-footer">
      <div class="lp-wide">
        <div class="lp-cobrand">
          <div class="lp-cobrand-main">
            <p class="lp-cobrand-title">RANCHO SAN CARLOS</p>
            <p class="lp-cobrand-sub">Ganadería bovina para carne y caprina · Coahuila</p>
          </div>
          <span class="lp-cobrand-divider" aria-hidden="true" />
          <div class="lp-cobrand-second">
            <p class="lp-cobrand-title">LA CREMERÍA SAN CARLOS</p>
            <p class="lp-cobrand-sub">Queso canasta y panela de cabra · Producido en Rancho San Carlos</p>
          </div>
        </div>

        <div class="lp-footer-cols">
          <div>
            <h3>Origen y ubicación</h3>
            <p><strong>Rancho San Carlos y La Cremería San Carlos</strong></p>
            <p>Castaños, Coahuila, México (Región Centro-Desierto).</p>
            <p>Cría de becerros y cabritos; elaboración artesanal de quesos de leche de cabra.</p>
          </div>
          <div>
            <h3>Nuestros productos</h3>
            <ul>
              <li v-for="product in products" :key="product.id">
                <RouterLink :to="{ hash: '#productos' }" @click="catalogFilter = product.division">{{ product.name }}</RouterLink>
              </li>
            </ul>
          </div>
          <div>
            <h3>Presentaciones</h3>
            <ul>
              <li>Queso canasta: rueda de 4.5 kg y de 5 kg</li>
              <li>Queso panela: pieza de 1 kg</li>
              <li>Queso asadero (de proveedor): pieza de 500 g</li>
              <li>Cabrito: en pie o en canal</li>
              <li>Becerro: en pie, ~8 meses y ~150 kg</li>
            </ul>
          </div>
          <div>
            <h3>Nuestro control</h3>
            <p>Bitácora propia de sanidad, pesaje y ordeña para cada animal y cada lote de queso.</p>
            <ul class="lp-seals" aria-label="Compromisos de la casa">
              <li>100% leche de cabra</li>
              <li>Arete por animal</li>
              <li>Lote por queso</li>
              <li>Respeto al retiro sanitario</li>
            </ul>
          </div>
        </div>

        <p class="lp-footer-bottom">© {{ year }} Rancho San Carlos y La Cremería San Carlos · Castaños, Coahuila, México.</p>
      </div>
    </footer>

    <OrderDialog
      v-model="orderOpen"
      :lines="order.lines.value"
      :total="order.total.value"
      :has-quotes="order.hasQuotes.value"
      :summary="order.summary"
      @add="order.increase"
      @decrease="order.decrease"
      @remove="order.remove"
      @clear="order.clear"
    />
  </div>
</template>
