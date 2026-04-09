import { getToken, login, logout, me } from "./services/auth.js";
import { loadHistory } from "./services/history.js";
import { loadOpportunities } from "./services/opportunities.js";
import { runScanner } from "./services/scanner.js";
import { loadSettings, saveSettings } from "./services/settings.js";
import { getItem, setItem, STORAGE_KEYS } from "./services/storage.js";

const API_HINT = "API indisponível. Confirme se a API está em http://localhost:8080.";

const state = {
  token: null,
  me: null,
  tabContext: null,
  opportunities: [],
  history: [],
  scanStatus: "Aguardando login",
  filters: {
    search: "",
    source: "",
    minScore: 60,
    vehicleType: null,
    region: ""
  }
};

const refs = {
  loginForm: document.getElementById("login-form"),
  emailInput: document.getElementById("email-input"),
  passwordInput: document.getElementById("password-input"),
  sessionView: document.getElementById("session-view"),
  sessionEmail: document.getElementById("session-email"),
  logoutButton: document.getElementById("logout-button"),
  runNowButton: document.getElementById("run-now-button"),
  tabContext: document.getElementById("tab-context"),
  useDomainButton: document.getElementById("use-domain-button"),
  statusGrid: document.getElementById("status-grid"),
  searchInput: document.getElementById("search-input"),
  sourceInput: document.getElementById("source-input"),
  scoreInput: document.getElementById("score-input"),
  typeInput: document.getElementById("type-input"),
  regionInput: document.getElementById("region-input"),
  applyFiltersButton: document.getElementById("apply-filters-button"),
  saveSettingsButton: document.getElementById("save-settings-button"),
  opportunitiesCount: document.getElementById("opportunities-count"),
  opportunitiesList: document.getElementById("opportunities-list"),
  historyList: document.getElementById("history-list"),
  feedback: document.getElementById("feedback")
};

function escapeHtml(value) {
  return String(value ?? "")
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");
}

function normalizeDomain(domain) {
  return String(domain || "").replace(/^www\./i, "").trim();
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

    const path = parsed.pathname.trim();
    return path.length > 1;
  } catch {
    return false;
  }
}

function lotStatusLabel(status) {
  const statusNumber = Number(status);
  switch (statusNumber) {
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
    return `BOM PRECO (${score.toFixed(1)})`;
  }

  return `ACIMA DA MEDIA (${score.toFixed(1)})`;
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

function renderAuth() {
  const authenticated = !!state.token;
  refs.loginForm.classList.toggle("hidden", authenticated);
  refs.sessionView.classList.toggle("hidden", !authenticated);

  refs.runNowButton.disabled = !authenticated;
  refs.applyFiltersButton.disabled = !authenticated;
  refs.saveSettingsButton.disabled = !authenticated;

  refs.sessionEmail.textContent = authenticated && state.me
    ? `${state.me.email} | Plano ${planLabel(state.me.plan)}`
    : "";
}

function renderTabContext() {
  if (!state.tabContext) {
    refs.tabContext.textContent = "Sem contexto da aba.";
    refs.useDomainButton.disabled = true;
    return;
  }

  const domain = normalizeDomain(state.tabContext.domain);
  const title = state.tabContext.title ? ` | ${state.tabContext.title}` : "";
  refs.tabContext.textContent = domain ? `${domain}${title}` : "Sem contexto da aba.";
  refs.useDomainButton.disabled = !domain;
}

function renderStatus() {
  const maxScore = state.opportunities.length
    ? Math.max(...state.opportunities.map((item) => Number(item.score || 0)))
    : 0;
  const strongCount = state.opportunities.filter((item) => Number(item.score || 0) >= 75).length;
  const sourceCount = new Set(
    state.opportunities
      .map((item) => String(item.source || "").trim())
      .filter(Boolean)
  ).size;

  const chips = [
    ["Fontes ativas", String(sourceCount)],
    ["Lotes", String(state.opportunities.length)],
    ["Maior score", maxScore.toFixed(1)],
    ["Fortes", String(strongCount)],
    ["Varredura", state.scanStatus]
  ];

  refs.statusGrid.innerHTML = chips
    .map(([label, value]) => `<article class="status-chip"><span>${escapeHtml(label)}</span><strong>${escapeHtml(value)}</strong></article>`)
    .join("");
}

function renderFilters() {
  refs.searchInput.value = state.filters.search || "";
  refs.sourceInput.value = state.filters.source || "";
  refs.scoreInput.value = String(state.filters.minScore ?? 60);
  refs.typeInput.value = state.filters.vehicleType ? String(state.filters.vehicleType) : "";
  refs.regionInput.value = state.filters.region || "";
}

function readFilters() {
  const parsedScore = Number(refs.scoreInput.value);
  const minScore = Number.isFinite(parsedScore) ? Math.max(0, Math.min(100, parsedScore)) : 60;

  state.filters = {
    search: refs.searchInput.value.trim(),
    source: refs.sourceInput.value.trim(),
    minScore,
    vehicleType: refs.typeInput.value ? Number(refs.typeInput.value) : null,
    region: refs.regionInput.value.trim().toUpperCase().slice(0, 2)
  };
}

function mapSettingsToFilters(settings) {
  state.filters = {
    search: settings.search || "",
    source: settings.source || "",
    minScore: settings.minScore ?? 60,
    vehicleType: settings.vehicleType ?? null,
    region: settings.region || ""
  };
}

function filtersPayload() {
  return {
    search: state.filters.search,
    source: state.filters.source,
    minScore: Number(state.filters.minScore || 0),
    vehicleType: state.filters.vehicleType,
    region: state.filters.region || null,
    advancedFiltersEnabled: false
  };
}

function renderOpportunities() {
  const valid = state.opportunities.filter((item) => isValidLotUrl(item.lotUrl));
  refs.opportunitiesCount.textContent = String(valid.length);

  if (!valid.length) {
    refs.opportunitiesList.innerHTML = '<p class="muted">Nenhuma oportunidade encontrada.</p>';
    return;
  }

  refs.opportunitiesList.innerHTML = valid
    .map((item) => {
      const value = Number(item.value || 0);
      const location = item.location || "-";
      const risk = String(item.riskDecision || "SEM_INFO").replaceAll("_", " ");
      return `
        <article class="item-card">
          <div class="item-top">
            <span class="badge">${escapeHtml(item.source || "Sem fonte")}</span>
            <span class="badge score">${escapeHtml(scoreLabel(item))}</span>
          </div>
          <h3>${escapeHtml(item.title || "Lote sem título")}</h3>
          <div class="item-meta">
            <span>${escapeHtml(lotStatusLabel(item.status))}</span>
            <span>${escapeHtml(location)}</span>
            <span>Risco: ${escapeHtml(risk)}</span>
          </div>
          <p class="item-price">R$ ${value.toLocaleString("pt-BR", { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</p>
          <p class="muted">${escapeHtml(item.summary || "")}</p>
          <div class="item-actions">
            <button class="button primary open-lot" type="button" data-url="${escapeHtml(item.lotUrl)}">Abrir lote</button>
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
}

function renderHistory() {
  if (!state.history.length) {
    refs.historyList.innerHTML = '<p class="muted">Sem histórico recente.</p>';
    return;
  }

  refs.historyList.innerHTML = state.history
    .slice(0, 10)
    .map((entry) => {
      const date = new Date(entry.executedAtUtc).toLocaleString("pt-BR");
      return `
        <article class="item-card">
          <div class="item-top">
            <span class="badge">${escapeHtml(entry.source || "Execução")}</span>
            <span class="badge">${escapeHtml(entry.status || (entry.success ? "CONCLUIDO" : "FALHA"))}</span>
          </div>
          <div class="item-meta">
            <span>${escapeHtml(date)}</span>
            <span>Novos lotes: ${escapeHtml(String(entry.newLots ?? 0))}</span>
          </div>
          <p class="muted">${escapeHtml(entry.message || "")}</p>
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
        title: tab.title || ""
      };
    }

    try {
      const response = await chrome.tabs.sendMessage(tab.id, { type: "LEILAOAUTO_TAB_CONTEXT" });
      if (response) {
        return response;
      }
    } catch {
      // Fallback below when content script cannot run (e.g. chrome://).
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
      title: tab.title || ""
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

  const [opportunitiesResult, historyResult] = await Promise.allSettled([
    loadOpportunities(state.token, state.filters),
    loadHistory(state.token, 10)
  ]);

  if (opportunitiesResult.status === "fulfilled") {
    state.opportunities = opportunitiesResult.value.filter((item) => isValidLotUrl(item.lotUrl));
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
  state.filters = await getItem(STORAGE_KEYS.filters, state.filters);
  state.history = await getItem(STORAGE_KEYS.history, []);
  state.tabContext = await fetchTabContext();

  renderTabContext();
  renderFilters();

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
      mapSettingsToFilters(settings);
      renderFilters();
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
      mapSettingsToFilters(settings);
      renderFilters();
    } catch {
      // Keep local filters if settings endpoint is temporarily unavailable.
    }

    refs.passwordInput.value = "";
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
    setFeedback("Domínio da aba não identificado.", "error");
    return;
  }

  refs.sourceInput.value = normalizeDomain(state.tabContext.domain);
  if (!refs.searchInput.value && state.tabContext.vehicleHint) {
    refs.searchInput.value = state.tabContext.vehicleHint;
  }

  readFilters();
  await setItem(STORAGE_KEYS.filters, state.filters);
  setFeedback("Domínio aplicado aos filtros.", "ok");
});

refs.applyFiltersButton.addEventListener("click", async () => {
  if (!state.token) {
    setFeedback("Faça login para aplicar filtros.", "error");
    return;
  }

  readFilters();
  renderFilters();
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
    setFeedback("Faça login para salvar filtros.", "error");
    return;
  }

  readFilters();
  renderFilters();
  await setItem(STORAGE_KEYS.filters, state.filters);

  try {
    const saved = await saveSettings(state.token, filtersPayload());
    mapSettingsToFilters(saved);
    renderFilters();
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
    setFeedback("Faça login para rodar scanner.", "error");
    return;
  }

  refs.runNowButton.disabled = true;
  state.scanStatus = "Executando";
  renderStatus();
  setFeedback("Executando varredura...", "ok");

  try {
    const result = await runScanner(state.token);
    state.scanStatus = result.success
      ? `Concluída ${new Date(result.completedAtUtc).toLocaleTimeString("pt-BR")}`
      : "Falhou";
    renderStatus();

    await refreshData();
    setFeedback(result.message || `Varredura concluída: ${result.refreshedLots || 0} lotes.`, "ok");
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
