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
  authBlock: document.getElementById('auth-block'),
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
    return parsed.hostname.includes('.') && parsed.pathname.length > 1;
  } catch {
    return false;
  }
}

function setFeedback(message, type = 'ok') {
  refs.feedback.textContent = message || '';
  refs.feedback.classList.remove('ok', 'error');
  if (message) {
    refs.feedback.classList.add(type);
  }
}

function scoreLabel(score) {
  if (score >= 85) {
    return 'OPORTUNIDADE';
  }
  if (score >= 65) {
    return 'BOM_PRECO';
  }
  return 'ACIMA_DA_MEDIA';
}

function riskTone(riskDecision) {
  const normalized = (riskDecision || '').toUpperCase();
  if (normalized.includes('ALTO')) {
    return 'ALTO_RISCO';
  }
  if (normalized.includes('RISCO')) {
    return 'OPORTUNIDADE_COM_RISCO';
  }
  return 'COMPRA_SEGURA';
}

function renderAuth() {
  const authenticated = !!state.token;
  refs.loginForm.classList.toggle('hidden', authenticated);
  refs.sessionView.classList.toggle('hidden', !authenticated);
  refs.runNowButton.disabled = !authenticated;

  if (authenticated && state.me) {
    refs.sessionEmail.textContent = `${state.me.email} | Plano ${state.me.plan}`;
  } else {
    refs.sessionEmail.textContent = '';
  }
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
    { label: 'Varredura', value: state.token ? 'Pronta' : 'Sem login' }
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

    const card = document.createElement('article');
    card.className = 'item-card';

    const scoreValue = Number(item.score || 0);
    const label = item.scoreLabel || scoreLabel(scoreValue);
    const risk = item.riskDecision || riskTone(item.riskDecision);

    card.innerHTML = `
      <h3>${item.title}</h3>
      <p><strong>Fonte:</strong> ${item.source}</p>
      <p><strong>Score:</strong> ${label} (${scoreValue.toFixed(1)})</p>
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
    `;

    refs.historyList.appendChild(card);
  });
}

function readFiltersFromInputs() {
  state.filters = {
    search: refs.searchInput.value.trim(),
    source: refs.sourceInput.value.trim(),
    minScore: Number(refs.scoreInput.value || 60),
    vehicleType: refs.typeInput.value ? Number(refs.typeInput.value) : null,
    region: refs.regionInput.value.trim().toUpperCase()
  };
}

function writeFiltersToInputs() {
  refs.searchInput.value = state.filters.search || '';
  refs.sourceInput.value = state.filters.source || '';
  refs.scoreInput.value = String(state.filters.minScore ?? 60);
  refs.typeInput.value = state.filters.vehicleType ? String(state.filters.vehicleType) : '';
  refs.regionInput.value = state.filters.region || '';
}

async function loadPanelData() {
  if (!state.token) {
    state.opportunities = [];
    state.history = [];
    renderStatus();
    renderOpportunities();
    renderHistory();
    return;
  }

  const [opportunities, history] = await Promise.all([
    loadOpportunities(state.token, state.filters),
    loadHistory(state.token, 8)
  ]);

  state.opportunities = opportunities.filter((item) => isValidLotUrl(item.lotUrl));
  state.history = history;

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
  writeFiltersToInputs();

  if (state.token) {
    try {
      state.me = await me(state.token);
      const backendSettings = await loadSettings(state.token);
      state.filters = {
        search: backendSettings.search || state.filters.search,
        source: backendSettings.source || state.filters.source,
        minScore: backendSettings.minScore ?? state.filters.minScore,
        vehicleType: backendSettings.vehicleType ?? state.filters.vehicleType,
        region: backendSettings.region || state.filters.region
      };
      writeFiltersToInputs();
    } catch {
      state.token = null;
      state.me = null;
      await logout();
      setFeedback('Sessao expirada. Faça login novamente.', 'error');
    }
  }

  renderAuth();
  refs.runNowButton.disabled = !state.token;
  refs.applyFiltersButton.disabled = !state.token;
  refs.saveSettingsButton.disabled = !state.token;

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

    renderAuth();
    refs.applyFiltersButton.disabled = false;
    refs.saveSettingsButton.disabled = false;
    refs.runNowButton.disabled = false;

    await loadPanelData();
    setFeedback('Login realizado com sucesso.', 'ok');
    refs.passwordInput.value = '';
  } catch (error) {
    setFeedback(error.message || 'Falha no login.', 'error');
  }
});

refs.logoutButton.addEventListener('click', async () => {
  await logout();
  state.token = null;
  state.me = null;
  state.opportunities = [];
  state.history = [];

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
  await setItem(STORAGE_KEYS.filters, state.filters);

  try {
    await saveSettings(state.token, {
      search: state.filters.search,
      source: state.filters.source,
      minScore: Number(state.filters.minScore || 0),
      vehicleType: state.filters.vehicleType,
      region: state.filters.region || null,
      advancedFiltersEnabled: true
    });

    setFeedback('Configuracoes salvas.', 'ok');
  } catch (error) {
    setFeedback(error.message || 'Falha ao salvar configuracoes.', 'error');
  }
});

refs.runNowButton.addEventListener('click', async () => {
  if (!state.token) {
    setFeedback('Realize login para rodar a varredura.', 'error');
    return;
  }

  refs.runNowButton.disabled = true;
  setFeedback('Rodando varredura...', 'ok');

  try {
    const result = await runScanner(state.token);
    await loadPanelData();
    setFeedback(result.message || `Varredura concluida: ${result.refreshedLots} lotes.`, 'ok');
  } catch (error) {
    setFeedback(error.message || 'Falha na varredura.', 'error');
  } finally {
    refs.runNowButton.disabled = false;
  }
});

bootstrap();

