const DEFAULT_API_BASE_URL = 'http://localhost:8080/api';

export async function request(path, options = {}) {
  const {
    method = 'GET',
    token,
    body,
    query,
    headers = {}
  } = options;

  const queryString = query ? `?${new URLSearchParams(query).toString()}` : '';
  const response = await fetch(`${DEFAULT_API_BASE_URL}${path}${queryString}`, {
    method,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...headers
    },
    body: body ? JSON.stringify(body) : undefined
  });

  const contentType = response.headers.get('content-type') || '';
  const payload = contentType.includes('application/json')
    ? await response.json()
    : null;

  if (!response.ok) {
    const message = payload?.detail || payload?.title || 'Falha na comunicacao com API.';
    const error = new Error(message);
    error.status = response.status;
    error.payload = payload;
    throw error;
  }

  return payload;
}
