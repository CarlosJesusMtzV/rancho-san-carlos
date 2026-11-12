<script setup lang="ts">
import { UiBadge } from '../../../shared/components'
import { lifecycleLabels } from '../data/labels'
import type { LifecycleStatus } from '../types/herd'

defineProps<{ status: LifecycleStatus }>()
</script>

<template>
  <UiBadge :tone="status === 'active' ? 'success' : status === 'dead' ? 'danger' : 'warning'">
    {{ lifecycleLabels[status] }}
  </UiBadge>
</template>
