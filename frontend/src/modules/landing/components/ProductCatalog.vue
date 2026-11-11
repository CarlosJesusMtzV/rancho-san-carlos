<script setup lang="ts">
import { computed, ref } from 'vue'

import { divisionLabels, products } from '../data/catalog'
import type { Division, Presentation, Product } from '../types/catalog'

export type CatalogFilter = Division | 'all'

const filter = defineModel<CatalogFilter>({ default: 'all' })
const emit = defineEmits<{ add: [product: Product, presentation: Presentation] }>()

const tabs: { value: CatalogFilter; label: string }[] = [
  { value: 'all', label: `Ver todo (${products.length})` },
  { value: 'cheese', label: `${divisionLabels.cheese} · canasta, panela y asadero` },
  { value: 'goat', label: `${divisionLabels.goat} · cabritos` },
  { value: 'cattle', label: `${divisionLabels.cattle} · becerros` },
]

const visibleProducts = computed(() =>
  filter.value === 'all' ? products : products.filter((product) => product.division === filter.value),
)

// Fotos que no cargaron: se sustituyen por el emblema de la marca.
const missingImages = ref(new Set<string>())

function markMissing(id: string): void {
  missingImages.value = new Set(missingImages.value).add(id)
}
</script>

<template>
  <div>
    <div class="lp-tabs" role="group" aria-label="Filtrar productos por división">
      <button
        v-for="tab in tabs"
        :key="tab.value"
        type="button"
        class="lp-tab"
        :class="`lp-tab--${tab.value}`"
        :aria-pressed="filter === tab.value"
        @click="filter = tab.value"
      >
        <span v-if="tab.value !== 'all'" class="lp-tab-dot" aria-hidden="true" />
        {{ tab.label }}
      </button>
    </div>

    <div class="lp-products">
      <article v-for="product in visibleProducts" :key="product.id" class="lp-product" :class="`lp-product--${product.division}`">
        <div class="lp-product-media">
          <span class="lp-product-badge">{{ product.badge }}</span>
          <img
            v-if="!missingImages.has(product.id)"
            :src="product.image"
            :alt="product.imageAlt"
            loading="lazy"
            width="900"
            height="600"
            @error="markMissing(product.id)"
          />
          <div v-else class="lp-product-placeholder" role="img" :aria-label="product.imageAlt">
            <span>SC</span>
          </div>
        </div>
        <div class="lp-product-body">
          <span class="lp-product-subtitle">{{ product.subtitle }}</span>
          <h3 class="lp-product-title">{{ product.name }}</h3>
          <p class="lp-product-desc">{{ product.description }}</p>
          <p class="lp-product-specs"><strong>{{ product.specLabel }}:</strong> {{ product.spec }}</p>
          <div class="lp-product-footer">
            <span class="lp-product-price">{{ product.priceLabel }}</span>
            <div class="lp-product-actions">
              <button
                v-for="presentation in product.presentations"
                :key="presentation.id"
                type="button"
                class="lp-btn lp-btn--ink lp-btn--small"
                @click="emit('add', product, presentation)"
              >
                {{ presentation.price === null ? `Cotizar ${presentation.label.replace(/^\S+ /, '')}` : `Agregar ${presentation.label.toLowerCase()}` }}
              </button>
            </div>
          </div>
        </div>
      </article>
    </div>
  </div>
</template>
