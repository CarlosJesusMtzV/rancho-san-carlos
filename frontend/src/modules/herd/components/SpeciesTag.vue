<script setup lang="ts">
import { speciesLabels } from '../data/labels'
import type { SpeciesId } from '../types/herd'

defineProps<{ speciesId: SpeciesId }>()
</script>

<template>
  <span class="herd-species" :class="`herd-species--${speciesId}`">{{ speciesLabels[speciesId] }}</span>
</template>
