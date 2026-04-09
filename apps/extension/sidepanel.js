import { getToken, login, logout, me } from "./services/auth.js";
import { loadHistory } from "./services/history.js";
import { loadOpportunities } from "./services/opportunities.js";
import { runScanner } from "./services/scanner.js";
import { loadSettings, saveSettings } from "./services/settings.js";
import { getItem, setItem, STORAGE_KEYS } from "./services/storage.js";

const API_HINT = "API indisponível. Confirme se a API está em http://localhost:8080.";

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

const CATEGORIES = [
  { key: "all", label: "Todas", searchHint: "", vehicleType: null },
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
  minScore: 60,
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
  filters: { ...DEFAULT_FILTERS }
};

const refs = {
  loginForm: document.getElementById("login-form"),
  emailInput: document.getElementById("email-input"),
  passwordInput: document.getElementById("password-input"),
  sessionView: document.getElementById("session-view"),
  sessionEmail: document.getElementById("session-email"),
  logoutButton: document.getElementById("logout-button"),

  runNowButton: document.getElementById("run-now-button"),

  categoryInput: document.getElementById("category-input"),
  sourcesList: document.getElementById("sources-list"),
  searchInput: document.getElementById("search-input"),
  scoreInput: document.getElementById("score-input"),
  regionInput: document.getElementById("region-input"),
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
    return 60;
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

function isValidLotUrl(url) {
  if (!url || typeof url !== "string") {
    return false;
  }

  try {
    const parsed = new URL(url.trim());
    if (!/^https?:$/i.test(parsed.protocol)) {
      return false;
    }

    if (!parsed.hostname.includes(".")) {
      return false;
    }

    return parsed.pathname.trim().length > 1;
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
  refs.categoryInput.value = state.filters.categoryKey;
  refs.searchInput.value = state.filters.search;
  refs.scoreInput.value = String(state.filters.minScore);
  refs.regionInput.value = state.filters.region;
  refs.maxPriceInput.value = state.filters.maxPrice === null ? "" : String(state.filters.maxPrice);
  renderSources();
}

function readFiltersFromForm() {
  const selectedSources = Array.from(
    refs.sourcesList.querySelectorAll('input[type="checkbox"][data-source]:checked')
  ).map((checkbox) => checkbox.getAttribute("data-source") || "");

  state.filters = {
    categoryKey: getCategoryByKey(refs.categoryInput.value).key,
    activeSources: normalizeSources(selectedSources),
    search: refs.searchInput.value.trim(),
    minScore: clampScore(refs.scoreInput.value),
    region: refs.regionInput.value.trim().toUpperCase().slice(0, 10),
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
    advancedFiltersEnabled: false,
    category: category.label,
    activeSources: state.filters.activeSources,
    maxPrice: state.filters.maxPrice
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

  refs.runNowButton.disabled = !authenticated;
  refs.applyFiltersButton.disabled = !authenticated;
  refs.saveSettingsButton.disabled = !authenticated;
  refs.useDomainButton.disabled = !authenticated || !state.tabContext?.domain;

  refs.sourcesList.querySelectorAll('input[type="checkbox"]').forEach((checkbox) => {
    checkbox.disabled = !authenticated;
  });

  refs.sessionEmail.textContent = authenticated && state.me
    ? `${state.me.email} | Plano ${planLabel(state.me.plan)}`
    : "";
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
  const categoryLabel = getCategoryByKey(state.filters.categoryKey).label;

  refs.scanStatusText.textContent = state.scanStatus;
  refs.statusGrid.innerHTML = [
    ["Categoria", categoryLabel],
    ["Fontes ativas", String(state.filters.activeSources.length)],
    ["Resultados", String(state.opportunities.length)],
    ["Maior score", maxScore.toFixed(1)],
    ["Oportunidades fortes", String(strongCount)]
  ]
    .map(([label, value]) => `<div class="status-chip"><span>${escapeHtml(label)}</span><strong>${escapeHtml(value)}</strong></div>`)
    .join("");
}

function renderOpportunities() {
  refs.opportunitiesCount.textContent = String(state.opportunities.length);

  if (!state.opportunities.length) {
    refs.opportunitiesList.innerHTML = '<p class="muted">Nenhuma oportunidade encontrada para os filtros atuais.</p>';
    return;
  }

  refs.opportunitiesList.innerHTML = state.opportunities
    .map((item) => {
      const value = Number(item.value || 0);
      const categoryLabel = inferOpportunityCategory(item);
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
          <button class="btn btn-primary open-lot" type="button" data-url="${escapeHtml(item.lotUrl)}">Abrir lote</button>
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
        vehicleHint: ""
      };
    }

    try {
      const response = await chrome.tabs.sendMessage(tab.id, { type: "LEILAOAUTO_TAB_CONTEXT" });
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
      vehicleHint: ""
    };
  } catch {
    return null;
  }
}

async function handleSessionExpired(message = "Sessão expirada. Faça login novamente.") {
  await logout();
  state.token = null;
  state.me = null;
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

  if (state.token) {
    try {
      state.me = await me(state.token);
      state.scanStatus = "Pronta";
    } catch {
      await handleSessionExpired();
      return;
    }

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

refs.loginForm.addEventListener("submit", async (event) => {
  event.preventDefault();
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

refs.logoutButton.addEventListener("click", async () => {
  await logout();
  state.token = null;
  state.me = null;
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

refs.applyFiltersButton.addEventListener("click", async () => {
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
    setFeedback("Filtros aplicados.", "ok");
  } catch (error) {
    setFeedback(friendlyError(error), "error");
  }
});

refs.saveSettingsButton.addEventListener("click", async () => {
  if (!state.token) {
    setFeedback("Faça login para salvar preferências.", "error");
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

refs.runNowButton.addEventListener("click", async () => {
  if (!state.token) {
    setFeedback("Faça login para rodar o scanner.", "error");
    return;
  }

  readFiltersFromForm();

  if (!state.filters.activeSources.length) {
    setFeedback("Selecione ao menos uma fonte.", "error");
    return;
  }

  refs.runNowButton.disabled = true;
  state.scanStatus = "Executando varredura";
  renderStatus();
  setFeedback("Executando varredura...", "ok");

  try {
    const result = await runScanner(state.token);
    state.scanStatus = result.success
      ? `Concluída às ${new Date(result.completedAtUtc).toLocaleTimeString("pt-BR")}`
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
  } finally {
    refs.runNowButton.disabled = !state.token;
  }
});

bootstrap();
