<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiAlert, UiBasePage, UiButton, UiCard, UiField, UiLoadError } from '../../../shared/components'
import { formatDate, todayIso } from '../../../shared/utils/format'
import { getHerdCatalog, listAnimals, type AnimalSummary, type BreedItem, type LotItem } from '../../herd'
import { createBirth, listFemales } from '../api/reproductionApi'
import type { FemaleStatus } from '../types/reproduction'

const route = useRoute()
const router = useRouter()
const today = todayIso()

const females = ref<FemaleStatus[]>([])
const femaleDetails = ref<AnimalSummary[]>([])
const breeds = ref<BreedItem[]>([])
const lots = ref<LotItem[]>([])
const loadError = ref('')
const ready = ref(false)
const saveError = ref('')
const saving = ref(false)

interface OffspringRow {
  key: number
  visualTag: string
  officialTag: string
  sex: '' | 'M' | 'F'
  birthWeightKg: string
  breedId: string
  lotId: string
}

let nextKey = 1
const form = reactive({ damId: '', birthDate: today, stillborn: '0', complications: '', offspring: [] as OffspringRow[] })

const dam = computed(() => females.value.find((female) => String(female.animalId) === form.damId) ?? null)
const damDetail = computed(() => femaleDetails.value.find((animal) => String(animal.id) === form.damId) ?? null)
const openBreeding = computed(() =>
  dam.value?.breeding && ['pending', 'pregnant'].includes(dam.value.breeding.outcome) ? dam.value.breeding : null,
)
const damOptions = computed(() => [
  { value: '', label: 'Selecciona la madre' },
  ...females.value.map((female) => ({
    value: String(female.animalId),
    label: `${female.visualTag} · ${female.speciesId === 1 ? 'Vaca' : 'Chiva'}${female.breeding?.outcome === 'pregnant' ? ' · cargada' : female.breeding?.outcome === 'pending' ? ' · montada' : ''}`,
  })),
])
const breedOptions = computed(() => [
  { value: '', label: 'Igual que la madre' },
  ...breeds.value.filter((breed) => breed.speciesId === dam.value?.speciesId).map((breed) => ({ value: String(breed.id), label: breed.name })),
])
const lotOptions = computed(() => [
  { value: '', label: 'Igual que la madre' },
  ...lots.value.filter((lot) => lot.speciesId === dam.value?.speciesId).map((lot) => ({ value: String(lot.id), label: lot.code })),
])
const total = computed(() => form.offspring.length + (Number(form.stillborn) || 0))
const youngLabel = computed(() => (dam.value?.speciesId === 2 ? 'cabrito' : 'becerro'))

function addOffspring(): void {
  form.offspring.push({ key: nextKey++, visualTag: '', officialTag: '', sex: '', birthWeightKg: '', breedId: '', lotId: '' })
}

function removeOffspring(key: number): void {
  form.offspring = form.offspring.filter((row) => row.key !== key)
}

// Al cambiar de madre, raza y lote vuelven a «igual que la madre».
watch(() => form.damId, () => {
  for (const row of form.offspring) {
    row.breedId = ''
    row.lotId = ''
  }
})

async function load(): Promise<void> {
  loadError.value = ''
  try {
    const [femaleList, animalPage, catalog] = await Promise.all([
      listFemales(),
      listAnimals({ sex: 'F', status: 'active', pageSize: 200 }),
      getHerdCatalog(),
    ])
    females.value = femaleList
    femaleDetails.value = animalPage.items
    breeds.value = catalog.breeds
    lots.value = catalog.lots
    if (femaleList.some((female) => female.animalId === Number(route.query.madre))) form.damId = String(route.query.madre)
    if (form.offspring.length === 0) addOffspring()
    ready.value = true
  } catch (cause) {
    loadError.value = getApiErrorMessage(cause)
  }
}

function validate(): string {
  if (!form.damId) return 'Elige la madre.'
  if (!form.birthDate) return 'Indica la fecha del parto.'
  const stillborn = Number(form.stillborn)
  if (!Number.isInteger(stillborn) || stillborn < 0) return 'Las crías muertas deben ser un número entero.'
  if (total.value === 0 || total.value > 6) return 'Un parto tiene de 1 a 6 crías entre vivas y muertas.'
  const missing = form.offspring.findIndex((row) => !row.visualTag.trim() || !row.sex)
  if (missing >= 0) return `Cría ${missing + 1}: escribe su arete y su sexo.`
  return ''
}

async function save(): Promise<void> {
  if (saving.value) return
  saveError.value = validate()
  if (saveError.value) return
  saving.value = true
  try {
    const birth = await createBirth({
      damId: Number(form.damId),
      birthDate: form.birthDate,
      stillbornCount: Number(form.stillborn),
      complications: form.complications.trim() || null,
      offspring: form.offspring.map((row) => ({
        visualTag: row.visualTag.trim(),
        officialTag: row.officialTag.trim() || null,
        sex: row.sex as 'M' | 'F',
        birthWeightKg: row.birthWeightKg ? Number(row.birthWeightKg) : null,
        breedId: row.breedId ? Number(row.breedId) : null,
        lotId: row.lotId ? Number(row.lotId) : null,
      })),
    })
    const live = birth.offspringCount
    await router.push({
      name: 'reproduction-history',
      query: {
        hembra: birth.damId,
        guardado: `Parto de ${birth.damTag} registrado: ${live} cría${live === 1 ? '' : 's'} dada${live === 1 ? '' : 's'} de alta en el hato.`,
      },
    })
  } catch (cause) {
    saveError.value = getApiErrorMessage(cause)
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<template>
  <UiBasePage fluid title="Registrar parto" description="Las crías vivas se dan de alta en el hato con su arete; las muertas al nacer solo se cuentan.">
    <template #actions>
      <div class="panel-toolbar">
        <RouterLink class="btn secondary" :to="{ name: 'reproduction' }">Volver a reproducción</RouterLink>
      </div>
    </template>

    <UiLoadError v-if="loadError" :message="loadError" @retry="load" />
    <p v-else-if="!ready" aria-busy="true">Cargando…</p>

    <form v-else class="stack" novalidate @submit.prevent="save">
      <UiAlert v-if="saveError" tone="danger" title="No se registró">{{ saveError }}</UiAlert>

      <UiCard title="Parto">
        <div class="panel-form">
          <UiField v-model="form.damId" as="select" label="Madre *" :options="damOptions" />
          <UiField v-model="form.birthDate" label="Fecha del parto *" type="date" :max="today" required />
          <UiField v-model="form.stillborn" label="Crías muertas al nacer" type="number" min="0" max="6" step="1" inputmode="numeric" />
          <UiField v-model="form.complications" label="Complicaciones" maxlength="255" />
        </div>
        <UiAlert v-if="dam && openBreeding" tone="info" title="Se liga a su monta">
          Monta del {{ formatDate(openBreeding.matingDate) }} con {{ openBreeding.maleTag }}; parto probable {{ formatDate(openBreeding.expectedDueDate) }}.
          Las crías tendrán a {{ openBreeding.maleTag }} como padre.
        </UiAlert>
        <UiAlert v-else-if="dam" tone="warning" title="Sin monta registrada">
          {{ dam.visualTag }} no tiene una monta abierta: el parto se registra con padre desconocido.
        </UiAlert>
      </UiCard>

      <UiCard title="Crías vivas" :description="dam && damDetail ? `Por omisión: raza ${damDetail.breedName}${damDetail.lotCode ? `, lote ${damDetail.lotCode}` : ''}.` : undefined">
        <div v-for="(row, index) in form.offspring" :key="row.key" class="rp-offspring">
          <UiField v-model="row.visualTag" :label="`Arete de la cría ${index + 1} *`" maxlength="30" autocomplete="off" />
          <UiField
            v-model="row.sex"
            as="select"
            :label="`Sexo de la cría ${index + 1} *`"
            :options="[{ value: '', label: 'Selecciona' }, { value: 'M', label: 'Macho' }, { value: 'F', label: 'Hembra' }]"
          />
          <UiField v-model="row.birthWeightKg" :label="`Peso (kg) cría ${index + 1}`" type="number" min="0.1" max="100" step="0.01" inputmode="decimal" />
          <UiField v-model="row.breedId" as="select" :label="`Raza cría ${index + 1}`" :options="breedOptions" :disabled="!dam" />
          <UiField v-model="row.lotId" as="select" :label="`Lote cría ${index + 1}`" :options="lotOptions" :disabled="!dam" />
          <button type="button" class="panel-icon-button" :aria-label="`Quitar la cría ${index + 1}`" @click="removeOffspring(row.key)">Quitar</button>
        </div>
        <p v-if="form.offspring.length === 0" class="panel-muted">Sin crías vivas. Indica arriba cuántas nacieron muertas.</p>
        <UiButton variant="secondary" :disabled="total >= 6" @click="addOffspring">Agregar {{ youngLabel }}</UiButton>
      </UiCard>

      <div class="panel-actions">
        <UiButton type="submit" :loading="saving">Registrar parto</UiButton>
        <RouterLink class="btn secondary" :to="{ name: 'reproduction' }">Cancelar</RouterLink>
      </div>
    </form>
  </UiBasePage>
</template>

<style scoped>
.rp-offspring {
  display: grid;
  grid-template-columns: minmax(0, 1.3fr) minmax(0, 1fr) minmax(0, 1fr) minmax(0, 1.2fr) minmax(0, 1.2fr) auto;
  gap: 12px;
  align-items: end;
  padding-bottom: 16px;
  margin-bottom: 16px;
  border-bottom: 1px solid var(--color-border-light);
}
.rp-offspring :deep(.field) { margin-bottom: 0; }
@media (max-width: 900px) {
  .rp-offspring { grid-template-columns: 1fr 1fr; }
}
</style>
