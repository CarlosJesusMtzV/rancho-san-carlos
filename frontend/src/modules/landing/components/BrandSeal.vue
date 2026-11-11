<script setup lang="ts">
// Sello circular de origen. Colores fijos: representa una pieza impresa.
</script>

<template>
  <svg class="lp-seal" viewBox="0 0 200 200" role="img" aria-label="Sello Rancho y Cremería San Carlos, Castaños, Coahuila">
    <defs>
      <path id="lp-seal-arc-top" d="M 30,100 A 70,70 0 1,1 170,100" />
      <path id="lp-seal-arc-bottom" d="M 22,100 A 78,78 0 0,0 178,100" />
    </defs>
    <circle cx="100" cy="100" r="95" fill="#F7F4EC" stroke="#1A1918" stroke-width="3" />
    <circle cx="100" cy="100" r="88" fill="none" stroke="#D19A36" stroke-width="1.8" stroke-dasharray="6 4" />
    <circle cx="100" cy="100" r="54" fill="none" stroke="#1A1918" stroke-width="1.5" />
    <text font-family="Cinzel, Georgia, serif" font-size="10.5" font-weight="700" fill="#1A1918">
      <textPath href="#lp-seal-arc-top" startOffset="50%" text-anchor="middle" textLength="196" lengthAdjust="spacingAndGlyphs">RANCHO Y CREMERÍA SAN CARLOS</textPath>
    </text>
    <text font-family="Plus Jakarta Sans, sans-serif" font-size="9" font-weight="700" fill="#B05A3E" letter-spacing="2">
      <textPath href="#lp-seal-arc-bottom" startOffset="50%" text-anchor="middle">CASTAÑOS · COAHUILA</textPath>
    </text>
    <path d="M72 86C80 86 84 74 96 74M128 86C120 86 116 74 104 74" stroke="#1A1918" stroke-width="2" fill="none" stroke-linecap="round" />
    <text x="100" y="112" text-anchor="middle" font-family="Cinzel, Georgia, serif" font-size="30" font-weight="700" fill="#1A1918">SC</text>
    <text x="100" y="128" text-anchor="middle" font-family="Cormorant Garamond, Georgia, serif" font-style="italic" font-size="11" fill="#5E6B3E">Calidad artesanal</text>
  </svg>
</template>
