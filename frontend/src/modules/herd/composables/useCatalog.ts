import { computed, ref } from 'vue'

import { getApiErrorMessage } from '../../../shared/api/errors'
import { getCatalog } from '../api/herdApi'
import type { HerdCatalog, SpeciesId } from '../types/herd'

export function useCatalog() {
  const catalog = ref<HerdCatalog | null>(null)
  const loading = ref(false)
  const error = ref('')

  async function load(): Promise<void> {
    loading.value = true
    error.value = ''
    try {
      catalog.value = await getCatalog()
    } catch (cause) {
      error.value = getApiErrorMessage(cause)
    } finally {
      loading.value = false
    }
  }

  const hasBreeds = computed(() => (catalog.value?.breeds.length ?? 0) > 0)

  function breedsFor(speciesId: SpeciesId) {
    return catalog.value?.breeds.filter((breed) => breed.speciesId === speciesId) ?? []
  }

  function lotsFor(speciesId: SpeciesId | undefined) {
    const lots = catalog.value?.lots ?? []
    return speciesId ? lots.filter((lot) => lot.speciesId === speciesId) : lots
  }

  return { catalog, loading, error, load, hasBreeds, breedsFor, lotsFor }
}
