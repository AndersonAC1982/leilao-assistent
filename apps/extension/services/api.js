import { getItem, setItem, STORAGE_KEYS } from './storage.js';

const DEFAULT_EXTENSION_ENV = {
  environmentName: 'dev',
  apiBaseCandidates: [
    'http://localhost:8080/api',
    'http://127.0.0.1:8080/api',
    'http://localhost:5198/api',
    'http://127.0.0.1:5198/api',
    'http://localhost:5000/api',
    'http://127.0.0.1:5000/api'
  ]
};

const REQUEST_TIMEOUT_MS = 4000;
const HEALTH_TIMEOUT_MS = 1800;

let cachedApiBaseUrl = null;
let cachedExtensionEnv = null;

function normalizeApiBaseUrl(input) {
  if (!input || typeof input !== 'string') {
    return null;
  }

  let parsed;
  try {
    parsed = new URL(input.trim());
  } catch {
    return null;
  }

  if (!/^https?:$/i.test(parsed.protocol)) {
    return null;
  }

  let path = parsed.pathname || '/';
  path = path.replace(/\/+$/, '');

  if (!path || path === '/') {
    path = '/api';
  } else if (!/\/api$/i.test(path)) {
    path = `${path}/api`;
  }

  return `${parsed.protocol}//${parsed.host}${path}`;
}

function normalizeEnvConfig(config) {
  if (!config || typeof config !== 'object') {
    return { ...DEFAULT_EXTENSION_ENV };
  }

  const apiBaseCandidates = Array.isArray(config.apiBaseCandidates)
    ? config.apiBaseCandidates
        .map((candidate) => normalizeApiBaseUrl(candidate))
        .filter(Boolean)
    : [];

  return {
    environmentName: String(config.environmentName || DEFAULT_EXTENSION_ENV.environmentName).trim() || 'dev',
    apiBaseCandidates: apiBaseCandidates.length > 0
      ? apiBaseCandidates
      : [...DEFAULT_EXTENSION_ENV.apiBaseCandidates]
  };
}

async function getExtensionEnv() {
  if (cachedExtensionEnv) {
    return cachedExtensionEnv;
  }

  try {
    const response = await fetch(chrome.runtime.getURL('config/environment.json'), { cache: 'no-store' });
    if (!response.ok) {
      cachedExtensionEnv = { ...DEFAULT_EXTENSION_ENV };
      return cachedExtensionEnv;
    }

    const payload = await response.json();
    cachedExtensionEnv = normalizeEnvConfig(payload);
    return cachedExtensionEnv;
  } catch {
    cachedExtensionEnv = { ...DEFAULT_EXTENSION_ENV };
    return cachedExtensionEnv;
  }
}

function toHealthUrl(apiBaseUrl) {
  const parsed = new URL(apiBaseUrl);
  parsed.pathname = parsed.pathname.replace(/\/api$/i, '') || '/';
  parsed.pathname = parsed.pathname.replace(/\/+$/, '');
  parsed.pathname = `${parsed.pathname || ''}/health`.replace('//', '/');
  parsed.search = '';
  parsed.hash = '';
  return parsed.toString();
}

function uniqueCandidates(candidates) {
  const normalized = [];
  const seen = new Set();

  for (const candidate of candidates) {
    const apiBaseUrl = normalizeApiBaseUrl(candidate);
    if (!apiBaseUrl) {
      continue;
    }

    const key = apiBaseUrl.toLowerCase();
    if (seen.has(key)) {
      continue;
    }

    seen.add(key);
    normalized.push(apiBaseUrl);
  }

  return normalized;
}

async function fetchWithTimeout(url, options = {}, timeoutMs = REQUEST_TIMEOUT_MS) {
  const controller = new AbortController();
  const timeoutId = setTimeout(() => controller.abort(), timeoutMs);

  try {
    return await fetch(url, { ...options, signal: controller.signal });
  } finally {
    clearTimeout(timeoutId);
  }
}

function buildUnreachableError(apiBaseUrl, reason) {
  const error = new Error(
    `Nao foi possivel conectar na API (${apiBaseUrl}). Confirme se o backend esta online.`
  );
  error.code = 'API_UNREACHABLE';
  error.apiBaseUrl = apiBaseUrl;
  error.reason = reason;
  return error;
}

async function pingApi(apiBaseUrl) {
  try {
    const response = await fetchWithTimeout(toHealthUrl(apiBaseUrl), { method: 'GET' }, HEALTH_TIMEOUT_MS);
    return response.ok;
  } catch {
    return false;
  }
}

export async function getConfiguredApiBaseUrl() {
  const stored = await getItem(STORAGE_KEYS.apiBaseUrl, null);
  return normalizeApiBaseUrl(stored);
}

export async function setConfiguredApiBaseUrl(apiBaseUrl) {
  const normalized = normalizeApiBaseUrl(apiBaseUrl);
  if (!normalized) {
    return null;
  }

  cachedApiBaseUrl = normalized;
  await setItem(STORAGE_KEYS.apiBaseUrl, normalized);
  return normalized;
}

export async function discoverApiBaseUrl(force = false) {
  if (!force && cachedApiBaseUrl) {
    const stillHealthy = await pingApi(cachedApiBaseUrl);
    if (stillHealthy) {
      return { ok: true, apiBaseUrl: cachedApiBaseUrl, source: 'cache' };
    }
  }

  const extensionEnv = await getExtensionEnv();
  const configured = await getConfiguredApiBaseUrl();
  const candidates = uniqueCandidates([configured, ...extensionEnv.apiBaseCandidates]);

  for (const candidate of candidates) {
    const healthy = await pingApi(candidate);
    if (!healthy) {
      continue;
    }

    cachedApiBaseUrl = candidate;
    await setItem(STORAGE_KEYS.apiBaseUrl, candidate);
    return {
      ok: true,
      apiBaseUrl: candidate,
      source: configured && candidate === configured ? 'configured' : 'autodetect'
    };
  }

  const fallback = configured || normalizeApiBaseUrl(extensionEnv.apiBaseCandidates[0]);
  if (fallback) {
    cachedApiBaseUrl = fallback;
  }

  return {
    ok: false,
    apiBaseUrl: fallback,
    source: 'offline'
  };
}

export async function testApiConnection(apiBaseUrl) {
  const normalized = normalizeApiBaseUrl(apiBaseUrl);
  if (!normalized) {
    return {
      ok: false,
      apiBaseUrl: null,
      message: 'URL da API invalida. Use formato http://host:porta/api ou http://host:porta.'
    };
  }

  const healthy = await pingApi(normalized);
  return {
    ok: healthy,
    apiBaseUrl: normalized,
    message: healthy ? 'API online.' : `API offline em ${normalized}.`
  };
}

async function executeHttpRequest(apiBaseUrl, path, options = {}) {
  const {
    method = 'GET',
    token,
    body,
    query,
    headers = {},
    timeoutMs = REQUEST_TIMEOUT_MS
  } = options;

  const queryString = query ? `?${new URLSearchParams(query).toString()}` : '';

  let response;
  try {
    response = await fetchWithTimeout(`${apiBaseUrl}${path}${queryString}`, {
      method,
      headers: {
        'Content-Type': 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {}),
        ...headers
      },
      body: body ? JSON.stringify(body) : undefined
    }, timeoutMs);
  } catch (error) {
    throw buildUnreachableError(apiBaseUrl, error?.message || 'network_error');
  }

  const contentType = response.headers.get('content-type') || '';
  const payload = contentType.includes('application/json')
    ? await response.json()
    : null;

  if (!response.ok) {
    const message = payload?.detail || payload?.title || 'Falha na comunicacao com API.';
    const error = new Error(message);
    error.status = response.status;
    error.payload = payload;
    error.apiBaseUrl = apiBaseUrl;
    throw error;
  }

  return payload;
}

export async function request(path, options = {}) {
  const { baseUrl = null, retryWithAutoDetect = true } = options;

  const normalizedBaseUrl = normalizeApiBaseUrl(baseUrl);

  if (normalizedBaseUrl) {
    return executeHttpRequest(normalizedBaseUrl, path, options);
  }

  const extensionEnv = await getExtensionEnv();
  const discovered = await discoverApiBaseUrl(false);
  const firstBaseUrl = discovered.apiBaseUrl || normalizeApiBaseUrl(extensionEnv.apiBaseCandidates[0]);

  if (!firstBaseUrl) {
    throw buildUnreachableError('unknown', 'api_base_not_configured');
  }

  try {
    return await executeHttpRequest(firstBaseUrl, path, options);
  } catch (error) {
    if (error?.code !== 'API_UNREACHABLE' || !retryWithAutoDetect) {
      throw error;
    }

    const fallback = await discoverApiBaseUrl(true);
    if (!fallback.ok || !fallback.apiBaseUrl || fallback.apiBaseUrl === firstBaseUrl) {
      throw error;
    }

    return executeHttpRequest(fallback.apiBaseUrl, path, options);
  }
}
