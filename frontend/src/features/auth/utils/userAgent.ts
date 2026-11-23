/** Turns a raw User-Agent string into a short label such as "Chrome on Windows". */
export function describeUserAgent(userAgent: string): string {
  if (!userAgent) return 'Unknown browser'

  const browser =
    /Edg\//.test(userAgent) ? 'Edge'
    : /OPR\/|Opera/.test(userAgent) ? 'Opera'
    : /Firefox\//.test(userAgent) ? 'Firefox'
    : /Chrome\//.test(userAgent) ? 'Chrome'
    : /Safari\//.test(userAgent) ? 'Safari'
    : /curl\//i.test(userAgent) ? 'curl'
    : 'Unknown browser'

  const os =
    /Windows/.test(userAgent) ? 'Windows'
    : /Android/.test(userAgent) ? 'Android'
    : /iPhone|iPad|iOS/.test(userAgent) ? 'iOS'
    : /Mac OS X|Macintosh/.test(userAgent) ? 'macOS'
    : /Linux/.test(userAgent) ? 'Linux'
    : ''

  return os ? `${browser} on ${os}` : browser
}
