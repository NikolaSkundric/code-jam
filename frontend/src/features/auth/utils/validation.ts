const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
const USERNAME_PATTERN = /^[A-Za-z0-9_.-]+$/

export const PASSWORD_MIN_LENGTH = 8

export function validateEmail(value: string): string | undefined {
  if (!value.trim()) return 'Email is required'
  if (!EMAIL_PATTERN.test(value.trim())) return 'Enter a valid email address'
}

export function validateUsername(value: string): string | undefined {
  const v = value.trim()
  if (!v) return 'Username is required'
  if (v.length < 3 || v.length > 32) return 'Username must be 3-32 characters'
  if (!USERNAME_PATTERN.test(v)) return 'Use letters, numbers, dots, dashes and underscores only'
}

export function validatePassword(value: string): string | undefined {
  if (!value) return 'Password is required'
  if (value.length < PASSWORD_MIN_LENGTH) return `Password must be at least ${PASSWORD_MIN_LENGTH} characters`
}

export function validateConfirmPassword(password: string, confirm: string): string | undefined {
  if (!confirm) return 'Please confirm your password'
  if (password !== confirm) return 'Passwords do not match'
}
