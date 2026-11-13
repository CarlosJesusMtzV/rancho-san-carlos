# Primeros pasos en Windows

Dos formas de ver el proyecto. Empieza por la A: es la más rápida y basta para trabajar en el sitio y el panel.

## Programas

| Programa | Para qué | Necesario para |
| --- | --- | --- |
| [Git for Windows](https://git-scm.com/download/win) | Control de versiones y la terminal **Git Bash** (incluye OpenSSL) | A y B |
| [Node.js 22 LTS](https://nodejs.org/) | Correr el frontend | A |
| [Visual Studio Code](https://code.visualstudio.com/) con la extensión **Vue - Official** | Editar | A y B |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) (con WSL 2) | PostgreSQL, backend y frontend en contenedores | B |
| [Python 3](https://www.python.org/downloads/) (marcar «Add python.exe to PATH») | Script de arranque | B |
| [.NET SDK 10](https://dotnet.microsoft.com/download) y extensión **C# Dev Kit** | Compilar el backend fuera de Docker | Opcional |

## A. Solo el frontend (5 minutos)

El sitio público y el panel no llaman a la API todavía, así que funcionan sin backend ni base de datos.

```bash
cd /c/rancho-san-carlos-proyecto/rancho-san-carlos/frontend
npm install
npm run dev
```

Abre https://localhost:5173 (sitio público) y https://localhost:5173/panel (sistema). El navegador avisará del certificado local: elige «Avanzado» y «Continuar». Cada vez que guardes un archivo la página se actualiza sola.

## B. Proyecto completo con Docker

Con Docker Desktop abierto, desde **Git Bash**:

```bash
cd /c/rancho-san-carlos-proyecto/rancho-san-carlos
python scripts/setup-dev.py --project-name rancho-san-carlos
```

En Windows se usa `python` en lugar de `bash scripts/setup-dev.sh`, porque ese script busca `python3` y Windows suele instalarlo como `python`.

| Servicio | Dirección |
| --- | --- |
| Sitio y panel | https://localhost:5173 |
| API | https://localhost:7001/api/health |
| PostgreSQL | Solo dentro de Docker |

Consultar la base:

```bash
docker compose exec db psql -U tecnm_app -d tecnm_template
```

Dentro de `psql`: `\dt` lista las tablas, `\dv` las vistas, `SELECT * FROM products;` muestra el catálogo y `\q` sale.

`database.sql` solo se ejecuta la primera vez. Si ya habías creado la base antes, aplica una vez, en orden, las migraciones que te falten (`docs/migrations/`, lista en `specs/database.spec.md`):

```bash
docker compose exec -T db psql -U tecnm_app -d tecnm_template -v ON_ERROR_STOP=1 < docs/migrations/2026-11-11-01-sanidad.sql
docker compose exec -T db psql -U tecnm_app -d tecnm_template -v ON_ERROR_STOP=1 < docs/migrations/2026-11-11-02-reproduccion.sql
docker compose exec -T db psql -U tecnm_app -d tecnm_template -v ON_ERROR_STOP=1 < docs/migrations/2026-11-11-03-ordena.sql
docker compose exec -T db psql -U tecnm_app -d tecnm_template -v ON_ERROR_STOP=1 < docs/migrations/2026-11-13-04-queseria.sql
docker compose exec -T db psql -U tecnm_app -d tecnm_template -v ON_ERROR_STOP=1 < docs/migrations/2026-11-13-05-matanza.sql
docker compose exec -T db psql -U tecnm_app -d tecnm_template -v ON_ERROR_STOP=1 < docs/migrations/2026-11-13-06-ventas.sql
```

Para empezar de cero con otro volumen usa otro nombre de proyecto:

```bash
python scripts/setup-dev.py --project-name rancho-prueba --frontend-port 5180 --backend-port 7010
```

Detener: `docker compose stop`. Volver a arrancar: `docker compose up -d --wait`.

## Seguridad

- El script de arranque guarda los puertos y una contraseña de PostgreSQL generada al azar en `.env`. Ese archivo es solo de tu equipo: **nunca lo subas**.
- `--project-name rancho-san-carlos` separa contenedores y volumen de otros proyectos con la misma plantilla. Usa siempre el mismo nombre para conservar tus datos.
- Si un puerto está ocupado, el script lo avisa antes de construir o arrancar.
- No subas secretos, credenciales, datos personales, `.env`, llaves o certificados TLS (`.docker/certs/`) ni respaldos de la base. `.gitignore` ya los excluye; no lo fuerces con `git add -f`.
- Para documentar configuración usa los `.env.example` saneados, con valores vacíos o de ejemplo.
- Antes de cada `git push`, revisa con `git status` que no aparezca ninguno de esos archivos.

## Guardar el trabajo en Git

```bash
cd /c/rancho-san-carlos-proyecto/rancho-san-carlos
git init
git add .
git commit -m "Base del proyecto Rancho San Carlos"
```

Después crea un repositorio vacío en GitHub y sigue las instrucciones de «push an existing repository».

## Cómo seguir

1. Lee `specs/domain.spec.md`: reglas del negocio y orden de módulos.
2. El módulo **Hato** ya está hecho: con Docker abre https://localhost:5173/panel/hato, registra primero razas en «Razas y lotes» y después los animales. Sin Docker las pantallas cargan pero muestran que no hay conexión con la API.
3. **Sanidad** también: en https://localhost:5173/panel/sanidad registra primero los medicamentos y después las visitas y los eventos.
4. **Reproducción** también, en https://localhost:5173/panel/reproduccion. Para diagnosticar preñez hace falta una visita del veterinario registrada en Sanidad.
5. **Ordeña** en https://localhost:5173/panel/ordena: una ordeña al día con el total de litros y las cabras ordeñadas; las que tienen retiro de leche no se pueden marcar.
6. **Quesería** en https://localhost:5173/panel/queseria: tandas de canasta y panela con la leche de una ordeña, compras de asadero (de proveedor, leche de vaca), mermas y existencias con caducidad.
7. **Matanza de cabritos** en https://localhost:5173/panel/matanza: jornadas en rastro o con matancero, peso vivo y en canal de cada cabrito y canales por vender.
8. **Ventas** en https://localhost:5173/panel/ventas: clientes, ventas en el momento y encargos de queso, ganado en pie y canal por kilo; cobro al entregar y factura opcional.
9. Siguiente: página pública de trazabilidad con datos reales. Su spec va en `backend/Modules/<Modulo>/specs/` antes del código (flujo en `CONTRIBUTING.md`).
6. Coloca las fotos en `frontend/public/img/` con los nombres de `frontend/src/modules/landing/specs/landing.spec.md`.

## Problemas comunes

| Síntoma | Solución |
| --- | --- |
| `python3: command not found` o se abre la Microsoft Store | Usa `python scripts/setup-dev.py`; en Windows desactiva los «alias de ejecución de aplicaciones» de Python |
| `Falta docker` o `Cannot connect to the Docker daemon` | Abre Docker Desktop y espera a que diga «Engine running» |
| Puerto 5173 o 7001 ocupado | Agrega `--frontend-port 5180 --backend-port 7010` |
| `$'\r': command not found` al correr un `.sh` | El archivo tomó finales de línea de Windows; `.gitattributes` lo evita en clones nuevos. Arreglo: `sed -i 's/\r$//' scripts/*.sh` |
