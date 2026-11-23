export class ApiError extends Error {
  status: number
  /** Field-level messages, e.g. { email: 'Email is already taken' } */
  fieldErrors: Record<string, string>

  constructor(message: string, status: number, fieldErrors: Record<string, string> = {}) {
    super(message)
    this.status = status
    this.fieldErrors = fieldErrors
  }
}

type Method = 'GET' | 'POST' | 'PUT' | 'DELETE'

/**
 * JSON request helper. The JWT lives in an HttpOnly cookie, so requests must include credentials.
 * X-Requested-With is required by the API for state-changing requests (CSRF defence).
 * Accepts ASP.NET ProblemDetails / ValidationProblemDetails error bodies.
 */
async function request<T>(method: Method, url: string, body?: unknown): Promise<T> {
  const headers: Record<string, string> = { 'X-Requested-With': 'fetch' }
  const isForm = body instanceof FormData // the browser sets the multipart boundary itself
  if (body !== undefined && !isForm) headers['Content-Type'] = 'application/json'

  let response: Response
  try {
    response = await fetch(url, {
      method,
      credentials: 'include',
      headers,
      body: body === undefined ? undefined : isForm ? body : JSON.stringify(body),
    })
  } catch {
    throw new ApiError('Cannot reach the server. Please try again.', 0)
  }

  if (response.ok) {
    const text = await response.text()
    return (text ? JSON.parse(text) : undefined) as T
  }

  let message = 'Something went wrong. Please try again.'
  const fieldErrors: Record<string, string> = {}
  try {
    const problem = await response.json()
    if (typeof problem.detail === 'string') message = problem.detail
    else if (typeof problem.title === 'string') message = problem.title
    if (problem.errors && typeof problem.errors === 'object') {
      for (const [key, value] of Object.entries(problem.errors)) {
        const field = key.charAt(0).toLowerCase() + key.slice(1)
        fieldErrors[field] = Array.isArray(value) ? String(value[0]) : String(value)
      }
    }
  } catch {
    // non-JSON error body: keep the default message
  }
  throw new ApiError(message, response.status, fieldErrors)
}

export const getJson = <T>(url: string) => request<T>('GET', url)
export const postJson = <T = void>(url: string, body?: unknown) => request<T>('POST', url, body)
export const postForm = <T>(url: string, form: FormData) => request<T>('POST', url, form)
export const putJson = <T>(url: string, body: unknown) => request<T>('PUT', url, body)
export const deleteJson = <T = void>(url: string, body?: unknown) => request<T>('DELETE', url, body)
