const reservedTlds = ['.example', '.invalid', '.test', '.localhost', '.local'];
const invalidPathTokens = ['/', '/home', '/inicio', '/index', '/default'];

export function isValidLotUrl(url: string | null | undefined): boolean {
  if (!url) {
    return false;
  }

  try {
    const parsed = new URL(url);
    if (parsed.protocol !== 'http:' && parsed.protocol !== 'https:') {
      return false;
    }

    const host = parsed.hostname.toLowerCase();
    if (!host || host === 'localhost' || host === '127.0.0.1' || host === '::1') {
      return false;
    }

    if (reservedTlds.some((suffix) => host.endsWith(suffix))) {
      return false;
    }

    const path = parsed.pathname.replace(/\/+$/, '').toLowerCase() || '/';
    if (invalidPathTokens.includes(path)) {
      return false;
    }

    return /\d/.test(parsed.pathname + parsed.search);
  } catch {
    return false;
  }
}
