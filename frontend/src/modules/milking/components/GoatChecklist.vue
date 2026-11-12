<script setup lang="ts">
import { computed } from 'vue'

import { formatDate } from '../../../shared/utils/format'
import type { MilkingGoat } from '../types/milking'

const props = defineProps<{ goats: MilkingGoat[]; date: string }>()
const selected = defineModel<number[]>({ required: true })

const available = computed(() => props.goats.filter((goat) => goat.canBeMilked))
const allSelected = computed(() => available.value.length > 0 && available.value.every((goat) => selected.value.includes(goat.animalId)))

function toggle(id: number, checked: boolean): void {
  selected.value = checked ? [...selected.value, id] : selected.value.filter((item) => item !== id)
}

function toggleAll(): void {
  selected.value = allSelected.value ? [] : available.value.map((goat) => goat.animalId)
}
</script>

<template>
  <div>
    <div class="mk-head">
      <span aria-live="polite"><strong>{{ selected.length }}</strong> de {{ available.length }} cabras disponibles</span>
      <button v-if="available.length" type="button" class="panel-icon-button panel-icon-button--neutral" @click="toggleAll">
        {{ allSelected ? 'Quitar todas' : 'Marcar todas' }}
      </button>
    </div>
    <p v-if="goats.length === 0" class="panel-muted">No hay cabras hembra activas en el hato.</p>
    <div v-else class="mk-list" role="group" aria-label="Cabras ordeñadas">
      <label v-for="goat in goats" :key="goat.animalId" class="mk-goat" :class="{ 'mk-goat--blocked': !goat.canBeMilked }">
        <input
          type="checkbox"
          :checked="selected.includes(goat.animalId)"
          :disabled="!goat.canBeMilked"
          @change="toggle(goat.animalId, ($event.target as HTMLInputElement).checked)"
        />
        <span>
          <strong>{{ goat.visualTag }}</strong>
          <span v-if="!goat.canBeMilked" class="mk-blocked"> · retiro de leche hasta el {{ formatDate(goat.milkAllowedFrom) }}</span>
          <span v-else-if="goat.lotCode" class="panel-muted"> · {{ goat.lotCode }}</span>
        </span>
      </label>
    </div>
    <p v-if="goats.some((goat) => !goat.canBeMilked)" class="panel-muted">
      Las cabras con retiro de leche no se pueden marcar para el {{ formatDate(date) }}: su leche no va a quesería.
    </p>
  </div>
</template>

<style scoped>
.mk-head { display: flex; align-items: center; justify-content: space-between; gap: 12px; flex-wrap: wrap; }
.mk-list {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(min(100%, 220px), 1fr));
  gap: 4px 16px;
  max-height: 320px;
  overflow-y: auto;
  margin: 8px 0;
  padding: 8px 12px;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  background: var(--input-bg);
}
.mk-goat { display: flex; align-items: center; gap: 10px; min-height: 44px; cursor: pointer; }
.mk-goat input { width: 18px; height: 18px; accent-color: var(--color-primary); flex: none; }
.mk-goat--blocked { cursor: not-allowed; }
.mk-blocked { color: var(--color-warning-text); font-size: 14px; }
</style>
