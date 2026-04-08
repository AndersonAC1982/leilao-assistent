import { request } from './api.js';

export async function loadHistory(token, take = 8) {
  return request('/history', {
    token,
    query: { take: String(take) }
  });
}
