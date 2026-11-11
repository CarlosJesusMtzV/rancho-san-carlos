<script setup lang="ts">
import { UiBadge, UiBasePage, UiCallout, UiCard, UiLayout } from '../../../shared/components'

// Módulos planeados del sistema. Cada uno se construye como vertical slice con su spec.
const modules = [
  { name: 'Hato', scope: 'Especies, razas, lotes, animales y pesajes.', species: 'Bovino y caprino' },
  { name: 'Reproducción', scope: 'Montas, inseminaciones, partos y crías.', species: 'Bovino y caprino' },
  { name: 'Sanidad', scope: 'Eventos sanitarios, tratamientos, retiros y pruebas de laboratorio.', species: 'Bovino y caprino' },
  { name: 'Ordeña', scope: 'Litros por cabra y lotes de leche.', species: 'Solo caprino' },
  { name: 'Quesería', scope: 'Lotes de queso canasta, panela y asadero con trazabilidad a la leche.', species: 'Solo caprino' },
  { name: 'Matanza de cabritos', scope: 'Sacrificio y pelado para venta en canal, solo tras el retiro sanitario.', species: 'Solo caprino' },
  { name: 'Ventas', scope: 'Pedidos de queso, cabritos en pie o en canal y becerros vivos de unos 8 meses y 150 kg.', species: 'Bovino y caprino' },
]
</script>

<template>
  <UiBasePage fluid title="Panel" description="Sistema de gestión de Rancho San Carlos y La Cremería San Carlos">
    <UiLayout kind="stack">
      <UiCallout title="Reglas del negocio">
        Las vacas se crían únicamente para carne y se venden vivas, como becerros de unos 8 meses y 150 kg. Las chivas
        dan cabritos, que se venden en pie o en canal, y la leche con la que se elaboran los tres quesos. Solo se ordeñan
        cabras.
      </UiCallout>
      <UiCard title="Módulos planeados" description="Ninguno está implementado todavía; este panel no consulta la API.">
        <ul class="module-list">
          <li v-for="module in modules" :key="module.name">
            <div>
              <strong>{{ module.name }}</strong>
              <p>{{ module.scope }}</p>
            </div>
            <UiBadge :tone="module.species === 'Solo caprino' ? 'success' : 'info'">{{ module.species }}</UiBadge>
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
</style>
