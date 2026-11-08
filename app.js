/**
 * Tunetastic Community Localization Studio
 * Single Page Application Controller
 */

(function () {
  'use strict';

  // State
  let catalog = null;
  let currentLang = null;
  let currentFilter = 'all';
  let searchQuery = '';
  let currentPage = 1;
  const PAGE_SIZE = 50;

  // Cloudflare Worker Staging Endpoint
  const WORKER_URL = window.TUNETASTIC_WORKER_URL || "https://tunetastic-l10n-staging.amit-ku-p-2806.workers.dev";

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

  // Modals
  const submitModal = document.getElementById('submit-modal');
  const customLangModal = document.getElementById('custom-lang-modal');

  // =========================================================================
  // 1. INITIALIZATION & ROUTING
  // =========================================================================

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
      
      // Update Hero Global Stats
      if (document.getElementById('stat-total-strings')) {
        document.getElementById('stat-total-strings').textContent = catalog.totalStrings || 828;
      }
      if (document.getElementById('stat-lang-count') && catalog.languages) {
        const count = Object.values(catalog.languages).filter(l => l.code !== 'qps-ploc').length;
        document.getElementById('stat-lang-count').textContent = count;
      }
    } catch (err) {
      console.warn('Could not load data/catalog.json directly, using fallback structure.', err);
      catalog = {
        version: "1.0",
        totalStrings: 828,
        languages: {
          "es-ES": { code: "es-ES", name: "Spanish", native: "Español", flag: "🇪🇸", translatedCount: 0, totalCount: 828, progressPct: 0 },
          "de-DE": { code: "de-DE", name: "German", native: "Deutsch", flag: "🇩🇪", translatedCount: 0, totalCount: 828, progressPct: 0 },
          "fr-FR": { code: "fr-FR", name: "French", native: "Français", flag: "🇫🇷", translatedCount: 0, totalCount: 828, progressPct: 0 },
          "hi-IN": { code: "hi-IN", name: "Hindi", native: "हिन्दी", flag: "🇮🇳", translatedCount: 0, totalCount: 828, progressPct: 0 },
          "ja-JP": { code: "ja-JP", name: "Japanese", native: "日本語", flag: "🇯🇵", translatedCount: 0, totalCount: 828, progressPct: 0 },
          "ru-RU": { code: "ru-RU", name: "Russian", native: "Русский", flag: "🇷🇺", translatedCount: 0, totalCount: 828, progressPct: 0 },
          "pt-BR": { code: "pt-BR", name: "Portuguese (Brazil)", native: "Português", flag: "🇧🇷", translatedCount: 0, totalCount: 828, progressPct: 0 },
          "it-IT": { code: "it-IT", name: "Italian", native: "Italiano", flag: "🇮🇹", translatedCount: 0, totalCount: 828, progressPct: 0 },
          "zh-CN": { code: "zh-CN", name: "Chinese (Simplified)", native: "简体中文", flag: "🇨🇳", translatedCount: 0, totalCount: 828, progressPct: 0 },
          "ar-SA": { code: "ar-SA", name: "Arabic", native: "العربية", flag: "🇸🇦", translatedCount: 0, totalCount: 828, progressPct: 0 }
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
      if (langCode) {
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

    navHomeBtn.style.display = viewName === 'home' ? 'none' : 'inline-flex';
    navExportBtn.style.display = viewName === 'workspace' ? 'inline-flex' : 'none';
    navSubmitBtn.style.display = viewName === 'workspace' ? 'inline-flex' : 'none';

    if (viewName === 'home') homeView.classList.add('active');
    if (viewName === 'workspace') workspaceView.classList.add('active');
    if (viewName === 'admin') adminView.classList.add('active');
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  // =========================================================================
  // 2. VIEW 1: HOME DASHBOARD & GLOWING RADIAL DONUT GAUGES
  // =========================================================================

  function renderHomeLanguages() {
    if (!catalog || !catalog.languages) return;

    languagesGrid.innerHTML = '';
    const langs = Object.values(catalog.languages).filter(l => l.code !== 'qps-ploc');
    document.getElementById('lang-counter-badge').textContent = `${langs.length} Locales Active`;

    langs.forEach((lang, index) => {
      const localTrans = getStoredTranslations(lang.code);
      const localDoneCount = Object.keys(localTrans).length;
      const effectiveDone = Math.max(lang.translatedCount || 0, localDoneCount);
      const total = catalog.totalStrings || 828;
      const pct = Math.min(100, Math.round((effectiveDone / total) * 100));
      const left = Math.max(0, total - effectiveDone);

      const card = document.createElement('div');
      card.className = 'lang-card-item';
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

      card.innerHTML = `
        <div class="card-top-profile">
          <span class="flag-box">${lang.flag || '🌐'}</span>
          <div class="card-lang-titles">
            <span class="card-lang-name">${lang.name}</span>
            <span class="card-lang-native">${lang.native} • ${lang.code}</span>
          </div>
          <span class="studio-pill ${pct === 100 ? 'pill-translated' : 'pill-missing'}">
            ${pct === 100 ? '✓ Complete' : `${left} left`}
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
                    stroke-dasharray="${circ}" stroke-dashoffset="${circ}"></circle>
          </svg>
          <div class="donut-center-info">
            <span class="donut-pct-text">${pct}%</span>
            <span class="donut-pct-sub">Done</span>
          </div>
        </div>

        <div class="card-stats-footer">
          <span><strong>${effectiveDone}</strong> of ${total} translated</span>
          <span class="action-chip">
            Translate <span class="action-arrow">→</span>
          </span>
        </div>
      `;

      languagesGrid.appendChild(card);

      // Trigger animated donut draw
      requestAnimationFrame(() => {
        setTimeout(() => {
          const valRing = card.querySelector('.donut-val-ring');
          if (valRing) {
            valRing.style.strokeDashoffset = offset;
          }
        }, 120 + index * 40);
      });
    });
  }

  // =========================================================================
  // 3. VIEW 2: TRANSLATION WORKSPACE
  // =========================================================================

  function openWorkspace(langCode) {
    currentLang = langCode;
    const meta = (catalog.languages && catalog.languages[langCode]) || { name: langCode, flag: '🌐', native: langCode };

    document.getElementById('current-lang-flag').textContent = meta.flag || '🌐';
    document.getElementById('current-lang-name').textContent = meta.name || langCode;
    document.getElementById('current-lang-native').textContent = `${meta.native} (${langCode})`;
    document.getElementById('current-lang-code').textContent = langCode;

    currentPage = 1;
    currentFilter = 'all';
    searchQuery = '';
    searchInput.value = '';

    document.querySelectorAll('.filter-tab').forEach(p => p.classList.remove('active'));
    document.querySelector('.filter-tab[data-filter="all"]').classList.add('active');

    renderCards();
    updateWorkspaceHeaderStats();
  }

  function getStoredTranslations(langCode) {
    try {
      const raw = localStorage.getItem(`tunetastic_trans_${langCode}`);
      return raw ? JSON.parse(raw) : {};
    } catch {
      return {};
    }
  }

  function saveStoredTranslation(langCode, key, text) {
    try {
      const trans = getStoredTranslations(langCode);
      if (text && text.trim()) {
        trans[key] = text.trim();
      } else {
        delete trans[key];
      }
      localStorage.setItem(`tunetastic_trans_${langCode}`, JSON.stringify(trans));
      showAutoSaveFeedback();
      updateWorkspaceHeaderStats();
    } catch (err) {
      console.error('LocalStorage write failed:', err);
    }
  }

  function showAutoSaveFeedback() {
    const indicator = document.getElementById('auto-save-indicator');
    if (indicator) {
      indicator.innerHTML = '<span class="save-dot"></span> Saved to Browser';
      indicator.style.opacity = '1';
    }
  }

  function showToast(msg) {
    if (!toastNotification) return;
    toastMessage.textContent = msg;
    toastNotification.classList.add('show');
    clearTimeout(toastNotification._timer);
    toastNotification._timer = setTimeout(() => {
      toastNotification.classList.remove('show');
    }, 2400);
  }

  // =========================================================================
  // 4. SMART CONTEXT PREVIEW & TOKEN REPLACEMENT ENGINE
  // =========================================================================

  function generateSimulatedPreview(templateText, placeholders) {
    if (!templateText) return '';
    let preview = templateText;
    if (placeholders && placeholders.length > 0) {
      placeholders.forEach(ph => {
        const replacement = `<strong class="token-tag">${ph.sample}</strong>`;
        preview = preview.split(ph.token).join(replacement);
      });
    }
    return preview;
  }

  function validateTokens(sourceText, translatedText, placeholders) {
    if (!placeholders || placeholders.length === 0) return { isValid: true, missing: [] };
    const missing = [];
    placeholders.forEach(ph => {
      if (!translatedText.includes(ph.token)) {
        missing.push(ph.token);
      }
    });
    return { isValid: missing.length === 0, missing };
  }

  function renderCards() {
    if (!catalog || !catalog.items) return;

    const storedTrans = getStoredTranslations(currentLang);
    const filteredItems = catalog.items.filter(item => {
      const currentVal = storedTrans[item.key] || '';
      const isDone = Boolean(currentVal && currentVal.trim());

      // Filter tabs
      if (currentFilter === 'missing' && isDone) return false;
      if (currentFilter === 'completed' && !isDone) return false;
      if (currentFilter === 'tokens' && (!item.placeholders || item.placeholders.length === 0)) return false;
      if (currentFilter === 'humor' && !item.isHumor) return false;

      // Search box
      if (searchQuery) {
        const q = searchQuery.toLowerCase();
        const matchesKey = item.key.toLowerCase().includes(q);
        const matchesSource = item.source.toLowerCase().includes(q);
        const matchesTrans = currentVal.toLowerCase().includes(q);
        const matchesComment = (item.comment || '').toLowerCase().includes(q);
        if (!matchesKey && !matchesSource && !matchesTrans && !matchesComment) return false;
      }

      return true;
    });

    // Pagination
    const totalPages = Math.max(1, Math.ceil(filteredItems.length / PAGE_SIZE));
    if (currentPage > totalPages) currentPage = totalPages;
    const startIndex = (currentPage - 1) * PAGE_SIZE;
    const pageItems = filteredItems.slice(startIndex, startIndex + PAGE_SIZE);

    paginationInfo.textContent = `Page ${currentPage} of ${totalPages} (${filteredItems.length} matching)`;
    pagePrevBtn.disabled = currentPage === 1;
    pageNextBtn.disabled = currentPage === totalPages;

    cardsContainer.innerHTML = '';

    if (pageItems.length === 0) {
      cardsContainer.innerHTML = `
        <div style="text-align: center; padding: 64px 20px; color: var(--text-secondary); background: var(--surface-glass); border-radius: var(--radius-xl); border: 1px solid var(--border-glass);">
          🔍 No strings match your current filter or search criteria.
        </div>
      `;
      return;
    }

    pageItems.forEach((item, index) => {
      const translation = storedTrans[item.key] || '';
      const hasPlaceholders = item.placeholders && item.placeholders.length > 0;
      const sourcePreview = generateSimulatedPreview(item.source, item.placeholders);
      const tokenStatus = validateTokens(item.source, translation, item.placeholders);

      const card = document.createElement('div');
      card.className = `studio-card ${item.isHumor ? 'humor-card' : ''}`;
      card.id = `card-${item.key}`;
      card.style.setProperty('--i', index);

      card.innerHTML = `
        <div class="studio-card-header">
          <span class="key-badge-mono">${item.key}</span>
          <div class="card-tags">
            ${item.isHumor ? '<span class="studio-pill pill-humor">🎭 Cultural Humor / Quip</span>' : ''}
            ${hasPlaceholders ? `<span class="studio-pill ${tokenStatus.isValid ? 'pill-tokens' : 'pill-error'}" id="token-badge-${item.key}">Tokens (${item.placeholders.map(p => p.token).join('')})</span>` : ''}
            <span class="studio-pill ${translation ? 'pill-translated' : 'pill-missing'}" id="status-badge-${item.key}">
              ${translation ? '✓ Translated' : '○ Needs Translation'}
            </span>
          </div>
        </div>

        ${item.comment ? `<div class="breadcrumb-path">📍 ${escapeHtml(item.comment)}</div>` : ''}

        <div class="editors-split-grid">
          <div class="editor-column">
            <div class="column-header">
              <span>English (Source)</span>
              <button class="copy-mini-btn" title="Copy to clipboard">📋 Copy</button>
            </div>
            <div class="source-display-box">${escapeHtml(item.source)}</div>
          </div>

          <div class="editor-column">
            <div class="column-header">
              <span>${currentLang} Translation</span>
              <span id="char-count-${item.key}">${translation.length} chars</span>
            </div>
            <textarea class="translation-textarea" placeholder="Type ${currentLang} translation here...">${escapeHtml(translation)}</textarea>
          </div>
        </div>

        ${hasPlaceholders ? `
          <div class="in-app-preview-flyout">
            <div class="preview-flyout-top">
              <span class="preview-badge-title">
                <span class="preview-wave-icon"><span></span><span></span><span></span></span>
                Live Windows 11 In-App Context Preview
              </span>
              <span style="font-size: 11px; color: var(--text-tertiary);">${item.placeholders.map(p => `${p.token}: ${p.label}`).join(' • ')}</span>
            </div>
            <div class="preview-line">
              <span style="color: var(--text-tertiary); font-size: 12px; margin-right: 4px;">English:</span> ${sourcePreview}
            </div>
            <div class="preview-line" id="live-trans-preview-${item.key}">
              <span style="color: var(--text-tertiary); font-size: 12px; margin-right: 4px;">${currentLang}:</span> ${translation ? generateSimulatedPreview(translation, item.placeholders) : '<span style="color: var(--text-tertiary); font-style: italic;">(Type translation above to preview in-app render)</span>'}
            </div>
            <div class="token-missing-alert" id="token-warning-${item.key}" style="display: ${tokenStatus.isValid ? 'none' : 'flex'};">
              ⚠️ Missing placeholder token: <strong>${tokenStatus.missing.join(', ')}</strong>
            </div>
          </div>
        ` : ''}

        ${item.isHumor ? `
          <div class="transcreation-hint-card">
            <span style="font-size: 18px;">💡</span>
            <div>
              <strong>Cultural Humor Note:</strong> This is a witty/humorous empty-state quote. Do not translate literally; feel free to adapt it with a natural musical joke, pun, or warm advice native to your culture!
            </div>
          </div>
        ` : ''}
      `;

      // Copy Button Action with Bounce & Feedback
      const copyBtn = card.querySelector('.copy-mini-btn');
      copyBtn.onclick = () => {
        navigator.clipboard.writeText(item.source);
        copyBtn.innerHTML = '✓ Copied!';
        copyBtn.classList.add('copied');
        showToast(`Copied English source text!`);
        setTimeout(() => {
          copyBtn.innerHTML = '📋 Copy';
          copyBtn.classList.remove('copied');
        }, 1600);
      };

      // Real-time Textarea Typing Handler with Reactive Preview Glow
      const textarea = card.querySelector('.translation-textarea');
      textarea.addEventListener('input', (e) => {
        const val = e.target.value;
        saveStoredTranslation(currentLang, item.key, val);

        document.getElementById(`char-count-${item.key}`).textContent = `${val.length} chars`;

        const statusBadge = document.getElementById(`status-badge-${item.key}`);
        if (val.trim()) {
          statusBadge.textContent = '✓ Translated';
          statusBadge.className = 'studio-pill pill-translated';
        } else {
          statusBadge.textContent = '○ Needs Translation';
          statusBadge.className = 'studio-pill pill-missing';
        }

        if (hasPlaceholders) {
          const liveFlyout = card.querySelector('.in-app-preview-flyout');
          if (liveFlyout) {
            liveFlyout.classList.add('preview-active');
            clearTimeout(liveFlyout._activeTimer);
            liveFlyout._activeTimer = setTimeout(() => {
              liveFlyout.classList.remove('preview-active');
            }, 700);
          }

          const liveBox = document.getElementById(`live-trans-preview-${item.key}`);
          const tokenWarn = document.getElementById(`token-warning-${item.key}`);
          const tokenBadge = document.getElementById(`token-badge-${item.key}`);
          const check = validateTokens(item.source, val, item.placeholders);

          liveBox.innerHTML = `<span style="color: var(--text-tertiary); font-size: 12px; margin-right: 4px;">${currentLang}:</span> ${val ? generateSimulatedPreview(val, item.placeholders) : '<span style="color: var(--text-tertiary); font-style: italic;">(Type translation above to preview in-app render)</span>'}`;

          if (check.isValid) {
            tokenWarn.style.display = 'none';
            tokenBadge.className = 'studio-pill pill-tokens';
          } else {
            tokenWarn.style.display = 'flex';
            tokenWarn.innerHTML = `⚠️ Missing placeholder token: <strong>${check.missing.join(', ')}</strong>`;
            tokenBadge.className = 'studio-pill pill-error';
          }
        }
      });

      cardsContainer.appendChild(card);
    });
  }

  function updateWorkspaceHeaderStats() {
    if (!catalog || !catalog.items) return;

    const storedTrans = getStoredTranslations(currentLang);
    const total = catalog.items.length;
    const done = Object.values(storedTrans).filter(v => Boolean(v && v.trim())).length;
    const missing = Math.max(0, total - done);
    const pct = Math.round((done / total) * 100);

    const humorCount = catalog.items.filter(i => i.isHumor).length;
    const tokenCount = catalog.items.filter(i => i.placeholders && i.placeholders.length > 0).length;

    document.getElementById('workspace-progress-fill').style.width = `${pct}%`;
    document.getElementById('ws-pct-label').textContent = `${pct}% Complete`;
    document.getElementById('ws-done-count').textContent = `${done}`;
    document.getElementById('ws-missing-count').textContent = `${missing}`;
    document.getElementById('ws-humor-count').textContent = `${humorCount}`;
    if (document.getElementById('ws-tokens-count')) {
      document.getElementById('ws-tokens-count').textContent = `${tokenCount}`;
    }

    document.getElementById('count-all').textContent = total;
    document.getElementById('count-missing').textContent = missing;
    document.getElementById('count-done').textContent = done;
    document.getElementById('count-tokens').textContent = tokenCount;
    document.getElementById('count-humor').textContent = humorCount;
  }

  // =========================================================================
  // 5. VIEW 3: ADMIN REVIEW QUEUE
  // =========================================================================

  async function loadAdminQueue() {
    const container = document.getElementById('admin-submissions-container');
    container.innerHTML = `<div style="text-align: center; padding: 48px; color: var(--text-secondary);"><div class="loading-spinner"></div>Connecting to staging storage...</div>`;

    try {
      const res = await fetch(`${WORKER_URL}/api/pending`);
      if (!res.ok) throw new Error(`HTTP ${res.status}`);
      const data = await res.json();
      const submissions = data.submissions || [];

      document.getElementById('admin-pending-badge').textContent = `${submissions.length} Submissions Staged`;

      if (submissions.length === 0) {
        container.innerHTML = `
          <div style="text-align: center; padding: 48px; background: var(--surface-glass); border-radius: var(--radius-xl); border: 1px solid var(--border-glass); color: var(--text-secondary);">
            🎉 The staging queue is completely clear! No community submissions are pending review.
          </div>
        `;
        return;
      }

      container.innerHTML = '';
      submissions.forEach(sub => {
        const card = document.createElement('div');
        card.className = 'studio-card';

        const keys = Object.keys(sub.translations || {});
        let itemsHtml = '';
        keys.forEach(k => {
          itemsHtml += `
            <div style="border-bottom: 1px solid var(--border-glass); padding: 12px 0; display: grid; grid-template-columns: 1fr 1fr; gap: 14px; align-items: center;">
              <div>
                <strong style="color: var(--accent-cyan); font-family: var(--font-mono); font-size: 13px;">${k}</strong>
                <div style="font-size: 13px; color: var(--text-secondary); margin-top: 2px;">Proposed: "${escapeHtml(sub.translations[k])}"</div>
              </div>
              <input type="text" class="form-input" value="${escapeHtml(sub.translations[k])}" id="edit-${sub.id}-${k}">
            </div>
          `;
        });

        card.innerHTML = `
          <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 14px; border-bottom: 1px solid var(--border-glass); padding-bottom: 12px;">
            <div>
              <strong style="font-size: 18px;">🌐 ${sub.language}</strong>
              <span style="color: var(--text-secondary); font-size: 13px; margin-left: 10px;">Submitted by <strong>@${sub.contributor}</strong> (${keys.length} strings)</span>
            </div>
            <button class="btn btn-primary-gradient approve-btn" data-id="${sub.id}">✅ Approve Batch</button>
          </div>
          <div style="display: flex; flex-direction: column;">
            ${itemsHtml}
          </div>
        `;

        card.querySelector('.approve-btn').onclick = async () => {
          await approveSubmission(sub, card);
        };

        container.appendChild(card);
      });
    } catch (err) {
      container.innerHTML = `
        <div style="padding: 28px; background: var(--surface-glass); border-radius: var(--radius-xl); border: 1px solid var(--border-glass);">
          <h4 style="color: var(--status-warning); font-size: 16px; margin-bottom: 8px;">Staging Worker Endpoint Offline</h4>
          <p style="font-size: 13.5px; color: var(--text-secondary); margin-bottom: 14px; line-height: 1.5;">
            Could not reach <code>${WORKER_URL}</code>. Deploy the Cloudflare worker from <code>cloudflare-worker/</code> or test locally.
          </p>
          <div style="font-size: 13px; background: rgba(0,0,0,0.4); padding: 14px; border-radius: var(--radius-md); border-left: 3px solid var(--accent-cyan);">
            <strong>Local Demonstration Mode:</strong> All your translations in this session are safely stored in your browser's local cache!
          </div>
        </div>
      `;
    }
  }

  async function approveSubmission(sub, cardElement) {
    const authPassword = prompt("Enter Maintainer Password:", "");
    if (!authPassword) return;

    try {
      const res = await fetch(`${WORKER_URL}/api/approve`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${authPassword}`
        },
        body: JSON.stringify({
          language: sub.language,
          approvedTranslations: sub.translations,
          removeSubmissionIds: [sub.id]
        })
      });

      if (res.ok) {
        cardElement.style.opacity = '0.5';
        cardElement.innerHTML = `<div style="padding: 20px; color: var(--status-success); font-weight: 700;">✅ Approved! Ready to sync to git on your next manual GitHub Actions run.</div>`;
        showToast("Batch approved into staging!");
      } else {
        alert("Authorization failed or server error.");
      }
    } catch (err) {
      alert("Could not reach staging server: " + err.message);
    }
  }

  // =========================================================================
  // 6. SUBMISSION & EXPORT FLOW
  // =========================================================================

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

    // Custom Lang Modal
    document.getElementById('add-custom-lang-btn').onclick = () => { customLangModal.classList.add('active'); };
    document.getElementById('custom-lang-cancel-btn').onclick = () => { customLangModal.classList.remove('active'); };
    document.getElementById('custom-lang-confirm-btn').onclick = () => {
      const code = document.getElementById('custom-lang-code-input').value.trim();
      const name = document.getElementById('custom-lang-name-input').value.trim() || code;
      if (code) {
        if (!catalog.languages) catalog.languages = {};
        catalog.languages[code] = { code, name, native: name, flag: '🌐', translatedCount: 0, totalCount: catalog.totalStrings || 828, progressPct: 0 };
        customLangModal.classList.remove('active');
        window.location.hash = `#translate/${code}`;
      }
    };

    // Filter pills
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

  function openSubmitModal() {
    const stored = getStoredTranslations(currentLang);
    const count = Object.keys(stored).length;
    if (count === 0) {
      alert("You haven't translated any strings yet for this language!");
      return;
    }
    document.getElementById('modal-submit-lang').textContent = currentLang;
    document.getElementById('modal-submit-count').textContent = count;
    submitModal.classList.add('active');
  }

  async function submitTranslations() {
    const author = document.getElementById('submit-author-input').value.trim() || 'Community Member';
    const stored = getStoredTranslations(currentLang);
    const count = Object.keys(stored).length;

    const btn = document.getElementById('modal-confirm-submit-btn');
    btn.disabled = true;
    btn.textContent = 'Submitting...';

    try {
      const res = await fetch(`${WORKER_URL}/api/submit`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          language: currentLang,
          contributor: author,
          translations: stored
        })
      });

      if (res.ok) {
        showToast(`🎉 Submitted ${count} strings for ${currentLang}!`);
        submitModal.classList.remove('active');
      } else {
        throw new Error(`Server returned ${res.status}`);
      }
    } catch (err) {
      console.warn("Could not reach Cloudflare Worker, falling back to local export prompt:", err);
      if (confirm(`Could not connect to live staging server (${err.message}).\nWould you like to download your completed Resources.resw file instead?`)) {
        exportReswFile();
        submitModal.classList.remove('active');
      }
    } finally {
      btn.disabled = false;
      btn.textContent = 'Send to Staging';
    }
  }

  function exportReswFile() {
    if (!catalog || !catalog.items) return;
    const stored = getStoredTranslations(currentLang);

    const xmlLines = [
      '<?xml version=\'1.0\' encoding=\'utf-8\'?>',
      '<root>',
      '  <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>',
      '  <resheader name="version"><value>2.0</value></resheader>',
      '  <resheader name="reader"><value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>',
      '  <resheader name="writer"><value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value></resheader>'
    ];

    catalog.items.forEach(item => {
      const val = stored[item.key] || '';
      if (val) {
        const escapedVal = escapeXml(val);
        const escapedComment = item.comment ? escapeXml(item.comment) : '';
        xmlLines.push(`  <data name="${escapeXml(item.key)}" xml:space="preserve">`);
        xmlLines.push(`    <value>${escapedVal}</value>`);
        if (escapedComment) {
          xmlLines.push(`    <comment>${escapedComment}</comment>`);
        }
        xmlLines.push(`  </data>`);
      }
    });

    xmlLines.push('</root>\n');

    const blob = new Blob([xmlLines.join('\n')], { type: 'application/xml;charset=utf-8' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = `Resources_${currentLang || 'strings'}.resw`;
    a.click();
    URL.revokeObjectURL(a.href);
    showToast("Downloaded Resources.resw!");
  }

  // Utilities
  function escapeHtml(str) {
    if (!str) return '';
    return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
  }

  function escapeXml(str) {
    if (!str) return '';
    return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&apos;');
  }

  // Run
  document.addEventListener('DOMContentLoaded', init);

})();
