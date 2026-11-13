<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiAlert, UiBasePage, UiButton, UiCard, UiDialog, UiField, UiLoadError, UiToast } from '../../../shared/components'
import { formatDate, formatMoney, todayIso } from '../../../shared/utils/format'
import { createRun, deleteRun, getRun, listKids, updateRun } from '../api/slaughterApi'
import type { SlaughterCandidate, SlaughterRunDetail } from '../types/slaughter'

interface KidRow {
  animalId: number
  visualTag: string
  detail: string
  lastWeightKg: number | null
  blockedUntil: string | null
  sold: boolean
}

const route = useRoute()
const router = useRouter()
const toast = ref<InstanceType<typeof UiToast> | null>(null)
const today = todayIso()

const editId = computed(() => (route.params.id ? Number(route.params.id) : null))
const run = ref<SlaughterRunDetail | null>(null)
const candidates = ref<SlaughterCandidate[]>([])
const loadError = ref('')
const ready = ref(false)
const loadingKids = ref(false)

const form = reactive({ runDate: today, slaughterhouse: '', cost: '', notes: '' })
const weights = reactive<Record<number, { live: string; carcass: string }>>({})
const saveError = ref('')
const saving = ref(false)
const deleteOpen = ref(false)
const deleting = ref(false)
const deleteError = ref('')

const kg = (value: number) => `${value.toLocaleString('es-MX', { maximumFractionDigits: 2 })} kg`
const age = (months: number | null) => (months === null ? 'sin fecha de nacimiento' : months === 1 ? '1 mes' : `${months} meses`)

// Los cabritos de la jornada (ya sacrificados) van primero; luego los caprinos activos.
const rows = computed<KidRow[]>(() => [
  ...(run.value?.kids ?? []).map((kid) => ({
    animalId: kid.animalId,
    visualTag: kid.visualTag,
    detail: `${kid.sex === 'M' ? 'macho' : 'hembra'}, ${age(kid.ageMonths)}`,
    lastWeightKg: null,
    blockedUntil: null,
    sold: kid.sold,
  })),
  ...candidates.value.map((kid) => ({
    animalId: kid.animalId,
    visualTag: kid.visualTag,
    detail: `${kid.sex === 'M' ? 'macho' : 'hembra'}, ${age(kid.ageMonths)}${kid.lastWeightKg !== null ? `, último peso ${kg(kid.lastWeightKg)}` : ''}`,
    lastWeightKg: kid.lastWeightKg,
    blockedUntil: kid.canBeSlaughtered ? null : kid.meatAllowedFrom,
    sold: false,
  })),
])
const selectedRows = computed(() => rows.value.filter((row) => weights[row.animalId]))

const totals = computed(() => {
  const live = selectedRows.value.reduce((sum, row) => sum + (Number(weights[row.animalId].live) || 0), 0)
  const carcass = selectedRows.value.reduce((sum, row) => sum + (Number(weights[row.animalId].carcass) || 0), 0)
  const count = selectedRows.value.length
  const cost = form.cost === '' ? null : Number(form.cost)
  return { count, live, carcass, yieldPercent: live > 0 ? Math.round((carcass / live) * 1000) / 10 : null, perKid: cost !== null && count ? cost / count : null }
})

function yieldOf(animalId: number): string {
  const live = Number(weights[animalId]?.live)
  const carcass = Number(weights[animalId]?.carcass)
  return live > 0 && carcass > 0 ? `${Math.round((carcass / live) * 1000) / 10} %` : '—'
}

function toggle(row: KidRow, checked: boolean): void {
  if (checked) weights[row.animalId] = { live: '', carcass: '' }
  else delete weights[row.animalId]
}

async function loadKids(date: string): Promise<void> {
  loadingKids.value = true
  try {
    candidates.value = await listKids(date)
    // Si al cambiar la fecha un cabrito queda en retiro, se desmarca.
    for (const kid of candidates.value) if (!kid.canBeSlaughtered && weights[kid.animalId]) delete weights[kid.animalId]
  } catch (cause) {
    saveError.value = getApiErrorMessage(cause)
  } finally {
    loadingKids.value = false
  }
}

function fill(detail: SlaughterRunDetail): void {
  run.value = detail
  Object.assign(form, { runDate: detail.runDate, slaughterhouse: detail.slaughterhouse, cost: detail.cost === null ? '' : String(detail.cost), notes: detail.notes ?? '' })
  for (const key of Object.keys(weights)) delete weights[Number(key)]
  for (const kid of detail.kids) weights[kid.animalId] = { live: String(kid.liveWeightKg), carcass: String(kid.carcassWeightKg) }
}

async function load(): Promise<void> {
  loadError.value = ''
  try {
    if (editId.value) {
      const detail = await getRun(editId.value)
      fill(detail)
      candidates.value = await listKids(detail.runDate)
    } else {
      candidates.value = await listKids(form.runDate)
    }
    ready.value = true
  } catch (cause) {
    loadError.value = getApiErrorMessage(cause)
  }
}

function validate(): string {
  if (!form.runDate) return 'Indica la fecha de la matanza.'
  if (!form.slaughterhouse.trim()) return 'Escribe el rastro o matancero.'
  if (form.cost !== '' && !(Number(form.cost) >= 0)) return 'El pago no puede ser negativo.'
  if (selectedRows.value.length === 0) return 'Marca al menos un cabrito.'
  for (const row of selectedRows.value) {
    const live = Number(weights[row.animalId].live)
    const carcass = Number(weights[row.animalId].carcass)
    if (!(live > 0) || !(carcass > 0)) return `${row.visualTag}: indica el peso vivo y el peso en canal.`
    if (carcass > live) return `${row.visualTag}: la canal no puede pesar más que el cabrito vivo.`
  }
  return ''
}

async function save(): Promise<void> {
  if (saving.value) return
  saveError.value = validate()
  if (saveError.value) return
  saving.value = true
  const input = {
    slaughterhouse: form.slaughterhouse.trim(),
    cost: form.cost === '' ? null : Number(form.cost),
    notes: form.notes.trim() || null,
    kids: selectedRows.value.map((row) => ({ animalId: row.animalId, liveWeightKg: Number(weights[row.animalId].live), carcassWeightKg: Number(weights[row.animalId].carcass) })),
  }
  try {
    if (run.value) {
      fill(await updateRun(run.value.id, input))
      candidates.value = await listKids(form.runDate)
      toast.value?.show('Matanza actualizada.')
    } else {
      const saved = await createRun({ ...input, runDate: form.runDate })
      await router.push({
        name: 'slaughter',
        query: { guardado: `Matanza del ${formatDate(saved.runDate)} registrada: ${saved.kidCount} ${saved.kidCount === 1 ? 'cabrito' : 'cabritos'}, ${kg(saved.carcassWeightKg)} en canal.` },
      })
    }
  } catch (cause) {
    saveError.value = getApiErrorMessage(cause)
  } finally {
    saving.value = false
  }
}

async function confirmDelete(): Promise<void> {
  if (!run.value || deleting.value) return
  deleting.value = true
  deleteError.value = ''
  try {
    await deleteRun(run.value.id)
    await router.push({ name: 'slaughter', query: { guardado: `Matanza del ${formatDate(run.value.runDate)} eliminada; sus cabritos vuelven al hato.` } })
  } catch (cause) {
    deleteError.value = getApiErrorMessage(cause)
  } finally {
    deleting.value = false
  }
}

watch(() => form.runDate, (date) => {
  if (date && !run.value && ready.value) void loadKids(date)
})

onMounted(load)
</script>

<template>
  <UiBasePage fluid :title="run ? `Matanza del ${formatDate(run.runDate)}` : 'Registrar matanza'" description="Cabritos llevados el mismo día a un rastro o matancero externo.">
    <template #actions>
      <div class="panel-toolbar">
        <RouterLink class="btn secondary" :to="{ name: 'slaughter' }">Volver a matanza</RouterLink>
        <UiButton v-if="run" variant="danger" @click="deleteOpen = true">Eliminar</UiButton>
      </div>
    </template>

    <UiLoadError v-if="loadError" :message="loadError" @retry="load" />
    <p v-else-if="!ready" aria-busy="true">Cargando…</p>

    <form v-else class="stack" novalidate @submit.prevent="save">
      <UiAlert v-if="saveError" tone="danger" title="No se guardó">{{ saveError }}</UiAlert>

      <UiCard title="Jornada" :description="run ? 'La fecha no cambia; si fue otro día, elimínala y regístrala de nuevo.' : undefined">
        <div class="panel-form">
          <UiField v-model="form.runDate" label="Fecha *" type="date" :max="today" :disabled="!!run" required />
          <UiField v-model="form.slaughterhouse" label="Rastro o matancero *" maxlength="120" placeholder="Ej. Rastro Municipal de Castaños" />
          <UiField v-model="form.cost" label="Pago total (MXN)" type="number" min="0" step="0.01" inputmode="decimal" />
          <UiField v-model="form.notes" label="Notas" maxlength="255" />
        </div>
      </UiCard>

      <UiCard title="Cabritos" :description="`${totals.count} marcado${totals.count === 1 ? '' : 's'} · del más joven al más viejo`">
        <p v-if="loadingKids" aria-busy="true">Revisando cabritos…</p>
        <p v-else-if="rows.length === 0" class="panel-muted">No hay caprinos activos en el hato.</p>
        <div v-else class="sl-list" role="group" aria-label="Cabritos de la jornada">
          <label v-for="row in rows" :key="row.animalId" class="sl-kid" :class="{ 'sl-kid--blocked': row.blockedUntil || row.sold }">
            <input
              type="checkbox"
              :checked="!!weights[row.animalId]"
              :disabled="!!row.blockedUntil || row.sold"
              @change="toggle(row, ($event.target as HTMLInputElement).checked)"
            />
            <span>
              <strong>{{ row.visualTag }}</strong>
              <span class="panel-muted"> · {{ row.detail }}</span>
              <span v-if="row.blockedUntil" class="sl-warning"> · retiro de carne hasta el {{ formatDate(row.blockedUntil) }}</span>
              <span v-else-if="row.sold" class="sl-warning"> · canal en un pedido</span>
            </span>
          </label>
        </div>
      </UiCard>

      <UiCard v-if="selectedRows.length" title="Pesos" description="Peso del cabrito vivo y de la canal ya pelada.">
        <div v-for="row in selectedRows" :key="row.animalId" class="sl-weights">
          <p class="sl-tag"><strong>{{ row.visualTag }}</strong></p>
          <UiField
            v-model="weights[row.animalId].live"
            :label="`Peso vivo de ${row.visualTag} (kg) *`"
            type="number"
            min="0.01"
            max="100"
            step="0.01"
            inputmode="decimal"
            :placeholder="row.lastWeightKg !== null ? `Último: ${row.lastWeightKg}` : undefined"
          />
          <UiField v-model="weights[row.animalId].carcass" :label="`Canal de ${row.visualTag} (kg) *`" type="number" min="0.01" max="100" step="0.01" inputmode="decimal" />
          <p class="sl-yield"><span class="panel-muted">Rendimiento</span> {{ yieldOf(row.animalId) }}</p>
        </div>
        <p class="panel-muted">
          Total: {{ kg(totals.live) }} vivo, {{ kg(totals.carcass) }} en canal<template v-if="totals.yieldPercent !== null"> ({{ totals.yieldPercent }} %)</template><template v-if="totals.perKid !== null"> · {{ formatMoney(totals.perKid) }} por cabrito</template>.
        </p>
      </UiCard>

      <div class="panel-actions">
        <UiButton type="submit" :loading="saving">{{ run ? 'Guardar cambios' : 'Registrar matanza' }}</UiButton>
        <RouterLink class="btn secondary" :to="{ name: 'slaughter' }">Cancelar</RouterLink>
      </div>
    </form>

    <UiDialog v-model="deleteOpen" title="Eliminar matanza">
      <p v-if="run">Se eliminará la matanza del {{ formatDate(run.runDate) }} y sus {{ run.kidCount }} cabritos volverán al hato como activos.</p>
      <UiAlert v-if="deleteError" tone="danger" title="No se eliminó">{{ deleteError }}</UiAlert>
      <template #actions>
        <UiButton variant="danger" :loading="deleting" @click="confirmDelete">Eliminar matanza</UiButton>
        <UiButton variant="secondary" @click="deleteOpen = false">Cancelar</UiButton>
      </template>
    </UiDialog>

    <UiToast ref="toast" />
  </UiBasePage>
</template>

<style scoped>
.sl-list {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(min(100%, 280px), 1fr));
  gap: 4px 16px;
  max-height: 360px;
  overflow-y: auto;
  padding: 8px 12px;
  border: 1px solid var(--color-border);
  border-radius: 8px;
  background: var(--input-bg);
}
.sl-kid { display: flex; align-items: center; gap: 10px; min-height: 44px; cursor: pointer; }
.sl-kid input { width: 18px; height: 18px; accent-color: var(--color-primary); flex: none; }
.sl-kid--blocked { cursor: not-allowed; }
.sl-warning { color: var(--color-warning-text); font-size: 14px; }
.sl-weights {
  display: grid;
  grid-template-columns: minmax(0, 0.7fr) minmax(0, 1fr) minmax(0, 1fr) minmax(0, 0.7fr);
  gap: 12px;
  align-items: end;
  padding-bottom: 12px;
  margin-bottom: 12px;
  border-bottom: 1px solid var(--color-border-light);
}
.sl-weights :deep(.field) { margin-bottom: 0; }
.sl-tag, .sl-yield { margin: 0 0 12px; }
@media (max-width: 700px) {
  .sl-weights { grid-template-columns: 1fr 1fr; }
  .sl-tag, .sl-yield { grid-column: span 2; margin: 0; }
}
</style>
