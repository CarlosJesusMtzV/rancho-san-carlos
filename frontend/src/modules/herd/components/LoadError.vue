<script setup lang="ts">
import { UiAlert, UiButton } from '../../../shared/components'

defineProps<{ message: string }>()
const emit = defineEmits<{ retry: [] }>()
</script>

<template>
  <UiAlert tone="danger" title="No se pudo cargar la información">
    <p>{{ message }}</p>
    <UiButton variant="secondary" @click="emit('retry')">Reintentar</UiButton>
  </UiAlert>
</template>
