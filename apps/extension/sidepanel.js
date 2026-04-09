import { getToken, login, logout, me } from './services/auth.js';
import { loadHistory } from './services/history.js';
import { loadOpportunities } from './services/opportunities.js';
import { runScanner } from './services/scanner.js';
import { loadSettings, saveSettings } from './services/settings.js';
import { getItem, setItem, STORAGE_KEYS } from './services/storage.js';

const state = {
  token: null,
  me: null,
  opportunities: [],
  history: [],
  scanStatus: 'Sem login',
  filters: {
    search: '',
    source: '',
    minScore: 60,
    vehicleType: null,
    region: ''
  }
};

const refs = {
  loginForm: document.getElementById('login-form'),
  emailInput: document.getElementById('email-input'),
  passwordInput: document.getElementById('password-input'),
  sessionView: document.getElementById('session-view'),
  sessionEmail: document.getElementById('session-email'),
  logoutButton: document.getElementById('logout-button'),
  runNowButton: document.getElementById('run-now-button'),
  statusGrid: document.getElementById('status-grid'),
  searchInput: document.getElementById('search-input'),
  sourceInput: document.getElementById('source-input'),
  scoreInput: document.getElementById('score-input'),
  typeInput: document.getElementById('type-input'),
  regionInput: document.getElementById('region-input'),
  applyFiltersButton: document.getElementById('apply-filters-button'),
  saveSettingsButton: document.getElementById('save-settings-button'),
  opportunitiesList: document.getElementById('opportunities-list'),
  historyList: document.getElementById('history-list'),
  feedback: document.getElementById('feedback')
};

function isValidLotUrl(url) {
  if (!url || typeof url !== 'string') {
    return false;
  }

  const trimmed = url.trim();
  if (!trimmed.startsWith('http://') && !trimmed.startsWith('https://')) {
    return false;
  }

  try {
    const parsed = new URL(trimmed);
    const hasSpecificPath = parsed.pathname.length > 1;
    const hasQuery = parsed.search.length > 1;
    return parsed.hostname.includes('.') && (hasSpecificPath || hasQuery);
  } catch {
    return false;
  }
}

function toPlanLabel(plan) {
  switch (Number(plan)) {
    case 1:
      return 'Free';
    case 2:
      return 'Pro';
    case 3:
      return 'Premium';
    case 4:
      return 'Elite';
    default:
      return String(plan ?? 'N/A');
  }
}

function toScoreLabel(label, score) {
  const normalized = (label || '').trim().toUpperCase();
  if (normalized) {
    return normalized.replaceAll('_', ' ');
  }

  if (score >= 85) {
    return 'OPORTUNIDADE';
  }

  if (score >= 65) {
    return 'BOM PRECO';
  }

  return 'ACIMA DA MEDIA';
}

function toRiskLabel(riskDecision) {
  const normalized = (riskDecision || '').trim().toUpperCase();
  if (!normalized) {
    return 'COMPRA SEGURA';
  }

  if (normalized.includes('ALTO')) {
    return 'ALTO RISCO';
  }

  if (normalized.includes('RISCO')) {
    return 'OPORTUNIDADE COM RISCO';
  }

  if (normalized.includes('SEGURA')) {
    return 'COMPRA SEGURA';
  }

  return normalized.replaceAll('_', ' ');
}

function setFeedback(message, type = 'ok') {
  refs.feedback.textContent = message || '';
  refs.feedback.classList.remove('ok', 'error');
  if (message) {
    refs.feedback.classList.add(type);
  }
}

async function handleSessionExpired(message = 'Sessao expirada. Faca login novamente.') {
  await logout();
  state.token = null;
  state.me = null;
  state.opportunities = [];
  state.history = await getItem(STORAGE_KEYS.history, []);
  state.scanStatus = 'Sem login';

  renderAuth();
  renderStatus();
  renderOpportunities();
  renderHistory();
  setFeedback(message, 'error');
}

function renderAuth() {
  const authenticated = !!state.token;
  refs.loginForm.classList.toggle('hidden', authenticated);
  refs.sessionView.classList.toggle('hidden', !authenticated);
  refs.runNowButton.disabled = !authenticated;
  refs.applyFiltersButton.disabled = !authenticated;
  refs.saveSettingsButton.disabled = !authenticated;

  if (authenticated && state.me) {
    refs.sessionEmail.textContent = `${state.me.email} | Plano ${toPlanLabel(state.me.plan)}`;
    return;
  }

  refs.sessionEmail.textContent = '';
}

function renderStatus() {
  const maxScore = state.opportunities.length > 0
    ? Math.max(...state.opportunities.map((item) => item.score || 0))
    : 0;
  const strong = state.opportunities.filter((item) => (item.score || 0) >= 75).length;
  const sources = new Set(state.opportunities.map((item) => item.source)).size;

  const chips = [
    { label: 'Fontes ativas', value: String(sources) },
    { label: 'Lotes encontrados', value: String(state.opportunities.length) },
    { label: 'Maior score', value: maxScore.toFixed(1) },
    { label: 'Fortes', value: String(strong) },
    { label: 'Varredura', value: state.scanStatus }
  ];

  refs.statusGrid.innerHTML = '';
  chips.forEach((chip) => {
    const element = document.createElement('article');
    element.className = 'status-chip';
    element.innerHTML = `<span>${chip.label}</span><strong>${chip.value}</strong>`;
    refs.statusGrid.appendChild(element);
  });
}

function renderOpportunities() {
  refs.opportunitiesList.innerHTML = '';

  if (state.opportunities.length === 0) {
    refs.opportunitiesList.innerHTML = '<p class="muted">Nenhuma oportunidade encontrada.</p>';
    return;
  }

  state.opportunities.forEach((item) => {
    if (!isValidLotUrl(item.lotUrl)) {
      return;
    }

    const scoreValue = Number(item.score || 0);
    const score = toScoreLabel(item.scoreLabel, scoreValue);
    const risk = toRiskLabel(item.riskDecision);

    const card = document.createElement('article');
    card.className = 'item-card';
    card.innerHTML = `
      <h3>${item.title}</h3>
      <p><strong>Fonte:</strong> ${item.source}</p>
      <p><strong>Score:</strong> ${score} (${scoreValue.toFixed(1)})</p>
      <p><strong>Local:</strong> ${item.location || '-'}</p>
      <p><strong>Valor:</strong> R$ ${(item.value || 0).toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</p>
      <p><strong>Risco:</strong> ${risk} (${Number(item.riskScore || 0).toFixed(1)})</p>
      <p>${item.summary || ''}</p>
      <button class="primary open-lot" type="button">Abrir lote</button>
    `;

    const openButton = card.querySelector('.open-lot');
    openButton.addEventListener('click', () => {
      if (!isValidLotUrl(item.lotUrl)) {
        setFeedback('URL de lote invalida. Item bloqueado.', 'error');
        return;
      }

      chrome.tabs.create({ url: item.lotUrl });
    });

    refs.opportunitiesList.appendChild(card);
  });
}

function renderHistory() {
  refs.historyList.innerHTML = '';

  if (state.history.length === 0) {
    refs.historyList.innerHTML = '<p class="muted">Sem historico recente.</p>';
    return;
  }

  state.history.slice(0, 10).forEach((entry) => {
    const card = document.createElement('article');
    card.className = 'item-card';

    const date = new Date(entry.executedAtUtc).toLocaleString('pt-BR');
    card.innerHTML = `
      <h3>${entry.source}</h3>
      <p><strong>Data:</strong> ${date}</p>
      <p><strong>Status:</strong> ${entry.status}</p>
      <p><strong>Novos lotes:</strong> ${entry.newLots}</p>
      <p>${entry.message || ''}</p>
    `;

    refs.historyList.appendChild(card);
  });
}

function readFiltersFromInputs() {
  const parsedScore = Number(refs.scoreInput.value || 60);
  const minScore = Number.isFinite(parsedScore) ? Math.min(100, Math.max(0, parsedScore)) : 60;

  state.filters = {
    search: refs.searchInput.value.trim(),
    source: refs.sourceInput.value.trim(),
    minScore,
    vehicleType: refs.typeInput.value ? Number(refs.typeInput.value) : null,
    region: refs.regionInput.value.trim().toUpperCase().slice(0, 2)
  };
}

function writeFiltersToInputs() {
  refs.searchInput.value = state.filters.search || '';
  refs.sourceInput.value = state.filters.source || '';
  refs.scoreInput.value = String(state.filters.minScore ?? 60);
  refs.typeInput.value = state.filters.vehicleType ? String(state.filters.vehicleType) : '';
  refs.regionInput.value = state.filters.region || '';
}

function buildFiltersPayload() {
  return {
    search: state.filters.search,
    source: state.filters.source,
    minScore: Number(state.filters.minScore || 0),
    vehicleType: state.filters.vehicleType,
    region: state.filters.region || null,
    advancedFiltersEnabled: true
  };
}

function applyBackendSettingsToState(settings) {
  state.filters = {
    search: settings.search || '',
    source: settings.source || '',
    minScore: settings.minScore ?? 60,
    vehicleType: settings.vehicleType ?? null,
    region: settings.region || ''
  };
}

async function loadPanelData() {
  if (!state.token) {
    state.opportunities = [];
    state.history = await getItem(STORAGE_KEYS.history, []);
    renderStatus();
    renderOpportunities();
    renderHistory();
    return;
  }

  const [opportunitiesResult, historyResult] = await Promise.allSettled([
    loadOpportunities(state.token, state.filters),
    loadHistory(state.token, 8)
  ]);

  if (opportunitiesResult.status === 'fulfilled') {
    state.opportunities = opportunitiesResult.value.filter((item) => isValidLotUrl(item.lotUrl));
  } else {
    if (opportunitiesResult.reason?.status === 401) {
      await handleSessionExpired();
      return;
    }

    state.opportunities = [];
    setFeedback(opportunitiesResult.reason?.message || 'Falha ao carregar oportunidades.', 'error');
  }

  if (historyResult.status === 'fulfilled') {
    state.history = historyResult.value;
    await setItem(STORAGE_KEYS.history, historyResult.value);
  } else {
    if (historyResult.reason?.status === 401) {
      await handleSessionExpired();
      return;
    }

    state.history = await getItem(STORAGE_KEYS.history, []);
    if (state.history.length > 0) {
      setFeedback('Historico offline carregado do armazenamento local.', 'ok');
    }
  }

  renderStatus();
  renderOpportunities();
  renderHistory();
}

async function bootstrap() {
  refs.runNowButton.disabled = true;
  refs.applyFiltersButton.disabled = true;
  refs.saveSettingsButton.disabled = true;

  state.token = await getToken();
  state.filters = await getItem(STORAGE_KEYS.filters, state.filters);
  state.history = await getItem(STORAGE_KEYS.history, []);
  writeFiltersToInputs();

  if (state.token) {
    try {
      state.me = await me(state.token);
      state.scanStatus = 'Pronta';
    } catch {
      await handleSessionExpired();
      return;
    }

    try {
      const backendSettings = await loadSettings(state.token);
      applyBackendSettingsToState(backendSettings);
      writeFiltersToInputs();
    } catch (error) {
      if (error?.status === 401) {
        await handleSessionExpired();
        return;
      }

      setFeedback('Nao foi possivel carregar configuracoes do servidor. Usando filtros locais.', 'error');
    }
  }

  renderAuth();

  try {
    await loadPanelData();
  } catch (error) {
    setFeedback(error.message || 'Falha ao carregar dados.', 'error');
  }
}

refs.loginForm.addEventListener('submit', async (event) => {
  event.preventDefault();
  setFeedback('Entrando...', 'ok');

  try {
    const email = refs.emailInput.value.trim();
    const password = refs.passwordInput.value;

    await login(email, password);
    state.token = await getToken();
    state.me = await me(state.token);
    state.scanStatus = 'Pronta';

    try {
      const backendSettings = await loadSettings(state.token);
      applyBackendSettingsToState(backendSettings);
      writeFiltersToInputs();
    } catch {
      // Keep local filters when backend settings are unavailable.
    }

    renderAuth();

    await loadPanelData();
    setFeedback('Login realizado com sucesso.', 'ok');
    refs.passwordInput.value = '';
  } catch (error) {
    if (error?.status === 401) {
      setFeedback('Credenciais invalidas.', 'error');
      return;
    }

    setFeedback(error.message || 'Falha no login.', 'error');
  }
});

refs.logoutButton.addEventListener('click', async () => {
  await logout();
  state.token = null;
  state.me = null;
  state.opportunities = [];
  state.history = await getItem(STORAGE_KEYS.history, []);
  state.scanStatus = 'Sem login';

  renderAuth();
  renderStatus();
  renderOpportunities();
  renderHistory();
  setFeedback('Sessao encerrada.', 'ok');
});

refs.applyFiltersButton.addEventListener('click', async () => {
  if (!state.token) {
    setFeedback('Realize login para aplicar filtros.', 'error');
    return;
  }

  readFiltersFromInputs();
  writeFiltersToInputs();
  await setItem(STORAGE_KEYS.filters, state.filters);

  try {
    await loadPanelData();
    setFeedback('Filtros aplicados.', 'ok');
  } catch (error) {
    setFeedback(error.message || 'Falha ao aplicar filtros.', 'error');
  }
});

refs.saveSettingsButton.addEventListener('click', async () => {
  if (!state.token) {
    setFeedback('Realize login para configurar.', 'error');
    return;
  }

  readFiltersFromInputs();
  writeFiltersToInputs();
  await setItem(STORAGE_KEYS.filters, state.filters);

  try {
    const saved = await saveSettings(state.token, buildFiltersPayload());
    applyBackendSettingsToState(saved);
    writeFiltersToInputs();
    setFeedback('Configuracoes salvas no servidor.', 'ok');
  } catch (error) {
    if (error?.status === 401) {
      await handleSessionExpired();
      return;
    }

    setFeedback(error.message || 'Falha ao salvar configuracoes.', 'error');
  }
});

refs.runNowButton.addEventListener('click', async () => {
  if (!state.token) {
    setFeedback('Realize login para rodar a varredura.', 'error');
    return;
  }

  refs.runNowButton.disabled = true;
  state.scanStatus = 'Executando';
  renderStatus();
  setFeedback('Rodando varredura...', 'ok');

  try {
    const result = await runScanner(state.token);
    state.scanStatus = result.success
      ? `Concluida ${new Date(result.completedAtUtc).toLocaleTimeString('pt-BR')}`
      : 'Falhou';

    await loadPanelData();
    setFeedback(result.message || `Varredura concluida: ${result.refreshedLots} lotes.`, 'ok');
  } catch (error) {
    if (error?.status === 401) {
      await handleSessionExpired();
      return;
    }

    state.scanStatus = 'Falhou';
    renderStatus();
    setFeedback(error.message || 'Falha na varredura.', 'error');
  } finally {
    refs.runNowButton.disabled = !state.token;
  }
});

bootstrap();
