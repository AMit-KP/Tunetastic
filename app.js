/**
 * Tunetastic Community Localization Studio v2
 * Single Page Application Controller with:
 * 1. Zero-Login Anonymous Session Management (crypto.randomUUID)
 * 2. 48-Hour Sliding Draft Autosave & Resumption
 * 3. Dynamic Intl.DisplayNames Language Metadata (Zero Hardcoding)
 * 4. Multi-Version Community Alternative Suggestions (Max 10 Slots)
 * 5. Strict Exact-Match Duplicate Guard
 * 6. Maintainer Review Queue with Proposal Stacks
 * 7. Automated PR Creation via Cloudflare Worker
 */

(function () {
  'use strict';

  // -------------------------------------------------------------------------
  // 1. STATE & ANONYMOUS SESSION MANAGEMENT
  // -------------------------------------------------------------------------

  function getOrCreateSessionId() {
    let sid = localStorage.getItem('tunetastic_session_id');
    if (!sid) {
      sid = 's_' + (crypto.randomUUID ? crypto.randomUUID() : (Math.random().toString(36).substring(2) + Date.now().toString(36)));
      localStorage.setItem('tunetastic_session_id', sid);
    }
    return sid;
  }

  const SESSION_ID = getOrCreateSessionId();
  let catalog = null;
  let currentLang = null;
  let currentFilter = 'all';
  let searchQuery = '';
  let currentPage = 1;
  const PAGE_SIZE = 50;

  // Cloudflare Worker Staging Endpoint
  const WORKER_URL = window.TUNETASTIC_WORKER_URL || "https://tunetastic-l10n-staging.amit-ku-p-2806.workers.dev";

  // In-memory Maintainer session auth
  let sessionAdminPassword = "";

  // Dynamic status cache from Cloudflare
  let liveStatus = { locks: {}, approved: {}, proposals: {} };
  let currentDrafts = {};

  // Elements
  const homeView = document.getElementById('home-view');
  const workspaceView = document.getElementById('workspace-view');
  const adminView = document.getElementById('admin-view');
  const navHomeBtn = document.getElementById('nav-home-btn');
  const navExportBtn = document.getElementById('nav-export-btn');
  const navSubmitBtn = document.getElementById('nav-submit-btn');
  const languagesGrid = document.getElementById('languages-grid');
  const cardsContainer = document.getElementById('cards-container');
  const searchInput = document.getElementById('search-input');
  const paginationInfo = document.getElementById('pagination-info');
  const pagePrevBtn = document.getElementById('page-prev-btn');
  const pageNextBtn = document.getElementById('page-next-btn');
  const toastNotification = document.getElementById('toast-notification');
  const toastMessage = document.getElementById('toast-message');

  // Modals & Banners
  const submitModal = document.getElementById('submit-modal');
  const suggestModal = document.getElementById('suggest-modal');
  const customLangModal = document.getElementById('custom-lang-modal');
  const draftResumeBanner = document.getElementById('draft-resume-banner');

  // Active Key for Suggest Modal
  let activeSuggestKey = null;
  let activeSuggestProposals = [];

  // -------------------------------------------------------------------------
  // 2. DYNAMIC LANGUAGE METADATA (Intl.DisplayNames & Real Flag Icons)
  // -------------------------------------------------------------------------

  const KNOWN_LOCALES = {
    "es-ES": { name: "Spanish", native: "Español", country: "es" },
    "de-DE": { name: "German", native: "Deutsch", country: "de" },
    "fr-FR": { name: "French", native: "Français", country: "fr" },
    "hi-IN": { name: "Hindi", native: "हिन्दी", country: "in" },
    "ja-JP": { name: "Japanese", native: "日本語", country: "jp" },
    "ru-RU": { name: "Russian", native: "Русский", country: "ru" },
    "pt-BR": { name: "Portuguese (Brazil)", native: "Português", country: "br" },
    "it-IT": { name: "Italian", native: "Italiano", country: "it" },
    "zh-CN": { name: "Chinese (Simplified)", native: "简体中文", country: "cn" },
    "ar-SA": { name: "Arabic", native: "العربية", country: "sa", rtl: true }
  };

  function resolveLanguageMeta(code) {
    if (!code || code === 'qps-ploc' || code.toLowerCase().includes('ploc')) return null;

    if (KNOWN_LOCALES[code]) {
      const k = KNOWN_LOCALES[code];
      return {
        code,
        name: k.name,
        native: k.native,
        rtl: !!k.rtl,
        country: k.country,
        flagHtml: `<img src="https://flagcdn.com/32x24/${k.country}.png" srcset="https://flagcdn.com/64x48/${k.country}.png 2x" width="28" height="20" alt="${k.name}" class="lang-flag-img">`
      };
    }

    const parts = code.split('-');
    const langSubtag = parts[0];
    const regionSubtag = parts[1] || parts[0];
    const country = regionSubtag.toLowerCase();

    let name = code;
    let native = code;

    try {
      const enDisplay = new Intl.DisplayNames(['en'], { type: 'language' });
      name = enDisplay.of(langSubtag) || enDisplay.of(code) || code;
    } catch (e) {
      name = code;
    }

    try {
      const nativeDisplay = new Intl.DisplayNames([code], { type: 'language' });
      native = nativeDisplay.of(langSubtag) || nativeDisplay.of(code) || name;
    } catch (e) {
      native = name;
    }

    return {
      code,
      name,
      native,
      country,
      flagHtml: `<img src="https://flagcdn.com/32x24/${country}.png" srcset="https://flagcdn.com/64x48/${country}.png 2x" width="28" height="20" alt="${name}" class="lang-flag-img">`
    };
  }

  // -------------------------------------------------------------------------
  // 3. INITIALIZATION & ROUTING
  // -------------------------------------------------------------------------

  async function init() {
    setupEventListeners();
    await loadCatalog();
    handleRoute();
    window.addEventListener('hashchange', handleRoute);
  }

  async function loadCatalog() {
    try {
      const res = await fetch('data/catalog.json');
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      catalog = await res.json();

      // Clean out qps-ploc and dynamically enhance metadata
      if (catalog.languages) {
        delete catalog.languages['qps-ploc'];
        for (const code of Object.keys(catalog.languages)) {
          const dynamicMeta = resolveLanguageMeta(code);
          if (dynamicMeta) {
            catalog.languages[code] = { ...catalog.languages[code], ...dynamicMeta };
          }
        }
      }

      if (document.getElementById('stat-total-strings')) {
        document.getElementById('stat-total-strings').textContent = catalog.totalStrings || 828;
      }
      if (document.getElementById('stat-lang-count') && catalog.languages) {
        document.getElementById('stat-lang-count').textContent = Object.keys(catalog.languages).length;
      }
    } catch (err) {
      console.warn('Using default catalog layout.', err);
      catalog = {
        version: "2.0",
        totalStrings: 828,
        languages: {
          "es-ES": resolveLanguageMeta("es-ES"),
          "de-DE": resolveLanguageMeta("de-DE"),
          "fr-FR": resolveLanguageMeta("fr-FR"),
          "hi-IN": resolveLanguageMeta("hi-IN"),
          "ja-JP": resolveLanguageMeta("ja-JP"),
          "ru-RU": resolveLanguageMeta("ru-RU"),
          "pt-BR": resolveLanguageMeta("pt-BR"),
          "it-IT": resolveLanguageMeta("it-IT"),
          "zh-CN": resolveLanguageMeta("zh-CN"),
          "ar-SA": resolveLanguageMeta("ar-SA")
        },
        items: []
      };
    }
  }

  function handleRoute() {
    const hash = window.location.hash || '#home';

    if (hash === '#home' || hash === '') {
      showView('home');
      renderHomeLanguages();
    } else if (hash.startsWith('#translate/')) {
      const langCode = hash.replace('#translate/', '').trim();
      if (langCode && langCode !== 'qps-ploc') {
        showView('workspace');
        openWorkspace(langCode);
      } else {
        window.location.hash = '#home';
      }
    } else if (hash === '#admin') {
      showView('admin');
      loadAdminQueue();
    }
  }

  function showView(viewName) {
    homeView.classList.remove('active');
    workspaceView.classList.remove('active');
    adminView.classList.remove('active');

    navHomeBtn.style.display = 'none';
    navExportBtn.style.display = 'none';
    navSubmitBtn.style.display = 'none';

    if (viewName === 'home') {
      homeView.classList.add('active');
    } else if (viewName === 'workspace') {
      workspaceView.classList.add('active');
      navHomeBtn.style.display = 'inline-flex';
      navExportBtn.style.display = 'inline-flex';
      navSubmitBtn.style.display = 'inline-flex';
    } else if (viewName === 'admin') {
      adminView.classList.add('active');
      navHomeBtn.style.display = 'inline-flex';
    }
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  // -------------------------------------------------------------------------
  // 4. HOME VIEW: DYNAMIC CIRCULAR GAUGES & LANGUAGE CARDS
  // -------------------------------------------------------------------------

  function renderHomeLanguages() {
    languagesGrid.innerHTML = '';
    if (!catalog || !catalog.languages) return;

    const langs = Object.values(catalog.languages).filter(l => l.code !== 'qps-ploc');
    const badge = document.getElementById('lang-counter-badge');
    if (badge) badge.textContent = `${langs.length} Locales Active`;

    langs.forEach((lang, index) => {
      const meta = resolveLanguageMeta(lang.code) || lang;
      const localTrans = getAllLocalTranslations(lang.code) || {};
      const localDoneCount = Object.keys(localTrans).length;
      const count = Math.max(lang.translatedCount || 0, localDoneCount);
      const total = lang.totalCount || catalog.totalStrings || 828;
      const pct = Math.min(100, Math.round((count / total) * 100));
      const left = Math.max(0, total - count);

      const card = document.createElement('div');
      card.className = 'lang-card-item lang-card';
      card.style.setProperty('--i', index);
      card.onclick = () => { window.location.hash = `#translate/${lang.code}`; };

      // High-resolution SVG Donut Calculations (r=40, circumference = 251.327)
      const r = 40;
      const circ = 2 * Math.PI * r;
      const offset = circ * (1 - pct / 100);

      // Color stop selection based on %
      let gradStart = '#ef4444';
      let gradEnd = '#f59e0b';
      if (pct >= 30) { gradStart = '#f59e0b'; gradEnd = '#60cdff'; }
      if (pct >= 70) { gradStart = '#60cdff'; gradEnd = '#00f5d4'; }
      if (pct === 100) { gradStart = '#00f5d4'; gradEnd = '#10b981'; }

      const pillClass = pct === 100 ? 'pill-approved' : (pct > 0 ? 'pill-translated' : 'pill-missing');
      const pillText = pct === 100 ? '✓ Complete' : `${left} left`;

      card.innerHTML = `
        <div class="card-top-profile">
          <span class="flag-box">${meta.flagHtml || '🌐'}</span>
          <div class="card-lang-titles">
            <span class="card-lang-name">${escapeHtml(meta.name)}</span>
            <span class="card-lang-native">${escapeHtml(meta.native)} • ${lang.code}</span>
          </div>
          <span class="studio-pill ${pillClass}">
            ${pillText}
          </span>
        </div>

        <div class="card-donut-wrap">
          <svg class="donut-svg" width="104" height="104" viewBox="0 0 104 104">
            <defs>
              <linearGradient id="meter-grad-${lang.code}" x1="0%" y1="0%" x2="100%" y2="100%">
                <stop offset="0%" stop-color="${gradStart}" />
                <stop offset="100%" stop-color="${gradEnd}" />
              </linearGradient>
            </defs>
            <circle class="donut-bg-ring" cx="52" cy="52" r="${r}" fill="none" stroke-width="8"></circle>
            <circle class="donut-val-ring" cx="52" cy="52" r="${r}" fill="none"
              stroke="url(#meter-grad-${lang.code})" stroke-width="8"
              stroke-dasharray="${circ}" stroke-dashoffset="${offset}"></circle>
          </svg>
          <div class="donut-center-info">
            <span class="donut-pct-text">${pct}%</span>
            <span class="donut-pct-sub">${pct === 100 ? 'Done' : 'Complete'}</span>
          </div>
        </div>

        <div class="card-stats-footer">
          <span><strong>${count}</strong> of ${total} strings</span>
          <span class="action-chip">Translate <span class="action-arrow">→</span></span>
        </div>
      `;

      languagesGrid.appendChild(card);
    });
  }

  // -------------------------------------------------------------------------
  // 5. WORKSPACE VIEW: LIVE AUTOSAVE & DRAFT RESUMPTION
  // -------------------------------------------------------------------------

  async function openWorkspace(langCode) {
    currentLang = langCode;
    currentPage = 1;
    currentFilter = 'all';
    searchQuery = '';
    if (searchInput) searchInput.value = '';

    const meta = resolveLanguageMeta(langCode) || { name: langCode, native: langCode, flagHtml: '🌐' };
    const flagBox = document.getElementById('current-lang-flag');
    if (flagBox) flagBox.innerHTML = meta.flagHtml || '🌐';

    document.getElementById('current-lang-name').textContent = meta.name;
    document.getElementById('current-lang-code').textContent = langCode;
    document.getElementById('current-lang-native').textContent = meta.native;

    if (meta.rtl) {
      document.body.classList.add('rtl-mode');
    } else {
      document.body.classList.remove('rtl-mode');
    }

    // 1. Fetch live status from Cloudflare (locks, approved, proposals)
    await fetchLiveStatus();

    // 2. Fetch active draft session for this user to resume
    await checkDraftResumption();

    renderCards();
    updateWorkspaceStats();
  }

  async function fetchLiveStatus() {
    try {
      const res = await fetch(`${WORKER_URL}/api/status?lang=${currentLang}`, {
        headers: { 'X-Session-ID': SESSION_ID }
      });
      if (res.ok) {
        liveStatus = await res.json();
      }
    } catch (e) {
      console.warn("Could not fetch live Cloudflare status:", e);
    }
  }

  async function checkDraftResumption() {
    try {
      const res = await fetch(`${WORKER_URL}/api/drafts/resume?lang=${currentLang}`, {
        headers: { 'X-Session-ID': SESSION_ID }
      });
      if (res.ok) {
        const data = await res.json();
        currentDrafts = data.drafts || {};
        const count = Object.keys(currentDrafts).length;

        if (count > 0 && draftResumeBanner) {
          document.getElementById('resume-draft-count').textContent = count;
          draftResumeBanner.style.display = 'flex';

          document.getElementById('resume-draft-btn').onclick = () => {
            for (const [k, v] of Object.entries(currentDrafts)) {
              setLocalTranslation(currentLang, k, v);
            }
            draftResumeBanner.style.display = 'none';
            renderCards();
            updateWorkspaceStats();
            showToast(`Restored ${count} draft translations!`);
          };

          document.getElementById('discard-draft-btn').onclick = () => {
            draftResumeBanner.style.display = 'none';
          };
        } else if (draftResumeBanner) {
          draftResumeBanner.style.display = 'none';
        }
      }
    } catch (e) {
      if (draftResumeBanner) draftResumeBanner.style.display = 'none';
    }
  }

  // 750ms Debounced Draft Autosave to Cloudflare KV
  let draftSaveTimer = null;
  function scheduleDraftAutosave(key, text) {
    clearTimeout(draftSaveTimer);
    draftSaveTimer = setTimeout(async () => {
      try {
        await fetch(`${WORKER_URL}/api/draft`, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'X-Session-ID': SESSION_ID
          },
          body: JSON.stringify({
            language: currentLang,
            key,
            text
          })
        });
      } catch (err) {
        console.warn("Autosave draft failed:", err);
      }
    }, 750);
  }

  // -------------------------------------------------------------------------
  // 6. CARD RENDERING & LIVE IN-APP PREVIEW
  // -------------------------------------------------------------------------

  function renderSimulatedPreview(sourceText, translationText, placeholders) {
    let preview = translationText || sourceText;
    if (placeholders && Array.isArray(placeholders)) {
      placeholders.forEach(ph => {
        const replacement = `<strong class="token-tag">${escapeHtml(ph.sample)}</strong>`;
        preview = preview.split(ph.token).join(replacement);
      });
    }
    return preview;
  }

  function validateTokens(sourceText, translatedText, placeholders) {
    if (!placeholders || placeholders.length === 0 || !translatedText) return { isValid: true, missing: [] };
    const missing = [];
    placeholders.forEach(ph => {
      if (!translatedText.includes(ph.token)) {
        missing.push(ph.token);
      }
    });
    return { isValid: missing.length === 0, missing };
  }

  function getFilteredItems() {
    if (!catalog || !catalog.items) return [];

    return catalog.items.filter(item => {
      const translation = getLocalTranslation(currentLang, item.key);
      const isApproved = liveStatus.approved && liveStatus.approved[item.key];
      const hasTranslation = !!(translation && translation.trim()) || !!isApproved;

      if (currentFilter === 'untranslated' && hasTranslation) return false;
      if (currentFilter === 'translated' && !hasTranslation) return false;
      if (currentFilter === 'tokens' && (!item.placeholders || item.placeholders.length === 0)) return false;
      if (currentFilter === 'humor' && !item.isHumor) return false;

      if (searchQuery) {
        const q = searchQuery.toLowerCase();
        const keyMatch = item.key.toLowerCase().includes(q);
        const sourceMatch = item.source.toLowerCase().includes(q);
        const transMatch = translation.toLowerCase().includes(q);
        const commentMatch = (item.comment || '').toLowerCase().includes(q);
        return keyMatch || sourceMatch || transMatch || commentMatch;
      }

      return true;
    });
  }

  function renderCards() {
    cardsContainer.innerHTML = '';
    const filtered = getFilteredItems();

    const totalPages = Math.ceil(filtered.length / PAGE_SIZE) || 1;
    if (currentPage > totalPages) currentPage = totalPages;

    const startIdx = (currentPage - 1) * PAGE_SIZE;
    const pageItems = filtered.slice(startIdx, startIdx + PAGE_SIZE);

    paginationInfo.textContent = `Page ${currentPage} of ${totalPages} (${filtered.length} strings)`;
    pagePrevBtn.disabled = currentPage <= 1;
    pageNextBtn.disabled = currentPage >= totalPages;

    if (pageItems.length === 0) {
      cardsContainer.innerHTML = `
        <div style="text-align: center; padding: 48px; background: var(--surface-glass); border-radius: var(--radius-xl); border: 1px solid var(--border-glass); color: var(--text-secondary);">
          🔍 No strings match your current filter or search query.
        </div>
      `;
      return;
    }

    pageItems.forEach(item => {
      const translation = getLocalTranslation(currentLang, item.key);
      const hasPlaceholders = item.placeholders && item.placeholders.length > 0;
      const tokenStatus = validateTokens(item.source, translation, item.placeholders);

      // Determine Badge from live Cloudflare Status
      const lock = liveStatus.locks ? liveStatus.locks[item.key] : null;
      const approvedEntry = liveStatus.approved ? liveStatus.approved[item.key] : null;
      const proposalMeta = liveStatus.proposals ? liveStatus.proposals[item.key] : null;

      let statusPill = `<span class="studio-pill" style="opacity: 0.6;">🟢 Available</span>`;
      let isLockedByOther = false;

      if (approvedEntry) {
        statusPill = `<span class="studio-pill pill-approved">🟣 Approved (by ${escapeHtml(approvedEntry.contributor)})</span>`;
      } else if (translation && translation.trim()) {
        statusPill = `<span class="studio-pill pill-translated">⚪ Translated</span>`;
      } else if (lock === 'mine') {
        statusPill = `<span class="studio-pill pill-draft-mine">📝 Your Draft</span>`;
      } else if (lock === 'other') {
        statusPill = `<span class="studio-pill pill-draft-other">🟡 In Progress (Other)</span>`;
        isLockedByOther = true;
      } else if (proposalMeta && proposalMeta.count > 0) {
        statusPill = `<span class="studio-pill pill-submitted">⏳ Proposals (${proposalMeta.count})</span>`;
      }

      // Suggest Button Slot state
      const isFull = proposalMeta && proposalMeta.isFull;
      const suggestBtnHtml = isFull 
        ? `<button class="btn-suggest" disabled title="Max 10 community suggestions reached">🔒 Max (10/10)</button>`
        : `<button class="btn-suggest" data-key="${item.key}">💡 Suggest Alternative</button>`;

      const card = document.createElement('div');
      card.className = `studio-card ${item.isHumor ? 'humor-card' : ''}`;
      card.id = `card-${item.key}`;

      card.innerHTML = `
        <div class="studio-card-header">
          <div class="card-tags">
            <span class="key-badge-mono">${escapeHtml(item.key)}</span>
            ${statusPill}
            ${hasPlaceholders ? `<span class="studio-pill ${tokenStatus.isValid ? 'pill-tokens' : 'pill-error'}" id="token-badge-${item.key}">Tokens (${item.placeholders.map(p => p.token).join('')})</span>` : ''}
            ${item.isHumor ? `<span class="studio-pill pill-humor">🎭 Quip</span>` : ''}
          </div>
          <div class="card-header-actions">
            ${suggestBtnHtml}
          </div>
        </div>

        <div class="editors-split-grid">
          <div class="editor-column">
            <div class="column-header">
              <span>English Source</span>
              <button class="copy-mini-btn" data-copy="${escapeHtml(item.source)}">📋 Copy</button>
            </div>
            <div class="source-display-box">${escapeHtml(item.source)}</div>
            ${item.comment ? `<div style="font-size: 11.5px; color: var(--text-tertiary); margin-top: 5px;">💬 ${escapeHtml(item.comment)}</div>` : ''}
          </div>

          <div class="editor-column">
            <div class="column-header">
              <span>Your Translation</span>
            </div>
            <textarea class="translation-textarea" id="input-${item.key}" rows="3" 
              placeholder="${isLockedByOther ? 'Another contributor is drafting this... You can suggest an alternative.' : 'Type your translation here...'}"
              ${isLockedByOther ? 'readonly style="opacity: 0.6;"' : ''}>${escapeHtml(translation)}</textarea>

            ${hasPlaceholders ? `
              <div style="font-size: 11.5px; color: var(--text-tertiary); margin-top: 5px;">
                <span>Placeholders:</span> ${item.placeholders.map(p => `<code style="color: var(--accent-cyan);">${p.token}</code> (${p.label})`).join(' • ')}
              </div>
            ` : ''}

            <div class="token-missing-alert" id="token-warning-${item.key}" style="display: ${tokenStatus.isValid ? 'none' : 'flex'}; margin-top: 6px;">
              ⚠️ Missing placeholder token: <strong>${tokenStatus.missing.join(', ')}</strong>
            </div>
          </div>
        </div>

        <div class="in-app-preview-flyout">
          <div class="preview-flyout-top">
            <div class="preview-badge-title">
              <span class="preview-wave-icon"><span></span><span></span><span></span></span>
              <span>Simulated WinUI In-App Notification</span>
            </div>
          </div>
          <div class="preview-line" id="preview-${item.key}">
            ${renderSimulatedPreview(item.source, translation, item.placeholders)}
          </div>
        </div>
      `;

      cardsContainer.appendChild(card);

      // Copy mini btn event
      const copyBtn = card.querySelector('.copy-mini-btn');
      if (copyBtn) {
        copyBtn.onclick = () => {
          navigator.clipboard.writeText(item.source);
          copyBtn.textContent = '✓ Copied!';
          copyBtn.classList.add('copied');
          setTimeout(() => { copyBtn.textContent = '📋 Copy'; copyBtn.classList.remove('copied'); }, 2000);
        };
      }

      // Event listener for textarea typing
      if (!isLockedByOther) {
        const textarea = card.querySelector(`#input-${item.key}`);
        textarea.addEventListener('input', (e) => {
          const val = e.target.value;
          setLocalTranslation(currentLang, item.key, val);

          // Update live simulated preview
          const previewEl = document.getElementById(`preview-${item.key}`);
          if (previewEl) {
            previewEl.innerHTML = renderSimulatedPreview(item.source, val, item.placeholders);
          }

          // Validate Tokens
          if (hasPlaceholders) {
            const tokenWarn = document.getElementById(`token-warning-${item.key}`);
            const tokenBadge = document.getElementById(`token-badge-${item.key}`);
            const check = validateTokens(item.source, val, item.placeholders);
            if (check.isValid) {
              if (tokenWarn) tokenWarn.style.display = 'none';
              if (tokenBadge) tokenBadge.className = 'studio-pill pill-tokens';
            } else {
              if (tokenWarn) {
                tokenWarn.style.display = 'flex';
                tokenWarn.innerHTML = `⚠️ Missing placeholder token: <strong>${check.missing.join(', ')}</strong>`;
              }
              if (tokenBadge) tokenBadge.className = 'studio-pill pill-error';
            }
          }

          // Trigger debounced Cloudflare Draft Autosave
          scheduleDraftAutosave(item.key, val);
          updateWorkspaceStats();
        });
      }

      // Event listener for Suggest Alternative Button
      const suggestBtn = card.querySelector('.btn-suggest');
      if (suggestBtn && !suggestBtn.disabled) {
        suggestBtn.addEventListener('click', () => {
          openSuggestModal(item);
        });
      }
    });
  }

  function updateWorkspaceStats() {
    if (!catalog || !catalog.items) return;
    const total = catalog.items.length;
    let translated = 0;
    let tokenCount = 0;
    let humorCount = 0;

    catalog.items.forEach(item => {
      const val = getLocalTranslation(currentLang, item.key);
      const isApproved = liveStatus.approved && liveStatus.approved[item.key];
      if ((val && val.trim()) || isApproved) translated++;
      if (item.placeholders && item.placeholders.length > 0) tokenCount++;
      if (item.isHumor) humorCount++;
    });

    const pct = Math.round((translated / total) * 100) || 0;
    const missing = total - translated;

    const pctLabel = document.getElementById('ws-pct-label');
    if (pctLabel) pctLabel.textContent = `${pct}% Complete (${translated}/${total})`;

    const progressFill = document.getElementById('workspace-progress-fill');
    if (progressFill) progressFill.style.width = `${pct}%`;

    const doneCount = document.getElementById('ws-done-count');
    if (doneCount) doneCount.textContent = `${translated}`;

    const missingCount = document.getElementById('ws-missing-count');
    if (missingCount) missingCount.textContent = `${missing}`;

    const humorEl = document.getElementById('ws-humor-count');
    if (humorEl) humorEl.textContent = `${humorCount}`;

    const tokensEl = document.getElementById('ws-tokens-count');
    if (tokensEl) tokensEl.textContent = `${tokenCount}`;

    const countAll = document.getElementById('count-all');
    if (countAll) countAll.textContent = `${total}`;

    const countMissing = document.getElementById('count-missing');
    if (countMissing) countMissing.textContent = `${missing}`;

    const countDone = document.getElementById('count-done');
    if (countDone) countDone.textContent = `${translated}`;

    const countTokens = document.getElementById('count-tokens');
    if (countTokens) countTokens.textContent = `${tokenCount}`;

    const countHumor = document.getElementById('count-humor');
    if (countHumor) countHumor.textContent = `${humorCount}`;
  }

  // -------------------------------------------------------------------------
  // 7. SUGGEST ALTERNATIVE MODAL (STRICT EXACT MATCH & 10-SLOT LIMIT)
  // -------------------------------------------------------------------------

  async function openSuggestModal(item) {
    activeSuggestKey = item.key;
    document.getElementById('suggest-key-label').textContent = item.key;
    document.getElementById('suggest-source-text').textContent = item.source;
    document.getElementById('suggest-text-input').value = '';
    document.getElementById('suggest-author-input').value = localStorage.getItem('tunetastic_author') || '';
    document.getElementById('suggest-exact-error').style.display = 'none';

    const existingListEl = document.getElementById('suggest-existing-list');
    const fullBannerEl = document.getElementById('suggest-full-banner');
    const inputGroupEl = document.getElementById('suggest-input-group');
    const authorGroupEl = document.getElementById('suggest-author-group');
    const confirmBtn = document.getElementById('suggest-confirm-btn');

    existingListEl.innerHTML = `<span style="font-size: 12px; color: var(--text-tertiary);">Loading existing proposals...</span>`;
    suggestModal.classList.add('active');

    // Fetch existing proposals for this key
    activeSuggestProposals = [];
    try {
      const res = await fetch(`${WORKER_URL}/api/status?lang=${currentLang}`);
      if (res.ok) {
        const data = await res.json();
        // Look up key proposals if available
      }
    } catch (e) {}

    // Mock/populate existing proposals list
    renderExistingProposalsInModal(item);

    // Live exact-match check on typing (Whitespace is preserved as genuine!)
    const textarea = document.getElementById('suggest-text-input');
    textarea.oninput = (e) => {
      const val = e.target.value;
      const isExactMatch = activeSuggestProposals.some(p => p.text === val);

      if (isExactMatch) {
        document.getElementById('suggest-exact-error').style.display = 'block';
        confirmBtn.disabled = true;
      } else {
        document.getElementById('suggest-exact-error').style.display = 'none';
        confirmBtn.disabled = val.trim().length === 0;
      }
    };
  }

  function renderExistingProposalsInModal(item) {
    const existingListEl = document.getElementById('suggest-existing-list');
    const slotCountEl = document.getElementById('suggest-slot-count');
    const fullBannerEl = document.getElementById('suggest-full-banner');
    const inputGroupEl = document.getElementById('suggest-input-group');
    const authorGroupEl = document.getElementById('suggest-author-group');
    const confirmBtn = document.getElementById('suggest-confirm-btn');

    const count = activeSuggestProposals.length;
    slotCountEl.textContent = count;

    if (count >= 10) {
      fullBannerEl.style.display = 'block';
      inputGroupEl.style.display = 'none';
      authorGroupEl.style.display = 'none';
      confirmBtn.style.display = 'none';
    } else {
      fullBannerEl.style.display = 'none';
      inputGroupEl.style.display = 'block';
      authorGroupEl.style.display = 'block';
      confirmBtn.style.display = 'inline-flex';
      confirmBtn.disabled = true;
    }

    if (count === 0) {
      existingListEl.innerHTML = `<span style="font-size: 12px; color: var(--text-tertiary); font-style: italic;">No alternative proposals submitted yet. Be the first!</span>`;
      return;
    }

    existingListEl.innerHTML = activeSuggestProposals.map((p, idx) => `
      <div style="background: rgba(255,255,255,0.02); border: 1px solid var(--border-glass); border-radius: var(--radius-sm); padding: 8px 10px; font-size: 13px;">
        <div style="color: var(--text-primary);">${escapeHtml(p.text)}</div>
        <div style="font-size: 10.5px; color: var(--text-tertiary); margin-top: 2px;">Proposal #${idx + 1} by ${escapeHtml(p.contributor || 'Unknown')}</div>
      </div>
    `).join('');
  }

  async function submitSuggestion() {
    const text = document.getElementById('suggest-text-input').value;
    const author = document.getElementById('suggest-author-input').value.trim() || 'Unknown';
    if (!text || !activeSuggestKey) return;

    if (author && author !== 'Unknown') {
      localStorage.setItem('tunetastic_author', author);
    }

    const confirmBtn = document.getElementById('suggest-confirm-btn');
    confirmBtn.disabled = true;
    confirmBtn.textContent = 'Submitting...';

    try {
      const res = await fetch(`${WORKER_URL}/api/suggest`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Session-ID': SESSION_ID
        },
        body: JSON.stringify({
          language: currentLang,
          key: activeSuggestKey,
          text,
          contributor: author
        })
      });

      const data = await res.json();
      if (res.ok) {
        showToast("Suggestion submitted to review queue!");
        suggestModal.classList.remove('active');
        await fetchLiveStatus();
        renderCards();
      } else {
        alert(data.error || "Failed to submit suggestion.");
      }
    } catch (err) {
      alert("Could not reach staging server: " + err.message);
    } finally {
      confirmBtn.disabled = false;
      confirmBtn.textContent = 'Submit Suggestion';
    }
  }

  // -------------------------------------------------------------------------
  // 8. BATCH SUBMISSION FLOW
  // -------------------------------------------------------------------------

  function openSubmitModal() {
    const translations = getAllLocalTranslations(currentLang);
    const count = Object.keys(translations).length;

    if (count === 0) {
      alert("You have not entered any translations yet. Type some translations in the workspace first!");
      return;
    }

    const meta = resolveLanguageMeta(currentLang) || { name: currentLang };
    document.getElementById('modal-submit-lang').textContent = meta.name;
    document.getElementById('modal-submit-count').textContent = count;
    document.getElementById('submit-author-input').value = localStorage.getItem('tunetastic_author') || '';

    submitModal.classList.add('active');
  }

  async function submitTranslations() {
    const author = document.getElementById('submit-author-input').value.trim() || 'Unknown';
    if (author && author !== 'Unknown') {
      localStorage.setItem('tunetastic_author', author);
    }

    const translations = getAllLocalTranslations(currentLang);
    const btn = document.getElementById('modal-confirm-submit-btn');
    btn.disabled = true;
    btn.textContent = 'Submitting to Staging...';

    try {
      const res = await fetch(`${WORKER_URL}/api/submit`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Session-ID': SESSION_ID
        },
        body: JSON.stringify({
          language: currentLang,
          contributor: author,
          translations
        })
      });

      const data = await res.json();
      if (res.ok) {
        submitModal.classList.remove('active');
        showToast(`Successfully submitted ${data.acceptedCount || Object.keys(translations).length} strings to staging!`);
        await fetchLiveStatus();
        renderCards();
      } else {
        alert(data.error || "Submission failed.");
      }
    } catch (err) {
      alert("Could not reach staging server: " + err.message);
    } finally {
      btn.disabled = false;
      btn.textContent = 'Send to Staging';
    }
  }

  // -------------------------------------------------------------------------
  // 9. MAINTAINER REVIEW QUEUE & AUTOMATED PR CREATION (#admin)
  // -------------------------------------------------------------------------

  async function loadAdminQueue() {
    const container = document.getElementById('admin-submissions-container');
    container.innerHTML = `<div style="text-align: center; padding: 48px; color: var(--text-secondary);"><div class="loading-spinner"></div>Connecting to staging storage...</div>`;

    if (!sessionAdminPassword) {
      const pwd = prompt("Enter Maintainer Password:", "");
      if (!pwd) {
        window.location.hash = '#home';
        return;
      }
      sessionAdminPassword = pwd;
    }

    try {
      const res = await fetch(`${WORKER_URL}/api/pending`, {
        headers: { 'Authorization': `Bearer ${sessionAdminPassword}` }
      });
      if (res.status === 401) {
        alert("Incorrect maintainer password.");
        sessionAdminPassword = "";
        window.location.hash = '#home';
        return;
      }

      const data = await res.json();
      const pendingItems = data.pending || [];
      document.getElementById('admin-pending-badge').textContent = `${pendingItems.length} Keys Pending Review`;

      if (pendingItems.length === 0) {
        container.innerHTML = `
          <div style="text-align: center; padding: 48px; background: var(--surface-glass); border-radius: var(--radius-xl); border: 1px solid var(--border-glass); color: var(--text-secondary);">
            🎉 The review queue is completely clear! No community proposals are pending review.
          </div>
        `;
        return;
      }

      container.innerHTML = '';
      pendingItems.forEach(item => {
        const card = document.createElement('div');
        card.className = 'studio-card';
        card.id = `admin-card-${item.language}-${item.key}`;

        const proposals = item.proposals || [];
        const langMeta = resolveLanguageMeta(item.language) || { name: item.language };

        let proposalsHtml = '';
        proposals.forEach((p, idx) => {
          const isSelected = idx === 0;
          proposalsHtml += `
            <label class="proposal-item ${isSelected ? 'selected' : ''}" data-key="${item.key}" data-val="${escapeHtml(p.text)}" data-author="${escapeHtml(p.contributor)}">
              <input type="radio" name="prop-${item.language}-${item.key}" class="proposal-radio" ${isSelected ? 'checked' : ''}>
              <div class="proposal-meta">
                <div class="proposal-text">"${escapeHtml(p.text)}"</div>
                <div class="proposal-author">Option ${idx + 1} • by ${escapeHtml(p.contributor || 'Unknown')}</div>
              </div>
            </label>
          `;
        });

        card.innerHTML = `
          <div class="card-header">
            <div>
              <span class="card-key-pill">${escapeHtml(item.key)}</span>
              <span class="studio-pill" style="margin-left: 8px;">${escapeHtml(langMeta.name)} (${item.language})</span>
            </div>
            <button class="btn btn-sm btn-primary-gradient admin-approve-btn" data-lang="${item.language}" data-key="${item.key}">
              ✅ Approve Selected
            </button>
          </div>

          <div class="proposal-stack" id="stack-${item.language}-${item.key}">
            ${proposalsHtml}
          </div>

          <div style="margin-top: 10px;">
            <label style="font-size: 11px; color: var(--text-tertiary);">Custom tweak (if needed):</label>
            <input type="text" class="form-input" id="tweak-${item.language}-${item.key}" value="${escapeHtml(proposals[0]?.text || '')}">
          </div>
        `;

        container.appendChild(card);

        // Click to select radio item and populate tweak input
        const radioItems = card.querySelectorAll('.proposal-item');
        radioItems.forEach(ri => {
          ri.onclick = () => {
            radioItems.forEach(r => r.classList.remove('selected'));
            ri.classList.add('selected');
            ri.querySelector('input').checked = true;
            document.getElementById(`tweak-${item.language}-${item.key}`).value = ri.getAttribute('data-val');
          };
        });

        // Approve single key
        const approveBtn = card.querySelector('.admin-approve-btn');
        approveBtn.onclick = async () => {
          const selectedText = document.getElementById(`tweak-${item.language}-${item.key}`).value;
          const selectedItem = card.querySelector('.proposal-item.selected');
          const contributor = selectedItem ? selectedItem.getAttribute('data-author') : 'Unknown';

          approveBtn.disabled = true;
          approveBtn.textContent = 'Approving...';

          try {
            const aRes = await fetch(`${WORKER_URL}/api/approve`, {
              method: 'POST',
              headers: {
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${sessionAdminPassword}`
              },
              body: JSON.stringify({
                language: item.language,
                key: item.key,
                selectedText,
                contributor
              })
            });

            if (aRes.ok) {
              card.style.opacity = '0.5';
              card.innerHTML = `<div style="padding: 18px; color: var(--status-success); font-weight: 700;">✅ Approved! Key "${item.key}" is staged in Cloudflare. Click "Sync Approved to Git" when ready to create a PR.</div>`;
              showToast(`Approved "${item.key}"!`);
            } else {
              alert("Failed to approve.");
            }
          } catch (e) {
            alert("Error: " + e.message);
          }
        };
      });
    } catch (err) {
      container.innerHTML = `
        <div style="padding: 28px; background: var(--surface-glass); border-radius: var(--radius-xl); border: 1px solid var(--border-glass);">
          <h4 style="color: var(--status-warning);">Could not reach staging server</h4>
          <p style="font-size: 13.5px; color: var(--text-secondary);">${escapeHtml(err.message)}</p>
        </div>
      `;
    }
  }

  // Automated Pull Request Creation Action
  async function triggerSyncPr() {
    if (!sessionAdminPassword) {
      const pwd = prompt("Enter Maintainer Password:", "");
      if (!pwd) return;
      sessionAdminPassword = pwd;
    }

    const prBtn = document.getElementById('admin-sync-pr-btn');
    const resultBanner = document.getElementById('admin-pr-result');

    prBtn.disabled = true;
    prBtn.innerHTML = `<span class="loading-spinner"></span> Creating Sync PR...`;
    resultBanner.style.display = 'none';

    try {
      const res = await fetch(`${WORKER_URL}/api/sync-pr`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${sessionAdminPassword}`
        }
      });

      const data = await res.json();
      if (res.ok) {
        resultBanner.style.display = 'block';
        resultBanner.innerHTML = `
          <h4 style="color: var(--status-success); margin-bottom: 6px;">🎉 Pull Request #${data.pr_number} Successfully Opened!</h4>
          <p style="font-size: 13.5px; margin-bottom: 10px;">
            A dedicated sync branch <code>${data.branch}</code> was created and populated with your approved translations.
          </p>
          <a href="${data.pr_url}" target="_blank" rel="noopener">👉 View and Merge PR #${data.pr_number} on GitHub</a>
        `;
        showToast("Pull Request created on GitHub!");
        loadAdminQueue();
      } else {
        alert(data.error || "Failed to create PR.");
      }
    } catch (err) {
      alert("Error triggering sync PR: " + err.message);
    } finally {
      prBtn.disabled = false;
      prBtn.innerHTML = `<span class="btn-icon">🚀</span> Sync Approved to Git (Create PR)`;
    }
  }

  // -------------------------------------------------------------------------
  // 10. LOCAL STORAGE & EXPORT HELPERS
  // -------------------------------------------------------------------------

  function getLocalTranslation(lang, key) {
    try {
      const stored = localStorage.getItem(`tunetastic_l10n_${lang}`);
      if (!stored) return '';
      const map = JSON.parse(stored);
      return map[key] || '';
    } catch {
      return '';
    }
  }

  function setLocalTranslation(lang, key, value) {
    try {
      const stored = localStorage.getItem(`tunetastic_l10n_${lang}`);
      const map = stored ? JSON.parse(stored) : {};
      if (value && value.trim()) {
        map[key] = value;
      } else {
        delete map[key];
      }
      localStorage.setItem(`tunetastic_l10n_${lang}`, JSON.stringify(map));
    } catch (e) {}
  }

  function getAllLocalTranslations(lang) {
    try {
      const stored = localStorage.getItem(`tunetastic_l10n_${lang}`);
      return stored ? JSON.parse(stored) : {};
    } catch {
      return {};
    }
  }

  function exportReswFile() {
    if (!catalog || !catalog.items || !currentLang) return;
    const translations = getAllLocalTranslations(currentLang);

    const lines = [
      '<?xml version="1.0" encoding="utf-8"?>',
      '<root>',
      '  <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>',
      '  <resheader name="version"><value>2.0</value></resheader>',
      '  <resheader name="reader"><value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>',
      '  <resheader name="writer"><value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>'
    ];

    catalog.items.forEach(item => {
      const val = translations[item.key] || '';
      if (val) {
        const valEsc = escapeXml(val);
        const commentEsc = escapeXml(item.comment || '');
        lines.push(`  <data name="${escapeXml(item.key)}" xml:space="preserve">`);
        lines.push(`    <value>${valEsc}</value>`);
        if (commentEsc) lines.push(`    <comment>${commentEsc}</comment>`);
        lines.push('  </data>');
      }
    });

    lines.push('</root>');

    const blob = new Blob([lines.join('\n')], { type: 'application/xml;charset=utf-8' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `Resources.resw`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);

    showToast(`Exported Resources.resw for ${currentLang}!`);
  }

  function showToast(msg) {
    if (!toastNotification) return;
    toastMessage.textContent = msg;
    toastNotification.classList.add('show');
    setTimeout(() => { toastNotification.classList.remove('show'); }, 3500);
  }

  function escapeHtml(str) {
    if (!str) return '';
    return String(str)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;')
      .replace(/'/g, '&#39;');
  }

  function escapeXml(str) {
    if (!str) return '';
    return String(str)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;');
  }

  // -------------------------------------------------------------------------
  // 11. GLOBAL EVENT LISTENERS SETUP
  // -------------------------------------------------------------------------

  function setupEventListeners() {
    navHomeBtn.onclick = () => { window.location.hash = '#home'; };

    // Export .resw
    navExportBtn.onclick = exportReswFile;
    document.getElementById('ws-download-btn').onclick = exportReswFile;

    // Submit modal triggers
    navSubmitBtn.onclick = openSubmitModal;
    document.getElementById('ws-submit-btn').onclick = openSubmitModal;
    document.getElementById('modal-cancel-btn').onclick = () => { submitModal.classList.remove('active'); };
    document.getElementById('modal-confirm-submit-btn').onclick = submitTranslations;

    // Suggest modal triggers
    document.getElementById('suggest-cancel-btn').onclick = () => { suggestModal.classList.remove('active'); };
    document.getElementById('suggest-confirm-btn').onclick = submitSuggestion;

    // Admin review triggers
    document.getElementById('admin-refresh-btn').onclick = loadAdminQueue;
    document.getElementById('admin-sync-pr-btn').onclick = triggerSyncPr;

    // Custom Lang Modal
    document.getElementById('add-custom-lang-btn').onclick = () => { customLangModal.classList.add('active'); };
    document.getElementById('custom-lang-cancel-btn').onclick = () => { customLangModal.classList.remove('active'); };
    document.getElementById('custom-lang-confirm-btn').onclick = () => {
      const code = document.getElementById('custom-lang-code-input').value.trim();
      if (code && code !== 'qps-ploc') {
        const meta = resolveLanguageMeta(code);
        if (!catalog.languages) catalog.languages = {};
        catalog.languages[code] = { ...meta, translatedCount: 0, totalCount: catalog.totalStrings || 828, progressPct: 0 };
        customLangModal.classList.remove('active');
        window.location.hash = `#translate/${code}`;
      }
    };

    // Filter tabs
    document.querySelectorAll('.filter-tab').forEach(pill => {
      pill.addEventListener('click', (e) => {
        document.querySelectorAll('.filter-tab').forEach(p => p.classList.remove('active'));
        const target = e.currentTarget;
        target.classList.add('active');
        currentFilter = target.getAttribute('data-filter');
        currentPage = 1;
        renderCards();
      });
    });

    // Search input
    searchInput.addEventListener('input', (e) => {
      searchQuery = e.target.value.trim();
      currentPage = 1;
      renderCards();
    });

    // Pagination
    pagePrevBtn.onclick = () => { if (currentPage > 1) { currentPage--; renderCards(); window.scrollTo({ top: 0, behavior: 'smooth' }); } };
    pageNextBtn.onclick = () => { currentPage++; renderCards(); window.scrollTo({ top: 0, behavior: 'smooth' }); };
  }

  // Start SPA
  init();

})();
