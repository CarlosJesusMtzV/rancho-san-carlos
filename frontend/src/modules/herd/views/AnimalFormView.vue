<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from 'vue'
import { RouterLink, useRoute, useRouter } from 'vue-router'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { UiAlert, UiBasePage, UiButton, UiCard, UiField, UiRadio } from '../../../shared/components'
import { createAnimal, getAnimal, listAnimals, updateAnimal } from '../api/herdApi'
import LoadError from '../components/LoadError.vue'
import { useCatalog } from '../composables/useCatalog'
import { originLabels, stagesFor, todayIso } from '../data/labels'
import type { AnimalInput, AnimalSummary, Origin, Sex, SpeciesId } from '../types/herd'
import '../styles.css'

const route = useRoute()
const router = useRouter()
const { catalog, hasBreeds, breedsFor, lotsFor, load: loadCatalog, error: catalogError } = useCatalog()

const editingId = computed(() => (route.params.id ? Number(route.params.id) : null))
const loadError = ref('')
const saveError = ref('')
const saving = ref(false)
const ready = ref(false)
const parents = ref<AnimalSummary[]>([])

// Los controles trabajan con texto; se convierte al guardar.
const form = reactive({
  speciesId: '1' as '1' | '2',
  visualTag: '',
  officialTag: '',
  sex: '' as Sex | '',
  breedId: '',
  lotId: '',
  birthDate: '',
  productionStage: '',
  origin: 'born_on_ranch' as Origin,
  birthWeightKg: '',
  sireId: '',
  damId: '',
  geneticsNotes: '',
})

const species = computed(() => Number(form.speciesId) as SpeciesId)
const today = todayIso()

const breedOptions = computed(() => [
  { value: '', label: 'Selecciona una raza' },
  ...breedsFor(species.value).map((breed) => ({ value: String(breed.id), label: breed.name })),
])
const lotOptions = computed(() => [
  { value: '', label: 'Sin lote' },
  ...lotsFor(species.value).map((lot) => ({ value: String(lot.id), label: lot.code })),
])
const stageOptions = computed(() => [
  { value: '', label: 'Sin etapa' },
  ...stagesFor(species.value, form.sex).map((stage) => ({ value: stage.code, label: stage.label })),
])
const sexOptions = [
  { value: '', label: 'Selecciona' },
  { value: 'M', label: 'Macho' },
  { value: 'F', label: 'Hembra' },
]
const originOptions = Object.entries(originLabels).map(([value, label]) => ({ value, label }))

function parentOptions(sex: Sex, emptyLabel: string) {
  return [
    { value: '', label: emptyLabel },
    ...parents.value
      .filter((animal) => animal.sex === sex && animal.id !== editingId.value)
      .map((animal) => ({ value: String(animal.id), label: `${animal.visualTag} · ${animal.breedName}` })),
  ]
}
const sireOptions = computed(() => parentOptions('M', 'Sin registrar'))
const damOptions = computed(() => parentOptions('F', 'Sin registrar'))

async function loadParents(): Promise<void> {
  try {
    const page = await listAnimals({ speciesId: species.value, status: 'all', pageSize: 200 })
    parents.value = page.items
  } catch {
    parents.value = []
  }
}

// Al cambiar especie o sexo se limpian las opciones que dejaron de ser válidas.
watch(species, () => {
  if (!ready.value) return
  form.breedId = ''
  form.lotId = ''
  form.productionStage = ''
  form.sireId = ''
  form.damId = ''
  void loadParents()
})
watch(() => form.sex, () => {
  if (form.productionStage && !stageOptions.value.some((option) => option.value === form.productionStage)) {
    form.productionStage = ''
  }
})

async function load(): Promise<void> {
  loadError.value = ''
  ready.value = false
  await loadCatalog()
  if (editingId.value) {
    try {
      const animal = await getAnimal(editingId.value)
      Object.assign(form, {
        speciesId: String(animal.speciesId),
        visualTag: animal.visualTag,
        officialTag: animal.officialTag ?? '',
        sex: animal.sex,
        breedId: String(animal.breedId),
        lotId: animal.lotId ? String(animal.lotId) : '',
        birthDate: animal.birthDate ?? '',
        productionStage: animal.productionStage ?? '',
        origin: animal.origin,
        birthWeightKg: animal.birthWeightKg === null ? '' : String(animal.birthWeightKg),
        sireId: animal.sire ? String(animal.sire.id) : '',
        damId: animal.dam ? String(animal.dam.id) : '',
        geneticsNotes: animal.geneticsNotes ?? '',
      })
    } catch (cause) {
      loadError.value = getApiErrorMessage(cause)
      return
    }
  }
  await loadParents()
  ready.value = true
}

const optional = (value: string): string | null => (value.trim() === '' ? null : value.trim())
const optionalNumber = (value: string): number | null => (value === '' ? null : Number(value))

async function save(): Promise<void> {
  if (saving.value) return
  saveError.value = ''
  if (!form.visualTag.trim() || !form.sex || !form.breedId) {
    saveError.value = 'Completa arete visual, sexo y raza.'
    return
  }

  const input: AnimalInput = {
    visualTag: form.visualTag.trim(),
    officialTag: optional(form.officialTag),
    breedId: Number(form.breedId),
    lotId: optionalNumber(form.lotId),
    sex: form.sex,
    birthDate: optional(form.birthDate),
    productionStage: optional(form.productionStage),
    origin: form.origin,
    birthWeightKg: optionalNumber(form.birthWeightKg),
    sireId: optionalNumber(form.sireId),
    damId: optionalNumber(form.damId),
    geneticsNotes: optional(form.geneticsNotes),
  }

  saving.value = true
  try {
    const animal = editingId.value
      ? await updateAnimal(editingId.value, input)
      : await createAnimal({ ...input, speciesId: species.value })
    await router.push({ name: 'herd-animal', params: { id: animal.id }, query: { guardado: '1' } })
  } catch (cause) {
    saveError.value = getApiErrorMessage(cause)
  } finally {
    saving.value = false
  }
}

onMounted(load)
</script>

<template>
  <UiBasePage
    fluid
    :title="editingId ? 'Editar animal' : 'Registrar animal'"
    description="Los campos con * son obligatorios. Raza, lote, etapa y padres se limitan a la especie elegida."
  >
    <LoadError v-if="loadError || catalogError" :message="loadError || catalogError" @retry="load" />
    <p v-else-if="!ready" aria-busy="true">Cargando…</p>

    <UiAlert v-else-if="catalog && !hasBreeds" tone="warning" title="Primero registra las razas">
      Necesitas al menos una raza de la especie para dar de alta animales.
      <RouterLink class="herd-link" :to="{ name: 'herd-catalog' }">Ir a razas y lotes</RouterLink>
    </UiAlert>

    <form v-else class="stack" novalidate @submit.prevent="save">
      <UiAlert v-if="saveError" tone="danger" title="No se guardó">{{ saveError }}</UiAlert>

      <UiCard title="Identificación">
        <div class="herd-form">
          <fieldset class="herd-radios herd-span" :disabled="!!editingId">
            <legend>Especie *</legend>
            <UiRadio v-model="form.speciesId" name="species" value="1" label="Bovino (carne)" />
            <UiRadio v-model="form.speciesId" name="species" value="2" label="Caprino (cabritos y leche)" />
          </fieldset>
          <UiField v-model="form.visualTag" label="Arete visual *" maxlength="30" autocomplete="off" hint="Se guarda en mayúsculas." required />
          <UiField v-model="form.officialTag" label="Arete oficial" maxlength="30" autocomplete="off" />
          <UiField v-model="form.sex" as="select" label="Sexo *" :options="sexOptions" required />
          <UiField v-model="form.breedId" as="select" label="Raza *" :options="breedOptions" required />
          <UiField v-model="form.lotId" as="select" label="Lote" :options="lotOptions" />
          <UiField v-model="form.productionStage" as="select" label="Etapa productiva" :options="stageOptions" />
        </div>
      </UiCard>

      <UiCard title="Nacimiento y origen">
        <div class="herd-form">
          <UiField v-model="form.birthDate" label="Fecha de nacimiento" type="date" :max="today" />
          <UiField v-model="form.origin" as="select" label="Origen" :options="originOptions" />
          <UiField v-model="form.birthWeightKg" label="Peso al nacer (kg)" type="number" min="0.1" max="100" step="0.01" inputmode="decimal" />
        </div>
      </UiCard>

      <UiCard title="Genealogía">
        <div class="herd-form">
          <UiField v-model="form.sireId" as="select" label="Padre" :options="sireOptions" />
          <UiField v-model="form.damId" as="select" label="Madre" :options="damOptions" />
          <div class="herd-span">
            <UiField v-model="form.geneticsNotes" as="textarea" label="Notas genéticas" rows="3" maxlength="2000" />
          </div>
        </div>
      </UiCard>

      <div class="herd-actions">
        <UiButton type="submit" :loading="saving">{{ editingId ? 'Guardar cambios' : 'Registrar animal' }}</UiButton>
        <RouterLink
          class="btn secondary"
          :to="editingId ? { name: 'herd-animal', params: { id: editingId } } : { name: 'herd' }"
        >
          Cancelar
        </RouterLink>
      </div>
    </form>
  </UiBasePage>
</template>
