<script setup lang="ts">
import { RouterLink } from 'vue-router'

import { UiBadge, UiBasePage, UiCallout, UiCard, UiLayout } from '../../../shared/components'

// Módulos del sistema. Cada uno se construye como vertical slice con su spec; `to` marca los disponibles.
const modules: { name: string; scope: string; species: string; to?: string }[] = [
  { name: 'Hato', scope: 'Especies, razas, lotes, animales, pesajes y becerros para venta.', species: 'Bovino y caprino', to: '/panel/hato' },
  { name: 'Reproducción', scope: 'Montas naturales, diagnóstico de preñez del veterinario, partos y alta de crías.', species: 'Bovino y caprino', to: '/panel/reproduccion' },
  { name: 'Sanidad', scope: 'Visitas del veterinario, eventos, tratamientos, retiros y pruebas de laboratorio.', species: 'Bovino y caprino', to: '/panel/sanidad' },
  { name: 'Ordeña', scope: 'Ordeña diaria: total de litros, cabras ordeñadas y lote de leche del día.', species: 'Solo caprino', to: '/panel/ordena' },
  { name: 'Quesería', scope: 'Tandas de canasta y panela con la leche del día, compras de asadero de proveedor, mermas y existencias.', species: 'Caprino; asadero de vaca, de proveedor', to: '/panel/queseria' },
  { name: 'Matanza de cabritos', scope: 'Sacrificio y pelado para venta en canal, solo tras el retiro sanitario.', species: 'Solo caprino' },
  { name: 'Ventas', scope: 'Pedidos de queso, cabritos en pie o en canal y becerros vivos de unos 8 meses y 150 kg.', species: 'Bovino y caprino' },
]
</script>

<template>
  <UiBasePage fluid title="Panel" description="Sistema de gestión de Rancho San Carlos y La Cremería San Carlos">
    <UiLayout kind="stack">
      <UiCallout title="Reglas del negocio">
        Las vacas se crían únicamente para carne y se venden vivas, como becerros de unos 8 meses y 150 kg. Las chivas
        dan cabritos, que se venden en pie o en canal, y la leche con la que se elaboran el canasta y el panela. Solo se
        ordeñan cabras. El asadero se compra hecho (leche de vaca) y solo se revende.
      </UiCallout>
      <UiCard title="Módulos" description="Se construyen en este orden. Los disponibles llevan enlace.">
        <ul class="module-list">
          <li v-for="module in modules" :key="module.name">
            <div>
              <RouterLink v-if="module.to" class="module-link" :to="module.to">{{ module.name }}</RouterLink>
              <strong v-else>{{ module.name }}</strong>
              <p>{{ module.scope }}</p>
            </div>
            <div class="module-tags">
              <UiBadge :tone="module.species === 'Solo caprino' ? 'success' : 'info'">{{ module.species }}</UiBadge>
              <UiBadge :tone="module.to ? 'success' : 'warning'">{{ module.to ? 'Disponible' : 'Pendiente' }}</UiBadge>
            </div>
          </li>
        </ul>
      </UiCard>
    </UiLayout>
  </UiBasePage>
</template>

<style scoped>
.module-list { list-style: none; margin: 0; padding: 0; display: grid; gap: 4px; }
.module-list li {
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-wrap: wrap;
  gap: 12px;
  padding: 12px 0;
  border-top: 1px solid var(--color-border-light);
}
.module-list li:first-child { border-top: 0; }
.module-list p { font-size: 14px; }
.module-link { font-weight: 700; text-decoration: underline; text-underline-offset: 3px; }
.module-tags { display: flex; flex-wrap: wrap; gap: 8px; }
</style>
