const STORAGE_KEYS = {
  token: 'multileilao_extension_token',
  filters: 'multileilao_extension_filters',
  history: 'multileilao_extension_history',
  apiBaseUrl: 'multileilao_extension_api_base_url'
};

const LEGACY_STORAGE_KEYS = {
  [STORAGE_KEYS.token]: 'leilaoauto_extension_token',
  [STORAGE_KEYS.filters]: 'leilaoauto_extension_filters',
  [STORAGE_KEYS.history]: 'leilaoauto_extension_history',
  [STORAGE_KEYS.apiBaseUrl]: 'leilaoauto_extension_api_base_url'
};

export async function getItem(key, fallbackValue = null) {
  const payload = await chrome.storage.local.get(key);
  if (payload[key] !== undefined) {
    return payload[key];
  }

  const legacyKey = LEGACY_STORAGE_KEYS[key];
  if (!legacyKey) {
    return fallbackValue;
  }

  const legacyPayload = await chrome.storage.local.get(legacyKey);
  const legacyValue = legacyPayload[legacyKey];
  if (legacyValue === undefined) {
    return fallbackValue;
  }

  await setItem(key, legacyValue);
  await chrome.storage.local.remove(legacyKey);
  return legacyValue;
}

export async function setItem(key, value) {
  await chrome.storage.local.set({ [key]: value });
}

export async function removeItem(key) {
  await chrome.storage.local.remove(key);
}

export { STORAGE_KEYS };
