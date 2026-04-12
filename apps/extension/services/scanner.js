import { request } from './api.js';

export async function runScanner(token, payload = null) {
  return request('/scanner/run', {
    method: 'POST',
    token,
    body: payload ?? {}
  });
}
