<script setup lang="ts">
import { computed, ref } from 'vue'

import { UiButton, UiField } from '../../../shared/components'
import { traceSamples } from '../data/traceSamples'

const query = ref(traceSamples[0].code)
const searchedCode = ref(traceSamples[0].code)

const record = computed(() => traceSamples.find((sample) => sample.code === searchedCode.value))

function search(code: string = query.value): void {
  const normalized = code.trim().toUpperCase()
  query.value = normalized
  searchedCode.value = normalized
}
</script>

<template>
  <div class="lp-trace">
    <aside class="lp-trace-side">
      <h3>Registros de muestra</h3>
      <p>Elige un lote de quesería o un registro ganadero para ver su ficha.</p>
      <button
        v-for="sample in traceSamples"
        :key="sample.code"
        type="button"
        class="lp-trace-sample"
        :aria-pressed="searchedCode === sample.code"
        @click="search(sample.code)"
      >
        <span>{{ sample.sampleLabel }}</span>
        <strong>{{ sample.code }}</strong>
      </button>
      <p class="lp-trace-note">Datos de demostración mientras se conecta el sistema de gestión del rancho.</p>
    </aside>

    <div class="lp-trace-main">
      <form class="lp-trace-form" @submit.prevent="search()">
        <UiField v-model="query" label="Código de lote o registro" placeholder="Ej. QC-104 o SC-BEC-214" autocomplete="off" />
        <UiButton type="submit">Consultar registro</UiButton>
      </form>

      <div class="lp-trace-result" :class="record ? `lp-trace-result--${record.division}` : 'lp-trace-result--missing'" aria-live="polite">
        <template v-if="record">
          <span class="lp-trace-badge">{{ record.badge }}</span>
          <h4>{{ record.title }}</h4>
          <dl class="lp-trace-fields">
            <div v-for="field in record.fields" :key="field.label">
              <dt>{{ field.label }}</dt>
              <dd>{{ field.value }}</dd>
            </div>
          </dl>
        </template>
        <template v-else>
          <span class="lp-trace-badge">Registro no encontrado</span>
          <h4>No localizamos el código «{{ searchedCode }}»</h4>
          <p>Prueba con uno de los registros de muestra de la lista.</p>
        </template>
      </div>
    </div>
  </div>
</template>
