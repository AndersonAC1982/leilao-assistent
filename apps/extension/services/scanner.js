import { request } from './api.js';

export async function runScanner(token) {
  return request('/scanner/run', {
    method: 'POST',
    token
  });
}
