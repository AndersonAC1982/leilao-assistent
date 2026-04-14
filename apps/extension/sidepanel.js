import { getToken, login, logout, me } from "./services/auth.js";
import { loadHistory } from "./services/history.js";
import { loadOpportunities } from "./services/opportunities.js";
import { runScanner } from "./services/scanner.js";
import { loadSettings, saveSettings } from "./services/settings.js";
import { getItem, setItem, STORAGE_KEYS } from "./services/storage.js";
import {
  discoverApiBaseUrl,
  getConfiguredApiBaseUrl,
  setConfiguredApiBaseUrl,
  testApiConnection
} from "./services/api.js";

const API_HINT = "Servidor indisponível no momento. Se necessário, use \"Ajustes de conexão\".";

const SOURCE_OPTIONS = [
  "Superbid",
  "Sodré Santoro",
  "VIP Leilões",
  "Freitas",
  "Zukerman",
  "Mega",
  "Pacto",
  "Milan"
];

const SOURCE_API_NAMES = {
  superbid: "Superbid",
  sodresantoro: "Sodre Santoro",
  vipleiloes: "VIP Leiloes",
  freitas: "Freitas",
  zukerman: "Zukerman",
  mega: "Mega Leiloes",
  pacto: "Pacto Leiloes",
  milan: "Milan Leiloes"
};

const SOURCE_CAPABILITIES = {
  "Superbid": { real: true, vehicles: true, realEstate: false },
  "Sodré Santoro": { real: true, vehicles: true, realEstate: false },
  "VIP Leilões": { real: true, vehicles: true, realEstate: false },
  "Mega": { real: true, vehicles: true, realEstate: false, advancedPlanOnly: true },
  "Freitas": { real: false, vehicles: true, realEstate: false },
  "Zukerman": { real: false, vehicles: false, realEstate: true },
  "Pacto": { real: false, vehicles: true, realEstate: false },
  "Milan": { real: false, vehicles: true, realEstate: false }
};

const CATEGORIES = [
  { key: "all", label: "Todos", searchHint: "", vehicleType: null },
  { key: "vehicles", label: "Veículos", searchHint: "", vehicleType: null },
  { key: "real_estate", label: "Imóveis", searchHint: "imovel", vehicleType: null },
  { key: "machines", label: "Máquinas e Equipamentos", searchHint: "maquina equipamento", vehicleType: null },
  { key: "materials", label: "Materiais / Estoque", searchHint: "material estoque", vehicleType: null },
  { key: "scrap", label: "Sucatas", searchHint: "sucata recuperavel", vehicleType: null },
  { key: "judicial", label: "Judicial", searchHint: "judicial", vehicleType: null },
  { key: "extrajudicial", label: "Extrajudicial", searchHint: "extrajudicial", vehicleType: null },
  { key: "misc", label: "Diversos", searchHint: "", vehicleType: null }
];

const DEFAULT_FILTERS = {
  categoryKey: "all",
  activeSources: [...SOURCE_OPTIONS],
  search: "",
  minScore: 20,
  region: "",
  maxPrice: null
};

const state = {
  token: null,
  me: null,
  tabContext: null,
  opportunities: [],
  history: [],
  scanStatus: "Aguardando login",
  filters: { ...DEFAULT_FILTERS },
  ui: {
    advancedMode: false,
    advancedCategoryManualOverride: false,
    lastExecutionCategoryKey: "all",
    lastExecutionRealSources: 0
  },
  api: {
    baseUrl: "",
    online: false
  }
};

const refs = {
  loginForm: document.getElementById("login-form"),
  emailInput: document.getElementById("email-input"),
  passwordInput: document.getElementById("password-input"),
  apiAdvancedBox: document.getElementById("api-advanced-box"),
  apiBaseInput: document.getElementById("api-base-input"),
  testApiButton: document.getElementById("test-api-button"),
  detectApiButton: document.getElementById("detect-api-button"),
  apiStatus: document.getElementById("api-status"),
  sessionView: document.getElementById("session-view"),
  sessionEmail: document.getElementById("session-email"),
  logoutButton: document.getElementById("logout-button"),

  runNowButton: document.getElementById("run-now-button"),
  toggleAdvancedButton: document.getElementById("toggle-advanced-button"),
  advancedFiltersPanel: document.getElementById("advanced-filters-panel"),
  coverageHint: document.getElementById("coverage-hint"),
  coverageWarning: document.getElementById("coverage-warning"),
  coverageWarningText: document.getElementById("coverage-warning-text"),
  coverageFixButton: document.getElementById("coverage-fix-button"),

  categoryInput: document.getElementById("category-input"),
  advancedCategoryInput: document.getElementById("advanced-category-input"),
  regionScopeInput: document.getElementById("region-scope-input"),
  regionAdvancedInput: document.getElementById("region-advanced-input"),
  sourcesList: document.getElementById("sources-list"),
  searchInput: document.getElementById("search-input"),
  scoreInput: document.getElementById("score-input"),
  maxPriceInput: document.getElementById("max-price-input"),
  applyFiltersButton: document.getElementById("apply-filters-button"),
  saveSettingsButton: document.getElementById("save-settings-button"),

  tabContext: document.getElementById("tab-context"),
  useDomainButton: document.getElementById("use-domain-button"),

  opportunitiesCount: document.getElementById("opportunities-count"),
  scanStatusText: document.getElementById("scan-status-text"),
  statusGrid: document.getElementById("status-grid"),
  opportunitiesList: document.getElementById("opportunities-list"),

  historyList: document.getElementById("history-list"),
  feedback: document.getElementById("feedback")
};

function escapeHtml(value) {
  return String(value ?? "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/\"/g, "&quot;")
    .replace(/'/g, "&#39;");
}

function normalizeText(value) {
  return String(value || "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .trim();
}

function normalizeKey(value) {
  return normalizeText(value).replace(/[^a-z0-9]/g, "");
}

function normalizeDomain(domain) {
  return String(domain || "").replace(/^www\./i, "").trim().toLowerCase();
}

function clampScore(value) {
  const parsed = Number(value);
  if (!Number.isFinite(parsed)) {
    return 20;
  }

  return Math.max(0, Math.min(100, parsed));
}

function normalizeMaxPrice(value) {
  if (value === null || value === undefined || value === "") {
    return null;
  }

  const parsed = Number(value);
  if (!Number.isFinite(parsed) || parsed <= 0) {
    return null;
  }

  return parsed;
}

function mapSourceLabel(rawSource) {
  const rawKey = normalizeKey(rawSource);
  if (!rawKey) {
    return null;
  }

  return SOURCE_OPTIONS.find((source) => {
    const sourceKey = normalizeKey(source);
    return sourceKey === rawKey || sourceKey.includes(rawKey) || rawKey.includes(sourceKey);
  }) ?? null;
}

function normalizeSources(sourceList) {
  const sources = Array.isArray(sourceList) ? sourceList : [];
  const normalized = [];

  for (const source of sources) {
    const mapped = mapSourceLabel(source);
    if (!mapped || normalized.includes(mapped)) {
      continue;
    }

    normalized.push(mapped);
  }

  return normalized;
}

function toApiSourceName(sourceLabel) {
  const key = normalizeKey(sourceLabel);
  if (!key) {
    return "";
  }

  const mappedKey = Object.keys(SOURCE_API_NAMES).find((candidate) =>
    key.includes(candidate) || candidate.includes(key)
  );

  return mappedKey ? SOURCE_API_NAMES[mappedKey] : sourceLabel;
}

function getCategoryByKey(categoryKey) {
  return CATEGORIES.find((category) => category.key === categoryKey) ?? CATEGORIES[0];
}

function resolveCategoryKey(categoryValue, vehicleType) {
  if (typeof categoryValue === "string" && categoryValue.trim()) {
    const trimmed = categoryValue.trim();
    const direct = CATEGORIES.find((category) => category.key === trimmed);
    if (direct) {
      return direct.key;
    }

    const normalizedCategory = normalizeKey(trimmed);
    const byLabel = CATEGORIES.find((category) => normalizeKey(category.label) === normalizedCategory);
    if (byLabel) {
      return byLabel.key;
    }
  }

  if (vehicleType !== null && vehicleType !== undefined) {
    return "vehicles";
  }

  return "all";
}

function toSimpleCategory(categoryKey) {
  if (categoryKey === "vehicles") {
    return "vehicles";
  }

  return "all";
}

function selectedCategoryKeyFromForm() {
  // Regra única de prioridade:
  // - modo simples sempre manda por padrão;
  // - modo avançado só sobrescreve após edição manual explícita no campo detalhado.
  const simpleCategory = toSimpleCategory(refs.categoryInput.value);
  if (!state.ui.advancedMode || !state.ui.advancedCategoryManualOverride) {
    return simpleCategory;
  }

  return getCategoryByKey(refs.advancedCategoryInput.value).key;
}

function syncAdvancedCategoryWithSimple() {
  const simpleCategory = toSimpleCategory(refs.categoryInput.value);
  refs.advancedCategoryInput.value = simpleCategory;
  state.ui.advancedCategoryManualOverride = false;
}

function allowsAdvancedSourcesForCurrentPlan() {
  return Number(state.me?.plan || 0) === 4;
}

function connectorSupportsCategory(sourceName, categoryKey) {
  const capability = SOURCE_CAPABILITIES[sourceName];
  if (!capability) {
    return categoryKey === "all";
  }

  if (capability.advancedPlanOnly && !allowsAdvancedSourcesForCurrentPlan()) {
    return false;
  }

  if (categoryKey === "all") {
    return true;
  }

  if (categoryKey === "vehicles") {
    return capability.vehicles;
  }

  if (categoryKey === "real_estate") {
    return capability.realEstate;
  }

  return false;
}

function getCategoryCoverage(categoryKey) {
  const category = getCategoryByKey(categoryKey);
  const activeSources = state.filters.activeSources;

  const supportedSources = activeSources.filter((source) => connectorSupportsCategory(source, category.key));
  const supportedRealSources = supportedSources.filter((source) => SOURCE_CAPABILITIES[source]?.real);

  return {
    category,
    activeSources,
    supportedSources,
    supportedRealSources
  };
}

function getBestCoveredCategoryKey() {
  const candidates = ["vehicles", "all"];
  const byCoverage = candidates
    .map((candidate) => ({
      candidate,
      count: getCategoryCoverage(candidate).supportedRealSources.length
    }))
    .sort((left, right) => right.count - left.count);

  return byCoverage[0]?.candidate || "all";
}

function renderCoverageHint() {
  if (!refs.coverageHint) {
    return;
  }

  const coverage = getCategoryCoverage(state.filters.categoryKey);
  const header = `Sua busca atual está usando a categoria "${coverage.category.label}".`;
  const base = `${coverage.supportedRealSources.length} fonte(s) reais compatíveis.`;

  if (coverage.supportedRealSources.length > 0) {
    refs.coverageHint.textContent = `${header} ${base} ${coverage.supportedRealSources.join(", ")}.`;
    refs.coverageWarning?.classList.add("hidden");
    if (refs.coverageFixButton) {
      refs.coverageFixButton.disabled = false;
    }
    return;
  }

  refs.coverageHint.textContent = `${header} ${base}`;
  if (!refs.coverageWarning || !refs.coverageWarningText) {
    return;
  }

  const fallbackCategory = getCategoryByKey(getBestCoveredCategoryKey()).label;
  refs.coverageWarningText.textContent = `${coverage.category.label} ainda não possui cobertura real suficiente nas fontes ativas. Tente ${fallbackCategory} ou Todos.`;
  refs.coverageWarning.classList.remove("hidden");
  if (refs.coverageFixButton) {
    refs.coverageFixButton.textContent = `Usar ${fallbackCategory}`;
    refs.coverageFixButton.disabled = !state.token;
  }
}

function canRunSearchNow() {
  if (!state.token) {
    return false;
  }

  const coverage = getCategoryCoverage(state.filters.categoryKey);
  return coverage.supportedRealSources.length > 0;
}

function updateRunButtonState() {
  if (!refs.runNowButton) {
    return;
  }

  if (!state.token) {
    refs.runNowButton.disabled = true;
    refs.runNowButton.removeAttribute("title");
    return;
  }

  const coverage = getCategoryCoverage(state.filters.categoryKey);
  const blocked = coverage.supportedRealSources.length === 0;
  refs.runNowButton.disabled = blocked;
  if (blocked) {
    refs.runNowButton.title = "Sem cobertura real para a categoria atual nas fontes ativas.";
  } else {
    refs.runNowButton.removeAttribute("title");
  }
}

function mergeSearch(baseSearch, categoryHint) {
  const parts = [String(baseSearch || "").trim(), String(categoryHint || "").trim()].filter(Boolean);
  if (parts.length === 0) {
    return "";
  }

  return Array.from(new Set(parts)).join(" ");
}

function parseBackendDate(value) {
  const parsed = new Date(value);
  if (Number.isNaN(parsed.getTime())) {
    return "-";
  }

  return parsed.toLocaleString("pt-BR");
}

function buildMapUrl(item) {
  const location = String(item.location || "").trim();
  if (!location || location === "-") {
    return null;
  }

  const query = `${item.title || ""} ${location}`.trim();
  if (!query) {
    return null;
  }

  return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(query)}`;
}

function isValidLotUrl(url) {
  if (!url || typeof url !== "string") {
    return false;
  }

  try {
    const parsed = new URL(url.trim());
    const host = parsed.hostname.toLowerCase();
    const path = parsed.pathname.toLowerCase();
    if (!/^https?:$/i.test(parsed.protocol)) {
      return false;
    }

    if (!parsed.hostname.includes(".")) {
      return false;
    }

    if (path.trim().length <= 1) {
      return false;
    }

    if (host.includes("superbid")) {
      return path.includes("/oferta/") && path !== "/oferta";
    }

    if (host.includes("sodresantoro")) {
      return /\/leilao\/\d+\/lote\/\d+/.test(path);
    }

    if (host.includes("vipleiloes")) {
      return path.includes("/evento/anuncio/");
    }

    if (host.includes("megaleiloes")) {
      const parts = path.split("/").filter(Boolean);
      return parts.length >= 5 && parts[parts.length - 1].includes("-");
    }

    return true;
  } catch {
    return false;
  }
}

function lotStatusLabel(status) {
  switch (Number(status)) {
    case 1:
      return "ATIVO";
    case 2:
      return "ENCERRADO";
    case 3:
      return "CONFIRMADO";
    default:
      return "RASCUNHO";
  }
}

function planLabel(plan) {
  switch (Number(plan)) {
    case 1:
      return "Free";
    case 2:
      return "Pro";
    case 3:
      return "Premium";
    case 4:
      return "Elite";
    default:
      return "N/A";
  }
}

function scoreLabel(item) {
  const score = Number(item.score || 0);
  const raw = String(item.scoreLabel || "").trim().toUpperCase();
  if (raw) {
    return `${raw.replaceAll("_", " ")} (${score.toFixed(1)})`;
  }

  if (score >= 85) {
    return `OPORTUNIDADE (${score.toFixed(1)})`;
  }

  if (score >= 65) {
    return `BOM PREÇO (${score.toFixed(1)})`;
  }

  return `ACIMA DA MÉDIA (${score.toFixed(1)})`;
}

function inferOpportunityCategory(item) {
  const selectedCategory = getCategoryByKey(state.filters.categoryKey);
  if (selectedCategory.key !== "all") {
    return selectedCategory.label;
  }

  const content = normalizeText(`${item.title || ""} ${item.summary || ""} ${item.source || ""}`);

  if (content.includes("extrajudicial")) {
    return "Extrajudicial";
  }

  if (content.includes("judicial")) {
    return "Judicial";
  }

  if (content.includes("imovel") || content.includes("apartamento") || content.includes("casa") || content.includes("terreno")) {
    return "Imóveis";
  }

  if (content.includes("maquina") || content.includes("equipamento") || content.includes("trator") || content.includes("escavadeira")) {
    return "Máquinas e Equipamentos";
  }

  if (content.includes("material") || content.includes("estoque")) {
    return "Materiais / Estoque";
  }

  if (content.includes("sucata") || content.includes("recuperavel") || content.includes("recuperável")) {
    return "Sucatas";
  }

  if (content.includes("veiculo") || content.includes("veículo") || content.includes("carro") || content.includes("moto") || content.includes("caminhao") || content.includes("caminhão")) {
    return "Veículos";
  }

  return "Diversos";
}

function matchesSelectedSource(opportunitySource) {
  if (!state.filters.activeSources.length) {
    return false;
  }

  const sourceKey = normalizeKey(opportunitySource);
  if (!sourceKey) {
    return false;
  }

  return state.filters.activeSources.some((selectedSource) => {
    const selectedKey = normalizeKey(selectedSource);
    return sourceKey.includes(selectedKey) || selectedKey.includes(sourceKey);
  });
}

function applyLocalOpportunityFilters(opportunities) {
  return opportunities
    .filter((item) => isValidLotUrl(item.lotUrl))
    .filter((item) => matchesSelectedSource(item.source))
    .filter((item) => {
      if (state.filters.maxPrice === null) {
        return true;
      }

      const value = Number(item.value || 0);
      return value > 0 && value <= state.filters.maxPrice;
    });
}

function friendlyError(error) {
  if (!error) {
    return "Falha inesperada.";
  }

  if (error?.code === "API_UNREACHABLE") {
    return API_HINT;
  }

  const message = String(error.message || "").trim();
  if (message === "Failed to fetch" || message.includes("NetworkError")) {
    return API_HINT;
  }

  return message || "Falha inesperada.";
}

function setFeedback(message, type = "ok") {
  refs.feedback.textContent = message || "";
  refs.feedback.classList.remove("ok", "error");
  if (message) {
    refs.feedback.classList.add(type);
  }
}

function setApiStatus(message, type = "ok") {
  if (!refs.apiStatus) {
    return;
  }

  refs.apiStatus.textContent = message || "";
  refs.apiStatus.classList.remove("ok", "error");
  refs.apiStatus.classList.add(type);
}

function showAdvancedConnectionBox() {
  if (refs.apiAdvancedBox && !refs.apiAdvancedBox.open) {
    refs.apiAdvancedBox.open = true;
  }
}

function renderAdvancedMode() {
  const expanded = !!state.ui.advancedMode;
  refs.toggleAdvancedButton.setAttribute("aria-expanded", expanded ? "true" : "false");
  refs.toggleAdvancedButton.textContent = expanded ? "Ocultar modo avançado" : "Modo avançado";
  refs.advancedFiltersPanel.classList.toggle("hidden", !expanded);
}

function setButtonBusy(button, busyText, isBusy) {
  if (!button) {
    return;
  }

  if (!button.dataset.idleText) {
    button.dataset.idleText = button.textContent || "";
  }

  if (isBusy) {
    button.textContent = busyText;
    button.disabled = true;
    return;
  }

  button.textContent = button.dataset.idleText;
}

async function withButtonBusy(button, busyText, handler) {
  setButtonBusy(button, busyText, true);
  try {
    await handler();
  } finally {
    setButtonBusy(button, busyText, false);
    renderAuth();
  }
}

function normalizeApiInput(value) {
  return String(value || "").trim();
}

async function initializeApiEndpoint() {
  if (!refs.apiBaseInput) {
    return;
  }

  const configured = await getConfiguredApiBaseUrl();
  if (configured) {
    refs.apiBaseInput.value = configured;
  }

  const discovery = await discoverApiBaseUrl(false);
  if (discovery.apiBaseUrl) {
    refs.apiBaseInput.value = discovery.apiBaseUrl;
    state.api.baseUrl = discovery.apiBaseUrl;
  }

  state.api.online = discovery.ok;

  if (discovery.ok) {
    setApiStatus("Conexão com servidor: online.", "ok");
  } else {
    setApiStatus("Conexão com servidor: offline.", "error");
    state.scanStatus = "API offline";
  }
}

async function ensureApiOnline(forceAutoDetect = true) {
  if (!refs.apiBaseInput) {
    return true;
  }

  const typedInput = normalizeApiInput(refs.apiBaseInput.value);

  if (typedInput) {
    const test = await testApiConnection(typedInput);
    if (test.ok) {
      const saved = await setConfiguredApiBaseUrl(test.apiBaseUrl);
      refs.apiBaseInput.value = saved || typedInput;
      state.api.baseUrl = saved || typedInput;
      state.api.online = true;
      setApiStatus("Conexão com servidor: online.", "ok");
      return true;
    }
  }

  if (!forceAutoDetect) {
    state.api.online = false;
    setApiStatus("Conexão com servidor: offline.", "error");
    showAdvancedConnectionBox();
    return false;
  }

  const auto = await discoverApiBaseUrl(true);
  if (auto.ok && auto.apiBaseUrl) {
    refs.apiBaseInput.value = auto.apiBaseUrl;
    state.api.baseUrl = auto.apiBaseUrl;
    state.api.online = true;
    setApiStatus("Conexão com servidor: online.", "ok");
    return true;
  }

  state.api.baseUrl = auto.apiBaseUrl || typedInput;
  state.api.online = false;
  setApiStatus("Conexão com servidor: offline.", "error");
  showAdvancedConnectionBox();
  return false;
}

function renderSources() {
  const selected = new Set(state.filters.activeSources);

  refs.sourcesList.innerHTML = SOURCE_OPTIONS.map((source, index) => {
    const id = `source-${index}`;
    const checked = selected.has(source) ? "checked" : "";
    return `
      <div class="source-item">
        <input id="${escapeHtml(id)}" type="checkbox" data-source="${escapeHtml(source)}" ${checked} />
        <label for="${escapeHtml(id)}">${escapeHtml(source)}</label>
      </div>
    `;
  }).join("");

  refs.sourcesList.querySelectorAll('input[type="checkbox"][data-source]').forEach((checkbox) => {
    checkbox.addEventListener("change", async () => {
      readFiltersFromForm();
      renderStatus();
      await setItem(STORAGE_KEYS.filters, state.filters);
    });
  });
}

function renderFilters() {
  const simpleCategory = toSimpleCategory(state.filters.categoryKey);
  refs.categoryInput.value = simpleCategory;

  if (!state.ui.advancedCategoryManualOverride) {
    refs.advancedCategoryInput.value = simpleCategory;
    state.filters.categoryKey = simpleCategory;
  } else {
    refs.advancedCategoryInput.value = state.filters.categoryKey;
  }

  refs.searchInput.value = state.filters.search;
  refs.scoreInput.value = String(state.filters.minScore);
  refs.regionScopeInput.value = state.filters.region === "SP" ? "SP" : "BR";
  refs.regionAdvancedInput.value = state.filters.region && state.filters.region !== "SP"
    ? state.filters.region
    : "";
  refs.maxPriceInput.value = state.filters.maxPrice === null ? "" : String(state.filters.maxPrice);
  renderSources();
  renderAdvancedMode();
  updateRunButtonState();
}

function readFiltersFromForm() {
  const selectedSources = Array.from(
    refs.sourcesList.querySelectorAll('input[type="checkbox"][data-source]:checked')
  ).map((checkbox) => checkbox.getAttribute("data-source") || "");

  const advancedRegion = refs.regionAdvancedInput.value.trim().toUpperCase().slice(0, 10);
  const regionByScope = refs.regionScopeInput.value === "SP" ? "SP" : "";

  const normalizedSources = normalizeSources(selectedSources);

  state.filters = {
    categoryKey: selectedCategoryKeyFromForm(),
    activeSources: normalizedSources.length > 0
      ? normalizedSources
      : (state.ui.advancedMode ? [] : [...SOURCE_OPTIONS]),
    search: refs.searchInput.value.trim(),
    minScore: clampScore(refs.scoreInput.value),
    region: advancedRegion || regionByScope,
    maxPrice: normalizeMaxPrice(refs.maxPriceInput.value)
  };
}

function buildOpportunityQuery() {
  const category = getCategoryByKey(state.filters.categoryKey);
  return {
    search: mergeSearch(state.filters.search, category.searchHint),
    source: state.filters.activeSources.length === 1 ? toApiSourceName(state.filters.activeSources[0]) : "",
    minScore: state.filters.minScore,
    vehicleType: category.vehicleType,
    region: state.filters.region || ""
  };
}

function settingsPayload() {
  const category = getCategoryByKey(state.filters.categoryKey);
  const query = buildOpportunityQuery();

  return {
    search: state.filters.search,
    source: state.filters.activeSources.length === 1 ? toApiSourceName(state.filters.activeSources[0]) : "",
    minScore: query.minScore,
    vehicleType: query.vehicleType,
    region: state.filters.region || null,
    advancedFiltersEnabled: state.ui.advancedMode,
    category: category.label,
    activeSources: state.filters.activeSources,
    maxPrice: state.filters.maxPrice
  };
}

function scannerPayload() {
  const category = getCategoryByKey(state.filters.categoryKey);
  const query = buildOpportunityQuery();

  return {
    category: category.label,
    activeSources: [...state.filters.activeSources],
    search: state.filters.search,
    minScore: state.filters.minScore,
    region: state.filters.region || null,
    maxPrice: state.filters.maxPrice,
    vehicleType: query.vehicleType
  };
}

function mergeSettings(settings, hadStoredSources) {
  if (!settings || typeof settings !== "object") {
    return;
  }

  const categoryKey = resolveCategoryKey(settings.category, settings.vehicleType);

  const activeSourcesFromSettings = normalizeSources(
    Array.isArray(settings.activeSources)
      ? settings.activeSources
      : []
  );

  const fallbackSingleSource = typeof settings.source === "string" ? normalizeSources([settings.source]) : [];
  const activeSources = activeSourcesFromSettings.length > 0
    ? activeSourcesFromSettings
    : (!hadStoredSources ? fallbackSingleSource : state.filters.activeSources);

  state.filters = {
    categoryKey,
    activeSources: activeSources.length > 0 ? activeSources : [...SOURCE_OPTIONS],
    search: typeof settings.search === "string" ? settings.search.trim() : state.filters.search,
    minScore: clampScore(settings.minScore ?? state.filters.minScore),
    region: typeof settings.region === "string" && settings.region.trim() ? settings.region.trim().toUpperCase() : state.filters.region,
    maxPrice: normalizeMaxPrice(settings.maxPrice)
  };
}

function renderAuth() {
  const authenticated = !!state.token;
  refs.loginForm.classList.toggle("hidden", authenticated);
  refs.sessionView.classList.toggle("hidden", !authenticated);

  refs.toggleAdvancedButton.disabled = !authenticated;
  refs.categoryInput.disabled = !authenticated;
  refs.advancedCategoryInput.disabled = !authenticated;
  refs.regionScopeInput.disabled = !authenticated;
  refs.regionAdvancedInput.disabled = !authenticated;
  refs.searchInput.disabled = !authenticated;
  refs.scoreInput.disabled = !authenticated;
  refs.maxPriceInput.disabled = !authenticated;
  refs.applyFiltersButton.disabled = !authenticated;
  refs.saveSettingsButton.disabled = !authenticated;
  refs.useDomainButton.disabled = !authenticated || !state.tabContext?.domain;

  refs.sourcesList.querySelectorAll('input[type="checkbox"]').forEach((checkbox) => {
    checkbox.disabled = !authenticated;
  });

  refs.sessionEmail.textContent = authenticated && state.me
    ? `${state.me.email} | Plano ${planLabel(state.me.plan)}`
    : "";

  updateRunButtonState();
}

function findSourceByDomain(domain) {
  const normalized = normalizeDomain(domain);
  if (!normalized) {
    return null;
  }

  const table = [
    { contains: "superbid", source: "Superbid" },
    { contains: "sodresantoro", source: "Sodré Santoro" },
    { contains: "vipleiloes", source: "VIP Leilões" },
    { contains: "freitas", source: "Freitas" },
    { contains: "zuk", source: "Zukerman" },
    { contains: "mega", source: "Mega" },
    { contains: "pacto", source: "Pacto" },
    { contains: "milan", source: "Milan" }
  ];

  const match = table.find((item) => normalized.includes(item.contains));
  return match ? match.source : null;
}

function renderTabContext() {
  if (!state.tabContext?.domain) {
    refs.tabContext.textContent = "Sem contexto da aba.";
    refs.useDomainButton.disabled = true;
    return;
  }

  const domain = normalizeDomain(state.tabContext.domain);
  const suggestedSource = findSourceByDomain(domain);
  refs.tabContext.textContent = suggestedSource
    ? `Domínio atual: ${domain} | Fonte sugerida: ${suggestedSource}`
    : `Domínio atual: ${domain} | Sem fonte mapeada`;

  refs.useDomainButton.disabled = !state.token;
}

function renderStatus() {
  const maxScore = state.opportunities.length
    ? Math.max(...state.opportunities.map((item) => Number(item.score || 0)))
    : 0;

  const strongCount = state.opportunities.filter((item) => Number(item.score || 0) >= 75).length;
  const coverage = getCategoryCoverage(state.filters.categoryKey);
  const categoryLabel = coverage.category.label;

  refs.scanStatusText.textContent = state.scanStatus;
  refs.statusGrid.innerHTML = [
    ["Categoria usada", categoryLabel],
    ["Fontes reais compatíveis", String(coverage.supportedRealSources.length)],
    ["Lotes encontrados", String(state.opportunities.length)],
    ["Maior score", maxScore.toFixed(1)],
    ["Oportunidades fortes", String(strongCount)]
  ]
    .map(([label, value]) => `<div class="status-chip"><span>${escapeHtml(label)}</span><strong>${escapeHtml(value)}</strong></div>`)
    .join("");

  renderCoverageHint();
  updateRunButtonState();
}

function buildEmptyStateMessage() {
  const coverage = getCategoryCoverage(state.filters.categoryKey);
  const hints = [
    "Nenhuma oportunidade encontrada para esta combinação de filtros.",
    `Sua busca atual está usando a categoria "${coverage.category.label}".`
  ];

  if (state.filters.categoryKey !== "all") {
    hints.push("Tente buscar em \"Todos\" para ampliar a varredura.");
  }

  if (state.filters.search) {
    hints.push("Tente remover ou simplificar o termo em \"O que você procura?\".");
  }

  if (state.filters.maxPrice !== null) {
    hints.push("Aumente ou remova a faixa de preço máxima.");
  }

  if (coverage.supportedRealSources.length === 0) {
    hints.push(`${coverage.category.label} ainda não possui cobertura real suficiente nas fontes ativas.`);
  }

  return `
    <div class="empty-state">
      ${hints.map((hint) => `<p class="muted">${escapeHtml(hint)}</p>`).join("")}
    </div>
  `;
}

function renderOpportunities() {
  refs.opportunitiesCount.textContent = String(state.opportunities.length);

  if (!state.opportunities.length) {
    refs.opportunitiesList.innerHTML = buildEmptyStateMessage();
    return;
  }

  refs.opportunitiesList.innerHTML = state.opportunities
    .map((item) => {
      const value = Number(item.value || 0);
      const categoryLabel = inferOpportunityCategory(item);
      const mapUrl = buildMapUrl(item);
      return `
        <article class="item">
          <div class="item-head">
            <span class="badge">${escapeHtml(item.source || "Sem fonte")}</span>
            <span class="badge badge-score">${escapeHtml(scoreLabel(item))}</span>
          </div>
          <h3>${escapeHtml(item.title || "Lote sem título")}</h3>
          <div class="item-meta">
            <span>Categoria: ${escapeHtml(categoryLabel)}</span>
            <span>Status: ${escapeHtml(lotStatusLabel(item.status))}</span>
            <span>Local: ${escapeHtml(item.location || "-")}</span>
          </div>
          <p class="item-price">R$ ${value.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</p>
          <p class="muted">${escapeHtml(item.summary || "")}</p>
          <div class="item-actions">
            <button class="btn btn-primary open-lot" type="button" data-url="${escapeHtml(item.lotUrl)}">Abrir lote</button>
            ${mapUrl ? `<button class="btn btn-ghost open-map" type="button" data-map="${escapeHtml(mapUrl)}">Mapa</button>` : ""}
          </div>
        </article>
      `;
    })
    .join("");

  refs.opportunitiesList.querySelectorAll(".open-lot").forEach((button) => {
    button.addEventListener("click", () => {
      const lotUrl = button.getAttribute("data-url") || "";
      if (!isValidLotUrl(lotUrl)) {
        setFeedback("URL inválida. Item bloqueado.", "error");
        return;
      }

      chrome.tabs.create({ url: lotUrl });
    });
  });

  refs.opportunitiesList.querySelectorAll(".open-map").forEach((button) => {
    button.addEventListener("click", () => {
      const mapUrl = button.getAttribute("data-map") || "";
      if (!mapUrl) {
        return;
      }

      chrome.tabs.create({ url: mapUrl });
    });
  });
}

function renderHistory() {
  if (!state.history.length) {
    refs.historyList.innerHTML = '<p class="muted">Sem histórico recente.</p>';
    return;
  }

  refs.historyList.innerHTML = state.history
    .slice(0, 10)
    .map((item) => {
      const status = item.status || (item.success ? "CONCLUÍDO" : "FALHA");
      return `
        <article class="item">
          <div class="item-head">
            <span class="badge">${escapeHtml(item.source || "Execução")}</span>
            <span class="badge">${escapeHtml(status)}</span>
          </div>
          <div class="item-meta">
            <span>${escapeHtml(parseBackendDate(item.executedAtUtc))}</span>
            <span>Novos lotes: ${escapeHtml(String(item.newLots ?? 0))}</span>
          </div>
          <p class="muted">${escapeHtml(item.message || "")}</p>
        </article>
      `;
    })
    .join("");
}

async function fetchTabContext() {
  try {
    const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
    if (!tab) {
      return null;
    }

    if (!tab.id) {
      return {
        href: tab.url || "",
        domain: "",
        title: tab.title || "",
        tabHint: ""
      };
    }

    try {
      const response = await chrome.tabs.sendMessage(tab.id, { type: "MULTILEILAO_TAB_CONTEXT" });
      if (response) {
        return response;
      }
    } catch {
      // Fallback below.
    }

    let domain = "";
    try {
      domain = tab.url ? new URL(tab.url).hostname : "";
    } catch {
      domain = "";
    }

    return {
      href: tab.url || "",
      domain,
      title: tab.title || "",
      tabHint: ""
    };
  } catch {
    return null;
  }
}

async function handleSessionExpired(message = "Sessão expirada. Faça login novamente.") {
  await logout();
  state.token = null;
  state.me = null;
  state.ui.advancedCategoryManualOverride = false;
  state.opportunities = [];
  state.history = await getItem(STORAGE_KEYS.history, []);
  state.scanStatus = "Aguardando login";

  renderAuth();
  renderStatus();
  renderOpportunities();
  renderHistory();
  setFeedback(message, "error");
}

async function refreshData() {
  if (!state.token) {
    state.opportunities = [];
    state.history = await getItem(STORAGE_KEYS.history, []);
    renderStatus();
    renderOpportunities();
    renderHistory();
    return;
  }

  const apiReady = await ensureApiOnline(false);
  if (!apiReady) {
    state.scanStatus = "API offline";
    renderStatus();
    setFeedback(API_HINT, "error");
    return;
  }

  const query = buildOpportunityQuery();

  const [opportunitiesResult, historyResult] = await Promise.allSettled([
    loadOpportunities(state.token, query),
    loadHistory(state.token, 10)
  ]);

  if (opportunitiesResult.status === "fulfilled") {
    state.opportunities = applyLocalOpportunityFilters(opportunitiesResult.value);
  } else {
    if (opportunitiesResult.reason?.status === 401) {
      await handleSessionExpired();
      return;
    }

    state.opportunities = [];
    setFeedback(friendlyError(opportunitiesResult.reason), "error");
  }

  if (historyResult.status === "fulfilled") {
    state.history = historyResult.value;
    await setItem(STORAGE_KEYS.history, historyResult.value);
  } else {
    if (historyResult.reason?.status === 401) {
      await handleSessionExpired();
      return;
    }

    state.history = await getItem(STORAGE_KEYS.history, []);
  }

  renderStatus();
  renderOpportunities();
  renderHistory();
}

async function bootstrap() {
  state.token = await getToken();
  state.ui.advancedCategoryManualOverride = false;

  const rawStoredFilters = await getItem(STORAGE_KEYS.filters, DEFAULT_FILTERS);
  const hadStoredSources = Array.isArray(rawStoredFilters?.activeSources);

  state.filters = {
    categoryKey: resolveCategoryKey(rawStoredFilters?.categoryKey, null),
    activeSources: normalizeSources(rawStoredFilters?.activeSources),
    search: String(rawStoredFilters?.search || "").trim(),
    minScore: clampScore(rawStoredFilters?.minScore),
    region: String(rawStoredFilters?.region || "").trim().toUpperCase().slice(0, 10),
    maxPrice: normalizeMaxPrice(rawStoredFilters?.maxPrice)
  };

  if (!state.filters.activeSources.length) {
    state.filters.activeSources = [...SOURCE_OPTIONS];
  }

  state.history = await getItem(STORAGE_KEYS.history, []);
  state.tabContext = await fetchTabContext();

  renderFilters();
  renderTabContext();
  await initializeApiEndpoint();

  if (state.token) {
    if (state.api.online) {
      try {
        state.me = await me(state.token);
        state.scanStatus = "Pronta";
      } catch (error) {
        if (error?.status === 401) {
          await handleSessionExpired();
          return;
        }

        state.scanStatus = "API offline";
        setFeedback(friendlyError(error), "error");
      }
    }

    if (state.me) {
      try {
        const settings = await loadSettings(state.token);
        mergeSettings(settings, hadStoredSources);
        renderFilters();
        await setItem(STORAGE_KEYS.filters, state.filters);
      } catch (error) {
        if (error?.status === 401) {
          await handleSessionExpired();
          return;
        }
      }
    }
  }

  renderAuth();
  renderStatus();
  renderOpportunities();
  renderHistory();

  try {
    await refreshData();
  } catch (error) {
    setFeedback(friendlyError(error), "error");
  }
}

refs.categoryInput?.addEventListener("change", async () => {
  syncAdvancedCategoryWithSimple();
  readFiltersFromForm();
  renderStatus();
  await setItem(STORAGE_KEYS.filters, state.filters);
});

refs.advancedCategoryInput?.addEventListener("change", async () => {
  const simpleCategory = toSimpleCategory(refs.categoryInput.value);
  state.ui.advancedCategoryManualOverride = state.ui.advancedMode
    && refs.advancedCategoryInput.value !== simpleCategory;

  if (!state.ui.advancedCategoryManualOverride) {
    refs.advancedCategoryInput.value = simpleCategory;
  }

  readFiltersFromForm();
  renderStatus();
  await setItem(STORAGE_KEYS.filters, state.filters);

  if (state.ui.advancedCategoryManualOverride) {
    setFeedback(`Modo avançado ativo: a busca usará "${getCategoryByKey(state.filters.categoryKey).label}".`, "ok");
  }
});

refs.coverageFixButton?.addEventListener("click", async () => {
  const fallbackCategory = getBestCoveredCategoryKey();
  refs.categoryInput.value = toSimpleCategory(fallbackCategory);
  syncAdvancedCategoryWithSimple();
  readFiltersFromForm();
  renderStatus();
  await setItem(STORAGE_KEYS.filters, state.filters);
  setFeedback(`Categoria ajustada para "${getCategoryByKey(state.filters.categoryKey).label}" para melhorar cobertura.`, "ok");
});

refs.toggleAdvancedButton?.addEventListener("click", async () => {
  state.ui.advancedMode = !state.ui.advancedMode;

  if (!state.ui.advancedMode) {
    refs.regionAdvancedInput.value = "";
    syncAdvancedCategoryWithSimple();
  }

  renderAdvancedMode();
  readFiltersFromForm();
  renderStatus();
  await setItem(STORAGE_KEYS.filters, state.filters);
});

refs.testApiButton?.addEventListener("click", async () => {
  await withButtonBusy(refs.testApiButton, "Testando...", async () => {
    const ok = await ensureApiOnline(true);
    if (ok) {
      setFeedback("Conexão com API validada.", "ok");
    } else {
      setFeedback("Servidor indisponível no momento.", "error");
      showAdvancedConnectionBox();
    }
  });
});

refs.detectApiButton?.addEventListener("click", async () => {
  await withButtonBusy(refs.detectApiButton, "Detectando...", async () => {
    const discovered = await discoverApiBaseUrl(true);
    if (discovered.ok && discovered.apiBaseUrl) {
      refs.apiBaseInput.value = discovered.apiBaseUrl;
      state.api.baseUrl = discovered.apiBaseUrl;
      state.api.online = true;
      setApiStatus("Conexão com servidor: online.", "ok");
      setFeedback("Endpoint detectado automaticamente.", "ok");
      return;
    }

    state.api.online = false;
    setApiStatus("Conexão com servidor: offline.", "error");
    setFeedback("Não foi possível detectar a API. Inicie o backend ou informe o endpoint manual.", "error");
    showAdvancedConnectionBox();
  });
});

refs.loginForm.addEventListener("submit", async (event) => {
  event.preventDefault();

  const submitButton = refs.loginForm.querySelector('button[type="submit"]');
  await withButtonBusy(submitButton, "Entrando...", async () => {
    const apiReady = await ensureApiOnline(true);
    if (!apiReady) {
      setFeedback("API offline. Sem conexão não é possível autenticar.", "error");
      return;
    }

    setFeedback("Realizando login...", "ok");

    try {
      await login(refs.emailInput.value.trim(), refs.passwordInput.value);
      state.token = await getToken();
      state.me = await me(state.token);
      state.scanStatus = "Pronta";

      try {
        const settings = await loadSettings(state.token);
        mergeSettings(settings, true);
        renderFilters();
      } catch {
        // Keep local settings if backend is unavailable.
      }

      refs.passwordInput.value = "";
      await setItem(STORAGE_KEYS.filters, state.filters);

      renderAuth();
      await refreshData();
      setFeedback("Login realizado com sucesso.", "ok");
    } catch (error) {
      if (error?.status === 401) {
        setFeedback("Credenciais inválidas.", "error");
        return;
      }

      setFeedback(friendlyError(error), "error");
    }
  });
});

refs.logoutButton.addEventListener("click", async () => {
  await logout();
  state.token = null;
  state.me = null;
  state.ui.advancedCategoryManualOverride = false;
  state.opportunities = [];
  state.history = await getItem(STORAGE_KEYS.history, []);
  state.scanStatus = "Aguardando login";

  renderAuth();
  renderStatus();
  renderOpportunities();
  renderHistory();
  setFeedback("Sessão encerrada.", "ok");
});

refs.useDomainButton.addEventListener("click", async () => {
  await withButtonBusy(refs.useDomainButton, "Aplicando...", async () => {
    if (!state.tabContext?.domain) {
      setFeedback("Domínio atual não identificado.", "error");
      return;
    }

    const matchedSource = findSourceByDomain(state.tabContext.domain);
    if (!matchedSource) {
      setFeedback("Domínio sem fonte mapeada.", "error");
      return;
    }

    state.filters.activeSources = [matchedSource];
    renderFilters();
    renderStatus();
    await setItem(STORAGE_KEYS.filters, state.filters);
    setFeedback(`Fonte ativa definida: ${matchedSource}.`, "ok");
  });
});

refs.applyFiltersButton.addEventListener("click", async () => {
  await withButtonBusy(refs.applyFiltersButton, "Aplicando...", async () => {
    if (!state.token) {
      setFeedback("Faça login para aplicar filtros.", "error");
      return;
    }

    readFiltersFromForm();

    if (!state.filters.activeSources.length) {
      setFeedback("Selecione ao menos uma fonte.", "error");
      return;
    }

    await setItem(STORAGE_KEYS.filters, state.filters);

    try {
      await refreshData();
      const categoryLabel = getCategoryByKey(state.filters.categoryKey).label;
      setFeedback(`Filtros aplicados. Sua busca atual está usando "${categoryLabel}".`, "ok");
    } catch (error) {
      setFeedback(friendlyError(error), "error");
    }
  });
});

refs.saveSettingsButton.addEventListener("click", async () => {
  await withButtonBusy(refs.saveSettingsButton, "Salvando...", async () => {
    if (!state.token) {
      setFeedback("Faça login para salvar preferências.", "error");
      return;
    }

    const apiReady = await ensureApiOnline(false);
    if (!apiReady) {
      setFeedback("API offline. Não foi possível salvar preferências.", "error");
      return;
    }

    readFiltersFromForm();

    if (!state.filters.activeSources.length) {
      setFeedback("Selecione ao menos uma fonte.", "error");
      return;
    }

    await setItem(STORAGE_KEYS.filters, state.filters);

    try {
      const saved = await saveSettings(state.token, settingsPayload());
      mergeSettings(saved, true);
      renderFilters();
      renderStatus();
      await setItem(STORAGE_KEYS.filters, state.filters);
      setFeedback("Preferências salvas no servidor.", "ok");
    } catch (error) {
      if (error?.status === 401) {
        await handleSessionExpired();
        return;
      }

      setFeedback(friendlyError(error), "error");
    }
  });
});

refs.runNowButton.addEventListener("click", async () => {
  await withButtonBusy(refs.runNowButton, "Buscando...", async () => {
    if (!state.token) {
      setFeedback("Faça login para buscar oportunidades.", "error");
      return;
    }

    const apiReady = await ensureApiOnline(false);
    if (!apiReady) {
      setFeedback("API offline. Não foi possível buscar oportunidades.", "error");
      return;
    }

    readFiltersFromForm();

    if (!state.filters.activeSources.length) {
      setFeedback("Selecione ao menos uma fonte.", "error");
      return;
    }

    const coverage = getCategoryCoverage(state.filters.categoryKey);
    if (coverage.supportedRealSources.length === 0) {
      setFeedback(
        `${coverage.category.label} ainda não possui cobertura real suficiente nas fontes ativas. Tente Veículos ou Todos.`,
        "error");
      renderStatus();
      return;
    }

    await setItem(STORAGE_KEYS.filters, state.filters);

    state.ui.lastExecutionCategoryKey = state.filters.categoryKey;
    state.ui.lastExecutionRealSources = coverage.supportedRealSources.length;
    state.scanStatus = `Buscando em "${coverage.category.label}" com ${coverage.supportedRealSources.length} fonte(s) reais compatíveis.`;
    renderStatus();
    setFeedback("Buscando oportunidades...", "ok");

    try {
      const result = await runScanner(state.token, scannerPayload());
      const executedCategory = getCategoryByKey(state.ui.lastExecutionCategoryKey).label;
      state.scanStatus = result.success
        ? `Concluída às ${new Date(result.completedAtUtc).toLocaleTimeString("pt-BR")} | Categoria usada: ${executedCategory} | Fontes reais: ${state.ui.lastExecutionRealSources}`
        : "Falhou";

      renderStatus();
      await refreshData();
      setFeedback(result.message || `Varredura concluída: ${result.refreshedLots || 0} lotes atualizados.`, "ok");
    } catch (error) {
      if (error?.status === 401) {
        await handleSessionExpired();
        return;
      }

      state.scanStatus = "Falhou";
      renderStatus();
      setFeedback(friendlyError(error), "error");
    }
  });
});

bootstrap();


