import { request } from './api.js';

export async function loadSettings(token) {
  return request('/settings', { token });
}

export async function saveSettings(token, payload) {
  return request('/settings', {
    method: 'PUT',
    token,
    body: payload
  });
}
