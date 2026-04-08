import { request } from './api.js';
import { getItem, removeItem, setItem, STORAGE_KEYS } from './storage.js';

export async function getToken() {
  return getItem(STORAGE_KEYS.token, null);
}

export async function login(email, password) {
  const response = await request('/auth/login', {
    method: 'POST',
    body: { email, password }
  });

  await setItem(STORAGE_KEYS.token, response.token);
  return response;
}

export async function me(token) {
  return request('/me', { token });
}

export async function logout() {
  await removeItem(STORAGE_KEYS.token);
}
