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
  // 1. STATE & ANONYMOUS SESSION MANAGEMENT (MULTI-TAB & INCOGNITO COMPATIBLE)
  // -------------------------------------------------------------------------

  const memoryStore = {};

  function safeStorageGet(key, preferSession = false) {
    try {
      if (isIncognitoMode && key.startsWith('tunetastic_l10n_')) {
        return memoryStore[key] || null;
      }
      if (preferSession && typeof window.sessionStorage !== 'undefined') {
        const val = window.sessionStorage.getItem(key);
        if (val !== null) return val;
      }
      if (typeof window.localStorage !== 'undefined') {
        const val = window.localStorage.getItem(key);
        if (val !== null) return val;
      }
      return memoryStore[key] || null;
    } catch {
      return memoryStore[key] || null;
    }
  }

  function safeStorageSet(key, value, preferSession = false) {
    try {
      if (isIncognitoMode && key.startsWith('tunetastic_l10n_')) {
        memoryStore[key] = String(value);
        return;
      }
      if (preferSession && typeof window.sessionStorage !== 'undefined') {
        window.sessionStorage.setItem(key, value);
      }
      if (typeof window.localStorage !== 'undefined') {
        window.localStorage.setItem(key, value);
      }
    } catch {
      memoryStore[key] = String(value);
    }
  }

  function safeStorageRemove(key) {
    try {
      if (typeof window.sessionStorage !== 'undefined') {
        window.sessionStorage.removeItem(key);
      }
      if (typeof window.localStorage !== 'undefined') {
        window.localStorage.removeItem(key);
      }
    } catch {
      delete memoryStore[key];
    }
  }

  function getOrCreateSessionId() {
    // Check sessionStorage first: ensures separate incognito tabs/windows each get their own distinct session!
    let sid = null;
    try {
      sid = window.sessionStorage.getItem('tunetastic_session_id');
      if (!sid) {
        sid = 's_' + (crypto.randomUUID ? crypto.randomUUID() : (Math.random().toString(36).substring(2) + Date.now().toString(36)));
        window.sessionStorage.setItem('tunetastic_session_id', sid);
      }
    } catch {
      try {
        sid = window.localStorage.getItem('tunetastic_session_id');
        if (!sid) {
          sid = 's_' + (crypto.randomUUID ? crypto.randomUUID() : (Math.random().toString(36).substring(2) + Date.now().toString(36)));
          window.localStorage.setItem('tunetastic_session_id', sid);
        }
      } catch {
        sid = 's_' + Math.random().toString(36).substring(2) + Date.now().toString(36);
      }
    }
    return sid;
  }

  const SESSION_ID = getOrCreateSessionId();

  // SVG Icons Replacement (Zero Emojis)
  const ICONS = {
    check: `<svg class="svg-icon" viewBox="0 0 16 16" width="13" height="13" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M3 8.5l3.5 3.5 6.5-7"/></svg>`,
    checkCircle: `<svg class="svg-icon" viewBox="0 0 16 16" width="13" height="13" fill="none" stroke="currentColor" stroke-width="1.4"><circle cx="8" cy="8" r="7"/><path d="M5 8.2l2 2 4-4" stroke-linecap="round" stroke-linejoin="round"/></svg>`,
    globe: `<svg class="svg-icon svg-globe" viewBox="0 0 16 16" width="14" height="14" fill="none" stroke="currentColor" stroke-width="1.3"><circle cx="8" cy="8" r="7"/><path d="M1 8h14M8 1c2.2 2 3.5 4.3 3.5 7s-1.3 5-3.5 7c-2.2-2-3.5-4.3-3.5-7s1.3-5 3.5-7z"/></svg>`,
    edit: `<svg class="svg-icon" viewBox="0 0 16 16" width="12" height="12" fill="none" stroke="currentColor" stroke-width="1.3"><path d="M11.5 1.5l3 3L5 14H2v-3l9.5-9.5z"/></svg>`,
    clock: `<svg class="svg-icon" viewBox="0 0 16 16" width="12" height="12" fill="none" stroke="currentColor" stroke-width="1.3"><circle cx="8" cy="8" r="7"/><path d="M8 4v4.5l3 1.5" stroke-linecap="round"/></svg>`,
    layers: `<svg class="svg-icon" viewBox="0 0 16 16" width="12" height="12" fill="none" stroke="currentColor" stroke-width="1.3"><path d="M8 1L1 5l7 4 7-4-7-4zM1 8.5l7 4 7-4M1 12l7 4 7-4"/></svg>`,
    bulb: `<svg class="svg-icon" viewBox="0 0 16 16" width="12" height="12" fill="none" stroke="currentColor" stroke-width="1.3"><path d="M8 1.5a4.5 4.5 0 0 0-4.5 4.5c0 2 1.2 3.4 2.2 4.5h4.6c1-1.1 2.2-2.5 2.2-4.5A4.5 4.5 0 0 0 8 1.5zM6 13h4M6.5 14.5h3"/></svg>`,
    lock: `<svg class="svg-icon" viewBox="0 0 16 16" width="12" height="12" fill="currentColor"><path d="M12 6V4a4 4 0 0 0-8 0v2H2v9a1 1 0 0 0 1 1h10a1 1 0 0 0 1-1V6h-2zM6 4a2 2 0 1 1 4 0v2H6V4zm2 8a1.5 1.5 0 1 1 0-3 1.5 1.5 0 0 1 0 3z"/></svg>`,
    sparkle: `<svg class="svg-icon" viewBox="0 0 16 16" width="12" height="12" fill="currentColor"><path d="M8 0l1.8 5.2L15 7l-5.2 1.8L8 14l-1.8-5.2L1 7l5.2-1.8L8 0z"/></svg>`,
    copy: `<svg class="svg-icon" viewBox="0 0 16 16" width="12" height="12" fill="none" stroke="currentColor" stroke-width="1.3"><rect x="5" y="5" width="9" height="9" rx="1.5"/><path d="M11 5V3a1.5 1.5 0 0 0-1.5-1.5H3A1.5 1.5 0 0 0 1.5 3v6.5A1.5 1.5 0 0 0 3 11h2"/></svg>`,
    comment: `<svg class="svg-icon" viewBox="0 0 16 16" width="12" height="12" fill="none" stroke="currentColor" stroke-width="1.3"><path d="M2 3a2 2 0 0 1 2-2h8a2 2 0 0 1 2 2v6a2 2 0 0 1-2 2H5.5L2 14.5V3z"/></svg>`,
    alert: `<svg class="svg-icon" viewBox="0 0 16 16" width="13" height="13" fill="currentColor"><path d="M7.1 1.5c.4-.7 1.4-.7 1.8 0l6.7 11.6c.4.7-.1 1.6-.9 1.6H1.3c-.8 0-1.3-.9-.9-1.6L7.1 1.5zM8 5a.8.8 0 0 0-.8.8v3.4a.8.8 0 0 0 1.6 0V5.8A.8.8 0 0 0 8 5zm0 6.6a.9.9 0 1 0 0 1.8.9.9 0 0 0 0-1.8z"/></svg>`,
    search: `<svg class="svg-icon" viewBox="0 0 16 16" width="14" height="14" fill="none" stroke="currentColor" stroke-width="1.4"><circle cx="7" cy="7" r="5"/><path d="M11 11l3.5 3.5" stroke-linecap="round"/></svg>`,
    rocket: `<svg class="svg-icon" viewBox="0 0 16 16" width="14" height="14" fill="currentColor"><path d="M9.5.5C8 2 7.7 4.2 8.3 5.7L4.1 9.9c-.3.3-.4.8-.2 1.2l1 2c.2.4.6.6 1 .5l2.2-.4 4.2-4.2c1.5.6 3.7.3 5.2-1.2 0 0 .5-2-1.2-3.7S12.6.5 9.5.5zM3.4 12.6l-2 2c-.4.4-.4 1 0 1.4.4.4 1 .4 1.4 0l2-2-1.4-1.4z"/></svg>`,
    arrowLeft: `<svg class="svg-icon" viewBox="0 0 16 16" width="12" height="12" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M10 13L5 8l5-5"/></svg>`,
    arrowRight: `<svg class="svg-icon" viewBox="0 0 16 16" width="12" height="12" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M6 3l5 5-5 5"/></svg>`
  };

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
  let isIncognitoMode = false;

  // Elements
  const homeView = document.getElementById('home-view');
  const workspaceView = document.getElementById('workspace-view');
  const adminView = document.getElementById('admin-view');
  const navHomeBtn = document.getElementById('nav-home-btn');
  const languagesGrid = document.getElementById('languages-grid');
  const cardsContainer = document.getElementById('cards-container');
  const searchInput = document.getElementById('search-input');
  const paginationInfo = document.getElementById('pagination-info');
  const pagePrevBtn = document.getElementById('page-prev-btn');
  const pageNextBtn = document.getElementById('page-next-btn');
  const toastNotification = document.getElementById('toast-notification');
  const toastMessage = document.getElementById('toast-message');

  // Modals & Centered Dialogs
  const submitModal = document.getElementById('submit-modal');
  const suggestModal = document.getElementById('suggest-modal');
  const draftResumeModal = document.getElementById('draft-resume-modal');
  const draftModalMainView = document.getElementById('draft-modal-main-view');
  const draftModalConfirmView = document.getElementById('draft-modal-confirm-view');

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
    "pt-BR": { name: "Portuguese", native: "Português", country: "br" },
    "it-IT": { name: "Italian", native: "Italiano", country: "it" },
    "zh-CN": { name: "Chinese", native: "简体中文", country: "cn" },
    "ar-SA": { name: "Arabic", native: "العربية", country: "sa", rtl: true }
  };

  function formatLanguageDisplayName(meta, langCode) {
    if (!meta) return langCode || '';
    const native = meta.native ? meta.native.trim() : '';
    const name = meta.name ? meta.name.trim() : '';
    const code = meta.code || langCode || '';

    if (native && name && native.toLowerCase() !== name.toLowerCase()) {
      return `${native} (${name})`;
    }
    return native || name || code;
  }

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
        flagHtml: `<img src="https://flagcdn.com/64x48/${k.country}.png" srcset="https://flagcdn.com/128x96/${k.country}.png 2x" width="42" height="30" alt="${k.name}" class="lang-flag-img">`
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
      flagHtml: `<img src="https://flagcdn.com/64x48/${country}.png" srcset="https://flagcdn.com/128x96/${country}.png 2x" width="42" height="30" alt="${name}" class="lang-flag-img">`
    };
  }

  // -------------------------------------------------------------------------
  // 3. REGION & LOCALE AUTO-DETECTION (Zero Permissions)
  // -------------------------------------------------------------------------

  const TIMEZONE_TO_LOCALE = {
    // Hindi (India)
    'Asia/Kolkata': 'hi-IN',
    'Asia/Calcutta': 'hi-IN',

    // Spanish (Spain & regions)
    'Europe/Madrid': 'es-ES',
    'Atlantic/Canary': 'es-ES',
    'Africa/Ceuta': 'es-ES',

    // German (Germany, Austria, Switzerland)
    'Europe/Berlin': 'de-DE',
    'Europe/Busingen': 'de-DE',
    'Europe/Vienna': 'de-DE',
    'Europe/Zurich': 'de-DE',

    // French (France, Belgium, Monaco)
    'Europe/Paris': 'fr-FR',
    'Europe/Brussels': 'fr-FR',
    'Europe/Monaco': 'fr-FR',

    // Japanese (Japan)
    'Asia/Tokyo': 'ja-JP',

    // Russian (Russia & regions)
    'Europe/Moscow': 'ru-RU',
    'Europe/Kaliningrad': 'ru-RU',
    'Europe/Samara': 'ru-RU',
    'Europe/Volgograd': 'ru-RU',
    'Europe/Kirov': 'ru-RU',
    'Europe/Astrakhan': 'ru-RU',
    'Europe/Saratov': 'ru-RU',
    'Europe/Ulyanovsk': 'ru-RU',
    'Asia/Yekaterinburg': 'ru-RU',
    'Asia/Omsk': 'ru-RU',
    'Asia/Novosibirsk': 'ru-RU',
    'Asia/Barnaul': 'ru-RU',
    'Asia/Tomsk': 'ru-RU',
    'Asia/Novokuznetsk': 'ru-RU',
    'Asia/Krasnoyarsk': 'ru-RU',
    'Asia/Irkutsk': 'ru-RU',
    'Asia/Chita': 'ru-RU',
    'Asia/Yakutsk': 'ru-RU',
    'Asia/Khandyga': 'ru-RU',
    'Asia/Vladivostok': 'ru-RU',
    'Asia/Ust-Nera': 'ru-RU',
    'Asia/Magadan': 'ru-RU',
    'Asia/Sakhalin': 'ru-RU',
    'Asia/Srednekolymsk': 'ru-RU',
    'Asia/Kamchatka': 'ru-RU',
    'Asia/Anadyr': 'ru-RU',

    // Portuguese (Brazil, Portugal)
    'America/Sao_Paulo': 'pt-BR',
    'America/Noronha': 'pt-BR',
    'America/Belem': 'pt-BR',
    'America/Fortaleza': 'pt-BR',
    'America/Recife': 'pt-BR',
    'America/Araguaina': 'pt-BR',
    'America/Maceio': 'pt-BR',
    'America/Bahia': 'pt-BR',
    'America/Santarem': 'pt-BR',
    'America/Campo_Grande': 'pt-BR',
    'America/Cuiaba': 'pt-BR',
    'America/Porto_Velho': 'pt-BR',
    'America/Boa_Vista': 'pt-BR',
    'America/Manaus': 'pt-BR',
    'America/Eirunepe': 'pt-BR',
    'America/Rio_Branco': 'pt-BR',
    'Europe/Lisbon': 'pt-BR',
    'Atlantic/Azores': 'pt-BR',
    'Atlantic/Madeira': 'pt-BR',

    // Italian (Italy, San Marino, Vatican)
    'Europe/Rome': 'it-IT',
    'Europe/San_Marino': 'it-IT',
    'Europe/Vatican': 'it-IT',

    // Chinese (China, Hong Kong, Taiwan)
    'Asia/Shanghai': 'zh-CN',
    'Asia/Urumqi': 'zh-CN',
    'Asia/Chongqing': 'zh-CN',
    'Asia/Harbin': 'zh-CN',
    'Asia/Hong_Kong': 'zh-CN',
    'Asia/Taipei': 'zh-CN',

    // Arabic (Saudi Arabia, UAE, Gulf & Middle East)
    'Asia/Riyadh': 'ar-SA',
    'Asia/Dubai': 'ar-SA',
    'Asia/Kuwait': 'ar-SA',
    'Asia/Qatar': 'ar-SA',
    'Asia/Bahrain': 'ar-SA',
    'Asia/Muscat': 'ar-SA',
    'Africa/Cairo': 'ar-SA',
    'Asia/Amman': 'ar-SA',
    'Asia/Beirut': 'ar-SA',
    'Asia/Baghdad': 'ar-SA'
  };

  const REGION_TO_LOCALE = {
    'IN': 'hi-IN',
    'ES': 'es-ES',
    'DE': 'de-DE',
    'AT': 'de-DE',
    'CH': 'de-DE',
    'FR': 'fr-FR',
    'BE': 'fr-FR',
    'JP': 'ja-JP',
    'RU': 'ru-RU',
    'BY': 'ru-RU',
    'KZ': 'ru-RU',
    'BR': 'pt-BR',
    'PT': 'pt-BR',
    'IT': 'it-IT',
    'CN': 'zh-CN',
    'HK': 'zh-CN',
    'TW': 'zh-CN',
    'SA': 'ar-SA',
    'AE': 'ar-SA',
    'KW': 'ar-SA',
    'QA': 'ar-SA',
    'BH': 'ar-SA',
    'OM': 'ar-SA',
    'EG': 'ar-SA'
  };

  const LANG_PREFIX_TO_LOCALE = {
    'hi': 'hi-IN',
    'es': 'es-ES',
    'de': 'de-DE',
    'fr': 'fr-FR',
    'ja': 'ja-JP',
    'ru': 'ru-RU',
    'pt': 'pt-BR',
    'it': 'it-IT',
    'zh': 'zh-CN',
    'ar': 'ar-SA'
  };

  function detectUserLocaleCode(availableCodes) {
    const codeSet = new Set(availableCodes);

    // 1. Inspect navigator.languages & navigator.language (Zero Permission)
    const browserLangs = (navigator.languages && navigator.languages.length)
      ? navigator.languages
      : [navigator.language || ''];

    for (const raw of browserLangs) {
      if (!raw) continue;
      const normalized = raw.trim();

      // Exact BCP-47 match
      for (const code of codeSet) {
        if (code.toLowerCase() === normalized.toLowerCase()) {
          return code;
        }
      }

      // Language subtag match (e.g. 'hi' -> 'hi-IN')
      const prefix = normalized.split('-')[0].toLowerCase();
      if (LANG_PREFIX_TO_LOCALE[prefix] && codeSet.has(LANG_PREFIX_TO_LOCALE[prefix])) {
        return LANG_PREFIX_TO_LOCALE[prefix];
      }

      // Region subtag match (e.g. 'en-IN' -> region 'IN' -> 'hi-IN')
      const parts = normalized.split('-');
      if (parts.length > 1) {
        const region = parts[1].toUpperCase();
        if (REGION_TO_LOCALE[region] && codeSet.has(REGION_TO_LOCALE[region])) {
          return REGION_TO_LOCALE[region];
        }
      }
    }

    // 2. Inspect IANA System Timezone via Intl API (Zero Permission)
    try {
      const tz = Intl.DateTimeFormat().resolvedOptions().timeZone;
      if (tz && TIMEZONE_TO_LOCALE[tz] && codeSet.has(TIMEZONE_TO_LOCALE[tz])) {
        return TIMEZONE_TO_LOCALE[tz];
      }
    } catch (e) {
      // Ignore if Intl timezone unavailable
    }

    return null;
  }

  function shuffleArray(array) {
    const arr = [...array];
    for (let i = arr.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [arr[i], arr[j]] = [arr[j], arr[i]];
    }
    return arr;
  }

  let sessionHomeLanguagesOrder = null;

  function getPrioritizedHomeLanguages(rawLangs) {
    if (sessionHomeLanguagesOrder && sessionHomeLanguagesOrder.length === rawLangs.length) {
      return sessionHomeLanguagesOrder;
    }

    const availableCodes = rawLangs.map(l => l.code);
    const detectedCode = detectUserLocaleCode(availableCodes);

    if (detectedCode) {
      const primary = rawLangs.find(l => l.code === detectedCode);
      const others = shuffleArray(rawLangs.filter(l => l.code !== detectedCode));
      sessionHomeLanguagesOrder = [primary, ...others];
    } else {
      sessionHomeLanguagesOrder = shuffleArray(rawLangs);
    }

    return sessionHomeLanguagesOrder;
  }


  // -------------------------------------------------------------------------
  // 3. INITIALIZATION & ROUTING
  // -------------------------------------------------------------------------

  async function init() {
    setupEventListeners();
    await checkAndShowIncognitoWarning();
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

  // Immediate active lock cleanup on page navigation / tab close
  function releaseActiveLocksOnExit() {
    if (isIncognitoMode) return;
    const lang = currentLang || safeStorageGet('tunetastic_last_lang');
    if (!lang || !SESSION_ID) return;

    const queryUrl = `${WORKER_URL}/api/drafts/release-lock?lang=${encodeURIComponent(lang)}&sessionId=${encodeURIComponent(SESSION_ID)}&_t=${Date.now()}`;
    const payload = JSON.stringify({
      language: lang,
      sessionId: SESSION_ID
    });

    // Layer 1: Modern fetch with keepalive: true (Outlives tab/window close in Chromium & Incognito)
    try {
      if (typeof fetch === 'function') {
        fetch(queryUrl, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json', 'X-Session-ID': SESSION_ID },
          body: payload,
          keepalive: true,
          mode: 'cors'
        }).catch(() => {});
      }
    } catch (e) {}

    // Layer 2: navigator.sendBeacon
    try {
      if (typeof navigator !== 'undefined' && navigator.sendBeacon) {
        const blob = new Blob([payload], { type: 'text/plain' });
        navigator.sendBeacon(queryUrl, blob);
      }
    } catch (e) {}

    // Layer 3: Image ping fallback (GET request without CORS preflight; works from file:///, localhost, and all Incognito modes)
    try {
      const img = new Image();
      img.src = queryUrl;
    } catch (e) {}
  }

  function showView(viewName) {
    if (viewName !== 'workspace') {
      stopLiveStatusPolling();
      stopDraftTimer();
      releaseActiveLocksOnExit();
    }
    homeView.classList.remove('active');
    workspaceView.classList.remove('active');
    adminView.classList.remove('active');

    navHomeBtn.style.display = 'none';

    cachedStickyGroup = document.querySelector('.workspace-sticky-group');
    cachedHeroCard = document.querySelector('.workspace-hero-card');
    cachedCtrlPanel = document.querySelector('.control-panel');
    if (cachedStickyGroup) cachedStickyGroup.classList.remove('is-condensed');
    if (cachedHeroCard) cachedHeroCard.classList.remove('is-condensed');
    if (cachedCtrlPanel) cachedCtrlPanel.classList.remove('is-hero-condensed');
    if (draftResumeModal) draftResumeModal.classList.remove('active');

    if (viewName === 'home') {
      homeView.classList.add('active');
    } else if (viewName === 'workspace') {
      workspaceView.classList.add('active');
      navHomeBtn.style.display = 'inline-flex';
    } else if (viewName === 'admin') {
      adminView.classList.add('active');
      navHomeBtn.style.display = 'inline-flex';
    }
    window.scrollTo({ top: 0, behavior: 'smooth' });
  }

  function syncNavLayoutMetrics() {
    const nav = document.querySelector('.nav-bar');
    if (nav) {
      const h = nav.offsetHeight || 68;
      document.documentElement.style.setProperty('--nav-height', `${h}px`);
    }
  }

  let isScrollThrottled = false;
  let cachedStickyGroup = null;
  let cachedHeroCard = null;
  let cachedCtrlPanel = null;

  function updateWorkspaceScrollState() {
    isScrollThrottled = false;
    if (!workspaceView || !workspaceView.classList.contains('active')) return;
    if (!cachedStickyGroup || !cachedStickyGroup.isConnected) {
      cachedStickyGroup = document.querySelector('.workspace-sticky-group');
    }
    if (!cachedHeroCard || !cachedHeroCard.isConnected) {
      cachedHeroCard = document.querySelector('.workspace-hero-card');
    }
    if (!cachedCtrlPanel || !cachedCtrlPanel.isConnected) {
      cachedCtrlPanel = document.querySelector('.control-panel');
    }
    if (!cachedHeroCard) return;

    const scrollY = window.scrollY || document.documentElement.scrollTop;
    if (scrollY > 70) {
      if (!cachedHeroCard.classList.contains('is-condensed')) {
        cachedHeroCard.classList.add('is-condensed');
        if (cachedStickyGroup) cachedStickyGroup.classList.add('is-condensed');
        if (cachedCtrlPanel) cachedCtrlPanel.classList.add('is-hero-condensed');
      }
    } else if (scrollY < 20) {
      if (cachedHeroCard.classList.contains('is-condensed')) {
        cachedHeroCard.classList.remove('is-condensed');
        if (cachedStickyGroup) cachedStickyGroup.classList.remove('is-condensed');
        if (cachedCtrlPanel) cachedCtrlPanel.classList.remove('is-hero-condensed');
      }
    }
  }

  function handleWorkspaceScroll() {
    if (!isScrollThrottled) {
      isScrollThrottled = true;
      window.requestAnimationFrame(updateWorkspaceScrollState);
    }
  }

  // -------------------------------------------------------------------------
  // 4. HOME VIEW: DYNAMIC CIRCULAR GAUGES & LANGUAGE CARDS
  // -------------------------------------------------------------------------

  function renderHomeLanguages() {
    languagesGrid.innerHTML = '';
    if (!catalog || !catalog.languages) return;

    const rawLangs = Object.values(catalog.languages).filter(l => l.code !== 'qps-ploc');
    const badge = document.getElementById('lang-counter-badge');
    if (badge) badge.textContent = `${rawLangs.length} Locales Active`;

    // Detected language comes 1st; remaining languages are randomized on each refresh
    const langs = getPrioritizedHomeLanguages(rawLangs);

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

      card.innerHTML = `
        <div class="card-top-profile">
          <span class="flag-box">${meta.flagHtml || ICONS.globe}</span>
          <div class="card-lang-titles">
            <span class="card-lang-name" title="${escapeHtml(formatLanguageDisplayName(meta, lang.code))}">${escapeHtml(formatLanguageDisplayName(meta, lang.code))}</span>
            <span class="card-lang-native">${escapeHtml(lang.code)}</span>
          </div>
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
          <div class="card-strings-badge ${pct === 100 ? 'status-done' : (count > 0 ? 'status-progress' : 'status-empty')}">
            <span class="strings-dot"></span>
            <span class="strings-count"><strong class="highlight-count">${count}</strong><span class="sep-slash">/</span>${total} strings</span>
          </div>
          <span class="action-chip">Translate <span class="action-arrow">${ICONS.arrowRight}</span></span>
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
    safeStorageSet('tunetastic_last_lang', langCode);
    currentPage = 1;
    currentFilter = 'all';
    searchQuery = '';
    if (searchInput) searchInput.value = '';

    const meta = resolveLanguageMeta(langCode) || { name: langCode, native: langCode, flagHtml: ICONS.globe };
    const flagBox = document.getElementById('current-lang-flag');
    if (flagBox) flagBox.innerHTML = meta.flagHtml || ICONS.globe;

    const fullTitle = formatLanguageDisplayName(meta, langCode);
    const langNameEl = document.getElementById('current-lang-name');
    if (langNameEl) langNameEl.textContent = fullTitle;
    const langCodeEl = document.getElementById('current-lang-code');
    if (langCodeEl) langCodeEl.textContent = langCode;

    const stickyGroup = document.querySelector('.workspace-sticky-group');
    const heroCard = document.querySelector('.workspace-hero-card');
    const ctrlPanel = document.querySelector('.control-panel');
    if (stickyGroup) stickyGroup.classList.remove('is-condensed');
    if (heroCard) heroCard.classList.remove('is-condensed');
    if (ctrlPanel) ctrlPanel.classList.remove('is-hero-condensed');
    if (draftResumeModal) draftResumeModal.classList.remove('active');

    if (meta.rtl) {
      document.body.classList.add('rtl-mode');
    } else {
      document.body.classList.remove('rtl-mode');
    }

    // Reset filter buttons active state to 'all'
    document.querySelectorAll('.filter-tab').forEach(p => {
      p.classList.toggle('active', p.getAttribute('data-filter') === 'all');
    });

    // In incognito mode, bypass Cloudflare drafts, locks, and live status polling
    if (!isIncognitoMode) {
      try {
        await Promise.allSettled([
          fetchLiveStatus(),
          checkDraftResumption()
        ]);
      } catch (e) {
        console.warn("Background workspace sync error:", e);
      }
    }

    // Render cards and update stats
    renderCards();
    updateWorkspaceStats();
    updateSaveIndicatorUi();
    if (!isIncognitoMode) {
      startLiveStatusPolling();
    }
    startDraftTimer();
  }

  async function fetchLiveStatus() {
    try {
      const res = await fetch(`${WORKER_URL}/api/status?lang=${currentLang}`, {
        headers: { 'X-Session-ID': SESSION_ID }
      });
      if (res.ok) {
        const data = await res.json();
        liveStatus = {
          locks: data?.locks || {},
          approved: data?.approved || {},
          proposals: data?.proposals || {}
        };
        // Auto-synchronize local submitted records with Cloudflare truth
        syncSubmittedWithServer(currentLang, liveStatus.proposals);
      }
    } catch (e) {
      console.warn("Could not fetch live Cloudflare status:", e);
    }
  }

  function syncSubmittedWithServer(lang, serverProposals) {
    if (!lang || !serverProposals || typeof serverProposals !== 'object') return;
    const subMap = getSubmittedTranslations(lang);
    if (!subMap || Object.keys(subMap).length === 0) return;
    let changed = false;
    for (const key of Object.keys(subMap)) {
      const serverMeta = serverProposals[key];
      // If the server confirmed there are NO active proposals in KV for this key (e.g. wiped or approved)
      if (!serverMeta || !serverMeta.count || serverMeta.count <= 0) {
        delete subMap[key];
        changed = true;
      }
    }
    if (changed) {
      safeStorageSet(`tunetastic_submitted_${lang}`, JSON.stringify(subMap));
    }
  }

  let livePollingTimer = null;
  let isPollingActive = false;

  function startLiveStatusPolling() {
    if (isIncognitoMode) return;
    stopLiveStatusPolling();
    livePollingTimer = setInterval(pollLiveStatus, 4000);
  }

  function stopLiveStatusPolling() {
    if (livePollingTimer) {
      clearInterval(livePollingTimer);
      livePollingTimer = null;
    }
  }

  async function pollLiveStatus() {
    if (isIncognitoMode || isPollingActive) return;
    if (!workspaceView || !workspaceView.classList.contains('active') || !currentLang) return;
    if (isDraftPendingResumption) return;

    isPollingActive = true;
    try {
      const prevLocks = JSON.stringify(liveStatus.locks || {});
      const prevApproved = JSON.stringify(liveStatus.approved || {});
      const prevProposals = JSON.stringify(liveStatus.proposals || {});

      await fetchLiveStatus();

      const newLocks = JSON.stringify(liveStatus.locks || {});
      const newApproved = JSON.stringify(liveStatus.approved || {});
      const newProposals = JSON.stringify(liveStatus.proposals || {});

      if (prevLocks !== newLocks || prevApproved !== newApproved || prevProposals !== newProposals) {
        const isUserTyping = Boolean(document.activeElement && document.activeElement.tagName === 'TEXTAREA');
        if (!isUserTyping) {
          renderCards();
        } else {
          syncLiveStatusToDOM();
        }
        updateWorkspaceStats();
      }
    } catch (e) {
      console.warn("Live status poll error:", e);
    } finally {
      isPollingActive = false;
    }
  }

  function syncLiveStatusToDOM() {
    if (!catalog || !catalog.items) return;
    const itemMap = new Map();
    catalog.items.forEach(i => itemMap.set(i.key, i));

    const cards = cardsContainer.querySelectorAll('.studio-card');
    cards.forEach(card => {
      const id = card.id;
      if (!id.startsWith('card-')) return;
      const key = id.replace('card-', '');
      const item = itemMap.get(key);
      if (!item) return;

      const statusInfo = getItemStatusInfo(item);
      const statusPill = card.querySelector('.card-status-pill');
      if (statusPill) {
        statusPill.className = `studio-pill card-status-pill ${statusInfo.pillClass}`;
        statusPill.innerHTML = `<span class="status-dot ${statusInfo.dotClass}"></span> ${statusInfo.label}`;
      }

      const textarea = card.querySelector('textarea.translation-textarea');
      if (textarea && document.activeElement !== textarea) {
        textarea.readOnly = statusInfo.isReadOnly;
        if (statusInfo.status === 'other') {
          textarea.placeholder = 'Another contributor is drafting this... You can suggest an alternative.';
          textarea.style.opacity = '0.6';
          textarea.style.cursor = 'default';
          textarea.style.background = 'rgba(255,255,255,0.02)';
          if (!getLocalTranslation(currentLang, item.key)) {
            textarea.value = '';
          }
          autoResizeTextarea(textarea);
        } else if (statusInfo.status === 'available') {
          textarea.placeholder = 'Type translation here...';
          textarea.style.opacity = '1';
          textarea.style.cursor = 'text';
          textarea.style.background = '';
          autoResizeTextarea(textarea);
        } else if (statusInfo.isReadOnly) {
          textarea.placeholder = 'This string is locked. Use \'Suggest Alternative\' to propose changes.';
          textarea.style.opacity = '0.85';
          textarea.style.cursor = 'default';
          textarea.style.background = 'rgba(255,255,255,0.02)';
          if (statusInfo.displayText) {
            textarea.value = statusInfo.displayText;
          }
          autoResizeTextarea(textarea);
        }
      }

      // Update Suggest button slot limit
      const proposalMeta = liveStatus && liveStatus.proposals ? liveStatus.proposals[item.key] : null;
      const isFull = proposalMeta && proposalMeta.isFull;
      const suggestBtn = card.querySelector('.btn-suggest');
      if (suggestBtn) {
        if (isFull) {
          suggestBtn.disabled = true;
          suggestBtn.innerHTML = `${ICONS.lock} Max (10/10)`;
        } else {
          suggestBtn.disabled = false;
          suggestBtn.innerHTML = `${ICONS.bulb} Suggest Alternative`;
        }
      }
    });
  }

  let isDraftPendingResumption = false;

  async function checkDraftResumption() {
    if (isIncognitoMode) {
      isDraftPendingResumption = false;
      return;
    }
    try {
      const res = await fetch(`${WORKER_URL}/api/drafts/resume?lang=${currentLang}`, {
        headers: { 'X-Session-ID': SESSION_ID }
      });
      let cloudDrafts = {};
      if (res.ok) {
        const data = await res.json();
        cloudDrafts = data.drafts || {};
      }

      // Check if local storage or Cloudflare has unsubmitted drafts
      const localDrafts = getAllLocalTranslations(currentLang);
      const combinedDrafts = { ...localDrafts, ...cloudDrafts };
      const count = Object.keys(combinedDrafts).length;

      if (count > 0 && draftResumeModal) {
        currentDrafts = combinedDrafts;
        isDraftPendingResumption = true;

        const countEl = document.getElementById('resume-draft-count');
        if (countEl) countEl.textContent = count;
        const confirmCountEl = document.getElementById('discard-confirm-count');
        if (confirmCountEl) confirmCountEl.textContent = count;
        const langEl = document.getElementById('resume-draft-lang');
        if (langEl) {
          const meta = resolveLanguageMeta(currentLang);
          langEl.textContent = meta ? formatLanguageDisplayName(meta, currentLang) : currentLang;
        }

        // Reset modal views to primary prompt
        if (draftModalMainView) draftModalMainView.style.display = 'block';
        if (draftModalConfirmView) draftModalConfirmView.style.display = 'none';

        draftResumeModal.classList.add('active');

        // Helper: Load drafts into workspace and close modal
        const restoreDraftsAndClose = () => {
          isDraftPendingResumption = false;
          for (const [k, v] of Object.entries(currentDrafts)) {
            setLocalTranslation(currentLang, k, v);
          }
          draftResumeModal.classList.remove('active');
          renderCards();
          updateWorkspaceStats();
          showToast(`Restored ${count} draft translations!`);
        };

        // Action 1: Resume Drafts
        const resumeBtn = document.getElementById('draft-resume-btn');
        if (resumeBtn) {
          resumeBtn.onclick = restoreDraftsAndClose;
        }

        // Action 2: Click Discard -> Switch to Warning Confirmation View
        const discardBtn = document.getElementById('draft-discard-btn');
        if (discardBtn) {
          discardBtn.onclick = () => {
            if (draftModalMainView) draftModalMainView.style.display = 'none';
            if (draftModalConfirmView) draftModalConfirmView.style.display = 'block';
          };
        }

        // Action 3: Keep Drafts -> Close popup and load drafts
        const cancelConfirmBtn = document.getElementById('draft-confirm-cancel-btn');
        if (cancelConfirmBtn) {
          cancelConfirmBtn.onclick = restoreDraftsAndClose;
        }

        // Action 4: Confirm Discard -> Wipe memory drafts, localStorage, and Cloudflare KV
        const confirmDiscardBtn = document.getElementById('draft-confirm-discard-btn');
        if (confirmDiscardBtn) {
          confirmDiscardBtn.onclick = async () => {
            isDraftPendingResumption = false;
            currentDrafts = {};
            safeStorageRemove(`tunetastic_l10n_${currentLang}`);
            safeStorageRemove(`tunetastic_draft_updated_${currentLang}`);

            // Clear in-memory locks for this session immediately
            if (liveStatus && liveStatus.locks) {
              for (const [k, lock] of Object.entries(liveStatus.locks)) {
                if (lock === 'mine') {
                  delete liveStatus.locks[k];
                }
              }
            }

            try {
              await fetch(`${WORKER_URL}/api/drafts/discard`, {
                method: 'POST',
                headers: {
                  'Content-Type': 'application/json',
                  'X-Session-ID': SESSION_ID
                },
                body: JSON.stringify({ language: currentLang })
              });
            } catch (e) {
              console.warn("Discard sync error:", e);
            }

            draftResumeModal.classList.remove('active');
            renderCards();
            updateWorkspaceStats();
            showToast('Saved drafts discarded.');
          };
        }
      } else {
        isDraftPendingResumption = false;
        if (draftResumeModal) draftResumeModal.classList.remove('active');
      }
    } catch (e) {
      isDraftPendingResumption = false;
      if (draftResumeModal) draftResumeModal.classList.remove('active');
    }
  }

  function updateSaveIndicatorUi(status = 'default') {
    const indicator = document.getElementById('auto-save-indicator');
    if (!indicator) return;

    if (isIncognitoMode) {
      indicator.className = 'ws-save-indicator ws-save-indicator-incognito';
      indicator.innerHTML = '<span class="save-dot save-dot-incognito"></span> Autosave Off (Incognito)';
      indicator.title = 'Draft autosave is disabled in Incognito mode. Translations are session-only and will be lost if this tab is closed.';
      return;
    }

    if (status === 'saving') {
      indicator.className = 'ws-save-indicator ws-save-indicator-saving';
      indicator.innerHTML = '<span class="save-dot save-dot-saving"></span> Saving...';
      indicator.title = 'Autosaving draft changes to browser and cloud...';
    } else {
      indicator.className = 'ws-save-indicator';
      indicator.innerHTML = '<span class="save-dot"></span> Saved to Browser';
      indicator.title = 'Changes automatically saved to your browser and cloud.';
    }
  }

  // 400ms Debounced Draft Autosave to cloud storage
  let draftSaveTimer = null;
  function scheduleDraftAutosave(key, text) {
    if (isIncognitoMode) {
      updateSaveIndicatorUi();
      return;
    }
    updateSaveIndicatorUi('saving');
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
      } finally {
        updateSaveIndicatorUi('default');
      }
    }, 400);
  }

  // -------------------------------------------------------------------------
  // 6. CARD RENDERING & LIVE IN-APP PREVIEW
  // -------------------------------------------------------------------------

  function getCatalogTranslation(lang, key) {
    if (!catalog || !catalog.languages || !catalog.languages[lang]) return '';
    const langObj = catalog.languages[lang];
    return (langObj.translations && langObj.translations[key]) ? langObj.translations[key] : '';
  }

  function getSubmittedTranslations(lang) {
    try {
      const raw = safeStorageGet(`tunetastic_submitted_${lang}`);
      return raw ? JSON.parse(raw) : {};
    } catch {
      return {};
    }
  }

  function isSubmittedKey(lang, key) {
    const all = getSubmittedTranslations(lang);
    return Boolean(all && all[key]);
  }

  function getLocalDraft(lang, key) {
    if (isDraftPendingResumption) return '';
    return getLocalTranslation(lang, key);
  }

  function renderSimulatedText(rawText, placeholders) {
    if (!rawText) return '';
    let preview = escapeHtml(rawText);
    if (placeholders && Array.isArray(placeholders)) {
      placeholders.forEach(ph => {
        const escToken = escapeHtml(ph.token);
        const replacement = `<strong class="token-tag">${escapeHtml(ph.sample)}</strong>`;
        preview = preview.split(escToken).join(replacement);
      });
    }
    return preview;
  }

  function autoResizeTextarea(ta) {
    if (!ta) return;
    ta.style.height = 'auto';
    ta.style.height = Math.max(40, ta.scrollHeight) + 'px';
  }

  function renderContextBlockHtml(comment, key) {
    if (!comment) {
      return `
        <div class="card-key-line">
          <span class="key-badge-mono">${escapeHtml(key)}</span>
        </div>
      `;
    }
    const parts = comment.split(/\s*->\s*/);
    if (parts.length > 1) {
      const pathParts = parts.slice(0, -1);
      const leafTitle = parts[parts.length - 1];
      const pathHtml = pathParts.map(p => `<span class="crumb">${escapeHtml(p)}</span>`).join('<span class="crumb-sep">›</span>');
      return `
        <div class="card-context-block">
          <div class="card-context-icon">${ICONS.comment}</div>
          <div class="card-context-details">
            <div class="card-context-path">${pathHtml}</div>
            <h4 class="card-context-title">${escapeHtml(leafTitle)}</h4>
            <div class="card-key-line">
              <span class="key-badge-mono">${escapeHtml(key)}</span>
            </div>
          </div>
        </div>
      `;
    }
    return `
      <div class="card-context-block">
        <div class="card-context-icon">${ICONS.comment}</div>
        <div class="card-context-details">
          <h4 class="card-context-title">${escapeHtml(comment)}</h4>
          <div class="card-key-line">
            <span class="key-badge-mono">${escapeHtml(key)}</span>
          </div>
        </div>
      </div>
    `;
  }

  function renderSimulatedPreview(sourceText, translationText, placeholders) {
    return renderSimulatedText(translationText || sourceText, placeholders);
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

  function getItemStatusInfo(item) {
    const approvedEntry = liveStatus && liveStatus.approved ? liveStatus.approved[item.key] : null;
    const proposalMeta = liveStatus && liveStatus.proposals ? liveStatus.proposals[item.key] : null;
    const isLocalSubmitted = isSubmittedKey(currentLang, item.key);
    const catalogTrans = getCatalogTranslation(currentLang, item.key);
    const localDraft = getLocalDraft(currentLang, item.key);
    const lock = liveStatus && liveStatus.locks ? liveStatus.locks[item.key] : null;

    // 1. Approved (Staged for release PR)
    if (approvedEntry) {
      return {
        status: 'approved',
        label: `Approved (${escapeHtml(approvedEntry.contributor || 'Maintainer')})`,
        pillClass: 'pill-approved',
        dotClass: 'dot-approved',
        isReadOnly: true,
        displayText: approvedEntry.translation || ''
      };
    }

    // 2. Submitted (Active in Cloudflare KV proposals)
    const hasServerProposals = Boolean(proposalMeta && proposalMeta.count > 0);
    if (hasServerProposals || (isLocalSubmitted && (!liveStatus || !liveStatus.proposals))) {
      const subMap = getSubmittedTranslations(currentLang);
      const subObj = subMap ? subMap[item.key] : null;

      let authorName = (subObj && subObj.contributor) || (proposalMeta && proposalMeta.latestContributor) || '';
      authorName = String(authorName).replace(/^@+/, '').trim();
      const contrib = authorName ? (authorName.includes('@') || authorName.includes(' ') ? authorName : `@${authorName}`) : (proposalMeta ? `${proposalMeta.count} proposal(s)` : 'Contributor');
      const text = (subObj && subObj.text) || (proposalMeta && proposalMeta.latestTranslation) || catalogTrans || '';

      return {
        status: 'submitted',
        label: `Submitted (${escapeHtml(contrib)})`,
        pillClass: 'pill-submitted',
        dotClass: 'dot-submitted',
        isReadOnly: true,
        displayText: text
      };
    } else if (isLocalSubmitted && liveStatus && liveStatus.proposals && !hasServerProposals) {
      // Server has confirmed NO proposals exist in KV for this key (e.g. KV was cleared or proposals synced)
      // Prune dead local submitted record so localStorage synchronizes with server state
      const subMap = getSubmittedTranslations(currentLang);
      if (subMap && subMap[item.key]) {
        delete subMap[item.key];
        safeStorageSet(`tunetastic_submitted_${currentLang}`, JSON.stringify(subMap));
      }
    }

    // 3. Already Translated (shipped in app catalog)
    if (catalogTrans && catalogTrans.trim()) {
      return {
        status: 'translated',
        label: 'Already Translated',
        pillClass: 'pill-translated',
        dotClass: 'dot-translated',
        isReadOnly: true,
        displayText: catalogTrans
      };
    }

    // 4. In progress by another contributor (locked in Cloudflare KV)
    if (lock === 'other') {
      return {
        status: 'other',
        label: 'In Progress By Others',
        pillClass: 'pill-draft-other',
        dotClass: 'dot-in-progress',
        isReadOnly: true,
        displayText: ''
      };
    }

    // 5. Drafted by you in progress
    if (!isDraftPendingResumption && Boolean(localDraft && localDraft.trim())) {
      return {
        status: 'mine',
        label: 'Drafted By You',
        pillClass: 'pill-draft-mine',
        dotClass: 'dot-draft',
        isReadOnly: false,
        displayText: localDraft
      };
    }

    // 6. Need Translation / Untranslated
    return {
      status: 'available',
      label: 'Need Translation',
      pillClass: 'pill-available',
      dotClass: 'dot-available',
      isReadOnly: false,
      displayText: ''
    };
  }

  function getFilteredItems() {
    if (!catalog || !catalog.items) return [];

    return catalog.items.filter(item => {
      const statusInfo = getItemStatusInfo(item);

      if (currentFilter === 'available' && statusInfo.status !== 'available') return false;
      if (currentFilter === 'mine' && statusInfo.status !== 'mine') return false;
      if (currentFilter === 'other' && statusInfo.status !== 'other') return false;
      if (currentFilter === 'submitted' && statusInfo.status !== 'submitted') return false;
      if (currentFilter === 'approved' && statusInfo.status !== 'approved') return false;
      if (currentFilter === 'translated' && statusInfo.status !== 'translated') return false;

      // Legacy filter fallback
      if (currentFilter === 'missing' && statusInfo.status !== 'available') return false;
      if (currentFilter === 'completed' && statusInfo.status !== 'translated' && statusInfo.status !== 'approved') return false;

      if (searchQuery) {
        const q = searchQuery.toLowerCase();
        const keyMatch = item.key.toLowerCase().includes(q);
        const sourceMatch = item.source.toLowerCase().includes(q);
        const transMatch = (statusInfo.displayText || '').toLowerCase().includes(q);
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
          ${ICONS.search} No strings match your current filter or search query.
        </div>
      `;
      return;
    }

    const meta = resolveLanguageMeta(currentLang) || { name: currentLang, native: currentLang };

    pageItems.forEach(item => {
      // Determine Badge and Read-Only state from status lifecycle
      const statusInfo = getItemStatusInfo(item);
      const translation = statusInfo.displayText || '';
      const isReadOnly = statusInfo.isReadOnly;
      const hasPlaceholders = item.placeholders && item.placeholders.length > 0;
      const tokenStatus = validateTokens(item.source, translation, item.placeholders);

      const statusPill = `<span class="studio-pill card-status-pill ${statusInfo.pillClass}"><span class="status-dot ${statusInfo.dotClass}"></span> ${statusInfo.label}</span>`;
      const proposalMeta = liveStatus && liveStatus.proposals ? liveStatus.proposals[item.key] : null;

      // Suggest Button Slot state
      const isFull = proposalMeta && proposalMeta.isFull;
      const suggestBtnHtml = isFull 
        ? `<button class="btn-suggest" disabled title="Max 10 community suggestions reached">${ICONS.lock} Max (10/10)</button>`
        : `<button class="btn-suggest" data-key="${escapeHtml(item.key)}">${ICONS.bulb} Suggest Alternative</button>`;

      const card = document.createElement('div');
      card.className = 'studio-card';
      card.id = `card-${item.key}`;

      const placeholderText = statusInfo.status === 'other'
        ? 'Another contributor is drafting this... You can suggest an alternative.'
        : (isReadOnly ? 'This string is locked. Use \'Suggest Alternative\' to propose changes.' : 'Type translation here...');

      const contextHtml = renderContextBlockHtml(item.comment, item.key);

      const sourcePreviewHtml = renderSimulatedText(item.source, item.placeholders);
      const targetPreviewHtml = renderSimulatedText(translation, item.placeholders);

      card.innerHTML = `
        <div class="studio-card-header">
          <div class="card-identity">
            ${contextHtml}
          </div>
          <div class="card-badges">
            ${statusPill}
            ${hasPlaceholders ? `<span class="studio-pill ${tokenStatus.isValid ? 'pill-tokens' : 'pill-error'}" id="token-badge-${item.key}">Tokens (${item.placeholders.map(p => p.token).join('')})</span>` : ''}
          </div>
        </div>

        <div class="editors-split-grid">
          <div class="editor-column source-column">
            <div class="column-header">
              <span>English Source</span>
            </div>
            <div class="source-display-box" title="${escapeHtml(item.source)}">${escapeHtml(item.source)}</div>
            <div class="column-actions-bar">
              <button class="copy-mini-btn" title="Copy English source text">${ICONS.copy} Copy</button>
            </div>
          </div>

          <div class="editor-column target-column">
            <div class="column-header">
              <span>Your Translation</span>
            </div>
            <textarea class="translation-textarea" rows="1" 
              placeholder="${placeholderText}"
              ${isReadOnly ? 'readonly style="opacity: 0.85; background: rgba(255,255,255,0.02); cursor: default;"' : ''}>${escapeHtml(translation)}</textarea>

            <div class="token-missing-alert" style="display: ${tokenStatus.isValid ? 'none' : 'flex'};">
              ${ICONS.alert} Missing placeholder token: <strong>${tokenStatus.missing.join(', ')}</strong>
            </div>

            <div class="column-actions-bar">
              ${suggestBtnHtml}
            </div>
          </div>
        </div>

        <div class="live-preview-box">
          <div class="preview-row preview-row-source" title="English Preview: ${escapeHtml(item.source)}">
            <span class="preview-pill preview-pill-en"><span class="preview-pulse pulse-cyan"></span> English Preview</span>
            <span class="preview-content preview-source-text">${sourcePreviewHtml}</span>
          </div>
          <div class="preview-row preview-row-target" title="Translation Preview: ${escapeHtml(translation || 'No translation yet')}">
            <span class="preview-pill preview-pill-tr"><span class="preview-pulse pulse-purple"></span> Translated Preview</span>
            <span class="preview-content preview-trans-text">${targetPreviewHtml || '<span class="preview-empty">No translation yet</span>'}</span>
          </div>
        </div>
      `;

      cardsContainer.appendChild(card);

      // Copy mini btn event
      const copyBtn = card.querySelector('.copy-mini-btn');
      if (copyBtn) {
        copyBtn.onclick = () => {
          navigator.clipboard.writeText(item.source);
          copyBtn.innerHTML = `${ICONS.check} Copied!`;
          copyBtn.classList.add('copied');
          setTimeout(() => { copyBtn.innerHTML = `${ICONS.copy} Copy`; copyBtn.classList.remove('copied'); }, 2000);
        };
      }

      // Textarea auto-sizing and typing event listeners
      const textarea = card.querySelector('textarea.translation-textarea');
      if (textarea) {
        autoResizeTextarea(textarea);
        if (!isReadOnly) {
          textarea.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') e.preventDefault();
          });
          textarea.addEventListener('input', (e) => {
            autoResizeTextarea(e.target);
            const val = e.target.value.replace(/[\r\n]+/g, ' ');
            setLocalTranslation(currentLang, item.key, val);

            // Update live simulated preview
            const previewTransEl = card.querySelector('.preview-trans-text');
            const previewTargetRow = card.querySelector('.preview-row-target');
            if (previewTransEl) {
              const rendered = renderSimulatedText(val, item.placeholders);
              previewTransEl.innerHTML = rendered || '<span class="preview-empty">No translation yet</span>';
              if (previewTargetRow) {
                previewTargetRow.setAttribute('title', val ? `Translation Preview: ${val}` : 'Translation Preview: No translation yet');
              }
            }

            // Update status pill on card dynamically
            const statusPillEl = card.querySelector('.card-status-pill');
            if (statusPillEl) {
              const updatedStatusInfo = getItemStatusInfo(item);
              statusPillEl.className = `studio-pill card-status-pill ${updatedStatusInfo.pillClass}`;
              statusPillEl.innerHTML = `<span class="status-dot ${updatedStatusInfo.dotClass}"></span> ${updatedStatusInfo.label}`;
            }

            // Validate Tokens
            if (hasPlaceholders) {
              const tokenWarn = card.querySelector('.token-missing-alert');
              const tokenBadge = card.querySelector('[id^="token-badge-"]');
              const check = validateTokens(item.source, val, item.placeholders);
              if (check.isValid) {
                if (tokenWarn) tokenWarn.style.display = 'none';
                if (tokenBadge) {
                  tokenBadge.className = 'studio-pill pill-tokens';
                  tokenBadge.innerHTML = `Tokens (${item.placeholders.map(p => p.token).join('')})`;
                }
              } else {
                if (tokenWarn) {
                  tokenWarn.style.display = 'flex';
                  tokenWarn.innerHTML = `${ICONS.alert} Missing placeholder token: <strong>${check.missing.join(', ')}</strong>`;
                }
                if (tokenBadge) {
                  tokenBadge.className = 'studio-pill pill-error';
                  tokenBadge.innerHTML = `Tokens (${item.placeholders.map(p => p.token).join('')})`;
                }
              }
            }

            // Trigger debounced Cloudflare Draft Autosave
            scheduleDraftAutosave(item.key, val);
            updateWorkspaceStats();
          });
        }
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
    let counts = {
      available: 0,
      mine: 0,
      other: 0,
      submitted: 0,
      approved: 0,
      translated: 0,
      tokens: 0
    };

    catalog.items.forEach(item => {
      const statusInfo = getItemStatusInfo(item);
      if (counts[statusInfo.status] !== undefined) {
        counts[statusInfo.status]++;
      }
      if (item.placeholders && item.placeholders.length > 0) counts.tokens++;
    });

    const completed = counts.translated + counts.approved + counts.mine;
    const remaining = total - completed;
    const pct = Math.round((completed / total) * 100) || 0;

    const pctLabel = document.getElementById('ws-pct-label');
    if (pctLabel) pctLabel.textContent = `${pct}% Complete (${completed}/${total})`;

    const progressFill = document.getElementById('workspace-progress-fill');
    if (progressFill) progressFill.style.width = `${pct}%`;

    const doneCount = document.getElementById('ws-done-count');
    if (doneCount) doneCount.textContent = `${completed}`;

    const missingCount = document.getElementById('ws-missing-count');
    if (missingCount) missingCount.textContent = `${remaining}`;

    const tokensEl = document.getElementById('ws-tokens-count');
    if (tokensEl) tokensEl.textContent = `${counts.tokens}`;

    // Filter tab counts
    const countAll = document.getElementById('count-all');
    if (countAll) countAll.textContent = `${total}`;

    const countAvailable = document.getElementById('count-available');
    if (countAvailable) countAvailable.textContent = `${counts.available}`;

    const countMine = document.getElementById('count-mine');
    if (countMine) countMine.textContent = `${counts.mine}`;

    const countOther = document.getElementById('count-other');
    if (countOther) countOther.textContent = `${counts.other}`;

    const countSubmitted = document.getElementById('count-submitted');
    if (countSubmitted) countSubmitted.textContent = `${counts.submitted}`;

    const countApproved = document.getElementById('count-approved');
    if (countApproved) countApproved.textContent = `${counts.approved}`;

    const countTranslated = document.getElementById('count-translated');
    if (countTranslated) countTranslated.textContent = `${counts.translated}`;

    // Update Submit Work button in workspace hero
    const wsSubmitBtn = document.getElementById('ws-submit-btn');
    if (wsSubmitBtn) {
      const draftCount = counts.mine;
      wsSubmitBtn.innerHTML = `<svg class="svg-icon" viewBox="0 0 16 16" width="14" height="14" fill="currentColor"><path d="M9.5.5C8 2 7.7 4.2 8.3 5.7L4.1 9.9c-.3.3-.4.8-.2 1.2l1 2c.2.4.6.6 1 .5l2.2-.4 4.2-4.2c1.5.6 3.7.3 5.2-1.2 0 0 .5-2-1.2-3.7S12.6.5 9.5.5zM3.4 12.6l-2 2c-.4.4-.4 1 0 1.4.4.4 1 .4 1.4 0l2-2-1.4-1.4z"/></svg> Submit Work ${draftCount > 0 ? `(${draftCount})` : ''}`;
      wsSubmitBtn.disabled = draftCount === 0;
      wsSubmitBtn.style.opacity = draftCount === 0 ? '0.6' : '1';
      wsSubmitBtn.style.cursor = draftCount === 0 ? 'not-allowed' : 'pointer';
    }

    // Update 48-Hour Sliding Draft Countdown Timer
    updateDraftTimerDisplay(counts.mine);
  }

  // -------------------------------------------------------------------------
  // 6b. 48-HOUR SLIDING DRAFT TIMER
  // -------------------------------------------------------------------------

  let draftCountdownTimer = null;

  function startDraftTimer() {
    stopDraftTimer();
    updateDraftTimerDisplay();
    draftCountdownTimer = setInterval(() => {
      updateDraftTimerDisplay();
    }, 1000);
  }

  function stopDraftTimer() {
    if (draftCountdownTimer) {
      clearInterval(draftCountdownTimer);
      draftCountdownTimer = null;
    }
  }

  function updateDraftTimerDisplay(explicitDraftCount) {
    const timerEl = document.getElementById('ws-draft-timer');
    const digitsEl = document.getElementById('ws-timer-digits');
    const labelEl = document.querySelector('.ws-draft-timer .timer-label');
    if (!timerEl || !digitsEl || !currentLang) return;

    if (isIncognitoMode) {
      timerEl.classList.add('is-incognito');
      timerEl.classList.remove('is-urgent');
      timerEl.setAttribute('title', 'Incognito Mode: Autosave is disabled. Drafts are session-only and will be lost on tab close.');
      if (labelEl) labelEl.textContent = 'Session:';
      digitsEl.textContent = 'Tab Only';
      return;
    }

    timerEl.classList.remove('is-incognito');
    const drafts = getAllLocalTranslations(currentLang);
    const draftCount = explicitDraftCount !== undefined ? explicitDraftCount : Object.keys(drafts).length;

    const DRAFT_WINDOW_MS = 48 * 60 * 60 * 1000; // 48 hours

    if (draftCount === 0) {
      timerEl.classList.remove('is-urgent');
      timerEl.setAttribute('title', '48-Hour Sliding Window: Your translations autosave to cloud storage and stay protected for 48 hours from your latest edit.');
      if (labelEl) labelEl.textContent = 'Autosave:';
      digitsEl.textContent = '48h Window';
      return;
    }

    let lastUpdated = parseInt(safeStorageGet(`tunetastic_draft_updated_${currentLang}`), 10);
    if (!lastUpdated || isNaN(lastUpdated)) {
      lastUpdated = Date.now();
      safeStorageSet(`tunetastic_draft_updated_${currentLang}`, String(lastUpdated));
    }

    const elapsed = Date.now() - lastUpdated;
    const remaining = Math.max(0, DRAFT_WINDOW_MS - elapsed);

    if (remaining <= 0) {
      timerEl.classList.add('is-urgent');
      if (labelEl) labelEl.textContent = 'Status:';
      digitsEl.textContent = 'Expired';
      timerEl.setAttribute('title', 'Your 48-hour draft window has expired. Type in any field to restart the 48-hour autosave window.');
      return;
    }

    const hours = Math.floor(remaining / (3600 * 1000));
    const minutes = Math.floor((remaining % (3600 * 1000)) / (60 * 1000));
    const seconds = Math.floor((remaining % (60 * 1000)) / 1000);

    if (hours < 2) {
      timerEl.classList.add('is-urgent');
    } else {
      timerEl.classList.remove('is-urgent');
    }

    if (labelEl) labelEl.textContent = 'Expires in:';
    digitsEl.textContent = `${hours}h ${String(minutes).padStart(2, '0')}m ${String(seconds).padStart(2, '0')}s`;
    timerEl.setAttribute('title', `Active 48-Hour Draft Window: ${hours}h ${minutes}m ${seconds}s remaining. Window automatically resets to 48 hours every time you edit.`);
  }

  // -------------------------------------------------------------------------
  // 7. SUGGEST ALTERNATIVE MODAL (STRICT EXACT MATCH & 10-SLOT LIMIT)
  // -------------------------------------------------------------------------

  async function openSuggestModal(item) {
    activeSuggestKey = item.key;
    document.getElementById('suggest-key-label').textContent = item.key;
    document.getElementById('suggest-source-text').textContent = item.source;
    document.getElementById('suggest-text-input').value = '';
    let savedAuthor = safeStorageGet('tunetastic_author') || '';
    savedAuthor = String(savedAuthor).replace(/^@+/, '').trim();
    document.getElementById('suggest-author-input').value = savedAuthor;
    document.getElementById('suggest-exact-error').style.display = 'none';

    const existingListEl = document.getElementById('suggest-existing-list');
    const fullBannerEl = document.getElementById('suggest-full-banner');
    const inputGroupEl = document.getElementById('suggest-input-group');
    const authorGroupEl = document.getElementById('suggest-author-group');
    const confirmBtn = document.getElementById('suggest-confirm-btn');

    existingListEl.innerHTML = `<span style="font-size: 12px; color: var(--text-tertiary);">Loading existing proposals...</span>`;
    suggestModal.classList.add('active');

    // Fetch existing proposals for this specific key
    activeSuggestProposals = [];
    try {
      const res = await fetch(`${WORKER_URL}/api/proposals?lang=${currentLang}&key=${encodeURIComponent(item.key)}`);
      if (res.ok) {
        const data = await res.json();
        activeSuggestProposals = Array.isArray(data.proposals) ? data.proposals : [];
      }
    } catch (e) {
      console.warn("Could not fetch proposals for key:", e);
    }

    // Populate existing proposals list
    renderExistingProposalsInModal(item);

    // Live exact-match check on typing (rejects duplicate text)
    const textarea = document.getElementById('suggest-text-input');
    const catalogTrans = getCatalogTranslation(currentLang, item.key);

    textarea.oninput = (e) => {
      const val = e.target.value;
      const isExactProposalMatch = activeSuggestProposals.some(p => p.text === val);
      const isCatalogExactMatch = catalogTrans && catalogTrans === val;
      const isExactMatch = isExactProposalMatch || isCatalogExactMatch;

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
    const rawAuthor = document.getElementById('suggest-author-input').value.trim() || 'Unknown';
    const cleanAuthor = rawAuthor.replace(/^@+/, '').trim();
    const author = cleanAuthor || 'Unknown';
    if (!text || !activeSuggestKey) return;

    if (cleanAuthor && cleanAuthor !== 'Unknown') {
      safeStorageSet('tunetastic_author', cleanAuthor);
    } else {
      safeStorageRemove('tunetastic_author');
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
        updateWorkspaceStats();
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
    document.getElementById('modal-submit-lang').textContent = formatLanguageDisplayName(meta, currentLang);
    document.getElementById('modal-submit-count').textContent = count;
    let savedAuthor = safeStorageGet('tunetastic_author') || '';
    savedAuthor = String(savedAuthor).replace(/^@+/, '').trim();
    document.getElementById('submit-author-input').value = savedAuthor;

    submitModal.classList.add('active');
  }

  async function submitTranslations() {
    const rawAuthor = document.getElementById('submit-author-input').value.trim() || 'Unknown';
    const cleanAuthor = rawAuthor.replace(/^@+/, '').trim();
    const author = cleanAuthor || 'Unknown';
    if (cleanAuthor && cleanAuthor !== 'Unknown') {
      safeStorageSet('tunetastic_author', cleanAuthor);
    } else {
      safeStorageRemove('tunetastic_author');
    }

    const translations = getAllLocalTranslations(currentLang);
    const count = Object.keys(translations).length;
    if (count === 0) {
      alert("No draft translations to submit.");
      return;
    }

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

        // Transition submitted keys from drafts to submitted state
        const submittedKey = `tunetastic_submitted_${currentLang}`;
        let existingSubmitted = {};
        try {
          existingSubmitted = JSON.parse(safeStorageGet(submittedKey) || '{}');
        } catch {}

        for (const [k, v] of Object.entries(translations)) {
          existingSubmitted[k] = {
            text: v,
            contributor: author,
            timestamp: Date.now()
          };
        }
        safeStorageSet(submittedKey, JSON.stringify(existingSubmitted));

        // Clear local active drafts
        safeStorageRemove(`tunetastic_l10n_${currentLang}`);
        safeStorageRemove(`tunetastic_draft_updated_${currentLang}`);
        currentDrafts = {};

        // Clear in-memory locks for this session immediately
        if (liveStatus && liveStatus.locks) {
          for (const [k, lock] of Object.entries(liveStatus.locks)) {
            if (lock === 'mine') {
              delete liveStatus.locks[k];
            }
          }
        }

        // Discard draft lock in Cloudflare
        try {
          await fetch(`${WORKER_URL}/api/drafts/discard`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'X-Session-ID': SESSION_ID },
            body: JSON.stringify({ language: currentLang })
          });
        } catch {}

        showToast(`Successfully submitted ${data.acceptedCount || count} strings to staging!`);
        await fetchLiveStatus();
        renderCards();
        updateWorkspaceStats();
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

  function requestAdminAuth() {
    return new Promise((resolve, reject) => {
      if (sessionAdminPassword) {
        resolve(sessionAdminPassword);
        return;
      }
      const modal = document.getElementById('admin-auth-modal');
      const input = document.getElementById('admin-password-input');
      const submitBtn = document.getElementById('admin-auth-submit-btn');
      const cancelBtn = document.getElementById('admin-auth-cancel-btn');
      const errorEl = document.getElementById('admin-auth-error');

      if (!modal || !input) {
        reject(new Error("Admin modal elements missing"));
        return;
      }

      input.value = '';
      if (errorEl) errorEl.style.display = 'none';
      submitBtn.disabled = false;
      submitBtn.textContent = 'Unlock Console';
      modal.classList.add('active');
      input.focus();

      const cleanup = () => {
        submitBtn.onclick = null;
        cancelBtn.onclick = null;
        input.onkeydown = null;
        modal.classList.remove('active');
      };

      const attemptUnlock = async () => {
        const pwd = input.value.trim();
        if (!pwd) return;
        submitBtn.disabled = true;
        submitBtn.textContent = 'Verifying...';
        if (errorEl) errorEl.style.display = 'none';

        try {
          const res = await fetch(`${WORKER_URL}/api/pending`, {
            headers: { 'Authorization': `Bearer ${pwd}` }
          });
          if (res.ok) {
            sessionAdminPassword = pwd;
            cleanup();
            resolve(pwd);
          } else {
            if (errorEl) {
              errorEl.textContent = 'Incorrect password. Access denied.';
              errorEl.style.display = 'block';
            }
            submitBtn.disabled = false;
            submitBtn.textContent = 'Unlock Console';
            input.select();
          }
        } catch (e) {
          if (errorEl) {
            errorEl.textContent = 'Could not reach staging server: ' + e.message;
            errorEl.style.display = 'block';
          }
          submitBtn.disabled = false;
          submitBtn.textContent = 'Unlock Console';
        }
      };

      submitBtn.onclick = attemptUnlock;
      input.onkeydown = (e) => {
        if (e.key === 'Enter') attemptUnlock();
        if (e.key === 'Escape') {
          cleanup();
          reject(new Error("Cancelled"));
        }
      };
      cancelBtn.onclick = () => {
        cleanup();
        reject(new Error("Cancelled"));
      };
    });
  }

  async function loadAdminQueue() {
    const container = document.getElementById('admin-submissions-container');
    container.innerHTML = `<div style="text-align: center; padding: 48px; color: var(--text-secondary);"><div class="loading-spinner"></div>Connecting to staging storage...</div>`;

    if (!sessionAdminPassword) {
      try {
        await requestAdminAuth();
      } catch {
        window.location.hash = '#home';
        return;
      }
    }

    try {
      const res = await fetch(`${WORKER_URL}/api/pending`, {
        headers: { 'Authorization': `Bearer ${sessionAdminPassword}` }
      });
      if (res.status === 401) {
        sessionAdminPassword = '';
        container.innerHTML = `
          <div style="padding: 28px; background: var(--surface-glass); border-radius: var(--radius-xl); border: 1px solid var(--border-glass); text-align: center;">
            <h4 style="color: #f87171;">Session Expired or Invalid Password</h4>
            <button class="btn btn-primary-gradient" style="margin-top: 14px;" onclick="loadAdminQueue()">Re-authenticate</button>
          </div>
        `;
        return;
      }

      const data = await res.json();
      const pendingItems = data.pending || [];
      document.getElementById('admin-pending-badge').textContent = `${pendingItems.length} Keys Submitted`;

      if (pendingItems.length === 0) {
        container.innerHTML = `
          <div style="text-align: center; padding: 48px; background: var(--surface-glass); border-radius: var(--radius-xl); border: 1px solid var(--border-glass); color: var(--text-secondary);">
            ${ICONS.checkCircle} The review queue is completely clear! No community proposals are submitted.
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
              <span class="studio-pill" style="margin-left: 8px;">${escapeHtml(formatLanguageDisplayName(langMeta, item.language))} • ${item.language}</span>
            </div>
            <button class="btn btn-sm btn-primary-gradient admin-approve-btn" data-lang="${item.language}" data-key="${item.key}">
              ${ICONS.check} Approve Selected
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
              if (liveStatus && liveStatus.approved && item.language === currentLang) {
                liveStatus.approved[item.key] = {
                  translation: selectedText,
                  contributor
                };
              }
              card.style.opacity = '0.5';
              card.innerHTML = `<div style="padding: 18px; color: var(--status-success); font-weight: 700;">${ICONS.check} Approved! Key "${item.key}" is staged on server. Click "Sync Approved to Git" when ready to create a PR.</div>`;
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
      try {
        await requestAdminAuth();
      } catch {
        return;
      }
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
          <h4 style="color: var(--status-success); margin-bottom: 6px;">${ICONS.checkCircle} Pull Request #${data.pr_number} Successfully Opened!</h4>
          <p style="font-size: 13.5px; margin-bottom: 10px;">
            A dedicated sync branch <code>${data.branch}</code> was created and populated with your approved translations.
          </p>
          <a href="${data.pr_url}" target="_blank" rel="noopener">View and Merge PR #${data.pr_number} on GitHub ${ICONS.arrowRight}</a>
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
      prBtn.innerHTML = `<span class="btn-icon">${ICONS.rocket}</span> Sync Approved to Git (Create PR)`;
    }
  }

  // -------------------------------------------------------------------------
  // 10. LOCAL STORAGE & EXPORT HELPERS
  // -------------------------------------------------------------------------

  function getLocalTranslation(lang, key) {
    try {
      const stored = safeStorageGet(`tunetastic_l10n_${lang}`);
      if (!stored) return '';
      const map = JSON.parse(stored);
      return map[key] || '';
    } catch {
      return '';
    }
  }

  function setLocalTranslation(lang, key, value) {
    try {
      const stored = safeStorageGet(`tunetastic_l10n_${lang}`);
      const map = stored ? JSON.parse(stored) : {};
      if (value && value.trim()) {
        map[key] = value;
        safeStorageSet(`tunetastic_draft_updated_${lang}`, String(Date.now()));
      } else {
        delete map[key];
        if (Object.keys(map).length === 0) {
          safeStorageRemove(`tunetastic_draft_updated_${lang}`);
        }
      }
      safeStorageSet(`tunetastic_l10n_${lang}`, JSON.stringify(map));
      updateDraftTimerDisplay();
    } catch (e) {}
  }

  function getAllLocalTranslations(lang) {
    try {
      const stored = safeStorageGet(`tunetastic_l10n_${lang}`);
      return stored ? JSON.parse(stored) : {};
    } catch {
      return {};
    }
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

    // Submit modal triggers
    document.getElementById('ws-submit-btn').onclick = openSubmitModal;
    document.getElementById('modal-cancel-btn').onclick = () => {
      const input = document.getElementById('submit-author-input');
      const val = input ? input.value.replace(/^@+/, '').trim() : '';
      if (val) {
        safeStorageSet('tunetastic_author', val);
      } else {
        safeStorageRemove('tunetastic_author');
      }
      submitModal.classList.remove('active');
    };
    document.getElementById('modal-confirm-submit-btn').onclick = submitTranslations;

    const submitAuthorInput = document.getElementById('submit-author-input');
    if (submitAuthorInput) {
      submitAuthorInput.addEventListener('input', (e) => {
        const val = e.target.value.replace(/^@+/, '').trim();
        if (val) {
          safeStorageSet('tunetastic_author', val);
        } else {
          safeStorageRemove('tunetastic_author');
        }
      });
    }

    // Suggest modal triggers
    document.getElementById('suggest-cancel-btn').onclick = () => {
      const input = document.getElementById('suggest-author-input');
      const val = input ? input.value.replace(/^@+/, '').trim() : '';
      if (val) {
        safeStorageSet('tunetastic_author', val);
      } else {
        safeStorageRemove('tunetastic_author');
      }
      suggestModal.classList.remove('active');
    };
    document.getElementById('suggest-confirm-btn').onclick = submitSuggestion;

    const suggestAuthorInput = document.getElementById('suggest-author-input');
    if (suggestAuthorInput) {
      suggestAuthorInput.addEventListener('input', (e) => {
        const val = e.target.value.replace(/^@+/, '').trim();
        if (val) {
          safeStorageSet('tunetastic_author', val);
        } else {
          safeStorageRemove('tunetastic_author');
        }
      });
    }

    // Admin review triggers
    document.getElementById('admin-refresh-btn').onclick = loadAdminQueue;
    document.getElementById('admin-sync-pr-btn').onclick = triggerSyncPr;

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

    // Sticky Workspace Collapsing Header Metrics & Scroll Watcher
    syncNavLayoutMetrics();
    window.addEventListener('resize', syncNavLayoutMetrics, { passive: true });
    window.addEventListener('scroll', handleWorkspaceScroll, { passive: true });

    // Live Cross-Browser Focus & Tab Visibility Sync
    window.addEventListener('focus', () => {
      if (workspaceView && workspaceView.classList.contains('active')) {
        pollLiveStatus();
      }
    });
    document.addEventListener('visibilitychange', () => {
      if (document.visibilityState === 'visible' && workspaceView && workspaceView.classList.contains('active')) {
        pollLiveStatus();
      }
    });

    // Immediate active lock cleanup on page navigation / tab close
    window.addEventListener('pagehide', releaseActiveLocksOnExit);
    window.addEventListener('beforeunload', releaseActiveLocksOnExit);
  }

  async function checkAndShowIncognitoWarning() {
    const banner = document.getElementById('incognito-warning-banner');
    const dismissBtn = document.getElementById('incognito-warning-dismiss');
    if (!banner) return;

    if (dismissBtn) {
      dismissBtn.onclick = () => {
        banner.style.display = 'none';
      };
    }

    let isPrivate = false;
    if (typeof window.detectIncognito === 'function') {
      try {
        const result = await window.detectIncognito();
        if (result && result.isPrivate) isPrivate = true;
      } catch {}
    }

    // Fast storage quota fallback
    if (!isPrivate && navigator.storage && navigator.storage.estimate) {
      try {
        const { quota } = await navigator.storage.estimate();
        if (quota && quota < 120000000) isPrivate = true;
      } catch {}
    }

    isIncognitoMode = isPrivate;
    if (isPrivate) {
      if (banner) banner.style.display = 'flex';
    }
    updateSaveIndicatorUi();
  }

  // Start SPA
  init();

})();
