const STORAGE_KEYS = {
  token: 'leilaoauto_extension_token',
  filters: 'leilaoauto_extension_filters',
  history: 'leilaoauto_extension_history'
};

export async function getItem(key, fallbackValue = null) {
  const payload = await chrome.storage.local.get(key);
  return payload[key] ?? fallbackValue;
}

export async function setItem(key, value) {
  await chrome.storage.local.set({ [key]: value });
}

export async function removeItem(key) {
  await chrome.storage.local.remove(key);
}

export { STORAGE_KEYS };
