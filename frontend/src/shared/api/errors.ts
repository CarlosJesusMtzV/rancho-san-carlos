import { isAxiosError } from 'axios'

interface ProblemBody {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

/** Mensaje en español para mostrar al usuario a partir de un error de la API. */
export function getApiErrorMessage(error: unknown): string {
  if (isAxiosError<ProblemBody>(error)) {
    if (!error.response) return 'No se pudo conectar con el servidor. Revisa que la API esté en ejecución.'

    const body = error.response.data
    if (body?.detail) return body.detail
    const firstFieldError = body?.errors ? Object.values(body.errors).flat()[0] : undefined
    if (firstFieldError) return firstFieldError
    if (error.response.status === 404) return 'El recurso solicitado no existe.'
  }

  return 'Ocurrió un error inesperado. Intenta de nuevo.'
}

/** Estado HTTP de un error de la API, si lo hay. */
export function getApiErrorStatus(error: unknown): number | undefined {
  return isAxiosError(error) ? error.response?.status : undefined
}
