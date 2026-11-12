<script setup lang="ts">
import { speciesLabels } from '../data/labels'
import type { SpeciesId } from '../types/herd'

defineProps<{ speciesId: SpeciesId }>()
</script>

<template>
  <span class="species-tag" :class="`species-tag--${speciesId}`">{{ speciesLabels[speciesId] }}</span>
</template>

<style scoped>
/* El estilo vive en el componente porque otros módulos (Sanidad) también lo usan. */
.species-tag {
  display: inline-flex;
  align-items: center;
  gap: 8px;
}
.species-tag::before {
  content: '';
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: var(--brand-terracota);
  flex: none;
}
.species-tag--2::before { background: var(--brand-mezquite); }
</style>
