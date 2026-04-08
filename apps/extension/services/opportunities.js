import { request } from './api.js';

export async function loadOpportunities(token, filters) {
  const query = {
    search: filters.search || '',
    source: filters.source || '',
    minScore: String(filters.minScore ?? 60),
    vehicleType: filters.vehicleType ? String(filters.vehicleType) : '',
    region: filters.region || ''
  };

  Object.keys(query).forEach((key) => {
    if (query[key] === '') {
      delete query[key];
    }
  });

  return request('/opportunities', { token, query });
}
