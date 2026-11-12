/**
 * Tunetastic Community Localization Studio v2
 * Single Page Application Controller with:
 * 1. Zero-Login Anonymous Session Management (crypto.randomUUID)
 * 2. 48-Hour Sliding Draft Autosave & Resumption
 * 3. Dynamic Intl.DisplayNames Language Metadata (Zero Hardcoding)
 * 4. Multi-Version Community Alternative Suggestions (Max 10 Slots)
 * 5. Strict Exact-Match Duplicate Guard
 * 6. Admin Review Queue with Proposal Stacks
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
        return null;
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
        return;
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
    // Check sessionStorage first: ensures distinct tabs/windows get independent collaborator sessions!
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
    eye: `<svg class="svg-icon" viewBox="0 0 16 16" width="12" height="12" fill="none" stroke="currentColor" stroke-width="1.3"><path d="M1 8s3-5 7-5 7 5 7 5-3 5-7 5-7-5-7-5z"/><circle cx="8" cy="8" r="2.5"/></svg>`,
    arrowLeft: `<svg class="svg-icon" viewBox="0 0 16 16" width="15" height="15" fill="currentColor"><path fill-rule="evenodd" d="M7.78 12.53a.75.75 0 01-1.06 0L2.47 8.28a.75.75 0 010-1.06l4.25-4.25a.75.75 0 011.06 1.06L4.81 7h7.44a.75.75 0 010 1.5H4.81l2.97 2.97a.75.75 0 010 1.06z"></path></svg>`,
    arrowRight: `<svg class="svg-icon" viewBox="0 0 16 16" width="15" height="15" fill="currentColor"><path fill-rule="evenodd" d="M8.22 3.47a.75.75 0 011.06 0l4.25 4.25a.75.75 0 010 1.06l-4.25 4.25a.75.75 0 01-1.06-1.06L11.19 9H3.75a.75.75 0 010-1.5h7.44L8.22 4.53a.75.75 0 010-1.06z"></path></svg>`,
    trash: `<svg class="svg-icon" viewBox="0 0 16 16" width="13" height="13" fill="none" stroke="currentColor" stroke-width="1.4" stroke-linecap="round" stroke-linejoin="round"><path d="M2 4h12M5.3 4V2.5a1 1 0 0 1 1-1h3.4a1 1 0 0 1 1 1V4m2 0v9.5a1.5 1.5 0 0 1-1.5 1.5h-6.4A1.5 1.5 0 0 1 3.3 13.5V4"/></svg>`,
    gitPullRequest: `<svg class="svg-icon" viewBox="0 0 16 16" width="13" height="13" fill="currentColor"><path fill-rule="evenodd" d="M7.177 3.073L9.573.677A.25.25 0 0110 .854v4.792a.25.25 0 01-.427.177L7.177 3.427a.25.25 0 010-.354zM3.75 2.5a.75.75 0 100 1.5.75.75 0 000-1.5zm-2.25.75a2.25 2.25 0 113 2.122v5.256a2.251 2.251 0 11-1.5 0V5.372A2.25 2.25 0 011.5 3.25zM11 2.5h-1V4h1a1 1 0 011 1v5.628a2.251 2.251 0 101.5 0V5A2.5 2.5 0 0011 2.5zm1 10.25a.75.75 0 111.5 0 .75.75 0 01-1.5 0zM3.75 12a.75.75 0 100 1.5.75.75 0 000-1.5z"></path></svg>`
  };

  let catalog = null;
  let currentLang = null;
  let currentFilter = 'all';
  let searchQuery = '';
  let currentPage = 1;
  const PAGE_SIZE = 50;

  // Cloudflare Worker Staging Endpoint
  const WORKER_URL = window.TUNETASTIC_WORKER_URL || "https://tunetastic-l10n-staging.amit-ku-p-2806.workers.dev";

  // In-memory Admin session auth
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
  const viewSubmissionsModal = document.getElementById('view-submissions-modal');
  const draftResumeModal = document.getElementById('draft-resume-modal');
  const draftModalMainView = document.getElementById('draft-modal-main-view');
  const draftModalConfirmView = document.getElementById('draft-modal-confirm-view');

  // Active Key for Modals
  let activeViewSubmissionsItem = null;

  // =========================================================================
  // [DEV_SIMULATION_START] - Set to false or delete this block to remove later
  // =========================================================================
  const ENABLE_DEV_SIMULATION = true;
  const SIM_LANG_CODE = "sim-DEV";

  const SIMULATION_DATA = {
    meta: {
      code: "sim-DEV",
      name: "Simulation Lab",
      native: "Simulation Lab",
      country: "un",
      flagHtml: `<span style="font-size: 24px; line-height: 1; display: inline-block;">🧪</span>`,
      totalCount: 840,
      translatedCount: 4,
      progressPct: 1,
      hasResw: true
    },
    // Official translations in app (Already Translated - Green)
    translations: {
      "AppTitle.Text": "Tunetastic [Official Build]",
      "SearchBox.PlaceholderText": "Search music library...",
      "ThemeButton_ToolTip": "Switch Dark or Light Theme",
      "ShuffleButton_ToolTip": "Random shuffle playback"
    },
    // User local drafts (Drafted By You - Purple)
    drafts: {
      "VolumeMuteButton_ToolTip": "Mute or Unmute Volume"
    },
    // Live Cloudflare status (Locks, Approved, Proposals)
    liveStatus: {
      locks: {
        "NextButton_ToolTip": "other",             // Orange: In Progress By Others
        "VolumeMuteButton_ToolTip": "mine"         // Purple: Drafted By You
      },
      approved: {
        // Yellow: Approved (Staged in Admin)
        "PlayPauseButton_ToolTip": {
          value: "Play or Pause Track",
          translation: "Play or Pause Track",
          contributor: "Priya",
          contributors: ["Priya"],
          approvedAt: new Date(Date.now() - 3600000).toISOString()
        },
        // Dual Badge: [ Approved ] + [ Submitted ] (Overwrite suggestion on approved string)
        "PreviousButton_ToolTip": {
          value: "Skip to Previous Track",
          translation: "Skip to Previous Track",
          contributor: "Rahul",
          contributors: ["Rahul"],
          approvedAt: new Date(Date.now() - 7200000).toISOString()
        }
      },
      proposals: {
        // Blue: Submitted (on untranslated string)
        "RewindButton_ToolTip": { count: 2, isFull: false },
        // Dual Badge: [ Approved ] + [ Submitted ]
        "PreviousButton_ToolTip": { count: 1, isFull: false },
        // Dual Badge: [ Already Translated ] + [ Submitted ] (Overwrite suggestion on translated string)
        "AppTitle.Text": { count: 2, isFull: false }
      }
    },
    // Detailed proposal history for View Submissions modal & Admin console
    proposals: {
      "RewindButton_ToolTip": [
        { id: "sim_p1", text: "Rewind 5 Seconds", contributor: "Amit", timestamp: new Date(Date.now() - 3600000).toISOString() },
        { id: "sim_p2", text: "Jump Back 5s", contributor: "Sarah", timestamp: new Date(Date.now() - 7200000).toISOString() }
      ],
      "PreviousButton_ToolTip": [
        { id: "sim_p3", text: "Previous Song / Rewind", contributor: "Carlos", timestamp: new Date(Date.now() - 1800000).toISOString() }
      ],
      "AppTitle.Text": [
        { id: "sim_p4", text: "Tunetastic Music", contributor: "Dev", timestamp: new Date(Date.now() - 5400000).toISOString() },
        { id: "sim_p5", text: "Tunetastic Player", contributor: "Elena", timestamp: new Date(Date.now() - 9000000).toISOString() }
      ]
    }
  };

  function applySimulationCatalog(cat) {
    if (!ENABLE_DEV_SIMULATION || !cat) return;
    if (!cat.languages) cat.languages = {};

    // Generate balanced simulated distribution across catalog items for rich UI visualization
    if (cat.items && cat.items.length > 20) {
      const items = cat.items;
      // 1. Translated: items 0 to 259 (~260 items)
      for (let i = 0; i < Math.min(260, items.length); i++) {
        const k = items[i].key;
        if (!SIMULATION_DATA.translations[k]) {
          SIMULATION_DATA.translations[k] = `[Sim-Translated] ${items[i].en}`;
        }
      }
      // 2. Approved: items 260 to 399 (~140 items)
      for (let i = 260; i < Math.min(400, items.length); i++) {
        const k = items[i].key;
        if (!SIMULATION_DATA.liveStatus.approved[k]) {
          SIMULATION_DATA.liveStatus.approved[k] = {
            value: `[Sim-Approved] ${items[i].en}`,
            translation: `[Sim-Approved] ${items[i].en}`,
            contributor: 'Community Contributor',
            contributors: ['Community Contributor'],
            approvedAt: new Date(Date.now() - 3600000).toISOString()
          };
        }
      }
      // 3. Submitted: items 400 to 489 (~90 items)
      for (let i = 400; i < Math.min(490, items.length); i++) {
        const k = items[i].key;
        if (!SIMULATION_DATA.liveStatus.proposals[k]) {
          SIMULATION_DATA.liveStatus.proposals[k] = { count: 1, isFull: false };
        }
      }
      // 4. Drafted by you: items 490 to 539 (~50 items)
      for (let i = 490; i < Math.min(540, items.length); i++) {
        const k = items[i].key;
        if (!SIMULATION_DATA.drafts[k]) {
          SIMULATION_DATA.drafts[k] = `[Sim-Draft] ${items[i].en}`;
        }
      }
      // 5. In progress by others: items 540 to 579 (~40 items)
      for (let i = 540; i < Math.min(580, items.length); i++) {
        const k = items[i].key;
        if (!SIMULATION_DATA.liveStatus.locks[k]) {
          SIMULATION_DATA.liveStatus.locks[k] = 'other';
        }
      }
      // Remaining items 580 to 839 (~260 items) remain untranslated (Need Translation)!
    }

    cat.languages[SIM_LANG_CODE] = {
      ...SIMULATION_DATA.meta,
      totalCount: cat.totalStrings || 840,
      translations: { ...SIMULATION_DATA.translations }
    };
  }
  // =========================================================================
  // [DEV_SIMULATION_END]
  // =========================================================================

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
    if (ENABLE_DEV_SIMULATION && code === SIM_LANG_CODE) return SIMULATION_DATA.meta;

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

      if (!catalog.languages) {
        catalog.languages = {};
      }
      delete catalog.languages['qps-ploc'];

      if (ENABLE_DEV_SIMULATION) {
        applySimulationCatalog(catalog);
      }

      // Enhance languages discovered from .resw files in the C# repository
      for (const code of Object.keys(catalog.languages)) {
        catalog.languages[code].hasResw = true;
        const dynamicMeta = resolveLanguageMeta(code);
        if (dynamicMeta) {
          catalog.languages[code] = { ...catalog.languages[code], ...dynamicMeta };
        }
      }

      if (document.getElementById('stat-total-strings')) {
        document.getElementById('stat-total-strings').textContent = catalog.totalStrings || 828;
      }
      if (document.getElementById('stat-lang-count') && catalog.languages) {
        document.getElementById('stat-lang-count').textContent = Object.keys(catalog.languages).length;
      }
    } catch (err) {
      console.warn('Could not load catalog.json:', err);
      catalog = {
        version: "2.0",
        totalStrings: 0,
        languages: {},
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
    if (viewName !== 'workspace') {
      stopLiveStatusPolling();
      stopDraftTimer();
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

  function getLanguageBreakdown(langCode) {
    const total = (catalog && catalog.items && catalog.items.length) || (catalog && catalog.totalStrings) || 840;
    let counts = {
      translated: 0,
      approved: 0,
      submitted: 0,
      mine: 0,
      other: 0,
      available: 0,
      tokens: 0
    };

    if (ENABLE_DEV_SIMULATION && langCode === SIM_LANG_CODE) {
      // In dev simulation, items are divided into distinct non-overlapping bands:
      // Translated: 260 items (0-259)
      // Approved: 140 items (260-399)
      // Submitted: 90 items (400-489)
      // Drafted by you: 50 items (490-539)
      // In progress by others: 40 items (540-579)
      // Need translation: remaining 260 items (580-839)
      // Total: 260 + 140 + 90 + 50 + 40 + 260 = 840
      counts.translated = 260;
      counts.approved = 140;
      counts.submitted = 90;
      counts.mine = 50;
      counts.other = 40;
      counts.available = 260;
      counts.tokens = 142;
    } else {
      const localTrans = getAllLocalTranslations(langCode) || {};
      const draftsRaw = safeStorageGet(`tunetastic_drafts_${langCode}`);
      let drafts = {};
      try { if (draftsRaw) drafts = JSON.parse(draftsRaw); } catch (e) {}
      const submitted = getSubmittedTranslations(langCode) || {};

      if (catalog && catalog.items && catalog.items.length > 0) {
        catalog.items.forEach(item => {
          const trans = getCatalogTranslation(langCode, item.key);
          const hasLocalDraft = Boolean(drafts[item.key] || localTrans[item.key]);
          const isSub = Boolean(submitted[item.key]);

          if (trans && trans.trim()) {
            counts.translated++;
          } else if (isSub) {
            counts.submitted++;
          } else if (hasLocalDraft) {
            counts.mine++;
          } else {
            counts.available++;
          }
          if (item.placeholders && item.placeholders.length > 0) counts.tokens++;
        });
      } else {
        const localDoneCount = Object.keys(localTrans).length;
        counts.translated = localDoneCount;
        counts.available = Math.max(0, total - localDoneCount);
      }
    }

    // Total percentile done is strictly approved + translated on both charts
    const completed = counts.translated + counts.approved;
    const pct = total > 0 ? Math.round((completed / total) * 100) : 0;

    // Full 6-category status distribution for Workspace (inside view)
    const catData = [
      { key: 'translated', label: 'Translated', count: counts.translated, color: '#2ed573', glow: 'rgba(46, 213, 115, 0.55)', filter: 'translated' },
      { key: 'approved', label: 'Approved', count: counts.approved, color: '#facc15', glow: 'rgba(250, 204, 21, 0.55)', filter: 'approved' },
      { key: 'submitted', label: 'Submitted', count: counts.submitted, color: '#3b82f6', glow: 'rgba(59, 130, 246, 0.55)', filter: 'submitted' },
      { key: 'mine', label: 'Drafted', count: counts.mine, color: '#a855f7', glow: 'rgba(168, 85, 247, 0.55)', filter: 'mine' },
      { key: 'other', label: 'In Progress', count: counts.other, color: '#f97316', glow: 'rgba(249, 115, 22, 0.55)', filter: 'other' },
      { key: 'available', label: 'Need Translation', count: counts.available, color: '#64748b', glow: 'rgba(100, 116, 139, 0.45)', filter: 'available' }
    ];

    // Simplified 3-category status distribution for Home Screen cards (outside view):
    // Only Translated, Approved, and Need Translation (all remaining non-done strings)
    const homeNeedTranslation = Math.max(0, total - (counts.translated + counts.approved));
    const homeCatData = [
      { key: 'translated', label: 'Translated', count: counts.translated, color: '#2ed573' },
      { key: 'approved', label: 'Approved', count: counts.approved, color: '#facc15' },
      { key: 'available', label: 'Need Translation', count: homeNeedTranslation, color: '#64748b' }
    ];

    return { total, completed, pct, counts, catData, homeCatData };
  }

  function renderHomeLanguages() {
    languagesGrid.innerHTML = '';
    if (!catalog || !catalog.languages) return;

    const rawLangs = Object.values(catalog.languages).filter(l => l.code !== 'qps-ploc');
    const badge = document.getElementById('lang-counter-badge');
    if (badge) badge.textContent = `${rawLangs.length} Locales Active`;

    if (rawLangs.length === 0) {
      languagesGrid.innerHTML = `
        <div style="grid-column: 1 / -1; text-align: center; padding: 48px 24px; color: var(--text-secondary); background: var(--surface-glass); border-radius: var(--radius-xl); border: 1px dashed var(--border-glass);">
          <div style="font-size: 32px; margin-bottom: 12px;">🌐</div>
          <h4 style="color: var(--text-primary); margin-bottom: 6px;">No Languages Active Yet</h4>
          <p style="font-size: 13.5px; max-width: 480px; margin: 0 auto; line-height: 1.5;">
            Add a language directory and <code>Resources.resw</code> file under <code>Strings/</code> in the C# application repository to automatically enable it here.
          </p>
        </div>
      `;
      return;
    }

    // Detected language comes 1st; remaining languages are randomized on each refresh
    const langs = getPrioritizedHomeLanguages(rawLangs);

    langs.forEach((lang, index) => {
      const meta = resolveLanguageMeta(lang.code) || lang;
      const breakdown = getLanguageBreakdown(lang.code);
      const total = breakdown.total;
      const completed = breakdown.completed;
      const pct = breakdown.pct;
      const catData = breakdown.homeCatData || breakdown.catData;

      const card = document.createElement('div');
      card.className = 'lang-card-item lang-card';
      card.style.setProperty('--i', index);
      card.onclick = () => { window.location.hash = `#translate/${lang.code}`; };

      // High-resolution Segmented Pie Chart for Language Card
      // Center (52, 52), R = 38, C = 2 * PI * 38 ~ 238.76104
      const R = 38;
      const C = 2 * Math.PI * R;
      const activeCats = catData.filter(c => c.count > 0);
      const hasMultiple = activeCats.length > 1;
      const gap = hasMultiple ? 1.8 : 0;

      let slicesHtml = '';
      let cumulativeOffset = 0;

      activeCats.forEach(cat => {
        const rawArc = (cat.count / total) * C;
        const visibleArc = Math.max(1.0, rawArc - gap);
        const remainingCirc = C - visibleArc;
        const catPct = total > 0 ? ((cat.count / total) * 100).toFixed(0) : 0;

        slicesHtml += `
          <circle class="card-donut-slice slice-${cat.key}"
            cx="52" cy="52" r="${R}" fill="none"
            stroke="${cat.color}" stroke-width="16"
            stroke-dasharray="${visibleArc} ${remainingCirc}"
            stroke-dashoffset="${-cumulativeOffset}"
            data-key="${cat.key}" data-label="${cat.label}" data-count="${cat.count}" data-color="${cat.color}"
            data-pct="${catPct}%">
          </circle>
        `;
        cumulativeOffset += rawArc;
      });

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
            <!-- Background Circular Track -->
            <circle class="donut-bg-ring" cx="52" cy="52" r="${R}" fill="none" stroke="rgba(255, 255, 255, 0.08)" stroke-width="16"></circle>
            <!-- Slices Group (rotated -90deg so 12 o'clock is 0) -->
            <g class="card-donut-slices" transform="rotate(-90 52 52)">
              ${slicesHtml}
            </g>
            <!-- Central Core Disc -->
            <circle cx="52" cy="52" r="28" fill="rgba(12, 18, 32, 0.96)" stroke="rgba(255, 255, 255, 0.07)" stroke-width="1"></circle>
            <!-- Integrated SVG Text HUD -->
            <g class="card-donut-hud" style="pointer-events: none; user-select: none;">
              <text class="card-hud-pct" x="52" y="47" text-anchor="middle" fill="#60cdff" font-size="15" font-weight="800" font-family="system-ui, -apple-system, sans-serif">${pct}%</text>
              <text class="card-hud-label" x="52" y="58" text-anchor="middle" fill="#94a3b8" font-size="6.5" font-weight="700" letter-spacing="0.4" font-family="system-ui, -apple-system, sans-serif">COMPLETE</text>
              <text class="card-hud-sub" x="52" y="67" text-anchor="middle" fill="#64748b" font-size="6" font-weight="600" font-family="system-ui, -apple-system, sans-serif">${completed} / ${total}</text>
            </g>
          </svg>
        </div>

        <div class="card-stats-footer">
          <div class="card-strings-badge ${pct === 100 ? 'status-done' : (completed > 0 ? 'status-progress' : 'status-empty')}">
            <span class="strings-dot"></span>
            <span class="strings-count"><strong class="highlight-count">${completed}</strong><span class="sep-slash">/</span>${total} strings</span>
          </div>
          <span class="action-chip">Translate <span class="action-arrow">${ICONS.arrowRight}</span></span>
        </div>
      `;

      // Slice hover interactivity on each card
      const slices = card.querySelectorAll('.card-donut-slice');
      const hudPct = card.querySelector('.card-hud-pct');
      const hudLbl = card.querySelector('.card-hud-label');
      const hudSub = card.querySelector('.card-hud-sub');

      slices.forEach(slice => {
        slice.addEventListener('mousedown', (e) => e.preventDefault());
        slice.addEventListener('mouseenter', (e) => {
          e.stopPropagation();
          const label = slice.getAttribute('data-label') || '';
          const count = slice.getAttribute('data-count') || '0';
          const slicePct = slice.getAttribute('data-pct') || '0%';
          const color = slice.getAttribute('data-color') || '#60cdff';

          if (hudPct) {
            hudPct.textContent = slicePct;
            hudPct.setAttribute('fill', color);
          }
          if (hudLbl) {
            hudLbl.textContent = label.toUpperCase();
            hudLbl.setAttribute('fill', color);
          }
          if (hudSub) {
            hudSub.textContent = `${count} strings`;
            hudSub.setAttribute('fill', '#94a3b8');
          }
        });

        slice.addEventListener('mouseleave', (e) => {
          e.stopPropagation();
          if (hudPct) {
            hudPct.textContent = `${pct}%`;
            hudPct.setAttribute('fill', '#60cdff');
          }
          if (hudLbl) {
            hudLbl.textContent = 'COMPLETE';
            hudLbl.setAttribute('fill', '#94a3b8');
          }
          if (hudSub) {
            hudSub.textContent = `${completed} / ${total}`;
            hudSub.setAttribute('fill', '#64748b');
          }
        });
      });

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

    // Always fetch live status (read-only for all sessions including incognito). Check resumption for standard sessions.
    try {
      const syncTasks = [fetchLiveStatus()];
      if (!isIncognitoMode) {
        syncTasks.push(checkDraftResumption());
      }
      await Promise.allSettled(syncTasks);
    } catch (e) {
      console.warn("Background workspace sync error:", e);
    }

    if (ENABLE_DEV_SIMULATION && langCode === SIM_LANG_CODE) {
      currentDrafts = { ...SIMULATION_DATA.drafts };
    }

    // Render cards and update stats
    renderCards();
    updateWorkspaceStats();
    updateSaveIndicatorUi();
    startLiveStatusPolling();
    startDraftTimer();
  }

  async function fetchLiveStatus() {
    if (ENABLE_DEV_SIMULATION && currentLang === SIM_LANG_CODE) {
      liveStatus = JSON.parse(JSON.stringify(SIMULATION_DATA.liveStatus));
      return;
    }
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
        const isEditing = document.activeElement && document.activeElement.tagName === 'TEXTAREA';
        if ((currentFilter !== 'all' || (searchQuery && searchQuery.trim())) && !isEditing) {
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

  function updateCardBadgesAndActions(card, item, statusInfo) {
    const badgesContainer = card.querySelector('.card-badges');
    if (badgesContainer) {
      badgesContainer.innerHTML = statusInfo.badges.map(b =>
        `<span class="studio-pill card-status-pill ${b.pillClass}"><span class="status-dot ${b.dotClass}"></span> ${b.label}</span>`
      ).join('');
    }

    const actionsBar = card.querySelector('.target-column .column-actions-bar');
    if (actionsBar) {
      const proposalMeta = liveStatus && liveStatus.proposals ? liveStatus.proposals[item.key] : null;
      const subCount = proposalMeta ? proposalMeta.count : 0;

      // 1. View Submissions button
      let viewSubBtn = actionsBar.querySelector('.btn-view-submissions');
      if (statusInfo.showViewSubmissionsBtn) {
        if (!viewSubBtn) {
          viewSubBtn = document.createElement('button');
          viewSubBtn.className = 'btn-view-submissions';
          viewSubBtn.setAttribute('data-key', item.key);
          viewSubBtn.onclick = () => openViewSubmissionsModal(item);
          actionsBar.prepend(viewSubBtn);
        }
        viewSubBtn.innerHTML = `${ICONS.eye} View Submissions${subCount > 0 ? ` (${subCount})` : ''}`;
      } else if (viewSubBtn) {
        viewSubBtn.remove();
      }

      // 2. Suggest Alternative button (Only for approved/translated when not yet unlocked)
      let suggestBtn = actionsBar.querySelector('.btn-suggest');
      if (statusInfo.showSuggestBtn) {
        if (!suggestBtn) {
          suggestBtn = document.createElement('button');
          suggestBtn.className = 'btn-suggest';
          suggestBtn.setAttribute('data-key', item.key);
          suggestBtn.onclick = () => unlockForSuggestion(item, card);
          actionsBar.appendChild(suggestBtn);
        }
        suggestBtn.innerHTML = `${ICONS.bulb} Suggest Alternative`;
      } else if (suggestBtn) {
        suggestBtn.remove();
      }
    }
  }

  function unlockForSuggestion(item, card) {
    const textarea = card.querySelector('textarea.translation-textarea');
    const approvedText = getApprovedTranslation(item.key);
    const catalogTrans = getCatalogTranslation(currentLang, item.key) || '';
    const localDraft = getLocalDraft(currentLang, item.key);

    // Keep approved/translated text so the user can make edits to it
    const existingVal = (localDraft && localDraft.trim())
      ? localDraft
      : ((textarea && textarea.value && textarea.value.trim()) ? textarea.value : (approvedText || catalogTrans || ''));

    setLocalTranslation(currentLang, item.key, existingVal);
    scheduleDraftAutosave(item.key, existingVal);

    if (textarea) {
      textarea.value = existingVal;
      textarea.readOnly = false;
      textarea.style.opacity = '1';
      textarea.style.cursor = 'text';
      textarea.style.background = '';
      autoResizeTextarea(textarea);
      textarea.focus();
      textarea.setSelectionRange(textarea.value.length, textarea.value.length);
    }

    // Update Live Preview row
    const previewTransEl = card.querySelector('.preview-trans-text');
    const previewTargetRow = card.querySelector('.preview-row-target');
    if (previewTransEl) {
      const rendered = renderSimulatedText(existingVal, item.placeholders);
      previewTransEl.innerHTML = rendered || '<span class="preview-empty">No translation yet</span>';
      if (previewTargetRow) {
        previewTargetRow.setAttribute('title', existingVal ? `Translation Preview: ${existingVal}` : 'Translation Preview: No translation yet');
      }
    }

    // Check placeholder tokens
    if (item.placeholders && item.placeholders.length > 0) {
      const tokenWarn = card.querySelector('.token-missing-alert');
      const check = validateTokens(item.source, existingVal, item.placeholders);
      if (tokenWarn) {
        tokenWarn.style.display = check.isValid ? 'none' : 'flex';
        tokenWarn.innerHTML = check.isValid ? '' : `${ICONS.alert} Missing placeholder token: <strong>${check.missing.join(', ')}</strong>`;
      }
    }

    const updatedStatus = getItemStatusInfo(item);
    updateCardBadgesAndActions(card, item, updatedStatus);
    updateWorkspaceStats();
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
      updateCardBadgesAndActions(card, item, statusInfo);

      const textarea = card.querySelector('textarea.translation-textarea');
      if (textarea && document.activeElement !== textarea) {
        textarea.readOnly = statusInfo.isReadOnly;
        textarea.placeholder = statusInfo.placeholderText;
        if (statusInfo.isReadOnly) {
          textarea.style.opacity = '0.85';
          textarea.style.cursor = 'default';
          textarea.style.background = 'rgba(255,255,255,0.02)';
          textarea.value = statusInfo.displayText || '';
        } else {
          textarea.style.opacity = '1';
          textarea.style.cursor = 'text';
          textarea.style.background = '';
          textarea.value = statusInfo.displayText || '';
        }
        autoResizeTextarea(textarea);
      }

      // Update Translated Preview row if present
      const previewTransEl = card.querySelector('.preview-trans-text');
      const previewTargetRow = card.querySelector('.preview-row-target');
      if (previewTransEl) {
        const val = statusInfo.displayText || '';
        const rendered = renderSimulatedText(val, item.placeholders);
        previewTransEl.innerHTML = rendered || '<span class="preview-empty">No translation yet</span>';
        if (previewTargetRow) {
          previewTargetRow.setAttribute('title', val ? `Translation Preview: ${val}` : 'Translation Preview: No translation yet');
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
      let cloudDraftUpdatedAt = null;
      if (res.ok) {
        const data = await res.json();
        cloudDrafts = data.drafts || {};
        cloudDraftUpdatedAt = data.updatedAt || null;
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

        // Helper: Load drafts into workspace and close modal (preserving original edit timestamp)
        const restoreDraftsAndClose = async () => {
          isDraftPendingResumption = false;

          // Preserve the original edit timestamp; clock should NOT reset on resumption!
          let existingUpdated = parseInt(safeStorageGet(`tunetastic_draft_updated_${currentLang}`), 10);
          if (!existingUpdated || isNaN(existingUpdated)) {
            if (cloudDraftUpdatedAt) {
              const parsed = new Date(cloudDraftUpdatedAt).getTime();
              if (!isNaN(parsed)) existingUpdated = parsed;
            }
          }
          if (!existingUpdated || isNaN(existingUpdated)) {
            existingUpdated = Date.now();
          }

          safeStorageSet(`tunetastic_l10n_${currentLang}`, JSON.stringify(currentDrafts), true);
          safeStorageSet(`tunetastic_draft_updated_${currentLang}`, String(existingUpdated), true);

          draftResumeModal.classList.remove('active');
          renderCards();
          updateWorkspaceStats();
          updateDraftTimerDisplay();
          showToast(`Restored ${count} draft translations!`);

          // Re-acquire active locks and sync drafts in Cloudflare KV under current session
          if (!isIncognitoMode && currentDrafts && Object.keys(currentDrafts).length > 0) {
            try {
              await fetch(`${WORKER_URL}/api/draft`, {
                method: 'POST',
                headers: {
                  'Content-Type': 'application/json',
                  'X-Session-ID': SESSION_ID
                },
                body: JSON.stringify({
                  language: currentLang,
                  drafts: currentDrafts,
                  updatedAt: new Date(existingUpdated).toISOString()
                })
              });
              await fetchLiveStatus();
              syncLiveStatusToDOM();
              updateWorkspaceStats();
            } catch (e) {
              console.warn("Failed to re-acquire draft locks on resumption:", e);
            }
          }
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
                } else if (lock === 'both') {
                  liveStatus.locks[k] = 'other';
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
    if (!isIncognitoMode) {
      updateSaveIndicatorUi('saving');
    }
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

  function getApprovedTranslation(key) {
    if (!liveStatus || !liveStatus.approved) return '';
    const entry = liveStatus.approved[key];
    if (!entry) return '';
    if (typeof entry === 'string') return entry;
    return entry.value || entry.translation || '';
  }

  function getItemStatusInfo(item) {
    const approvedEntry = liveStatus && liveStatus.approved ? liveStatus.approved[item.key] : null;
    const approvedText = getApprovedTranslation(item.key);
    const mergedEntry = liveStatus && liveStatus.merged ? liveStatus.merged[item.key] : null;
    const isMerged = Boolean(mergedEntry);
    const proposalMeta = liveStatus && liveStatus.proposals ? liveStatus.proposals[item.key] : null;
    const catalogTrans = getCatalogTranslation(currentLang, item.key);
    const localDraft = getLocalDraft(currentLang, item.key);
    const lock = liveStatus && liveStatus.locks ? liveStatus.locks[item.key] : null;

    const hasLocalDraft = Boolean(!isDraftPendingResumption && localDraft !== null && localDraft !== undefined && localDraft.trim().length > 0);
    const isOtherDrafting = lock === 'other' || lock === 'both';
    const hasServerProposals = Boolean(proposalMeta && proposalMeta.count > 0);

    // Stale local submitted record cleanup:
    // If key is approved/merged or server confirmed 0 proposals, purge stale local flag
    let isLocalSubmitted = isSubmittedKey(currentLang, item.key);
    if (isLocalSubmitted && (approvedEntry || isMerged || (liveStatus && liveStatus.proposals && !hasServerProposals))) {
      const subMap = getSubmittedTranslations(currentLang);
      if (subMap && subMap[item.key]) {
        delete subMap[item.key];
        safeStorageSet(`tunetastic_submitted_${currentLang}`, JSON.stringify(subMap));
      }
      isLocalSubmitted = false;
    }

    const isSubmitted = Boolean(hasServerProposals || isLocalSubmitted);
    const isApproved = Boolean(approvedEntry);
    const isTranslated = Boolean(isMerged || (catalogTrans && catalogTrans.trim()));

    // Base Primary Status
    let baseStatus = 'available';
    if (isApproved) {
      baseStatus = 'approved';
    } else if (isTranslated) {
      baseStatus = 'translated';
    } else if (isSubmitted) {
      baseStatus = 'submitted';
    } else if (isOtherDrafting) {
      baseStatus = 'other';
    } else if (hasLocalDraft) {
      baseStatus = 'mine';
    }

    // Determine Display Text & Read-Only state
    let displayText = '';
    let isReadOnly = false;
    let placeholderText = 'Type translation here...';
    let showSuggestBtn = false;

    // View Submissions button is strictly shown ONLY when there is a submitted proposal!
    const showViewSubmissionsBtn = isSubmitted;

    if (isApproved) {
      if (hasLocalDraft) {
        displayText = localDraft;
        isReadOnly = false;
        placeholderText = 'Edit approved translation...';
        showSuggestBtn = false;
      } else {
        displayText = approvedText;
        isReadOnly = true;
        placeholderText = 'Approved translation. Click Suggest Alternative to propose edits.';
        showSuggestBtn = true;
      }
    } else if (isTranslated) {
      const translatedVal = isMerged
        ? (mergedEntry.value || mergedEntry.translation || catalogTrans || '')
        : (catalogTrans || '');
      if (hasLocalDraft) {
        displayText = localDraft;
        isReadOnly = false;
        placeholderText = 'Edit existing translation...';
        showSuggestBtn = false;
      } else {
        displayText = translatedVal;
        isReadOnly = true;
        placeholderText = 'Existing translation. Click Suggest Alternative to propose edits.';
        showSuggestBtn = true;
      }
    } else if (isSubmitted) {
      showSuggestBtn = false;
      isReadOnly = false;
      if (hasLocalDraft) {
        displayText = localDraft;
        placeholderText = 'Type alternative suggestion here...';
      } else {
        displayText = '';
        placeholderText = 'Type alternative suggestion here...';
      }
    } else if (isOtherDrafting) {
      showSuggestBtn = false;
      isReadOnly = false;
      if (hasLocalDraft) {
        displayText = localDraft;
        placeholderText = 'In progress by another contributor... Type to suggest alternative.';
      } else {
        displayText = '';
        placeholderText = 'In progress by another contributor... Type to suggest alternative.';
      }
    } else { // available or mine
      showSuggestBtn = false;
      isReadOnly = false;
      displayText = hasLocalDraft ? localDraft : '';
      placeholderText = 'Type translation here...';
    }

    // Build Badges Array (Dual Badges: Approved + Submitted, Already Translated + Submitted)
    const badges = [];
    if (isApproved) {
      badges.push({ label: 'Approved', pillClass: 'pill-approved', dotClass: 'dot-approved' });
      if (isSubmitted) {
        badges.push({ label: 'Submitted', pillClass: 'pill-submitted', dotClass: 'dot-submitted' });
      }
      if (isOtherDrafting) {
        badges.push({ label: 'In Progress By Others', pillClass: 'pill-draft-other', dotClass: 'dot-in-progress' });
      }
      if (hasLocalDraft) {
        badges.push({ label: 'Drafted By You', pillClass: 'pill-draft-mine', dotClass: 'dot-draft' });
      }
    } else if (isTranslated) {
      badges.push({ label: 'Already Translated', pillClass: 'pill-translated', dotClass: 'dot-translated' });
      if (isSubmitted) {
        badges.push({ label: 'Submitted', pillClass: 'pill-submitted', dotClass: 'dot-submitted' });
      }
      if (isOtherDrafting) {
        badges.push({ label: 'In Progress By Others', pillClass: 'pill-draft-other', dotClass: 'dot-in-progress' });
      }
      if (hasLocalDraft) {
        badges.push({ label: 'Drafted By You', pillClass: 'pill-draft-mine', dotClass: 'dot-draft' });
      }
    } else if (isSubmitted) {
      badges.push({ label: 'Submitted', pillClass: 'pill-submitted', dotClass: 'dot-submitted' });
      if (isOtherDrafting) {
        badges.push({ label: 'In Progress By Others', pillClass: 'pill-draft-other', dotClass: 'dot-in-progress' });
      }
      if (hasLocalDraft) {
        badges.push({ label: 'Drafted By You', pillClass: 'pill-draft-mine', dotClass: 'dot-draft' });
      }
    } else if (isOtherDrafting) {
      badges.push({ label: 'In Progress By Others', pillClass: 'pill-draft-other', dotClass: 'dot-in-progress' });
      if (hasLocalDraft) {
        badges.push({ label: 'Drafted By You', pillClass: 'pill-draft-mine', dotClass: 'dot-draft' });
      }
    } else { // available or mine
      if (hasLocalDraft) {
        badges.push({ label: 'Drafted By You', pillClass: 'pill-draft-mine', dotClass: 'dot-draft' });
      } else {
        badges.push({ label: 'Need Translation', pillClass: 'pill-available', dotClass: 'dot-available' });
      }
    }

    return {
      status: baseStatus,
      isApproved,
      isTranslated,
      isSubmitted,
      hasLocalDraft,
      isOtherDrafting,
      isReadOnly,
      displayText,
      placeholderText,
      showSuggestBtn,
      showViewSubmissionsBtn,
      badges,
      label: badges[0].label,
      pillClass: badges[0].pillClass,
      dotClass: badges[0].dotClass
    };
  }

  function getFilteredItems() {
    if (!catalog || !catalog.items) return [];

    return catalog.items.filter(item => {
      const statusInfo = getItemStatusInfo(item);

      if (currentFilter === 'available' && (statusInfo.isApproved || statusInfo.isTranslated || statusInfo.isSubmitted || statusInfo.hasLocalDraft)) return false;
      if (currentFilter === 'mine' && !statusInfo.hasLocalDraft) return false;
      if (currentFilter === 'other' && !statusInfo.isOtherDrafting) return false;
      if (currentFilter === 'submitted' && !statusInfo.isSubmitted) return false;
      if (currentFilter === 'approved' && !statusInfo.isApproved) return false;
      if (currentFilter === 'translated' && (!statusInfo.isTranslated || statusInfo.isApproved)) return false;

      // Legacy filter fallback
      if (currentFilter === 'missing' && (statusInfo.isApproved || statusInfo.isTranslated || statusInfo.isSubmitted || statusInfo.hasLocalDraft)) return false;
      if (currentFilter === 'completed' && !statusInfo.isTranslated && !statusInfo.isApproved) return false;

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
    syncFilterPillActiveStates();
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

      const badgesHtml = statusInfo.badges.map(b => `<span class="studio-pill card-status-pill ${b.pillClass}"><span class="status-dot ${b.dotClass}"></span> ${b.label}</span>`).join('');
      const proposalMeta = liveStatus && liveStatus.proposals ? liveStatus.proposals[item.key] : null;
      const subCount = proposalMeta ? proposalMeta.count : 0;

      const viewSubmissionsBtnHtml = statusInfo.showViewSubmissionsBtn
        ? `<button class="btn-view-submissions" data-key="${escapeHtml(item.key)}">${ICONS.eye} View Submissions${subCount > 0 ? ` (${subCount})` : ''}</button>`
        : '';

      const suggestBtnHtml = statusInfo.showSuggestBtn
        ? `<button class="btn-suggest" data-key="${escapeHtml(item.key)}">${ICONS.bulb} Suggest Alternative</button>`
        : '';

      const card = document.createElement('div');
      card.className = 'studio-card';
      card.id = `card-${item.key}`;

      const contextHtml = renderContextBlockHtml(item.comment, item.key);

      const sourcePreviewHtml = renderSimulatedText(item.source, item.placeholders);
      const targetPreviewHtml = renderSimulatedText(translation, item.placeholders);

      card.innerHTML = `
        <div class="studio-card-header">
          <div class="card-identity">
            ${contextHtml}
          </div>
          <div class="card-badges">
            ${badgesHtml}
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
              <span>Translation</span>
            </div>
            <textarea class="translation-textarea" rows="1" 
              placeholder="${statusInfo.placeholderText}"
              ${isReadOnly ? 'readonly style="opacity: 0.85; background: rgba(255,255,255,0.02); cursor: default;"' : ''}>${escapeHtml(translation)}</textarea>

            <div class="token-missing-alert" style="display: ${tokenStatus.isValid ? 'none' : 'flex'};">
              ${ICONS.alert} Missing placeholder token: <strong>${tokenStatus.missing.join(', ')}</strong>
            </div>

            <div class="column-actions-bar">
              ${viewSubmissionsBtnHtml}
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

          // Update status badges and action bar buttons on card dynamically
          const updatedStatusInfo = getItemStatusInfo(item);
          updateCardBadgesAndActions(card, item, updatedStatusInfo);

          // Validate Tokens
          if (hasPlaceholders) {
            const tokenWarn = card.querySelector('.token-missing-alert');
            const check = validateTokens(item.source, val, item.placeholders);
            if (check.isValid) {
              if (tokenWarn) tokenWarn.style.display = 'none';
            } else {
              if (tokenWarn) {
                tokenWarn.style.display = 'flex';
                tokenWarn.innerHTML = `${ICONS.alert} Missing placeholder token: <strong>${check.missing.join(', ')}</strong>`;
              }
            }
          }

          // Trigger debounced Cloudflare Draft Autosave
          scheduleDraftAutosave(item.key, val);
          updateWorkspaceStats();
        });
      }

      // Event listener for View Submissions Button
      const viewSubBtn = card.querySelector('.btn-view-submissions');
      if (viewSubBtn) {
        viewSubBtn.addEventListener('click', () => {
          openViewSubmissionsModal(item);
        });
      }

      // Event listener for Suggest Alternative Button
      const suggestBtn = card.querySelector('.btn-suggest');
      if (suggestBtn) {
        suggestBtn.addEventListener('click', () => {
          unlockForSuggestion(item, card);
        });
      }
    });
  }

  let currentWorkspaceCategories = null;
  let lastWorkspaceStats = null;
  let lastWorkspaceSliceGeo = null;

  function updateWorkspaceStats() {
    if (!catalog || !catalog.items) return;
    const breakdown = getLanguageBreakdown(currentLang);
    const { total, completed, pct, counts, catData } = breakdown;
    const remaining = total - completed;

    const progressFill = document.getElementById('workspace-progress-fill');
    if (progressFill) progressFill.style.width = `${pct}%`;

    const doneCount = document.getElementById('ws-done-count');
    if (doneCount) doneCount.textContent = `${completed}`;

    const missingCount = document.getElementById('ws-missing-count');
    if (missingCount) missingCount.textContent = `${remaining}`;

    const tokensEl = document.getElementById('ws-tokens-count');
    if (tokensEl) tokensEl.textContent = `${counts.tokens || 142}`;

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

    currentWorkspaceCategories = catData;
    lastWorkspaceStats = { total, completed, remaining, pct, counts };

    renderDonutChart(catData, total, completed, pct);
    renderAppReadinessHud();
  }

  // -------------------------------------------------------------------------
  // 6a-1. INTERACTIVE TRANSLATION PIE CHART & SATELLITE CIRCUIT
  // -------------------------------------------------------------------------

  const WORKSPACE_CIRCUIT_CATEGORIES = [
    { key: 'translated', label: 'Translated', side: 'right', row: 1, color: '#2ed573', glow: 'rgba(46, 213, 115, 0.65)', filter: 'translated' },
    { key: 'approved', label: 'Approved', side: 'right', row: 2, color: '#facc15', glow: 'rgba(250, 204, 21, 0.65)', filter: 'approved' },
    { key: 'submitted', label: 'Submitted', side: 'right', row: 3, color: '#3b82f6', glow: 'rgba(59, 130, 246, 0.65)', filter: 'submitted' },
    { key: 'mine', label: 'Your Drafts', side: 'left', row: 3, color: '#a855f7', glow: 'rgba(168, 85, 247, 0.65)', filter: 'mine' },
    { key: 'other', label: 'In Progress', side: 'left', row: 2, color: '#f97316', glow: 'rgba(249, 115, 22, 0.65)', filter: 'other' },
    { key: 'available', label: 'Need Translation', side: 'left', row: 1, color: '#64748b', glow: 'rgba(100, 116, 139, 0.45)', filter: 'available' }
  ];

  const CIRCUIT_ROW_Y = { 1: 42, 2: 115, 3: 188 };
  const CIRCUIT_PILL_BOX_Y = { 1: 25, 2: 98, 3: 171 };

  let hudCounterAnimId = null;
  function animateCenterCounter(targetPct) {
    const hudPct = document.getElementById('pie-hud-pct') || document.getElementById('donut-hud-pct');
    if (!hudPct) return;
    if (hudCounterAnimId) cancelAnimationFrame(hudCounterAnimId);
    const duration = 750;
    const start = performance.now();
    function tick(now) {
      const p = Math.min(1, (now - start) / duration);
      const ease = 1 - Math.pow(1 - p, 3);
      const cur = Math.round(targetPct * ease);
      hudPct.textContent = `${cur}%`;
      if (p < 1) {
        hudCounterAnimId = requestAnimationFrame(tick);
      } else {
        hudPct.textContent = `${targetPct}%`;
      }
    }
    hudCounterAnimId = requestAnimationFrame(tick);
  }

  function updateCenterHudDefault(pct, completed, total, animate = false) {
    const hudPct = document.getElementById('pie-hud-pct') || document.getElementById('donut-hud-pct');
    const hudLbl = document.getElementById('pie-hud-label') || document.getElementById('donut-hud-label');
    const hudSub = document.getElementById('pie-hud-sub') || document.getElementById('donut-hud-sub');
    if (hudPct) {
      if (animate && pct > 0) {
        animateCenterCounter(pct);
      } else {
        hudPct.textContent = `${pct}%`;
      }
      hudPct.setAttribute('fill', '#60cdff');
      hudPct.style.color = '#60cdff';
    }
    if (hudLbl) {
      hudLbl.textContent = 'COMPLETE';
      hudLbl.setAttribute('fill', '#94a3b8');
      hudLbl.style.color = 'var(--text-tertiary)';
    }
    if (hudSub) {
      hudSub.textContent = `${completed} / ${total}`;
      hudSub.setAttribute('fill', '#64748b');
      hudSub.style.color = 'var(--text-tertiary)';
    }

    // Also synchronize condensed header progress bar
    const condPct = document.getElementById('condensed-pct-badge');
    const condCounts = document.getElementById('condensed-hud-counts');
    const condLabel = document.getElementById('condensed-hud-label');
    if (condPct) {
      condPct.textContent = `${pct}%`;
      condPct.style.color = 'var(--accent-cyan, #38bdf8)';
      condPct.style.borderColor = 'rgba(56, 189, 248, 0.3)';
    }
    if (condLabel) condLabel.textContent = 'PROGRESS';
    if (condCounts) condCounts.textContent = `${completed} / ${total} DONE`;
  }

  function renderDonutChart(catData, total, completed, pct) {
    const segmentsGroup = document.getElementById('donut-segments-group');
    const pathsGroup = document.getElementById('hero-connector-paths');
    const pillsGroup = document.getElementById('hero-satellite-pills');
    if (!segmentsGroup || !pathsGroup || !pillsGroup) return;

    segmentsGroup.innerHTML = '';
    pathsGroup.innerHTML = '';
    pillsGroup.innerHTML = '';

    // Center donut specs: R_mid = 62, stroke-width = 28 -> Outer rim R_outer = 76
    const R_mid = 62;
    const strokeW = 28;
    const R_outer = R_mid + (strokeW / 2); // 76
    const C = 2 * Math.PI * R_mid; // ~389.557489
    const Cx = 395;
    const Cy = 115;

    updateCenterHudDefault(pct, completed, total, true);

    if (total === 0) return;

    // Map catData into quick lookup
    const countMap = {};
    catData.forEach(c => { countMap[c.key] = c.count; });

    // Render Condensed Progress Bar Segments in Header Middle Slot
    const condBar = document.getElementById('condensed-progress-bar');
    if (condBar) {
      condBar.innerHTML = '';
      WORKSPACE_CIRCUIT_CATEGORIES.forEach(cat => {
        const cnt = countMap[cat.key] || 0;
        if (cnt > 0) {
          const segFrac = (cnt / total) * 100;
          const segEl = document.createElement('div');
          segEl.className = `condensed-seg seg-${cat.key}`;
          segEl.style.width = `${segFrac.toFixed(2)}%`;
          segEl.style.background = cat.color;
          segEl.style.boxShadow = `0 0 8px ${cat.color}80`;
          segEl.title = `${cat.label}: ${cnt} (${Math.round(segFrac)}%)`;
          segEl.addEventListener('click', e => {
            e.preventDefault();
            e.stopPropagation();
            triggerWorkspaceFilter(cat.filter);
          });
          condBar.appendChild(segEl);
        }
      });
    }

    // Step 1: Calculate cumulative angles for slices
    let cumulativeClockAngle = 0; // In radians, 0 = 12 o'clock
    const sliceGeo = {};

    WORKSPACE_CIRCUIT_CATEGORIES.forEach(cat => {
      const cnt = countMap[cat.key] || 0;
      const frac = total > 0 ? (cnt / total) : 0;
      const arcLength = frac * C;
      const angleSpan = frac * 2 * Math.PI;

      const startAngle = cumulativeClockAngle;
      const endAngle = cumulativeClockAngle + angleSpan;
      const midAngle = startAngle + (angleSpan / 2);

      // Screen angle in radians (0 = 3 o'clock / +X, clockwise)
      const screenMid = midAngle - (Math.PI / 2);
      const rimX = Cx + R_outer * Math.cos(screenMid);
      const rimY = Cy + R_outer * Math.sin(screenMid);

      sliceGeo[cat.key] = {
        count: cnt,
        pct: Math.round(frac * 100),
        frac,
        arcLength,
        angleSpan,
        startAngle,
        endAngle,
        midAngle,
        rimX,
        rimY
      };

      cumulativeClockAngle += angleSpan;
    });

    // Step 2: Render Slices
    const activeCats = WORKSPACE_CIRCUIT_CATEGORIES.filter(c => (countMap[c.key] || 0) > 0);
    const hasMultiple = activeCats.length > 1;
    const gap = hasMultiple ? 1.5 : 0;

    let curDashOffset = 0;
    WORKSPACE_CIRCUIT_CATEGORIES.forEach(cat => {
      const g = sliceGeo[cat.key];
      if (g.count > 0) {
        const visibleArc = Math.max(1.0, g.arcLength - gap);
        const circle = document.createElementNS('http://www.w3.org/2000/svg', 'circle');
        circle.setAttribute('class', `donut-segment segment-${cat.key}`);
        const segIdx = activeCats.indexOf(cat);
        circle.style.animationDelay = `${(segIdx * 0.08 + 0.04).toFixed(2)}s`;
        circle.setAttribute('cx', '0');
        circle.setAttribute('cy', '0');
        circle.setAttribute('r', `${R_mid}`);
        circle.setAttribute('fill', 'none');
        circle.setAttribute('stroke', cat.color);
        circle.setAttribute('stroke-width', `${strokeW}`);
        circle.setAttribute('stroke-dasharray', `${visibleArc.toFixed(2)} ${(C - visibleArc).toFixed(2)}`);
        circle.setAttribute('stroke-dashoffset', `${(-curDashOffset).toFixed(2)}`);
        circle.setAttribute('data-key', cat.key);
        circle.setAttribute('data-filter', cat.filter);
        circle.setAttribute('data-label', cat.label);
        circle.setAttribute('data-count', g.count);
        circle.setAttribute('role', 'button');
        circle.setAttribute('tabindex', '0');

        circle.addEventListener('mousedown', e => e.preventDefault());
        circle.addEventListener('mouseenter', () => highlightWorkspaceCategory(cat.key));
        circle.addEventListener('mouseleave', () => unhighlightWorkspaceCategory());
        circle.addEventListener('focus', () => highlightWorkspaceCategory(cat.key));
        circle.addEventListener('blur', () => unhighlightWorkspaceCategory());
        circle.addEventListener('keydown', e => {
          if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            triggerWorkspaceFilter(cat.filter);
          }
        });
        circle.addEventListener('click', e => {
          e.stopPropagation();
          circle.blur();
          triggerWorkspaceFilter(cat.filter);
        });

        segmentsGroup.appendChild(circle);
        curDashOffset += g.arcLength;
      }
    });

    // Save slice geometry globally for dynamic pointer line calculation
    lastWorkspaceSliceGeo = sliceGeo;

    // Single contextual pointer beam container (Option 2: Active-Only Single Pointer Beam)
    pathsGroup.innerHTML = '<path id="hero-single-pointer-path" class="single-pointer-path" d="" stroke="#60cdff" />';

    // Step 3: Render Symmetrical Satellite Pills
    WORKSPACE_CIRCUIT_CATEGORIES.forEach(cat => {
      const g = sliceGeo[cat.key];
      const isLeft = cat.side === 'left';
      const boxX = isLeft ? 10 : 590;
      const boxY = CIRCUIT_PILL_BOX_Y[cat.row];
      const isZero = g.count === 0;

      // Create ForeignObject Pill Element
      const fo = document.createElementNS('http://www.w3.org/2000/svg', 'foreignObject');
      fo.setAttribute('class', isLeft ? `pill-fo-left row-${cat.row}` : `pill-fo-right row-${cat.row}`);
      fo.setAttribute('x', `${boxX}`);
      fo.setAttribute('y', `${boxY}`);
      fo.setAttribute('width', '190');
      fo.setAttribute('height', '34');
      fo.setAttribute('style', 'overflow: visible; outline: none !important; border: none !important;');

      const pillDiv = document.createElement('div');
      pillDiv.setAttribute('xmlns', 'http://www.w3.org/1999/xhtml');
      pillDiv.setAttribute('data-key', cat.key);
      pillDiv.setAttribute('data-filter', cat.filter);

      if (isZero) {
        // Zero-count standby pill: perfectly aligned in row, no pointer beam
        pillDiv.className = 'satellite-pill pill-zero';
        pillDiv.title = `0 strings in ${cat.label}`;
        pillDiv.innerHTML = `
          <div class="pill-left-content"><span class="dot"></span><span>${cat.label}</span></div>
          <div><strong>0</strong><span class="sub"> 0%</span></div>
        `;
      } else {
        pillDiv.className = `satellite-pill pill-${cat.key}`;
        if (currentFilter === cat.filter) pillDiv.classList.add('is-active-filter');
        pillDiv.style.borderColor = `${cat.color}40`;
        pillDiv.style.background = `${cat.color}14`;
        pillDiv.innerHTML = `
          <div class="pill-left-content">
            <span class="dot" style="background: ${cat.color}; box-shadow: 0 0 6px ${cat.color};"></span>
            <span>${cat.label}</span>
          </div>
          <div>
            <strong style="color: ${cat.color};">${g.count}</strong>
            <span class="sub" style="color: #cbd5e1;"> ${g.pct}%</span>
          </div>
        `;

        pillDiv.addEventListener('mousedown', e => {
          e.preventDefault(); // Prevents focus rectangle in Chrome / WebKit
        });
        pillDiv.addEventListener('mouseenter', () => highlightWorkspaceCategory(cat.key));
        pillDiv.addEventListener('mouseleave', () => unhighlightWorkspaceCategory());
        pillDiv.addEventListener('click', e => {
          e.preventDefault();
          e.stopPropagation();
          pillDiv.blur();
          if (fo && fo.blur) fo.blur();
          if (window.getSelection) {
            try { window.getSelection().removeAllRanges(); } catch {}
          }
          triggerWorkspaceFilter(cat.filter);
        });
      }

      fo.appendChild(pillDiv);
      pillsGroup.appendChild(fo);
    });

    // Synchronize active filter selection
    syncFilterPillActiveStates();
  }

  function drawSinglePointerPath(catKey) {
    const line = document.getElementById('hero-single-pointer-path');
    if (!line || !lastWorkspaceSliceGeo) return;
    const g = lastWorkspaceSliceGeo[catKey];
    const cat = WORKSPACE_CIRCUIT_CATEGORIES.find(c => c.key === catKey);
    if (!g || !cat || g.count === 0) {
      clearSinglePointerPath();
      return;
    }

    const Cx = 395, Cy = 115, R_outer = 76;
    const isLeft = cat.side === 'left';
    const targetX = isLeft ? 200 : 590;
    const targetY = CIRCUIT_ROW_Y[cat.row];

    const IDEAL_CLOCK_ANGLES = {
      'available': 5.498,   // 10:30 o'clock
      'other': 4.712,       // 9:00 o'clock
      'mine': 3.927,        // 7:30 o'clock
      'translated': 0.785,  // 1:30 o'clock
      'approved': 1.571,    // 3:00 o'clock
      'submitted': 2.356    // 4:30 o'clock
    };

    const idealClock = IDEAL_CLOCK_ANGLES[cat.key];
    let connectClock = g.midAngle;

    if (g.angleSpan > 1.05) {
      const minA = g.startAngle + 0.12;
      const maxA = g.endAngle - 0.12;
      if (idealClock >= minA && idealClock <= maxA) {
        connectClock = idealClock;
      } else {
        const dMin = Math.abs(idealClock - minA);
        const dMax = Math.abs(idealClock - maxA);
        connectClock = dMin < dMax ? minA : maxA;
      }
    }

    const screenAngle = connectClock - (Math.PI / 2);
    const Sx = Cx + R_outer * Math.cos(screenAngle);
    const Sy = Cy + R_outer * Math.sin(screenAngle);

    let pathD = '';
    if (isLeft) {
      if (Sy > 175 && targetY >= 180) {
        const dropY = 210;
        const dropX = Sx - (dropY - Sy);
        const railX = 274;
        const chamferX = railX - Math.abs(dropY - targetY);
        pathD = `M ${Sx.toFixed(1)} ${Sy.toFixed(1)} L ${dropX.toFixed(1)} ${dropY} L ${railX} ${dropY} L ${chamferX.toFixed(1)} ${targetY} L ${targetX} ${targetY}`;
      } else {
        const dy = targetY - Sy;
        if (Math.abs(dy) <= 3) {
          pathD = `M ${Sx.toFixed(1)} ${Sy.toFixed(1)} L ${targetX} ${targetY}`;
        } else {
          const leadIn = 15;
          const diagEnd = targetX + leadIn; // 215
          const diagStart = diagEnd + Math.abs(dy);
          const safeDiagStart = Math.min(diagStart, 274);
          pathD = `M ${Sx.toFixed(1)} ${Sy.toFixed(1)} L ${safeDiagStart.toFixed(1)} ${Sy.toFixed(1)} L ${diagEnd} ${targetY} L ${targetX} ${targetY}`;
        }
      }
    } else {
      if (Sy > 175 && targetY >= 180) {
        const dropY = 210;
        const dropX = Sx + (dropY - Sy);
        const railX = 520;
        const chamferX = railX + Math.abs(dropY - targetY);
        pathD = `M ${Sx.toFixed(1)} ${Sy.toFixed(1)} L ${dropX.toFixed(1)} ${dropY} L ${railX} ${dropY} L ${chamferX.toFixed(1)} ${targetY} L ${targetX} ${targetY}`;
      } else {
        const dy = targetY - Sy;
        if (Math.abs(dy) <= 3) {
          pathD = `M ${Sx.toFixed(1)} ${Sy.toFixed(1)} L ${targetX} ${targetY}`;
        } else {
          const leadIn = 15;
          const diagEnd = targetX - leadIn; // 575
          const diagStart = diagEnd - Math.abs(dy);
          const safeDiagStart = Math.max(diagStart, 516);
          pathD = `M ${Sx.toFixed(1)} ${Sy.toFixed(1)} L ${safeDiagStart.toFixed(1)} ${Sy.toFixed(1)} L ${diagEnd} ${targetY} L ${targetX} ${targetY}`;
        }
      }
    }

    line.setAttribute('d', pathD);
    line.setAttribute('stroke', cat.color);
    line.style.filter = `drop-shadow(0 0 6px ${cat.color})`;
    line.classList.add('is-active');
  }

  function clearSinglePointerPath() {
    const line = document.getElementById('hero-single-pointer-path');
    if (line) {
      line.classList.remove('is-active');
      line.setAttribute('d', '');
    }
  }

  function highlightWorkspaceCategory(catKey, isPersistent = false) {
    if (!catalog || !catalog.items || !currentWorkspaceCategories) return;
    const total = catalog.items.length;
    const cat = currentWorkspaceCategories.find(c => c.key === catKey);
    if (!cat) return;

    // Highlight matching donut slice, dim others
    document.querySelectorAll('.donut-segment').forEach(s => {
      if (s.getAttribute('data-key') === catKey) {
        s.classList.add('is-hovered');
        s.style.strokeWidth = '34px';
        s.style.filter = `drop-shadow(0 0 14px ${cat.color})`;
        s.style.opacity = '1';
      } else {
        s.classList.remove('is-hovered');
        s.style.strokeWidth = '28px';
        s.style.filter = '';
        s.style.opacity = '0.28';
      }
    });

    // Highlight matching pill
    document.querySelectorAll('.satellite-pill').forEach(pill => {
      if (pill.getAttribute('data-key') === catKey) {
        pill.classList.add('is-hovered');
      } else {
        pill.classList.remove('is-hovered');
      }
    });

    // Draw single pointer beam to the hovered/selected category
    drawSinglePointerPath(catKey);

    // Update Center SVG HUD with slice details
    const hudPct = document.getElementById('pie-hud-pct') || document.getElementById('donut-hud-pct');
    const hudLbl = document.getElementById('pie-hud-label') || document.getElementById('donut-hud-label');
    const hudSub = document.getElementById('pie-hud-sub') || document.getElementById('donut-hud-sub');
    const pctVal = total > 0 ? (cat.count / total) * 100 : 0;
    const pctStr = pctVal > 0 && pctVal < 1 ? pctVal.toFixed(1) + '%' : `${Math.round(pctVal)}%`;

    if (hudPct) {
      hudPct.textContent = pctStr;
      hudPct.setAttribute('fill', cat.color);
      hudPct.style.color = cat.color;
    }
    if (hudLbl) {
      hudLbl.textContent = cat.label.toUpperCase();
      hudLbl.setAttribute('fill', cat.color);
      hudLbl.style.color = cat.color;
    }
    if (hudSub) {
      hudSub.textContent = `${cat.count} string${cat.count === 1 ? '' : 's'}`;
      hudSub.setAttribute('fill', '#94a3b8');
      hudSub.style.color = 'var(--text-tertiary)';
    }

    // Also update condensed header badge if active
    const condPct = document.getElementById('condensed-pct-badge');
    const condCounts = document.getElementById('condensed-hud-counts');
    const condLabel = document.getElementById('condensed-hud-label');
    if (condPct) {
      condPct.textContent = pctStr;
      condPct.style.color = cat.color;
      condPct.style.borderColor = `${cat.color}60`;
    }
    if (condLabel) condLabel.textContent = cat.label.toUpperCase();
    if (condCounts) condCounts.textContent = `${cat.count} string${cat.count === 1 ? '' : 's'}`;
  }

  function unhighlightWorkspaceCategory() {
    // If a filter is currently active, restore that active filter's highlight and pointer!
    if (currentFilter && currentFilter !== 'all') {
      const activeCat = WORKSPACE_CIRCUIT_CATEGORIES.find(c => c.filter === currentFilter);
      if (activeCat && lastWorkspaceSliceGeo && lastWorkspaceSliceGeo[activeCat.key] && lastWorkspaceSliceGeo[activeCat.key].count > 0) {
        highlightWorkspaceCategory(activeCat.key, true);
        return;
      }
    }

    // Otherwise, restore default donut state
    document.querySelectorAll('.donut-segment').forEach(s => {
      s.classList.remove('is-hovered');
      s.style.strokeWidth = '28px';
      s.style.filter = '';
      s.style.opacity = '1';
    });

    document.querySelectorAll('.satellite-pill').forEach(pill => {
      pill.classList.remove('is-hovered');
    });

    clearSinglePointerPath();

    if (lastWorkspaceStats) {
      updateCenterHudDefault(lastWorkspaceStats.pct, lastWorkspaceStats.completed, lastWorkspaceStats.total);
    }
  }

  function renderAppReadinessHud() {
    const hud = document.getElementById('ws-app-readiness-hud');
    if (!hud || !catalog || !catalog.items) return;

    const sections = [
      { key: 'player', label: 'Player UI', match: t => t.includes('player') || t.includes('playback') || t.includes('volume') || t.includes('rewind') || t.includes('forward') || t.includes('shuffle') || t.includes('repeat') },
      { key: 'library', label: 'Library', match: t => t.includes('library') || t.includes('songs') || t.includes('album') || t.includes('artist') || t.includes('search') || t.includes('sort') || t.includes('filter') || t.includes('folder') },
      { key: 'playlists', label: 'Playlists', match: t => t.includes('playlist') || t.includes('queue') || t.includes('favorite') || t.includes('recent') },
      { key: 'settings', label: 'Settings', match: t => t.includes('setting') || t.includes('theme') || t.includes('about') || t.includes('config') || t.includes('version') || t.includes('license') }
    ];

    const completedKeys = new Set();
    if (catalog.languages && catalog.languages[currentLang] && catalog.languages[currentLang].translations) {
      Object.keys(catalog.languages[currentLang].translations).forEach(k => completedKeys.add(k));
    }
    if (liveStatus && liveStatus.approved) {
      Object.keys(liveStatus.approved).forEach(k => completedKeys.add(k));
    }

    let rowsHtml = '';
    sections.forEach(sec => {
      let totalSec = 0;
      let doneSec = 0;
      catalog.items.forEach(item => {
        const searchStr = (item.key + ' ' + (item.comment || '')).toLowerCase();
        if (sec.match(searchStr)) {
          totalSec++;
          if (completedKeys.has(item.key)) doneSec++;
        }
      });
      if (totalSec === 0) totalSec = 1;
      const pct = Math.min(100, Math.round((doneSec / totalSec) * 100));

      let activeBars = 1;
      let color = '#fb923c';
      if (pct >= 90) { activeBars = 4; color = '#2ed573'; }
      else if (pct >= 60) { activeBars = 3; color = '#38bdf8'; }
      else if (pct >= 30) { activeBars = 2; color = '#facc15'; }
      else if (pct > 0) { activeBars = 1; color = '#fb923c'; }
      else { activeBars = 0; color = '#64748b'; }

      const barHeights = [5, 9, 13, 7];
      let barsHtml = '';
      for (let b = 0; b < 4; b++) {
        const isActive = b < activeBars;
        const bg = isActive ? color : 'rgba(255, 255, 255, 0.1)';
        const animClass = isActive ? 'is-animated' : '';
        barsHtml += `<span class="eq-bar ${animClass}" style="height: ${barHeights[b]}px; background: ${bg};"></span>`;
      }

      rowsHtml += `
        <div class="eq-row">
          <span class="eq-label">${sec.label}</span>
          <div class="eq-bars">${barsHtml}</div>
          <span class="eq-pct" style="color: ${color};">${pct}%</span>
        </div>
      `;
    });

    hud.innerHTML = `
      <div class="eq-panel">
        <div class="eq-header">
          <span>App Section Readiness</span>
          <span style="color: #60cdff;">Audio HUD</span>
        </div>
        ${rowsHtml}
      </div>
    `;
  }

  function triggerWorkspaceFilter(filterKey) {
    const targetFilter = (currentFilter === filterKey) ? 'all' : filterKey;
    const tab = document.querySelector(`.filter-tab[data-filter="${targetFilter}"]`);
    if (tab) {
      tab.click();
    } else {
      currentFilter = targetFilter;
      currentPage = 1;
      renderCards();
      syncFilterPillActiveStates();
    }
  }

  function syncFilterPillActiveStates() {
    document.querySelectorAll('.satellite-pill').forEach(pill => {
      const filter = pill.getAttribute('data-filter');
      const isActive = (currentFilter === filter);
      pill.classList.toggle('is-active-filter', isActive);
    });

    if (currentFilter && currentFilter !== 'all') {
      const cat = WORKSPACE_CIRCUIT_CATEGORIES.find(c => c.filter === currentFilter);
      if (cat && lastWorkspaceSliceGeo && lastWorkspaceSliceGeo[cat.key] && lastWorkspaceSliceGeo[cat.key].count > 0) {
        highlightWorkspaceCategory(cat.key, true);
      } else {
        unhighlightWorkspaceCategory();
      }
    } else {
      unhighlightWorkspaceCategory();
    }
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
  // 6b. VIEW COMMUNITY SUBMISSIONS MODAL
  // -------------------------------------------------------------------------

  function formatTimeAgo(dateStr) {
    if (!dateStr) return '';
    try {
      const timestamp = typeof dateStr === 'number' ? dateStr : Date.parse(dateStr);
      if (isNaN(timestamp)) return '';
      const diff = Math.floor((Date.now() - timestamp) / 1000);
      if (diff < 60) return 'Just now';
      const minutes = Math.floor(diff / 60);
      if (minutes < 60) return `${minutes}m ago`;
      const hours = Math.floor(minutes / 60);
      if (hours < 24) return `${hours}h ago`;
      const days = Math.floor(hours / 24);
      if (days < 30) return `${days}d ago`;
      return new Date(timestamp).toLocaleDateString();
    } catch {
      return '';
    }
  }

  async function openViewSubmissionsModal(item) {
    activeViewSubmissionsItem = item;
    const keyLabel = document.getElementById('view-submissions-key-label');
    const sourceEl = document.getElementById('view-submissions-source-text');
    const countEl = document.getElementById('view-submissions-count');
    const listEl = document.getElementById('view-submissions-list');

    if (keyLabel) keyLabel.textContent = item.key;
    if (sourceEl) sourceEl.textContent = item.source;
    if (countEl) countEl.textContent = '...';
    if (listEl) {
      listEl.innerHTML = `<span style="font-size: 12px; color: var(--text-tertiary);"><span class="loading-spinner" style="width: 14px; height: 14px; border-width: 1.5px; display: inline-block; vertical-align: middle; margin-right: 6px;"></span>Loading community submissions...</span>`;
    }

    if (viewSubmissionsModal) {
      viewSubmissionsModal.classList.add('active');
    }

    let proposals = [];
    if (ENABLE_DEV_SIMULATION && currentLang === SIM_LANG_CODE && SIMULATION_DATA.proposals[item.key]) {
      proposals = SIMULATION_DATA.proposals[item.key];
    } else {
      try {
        const res = await fetch(`${WORKER_URL}/api/proposals?lang=${currentLang}&key=${encodeURIComponent(item.key)}`);
        if (res.ok) {
          const data = await res.json();
          proposals = Array.isArray(data.proposals) ? data.proposals : [];
        }
      } catch (e) {
        console.warn("Could not fetch submissions for key:", e);
      }
    }

    // Fallback: If server returned no proposals (e.g. offline/mock), check local submitted store
    if (proposals.length === 0) {
      const localSubMap = getSubmittedTranslations(currentLang);
      if (localSubMap && localSubMap[item.key]) {
        const localEntry = localSubMap[item.key];
        proposals.push({
          id: 'local_' + Date.now(),
          text: localEntry.text,
          contributor: localEntry.contributor || safeStorageGet('tunetastic_author') || 'Anonymous',
          sessionId: SESSION_ID,
          timestamp: localEntry.timestamp ? new Date(localEntry.timestamp).toISOString() : new Date().toISOString()
        });
      }
    }

    renderViewSubmissionsList(proposals, item);
  }

  function renderViewSubmissionsList(proposals, item) {
    const listEl = document.getElementById('view-submissions-list');
    const countEl = document.getElementById('view-submissions-count');
    if (!listEl) return;

    if (countEl) countEl.textContent = proposals.length;

    if (proposals.length === 0) {
      listEl.innerHTML = `<span style="font-size: 12px; color: var(--text-tertiary); font-style: italic;">No submissions found for this string yet.</span>`;
      return;
    }

    listEl.innerHTML = proposals.map((p, idx) => {
      const rawContributor = p.contributor ? String(p.contributor).trim() : 'Unknown';
      const timeStr = formatTimeAgo(p.timestamp);

      return `
        <div style="background: rgba(255,255,255,0.03); border: 1px solid var(--border-glass); border-radius: var(--radius-md); padding: 10px 12px; display: flex; align-items: center; justify-content: space-between; gap: 10px;">
          <div style="flex: 1; min-width: 0;">
            <div style="color: var(--text-primary); font-size: 13.5px; font-weight: 500; word-break: break-word; line-height: 1.4;">${escapeHtml(p.text)}</div>
            <div style="font-size: 11px; color: var(--text-tertiary); margin-top: 4px; display: flex; align-items: center; gap: 8px; flex-wrap: wrap;">
              <span>By <strong style="color: var(--text-secondary);">${escapeHtml(rawContributor)}</strong></span>
              ${timeStr ? `<span>•</span><span>${escapeHtml(timeStr)}</span>` : ''}
              <span style="opacity: 0.6;">(#${idx + 1})</span>
            </div>
          </div>
          <button class="btn btn-glass sub-copy-btn" data-copy-val="${escapeHtml(p.text)}" title="Copy translation">
            ${ICONS.copy} Copy
          </button>
        </div>
      `;
    }).join('');

    // Attach 1-click copy listeners to buttons
    listEl.querySelectorAll('.sub-copy-btn').forEach(btn => {
      btn.onclick = () => {
        const textToCopy = btn.getAttribute('data-copy-val') || '';
        if (textToCopy) {
          navigator.clipboard.writeText(textToCopy);
          btn.innerHTML = `${ICONS.check} Copied!`;
          btn.classList.add('copied');
          showToast("Translation copied to clipboard!");
          setTimeout(() => {
            btn.innerHTML = `${ICONS.copy} Copy`;
            btn.classList.remove('copied');
          }, 2000);
        }
      };
    });
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
    savedAuthor = String(savedAuthor).trim();
    document.getElementById('submit-author-input').value = savedAuthor;

    const confirmBtn = document.getElementById('modal-confirm-submit-btn');
    if (confirmBtn) {
      confirmBtn.disabled = false;
      confirmBtn.textContent = 'Submit';
    }

    submitModal.classList.add('active');
  }

  async function submitTranslations() {
    const rawAuthor = document.getElementById('submit-author-input').value.trim() || 'Unknown';
    const author = rawAuthor || 'Unknown';
    if (rawAuthor && rawAuthor !== 'Unknown') {
      safeStorageSet('tunetastic_author', rawAuthor);
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
    btn.textContent = 'Submitting...';

    try {
      // Prepare details (English source & App Context) for GitHub Tracker table
      const details = {};
      const catMap = new Map();
      if (catalog && catalog.items) {
        catalog.items.forEach(i => catMap.set(i.key, i));
      }
      for (const k of Object.keys(translations)) {
        const item = catMap.get(k);
        const catalogTrans = getCatalogTranslation(currentLang, k);
        const appr = liveStatus && liveStatus.approved ? liveStatus.approved[k] : null;
        let prevVal = '';
        let prevStatus = '';
        if (appr && (appr.value || appr.translation)) {
          prevVal = appr.value || appr.translation;
          prevStatus = 'Approved';
        } else if (catalogTrans && catalogTrans.trim()) {
          prevVal = catalogTrans.trim();
          prevStatus = 'Already Translated';
        }

        details[k] = {
          source: (item && item.source) || '',
          context: (item && item.comment) || '',
          previousTranslation: prevVal,
          previousStatus: prevStatus
        };
      }

      const res = await fetch(`${WORKER_URL}/api/submit`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'X-Session-ID': SESSION_ID
        },
        body: JSON.stringify({
          language: currentLang,
          contributor: author,
          translations,
          details
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
            } else if (lock === 'both') {
              liveStatus.locks[k] = 'other';
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

        showToast(`Successfully submitted ${data.acceptedCount || count} translations!`);
        await fetchLiveStatus();
        renderCards();
        updateWorkspaceStats();
      } else {
        alert(data.error || "Submission failed.");
      }
    } catch (err) {
      alert("Could not reach server: " + err.message);
    } finally {
      btn.disabled = false;
      btn.textContent = 'Submit';
    }
  }

  // -------------------------------------------------------------------------
  // 9. ADMIN REVIEW QUEUE & AUTOMATED PR CREATION (#admin)
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
      submitBtn.textContent = 'Unlock';
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
            submitBtn.textContent = 'Unlock';
            input.select();
          }
        } catch (e) {
          if (errorEl) {
            errorEl.textContent = 'Could not reach server: ' + e.message;
            errorEl.style.display = 'block';
          }
          submitBtn.disabled = false;
          submitBtn.textContent = 'Unlock';
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
    if (!container) return;

    if (!sessionAdminPassword) {
      if (ENABLE_DEV_SIMULATION) {
        sessionAdminPassword = "sim_dev_bypass";
      } else {
        try {
          await requestAdminAuth();
        } catch {
          window.location.hash = '#home';
          return;
        }
      }
    }

    container.innerHTML = `
      <div style="text-align: center; padding: 48px; color: var(--text-secondary);">
        <span class="loading-spinner" style="width: 22px; height: 22px; display: inline-block; vertical-align: middle; margin-right: 8px;"></span>
        Synchronizing Git PR status and loading languages...
      </div>
    `;

    try {
      let pendingItems = [];
      let approvedPerLang = {};
      let mergedPerLang = {};

      try {
        const res = await fetch(`${WORKER_URL}/api/pending`, {
          headers: { 'Authorization': `Bearer ${sessionAdminPassword}` }
        });
        if (res.status === 401 && !ENABLE_DEV_SIMULATION) {
          sessionAdminPassword = '';
          container.innerHTML = `
            <div style="padding: 28px; background: var(--surface-glass); border-radius: var(--radius-xl); border: 1px solid var(--border-glass); text-align: center;">
              <h4 style="color: #f87171;">Session Expired or Invalid Password</h4>
              <button class="btn btn-primary-gradient" style="margin-top: 14px;" onclick="loadAdminQueue()">Re-authenticate</button>
            </div>
          `;
          return;
        }
        if (res.ok) {
          const data = await res.json();
          pendingItems = data.pending || [];
          approvedPerLang = data.approved || {};
          mergedPerLang = data.merged || {};
        }
      } catch (networkErr) {
        if (!ENABLE_DEV_SIMULATION) throw networkErr;
      }

      if (ENABLE_DEV_SIMULATION) {
        pendingItems = pendingItems.filter(p => p.language !== SIM_LANG_CODE);
        pendingItems.push(
          { language: SIM_LANG_CODE, key: "RewindButton_ToolTip", proposals: SIMULATION_DATA.proposals["RewindButton_ToolTip"] },
          { language: SIM_LANG_CODE, key: "PreviousButton_ToolTip", proposals: SIMULATION_DATA.proposals["PreviousButton_ToolTip"] },
          { language: SIM_LANG_CODE, key: "AppTitle.Text", proposals: SIMULATION_DATA.proposals["AppTitle.Text"] }
        );
        approvedPerLang[SIM_LANG_CODE] = { ...SIMULATION_DATA.liveStatus.approved };
      }

      // Resilient fallback: Merge liveStatus.approved if available for current language
      if (currentLang && liveStatus && liveStatus.approved && Object.keys(liveStatus.approved).length > 0) {
        if (!approvedPerLang[currentLang] || Object.keys(approvedPerLang[currentLang]).length === 0) {
          approvedPerLang[currentLang] = { ...liveStatus.approved };
        }
      }

      let totalApprovedCount = 0;
      for (const map of Object.values(approvedPerLang)) {
        totalApprovedCount += Object.keys(map).length;
      }

      const pendingBadge = document.getElementById('admin-pending-badge');
      if (pendingBadge) {
        pendingBadge.textContent = `${pendingItems.length} Submissions Pending`;
      }
      const approvedBadge = document.getElementById('admin-approved-badge');
      if (approvedBadge) {
        approvedBadge.textContent = `${totalApprovedCount} Approved (Staged)`;
      }

      // Sync All button state
      const syncAllBtn = document.getElementById('admin-sync-pr-btn');
      if (syncAllBtn) {
        syncAllBtn.disabled = totalApprovedCount === 0;
        syncAllBtn.style.opacity = totalApprovedCount === 0 ? '0.5' : '1';
        syncAllBtn.style.cursor = totalApprovedCount === 0 ? 'not-allowed' : 'pointer';
        syncAllBtn.title = totalApprovedCount === 0 ? 'No approved translations available to sync' : `Sync all ${totalApprovedCount} approved translations to Git`;
      }

      await renderAdminDashboard(pendingItems, approvedPerLang, mergedPerLang, container);
    } catch (err) {
      container.innerHTML = `
        <div style="padding: 28px; background: var(--surface-glass); border-radius: var(--radius-xl); border: 1px solid var(--border-glass);">
          <h4 style="color: var(--status-warning);">Could not reach server</h4>
          <p style="font-size: 13.5px; color: var(--text-secondary);">${escapeHtml(err.message)}</p>
        </div>
      `;
    }
  }

  async function renderAdminDashboard(pendingItems, approvedPerLang, mergedPerLang = {}, container) {
    if (!catalog || !catalog.items || catalog.items.length === 0) {
      await loadCatalog();
    }
    const catMap = new Map();
    if (catalog && catalog.items) {
      catalog.items.forEach(i => catMap.set(i.key, i));
    }

    // Collect all unique languages
    const langSet = new Set();
    pendingItems.forEach(p => langSet.add(p.language));
    Object.keys(approvedPerLang).forEach(l => langSet.add(l));
    const allLangs = Array.from(langSet).sort();

    if (allLangs.length === 0) {
      container.innerHTML = `
        <div style="text-align: center; padding: 56px 24px; background: var(--surface-glass); border-radius: var(--radius-xl); border: 1px solid var(--border-glass); color: var(--text-secondary);">
          <div style="font-size: 36px; margin-bottom: 12px;">🎉</div>
          <h3 style="color: var(--text-primary); margin-bottom: 6px; font-size: 18px;">Everything is Up to Date!</h3>
          <p style="font-size: 14px; max-width: 520px; margin: 0 auto; line-height: 1.5;">
            No pending submissions require review, and no approved translations are waiting for Git synchronization.
          </p>
        </div>
      `;
      return;
    }

    container.innerHTML = '';

    allLangs.forEach(lang => {
      const langMeta = resolveLanguageMeta(lang) || { name: lang, native: lang, flag: '🌐' };
      const langPending = pendingItems.filter(p => p.language === lang);
      const langApproved = approvedPerLang[lang] || {};
      const langMerged = mergedPerLang[lang] || {};
      const approvedKeys = Object.keys(langApproved);
      const openPr = approvedKeys.map(k => langApproved[k]).find(a => a.prNumber && a.prState === 'open');

      // Calculate overwrite proposals vs new submissions
      const overwriteItems = langPending.filter(p => {
        const hasAppr = langApproved[p.key] && (langApproved[p.key].value || langApproved[p.key].translation);
        const hasMerg = langMerged[p.key] && (langMerged[p.key].value || langMerged[p.key].translation);
        const hasCat = getCatalogTranslation(lang, p.key);
        return Boolean(hasAppr || hasMerg || (hasCat && hasCat.trim()));
      });
      const overwriteCount = overwriteItems.length;
      const newCount = langPending.length - overwriteCount;

      const groupCard = document.createElement('div');
      groupCard.className = 'admin-lang-group';
      groupCard.id = `admin-lang-${lang}`;

      // 1. Language Group Header
      const prPillHtml = openPr
        ? `<a href="${openPr.prUrl || '#'}" target="_blank" rel="noopener" class="admin-pr-link" title="Open Pull Request on GitHub">${ICONS.gitPullRequest} PR #${openPr.prNumber} (Open) ↗</a>`
        : '';

      const syncBtnDisabled = approvedKeys.length === 0;

      groupCard.innerHTML = `
        <div class="admin-lang-header">
          <div class="admin-lang-info">
            <span class="admin-lang-flag">${langMeta.flag || '🌐'}</span>
            <div class="admin-lang-names">
              <h3 class="admin-lang-title">${escapeHtml(formatLanguageDisplayName(langMeta, lang))}</h3>
              <span class="admin-lang-code">${lang}</span>
            </div>
            <div class="admin-lang-badges">
              <span class="studio-pill ${langPending.length > 0 ? 'pill-draft-mine' : 'pill-available'}">${langPending.length} Pending</span>
              <span class="studio-pill ${approvedKeys.length > 0 ? 'pill-approved' : 'pill-available'}">${approvedKeys.length} Approved</span>
              ${prPillHtml}
            </div>
          </div>
          <div class="admin-lang-actions">
            <button class="btn btn-sm btn-primary-gradient admin-lang-sync-btn" data-lang="${lang}" ${syncBtnDisabled ? 'disabled style="opacity: 0.5; cursor: not-allowed;"' : ''} title="${syncBtnDisabled ? 'No approved translations to sync' : `Create PR for ${approvedKeys.length} ${langMeta.name || lang} translations`}">
              <svg class="svg-icon" viewBox="0 0 16 16" width="13" height="13" fill="currentColor"><path fill-rule="evenodd" d="M7.177 3.073L9.573.677A.25.25 0 0110 .854v4.792a.25.25 0 01-.427.177L7.177 3.427a.25.25 0 010-.354zM3.75 2.5a.75.75 0 100 1.5.75.75 0 000-1.5zm-2.25.75a2.25 2.25 0 113 2.122v5.256a2.251 2.251 0 11-1.5 0V5.372A2.25 2.25 0 011.5 3.25zM11 2.5h-1V4h1a1 1 0 011 1v5.628a2.251 2.251 0 101.5 0V5A2.5 2.5 0 0011 2.5zm1 10.25a.75.75 0 111.5 0 .75.75 0 01-1.5 0zM3.75 12a.75.75 0 100 1.5.75.75 0 000-1.5z"></path></svg>
              Sync ${escapeHtml(langMeta.name || lang)} to Git (Create PR)
            </button>
          </div>
        </div>

        <div class="admin-lang-content">
          <!-- SUBGROUP: Pending Review Proposals -->
          <div class="admin-subgroup" style="${langPending.length === 0 ? 'display: none;' : ''}">
            <div class="admin-subgroup-title" style="flex-wrap: wrap; gap: 8px;">
              <div style="display: flex; align-items: center; gap: 10px; flex-wrap: wrap;">
                <span>Pending Review (${langPending.length})</span>
                ${overwriteCount > 0 ? `
                  <div class="admin-pending-filter-tabs" id="filter-tabs-${lang}">
                    <button class="admin-filter-pill active" data-filter="all" data-lang="${lang}">All (${langPending.length})</button>
                    <button class="admin-filter-pill" data-filter="new" data-lang="${lang}">New (${newCount})</button>
                    <button class="admin-filter-pill pill-warn" data-filter="overwrites" data-lang="${lang}">Overwrites (${overwriteCount})</button>
                  </div>
                ` : ''}
              </div>
            </div>
            <div class="admin-pending-container" id="pending-container-${lang}" style="display: flex; flex-direction: column; gap: 14px;"></div>
          </div>

          <!-- SUBGROUP: Approved Staged Translations -->
          <div class="admin-subgroup" style="margin-top: ${langPending.length > 0 ? '22px' : '0'};">
            <div class="admin-subgroup-title">
              <span>Approved Translations (${approvedKeys.length})</span>
              <span style="font-size: 11px; font-weight: 500; color: var(--text-tertiary); text-transform: none; letter-spacing: normal;">Staged on server for Git synchronization</span>
            </div>
            <div class="admin-approved-container" id="approved-container-${lang}"></div>
          </div>
        </div>
      `;

      container.appendChild(groupCard);

      // Wire per-language Sync button
      const langSyncBtn = groupCard.querySelector('.admin-lang-sync-btn');
      if (langSyncBtn && !syncBtnDisabled) {
        langSyncBtn.onclick = () => triggerSyncPr(lang, langSyncBtn);
      }

      // Wire pending filter buttons (All / New / Overwrites)
      const filterPills = groupCard.querySelectorAll(`.admin-filter-pill[data-lang="${lang}"]`);
      filterPills.forEach(pill => {
        pill.onclick = () => {
          filterPills.forEach(p => p.classList.remove('active'));
          pill.classList.add('active');
          const filterMode = pill.getAttribute('data-filter');
          const cards = groupCard.querySelectorAll(`#pending-container-${lang} .studio-card`);
          cards.forEach(c => {
            const isOvr = c.getAttribute('data-is-overwrite') === 'true';
            if (filterMode === 'all') {
              c.style.display = '';
            } else if (filterMode === 'new') {
              c.style.display = !isOvr ? '' : 'none';
            } else if (filterMode === 'overwrites') {
              c.style.display = isOvr ? '' : 'none';
            }
          });
        };
      });

      // Render Pending Cards for this language
      const pendingContainer = groupCard.querySelector(`#pending-container-${lang}`);
      if (pendingContainer && langPending.length > 0) {
        langPending.forEach(item => {
          const card = document.createElement('div');
          card.className = 'studio-card';
          card.id = `admin-card-${item.language}-${item.key}`;

          const proposals = item.proposals || [];
          const catItem = catMap.get(item.key);
          const sourceText = catItem ? catItem.source : '';
          const appContext = catItem ? catItem.comment : '';

          // Determine if this submission overwrites an approved or translated string
          const approvedObj = langApproved[item.key];
          const mergedObj = langMerged[item.key];
          const catalogTrans = getCatalogTranslation(item.language, item.key);

          let overwriteType = null; // 'approved' | 'translated' | null
          let previousText = '';
          let previousMeta = '';

          if (approvedObj && (approvedObj.value || approvedObj.translation)) {
            overwriteType = 'approved';
            previousText = approvedObj.value || approvedObj.translation;
            const author = Array.isArray(approvedObj.contributors) && approvedObj.contributors.length > 0
              ? approvedObj.contributors.join(', ')
              : (approvedObj.contributor || 'Maintainer');
            previousMeta = `Staged as Approved (by ${author}${approvedObj.prNumber ? ` • PR #${approvedObj.prNumber}` : ''})`;
          } else if (mergedObj && (mergedObj.value || mergedObj.translation)) {
            overwriteType = 'translated';
            previousText = mergedObj.value || mergedObj.translation;
            previousMeta = `Merged in Git repository (${mergedObj.prNumber ? `PR #${mergedObj.prNumber}` : 'Merged'})`;
          } else if (catalogTrans && catalogTrans.trim()) {
            overwriteType = 'translated';
            previousText = catalogTrans.trim();
            previousMeta = `Official in App / Resources.resw`;
          }

          card.setAttribute('data-is-overwrite', overwriteType ? 'true' : 'false');

          const overwritePillHtml = overwriteType === 'approved'
            ? `<span class="admin-overwrite-pill overwrite-approved" title="This string was already approved! Reviewing will update the approved translation."><svg class="svg-icon" viewBox="0 0 16 16" width="11" height="11" fill="none" stroke="currentColor" stroke-width="2"><path d="M8 2l6 11H2zM8 6v3m0 2h.01"/></svg> Overwrite: Approved</span>`
            : (overwriteType === 'translated'
              ? `<span class="admin-overwrite-pill overwrite-translated" title="This string is already active in the app! Reviewing will revise the official translation."><svg class="svg-icon" viewBox="0 0 16 16" width="11" height="11" fill="none" stroke="currentColor" stroke-width="2"><circle cx="8" cy="8" r="6"/><path d="M8 5v3l2 2"/></svg> Revision: Translated</span>`
              : '');

          const diffBoxHtml = overwriteType ? `
            <div class="admin-diff-card ${overwriteType === 'approved' ? 'diff-approved' : 'diff-translated'}">
              <div class="admin-diff-header">
                <div class="diff-header-title">
                  ${overwriteType === 'approved'
                    ? `<span class="diff-icon-amber"><svg class="svg-icon" viewBox="0 0 16 16" width="14" height="14" fill="none" stroke="currentColor" stroke-width="2"><path d="M8 2l6 11H2zM8 6v3m0 2h.01"/></svg></span> <strong>POTENTIAL OVERWRITE:</strong> This string is already staged as <strong>Approved</strong>`
                    : `<span class="diff-icon-cyan"><svg class="svg-icon" viewBox="0 0 16 16" width="14" height="14" fill="none" stroke="currentColor" stroke-width="2"><circle cx="8" cy="8" r="6"/><path d="M8 5v3l2 2"/></svg></span> <strong>PROPOSED REVISION:</strong> This string is already <strong>Translated</strong> in the application`}
                </div>
                <div class="diff-header-meta">${escapeHtml(previousMeta)}</div>
              </div>

              <div class="admin-diff-body">
                <!-- What was Before -->
                <div class="diff-pane diff-pane-before">
                  <div class="diff-pane-label">
                    <span class="diff-tag diff-tag-before">WHAT WAS BEFORE</span>
                    <span class="diff-state-hint">${overwriteType === 'approved' ? 'Staged Translation' : 'Active Translation'}</span>
                  </div>
                  <div class="diff-pane-text diff-text-before">
                    "${escapeHtml(previousText)}"
                  </div>
                </div>

                <!-- Arrow -->
                <div class="diff-arrow-divider">
                  <svg class="svg-icon" viewBox="0 0 16 16" width="16" height="16" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                    <line x1="2" y1="8" x2="14" y2="8"></line>
                    <polyline points="9 3 14 8 9 13"></polyline>
                  </svg>
                </div>

                <!-- What is Suggested -->
                <div class="diff-pane diff-pane-after">
                  <div class="diff-pane-label">
                    <span class="diff-tag diff-tag-after">WHAT IS SUGGESTED</span>
                    <span class="diff-state-hint" id="diff-after-author-${item.language}-${item.key}">Option 1 • by ${escapeHtml(String(proposals[0]?.contributor || 'Unknown').replace(/^@+/, ''))}</span>
                  </div>
                  <div class="diff-pane-text diff-text-after" id="diff-after-text-${item.language}-${item.key}">
                    "${escapeHtml(proposals[0]?.text || '')}"
                  </div>
                </div>
              </div>
            </div>
          ` : '';

          let proposalsHtml = '';
          proposals.forEach((p, idx) => {
            const isSelected = idx === 0;
            const authorClean = String(p.contributor || 'Unknown').replace(/^@+/, '');
            proposalsHtml += `
              <label class="proposal-item ${isSelected ? 'selected' : ''}" data-key="${item.key}" data-val="${escapeHtml(p.text)}" data-author="${escapeHtml(authorClean)}">
                <input type="radio" name="prop-${item.language}-${item.key}" class="proposal-radio" ${isSelected ? 'checked' : ''}>
                <div class="proposal-meta">
                  <div class="proposal-text">"${escapeHtml(p.text)}"</div>
                  <div class="proposal-author">Option ${idx + 1} • by ${escapeHtml(authorClean)}</div>
                </div>
              </label>
            `;
          });

          card.innerHTML = `
            <div class="card-header" style="display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 10px; margin-bottom: 12px;">
              <div style="display: flex; align-items: center; flex-wrap: wrap; gap: 6px;">
                <span class="card-key-pill">${escapeHtml(item.key)}</span>
                ${overwritePillHtml}
              </div>
              <div class="admin-card-actions">
                <button class="btn btn-sm btn-glass admin-clear-btn" data-lang="${item.language}" data-key="${item.key}" title="Discard and clear all submissions for this key" style="display: inline-flex; align-items: center; gap: 6px;">
                  ${ICONS.trash} Clear Submissions
                </button>
                <button class="btn btn-sm btn-primary-gradient admin-approve-btn" data-lang="${item.language}" data-key="${item.key}" style="display: inline-flex; align-items: center; gap: 6px;">
                  ${ICONS.check} Approve Selected
                </button>
              </div>
            </div>

            <!-- English Source & App Context Box -->
            <div class="admin-review-source" style="margin-bottom: 12px;">
              <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 6px;">
                <span style="font-size: 11px; font-weight: 700; color: var(--text-tertiary); text-transform: uppercase; letter-spacing: 0.5px;">English Source</span>
                ${appContext ? `<span style="font-size: 11px; color: var(--text-tertiary); max-width: 65%; text-align: right; overflow: hidden; text-overflow: ellipsis; white-space: nowrap;" title="${escapeHtml(appContext)}">${escapeHtml(appContext)}</span>` : ''}
              </div>
              <div style="display: flex; justify-content: space-between; align-items: flex-start; gap: 12px;">
                <div style="font-size: 14.5px; font-weight: 600; color: var(--text-primary); line-height: 1.5; word-break: break-word; user-select: text;">
                  ${escapeHtml(sourceText || '-')}
                </div>
                ${sourceText ? `
                  <button class="copy-mini-btn admin-copy-source-btn" data-text="${escapeHtml(sourceText)}" title="Copy English source" style="flex-shrink: 0; padding: 3px 8px; font-size: 11px; border-radius: var(--radius-sm); border: 1px solid var(--border-glass); background: rgba(255,255,255,0.04); color: var(--text-secondary); cursor: pointer; display: flex; align-items: center; gap: 4px;">
                    ${ICONS.copy} Copy
                  </button>
                ` : ''}
              </div>
            </div>

            <!-- What was Before vs What is Suggested Comparison -->
            ${diffBoxHtml}

            <div style="font-size: 11px; font-weight: 700; color: var(--text-tertiary); text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px;">
              Submitted Options (${proposals.length})
            </div>

            <div class="proposal-stack" id="stack-${item.language}-${item.key}">
              ${proposalsHtml}
            </div>

            <div style="margin-top: 10px;">
              <label style="font-size: 11px; color: var(--text-tertiary);">Custom tweak (if needed):</label>
              <input type="text" class="form-input" id="tweak-${item.language}-${item.key}" value="${escapeHtml(proposals[0]?.text || '')}">
            </div>
          `;

          pendingContainer.appendChild(card);

          // Radio selection with real-time diff preview sync
          const radioItems = card.querySelectorAll('.proposal-item');
          radioItems.forEach(ri => {
            ri.onclick = () => {
              radioItems.forEach(r => r.classList.remove('selected'));
              ri.classList.add('selected');
              ri.querySelector('input').checked = true;
              const val = ri.getAttribute('data-val') || '';
              document.getElementById(`tweak-${item.language}-${item.key}`).value = val;

              // Live update of Suggested diff pane
              const diffText = card.querySelector(`#diff-after-text-${item.language}-${item.key}`);
              if (diffText) diffText.textContent = `"${val}"`;
              const diffAuthor = card.querySelector(`#diff-after-author-${item.language}-${item.key}`);
              if (diffAuthor) {
                const authorClean = ri.getAttribute('data-author') || 'Unknown';
                const metaText = ri.querySelector('.proposal-author')?.textContent || `by ${authorClean}`;
                diffAuthor.textContent = metaText;
              }
            };
          });

          // Tweak input with real-time diff preview sync
          const tweakInput = card.querySelector(`#tweak-${item.language}-${item.key}`);
          if (tweakInput) {
            tweakInput.oninput = () => {
              const diffText = card.querySelector(`#diff-after-text-${item.language}-${item.key}`);
              if (diffText) diffText.textContent = `"${tweakInput.value}"`;
              const diffAuthor = card.querySelector(`#diff-after-author-${item.language}-${item.key}`);
              if (diffAuthor) diffAuthor.textContent = 'Custom tweak (editing)';
            };
          }

          // Copy English source
          const copySourceBtn = card.querySelector('.admin-copy-source-btn');
          if (copySourceBtn) {
            copySourceBtn.onclick = () => {
              const textToCopy = copySourceBtn.getAttribute('data-text') || '';
              navigator.clipboard.writeText(textToCopy);
              copySourceBtn.innerHTML = `${ICONS.check} Copied!`;
              copySourceBtn.classList.add('copied');
              showToast("English source copied!");
              setTimeout(() => {
                copySourceBtn.innerHTML = `${ICONS.copy} Copy`;
                copySourceBtn.classList.remove('copied');
              }, 1800);
            };
          }

          // Clear submissions
          const clearBtn = card.querySelector('.admin-clear-btn');
          if (clearBtn) {
            clearBtn.onclick = async () => {
              if (ENABLE_DEV_SIMULATION && item.language === SIM_LANG_CODE) {
                delete SIMULATION_DATA.liveStatus.proposals[item.key];
                delete SIMULATION_DATA.proposals[item.key];
                showToast(`Cleared submissions for "${item.key}" (Simulated)`);
                loadAdminQueue();
                return;
              }
              if (!confirm(`Are you sure you want to discard and clear all ${proposals.length} submitted option(s) for "${item.key}"?`)) return;
              clearBtn.disabled = true;
              clearBtn.textContent = 'Clearing...';
              try {
                const cRes = await fetch(`${WORKER_URL}/api/reject`, {
                  method: 'POST',
                  headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${sessionAdminPassword}` },
                  body: JSON.stringify({ language: item.language, key: item.key })
                });
                if (cRes.ok) {
                  showToast(`Cleared submissions for "${item.key}"`);
                  loadAdminQueue();
                } else {
                  alert("Failed to clear submissions.");
                  clearBtn.disabled = false;
                  clearBtn.innerHTML = `${ICONS.trash} Clear Submissions`;
                }
              } catch (e) {
                alert("Error: " + e.message);
                clearBtn.disabled = false;
                clearBtn.innerHTML = `${ICONS.trash} Clear Submissions`;
              }
            };
          }

          // Approve selected
          const approveBtn = card.querySelector('.admin-approve-btn');
          approveBtn.onclick = async () => {
            const selectedText = document.getElementById(`tweak-${item.language}-${item.key}`).value;
            const selectedItem = card.querySelector('.proposal-item.selected');
            const selectedContributor = selectedItem ? selectedItem.getAttribute('data-author') : 'Unknown';

            // Collect all unique contributors who submitted suggestions for this key so everyone gets credit
            const allKeyContributors = new Set();
            if (selectedContributor && selectedContributor !== 'Unknown') {
              allKeyContributors.add(selectedContributor);
            }
            (item.proposals || []).forEach(p => {
              const pAuthor = String(p.contributor || '').replace(/^@+/, '').trim();
              if (pAuthor && pAuthor !== 'Unknown') {
                allKeyContributors.add(pAuthor);
              }
            });
            const contributorsArray = Array.from(allKeyContributors);

            if (ENABLE_DEV_SIMULATION && item.language === SIM_LANG_CODE) {
              SIMULATION_DATA.liveStatus.approved[item.key] = {
                value: selectedText,
                translation: selectedText,
                contributor: selectedContributor,
                contributors: contributorsArray,
                approvedAt: new Date().toISOString()
              };
              delete SIMULATION_DATA.liveStatus.proposals[item.key];
              delete SIMULATION_DATA.proposals[item.key];
              showToast(`Approved "${item.key}"! (Simulated)`);
              loadAdminQueue();
              return;
            }

            approveBtn.disabled = true;
            approveBtn.textContent = 'Approving...';

            try {
              const aRes = await fetch(`${WORKER_URL}/api/approve`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${sessionAdminPassword}` },
                body: JSON.stringify({
                  language: item.language,
                  key: item.key,
                  selectedText,
                  contributor: selectedContributor,
                  contributors: contributorsArray
                })
              });

              if (aRes.ok) {
                showToast(`Approved "${item.key}"!`);
                loadAdminQueue();
              } else {
                alert("Failed to approve.");
                approveBtn.disabled = false;
                approveBtn.innerHTML = `${ICONS.check} Approve Selected`;
              }
            } catch (e) {
              alert("Error: " + e.message);
              approveBtn.disabled = false;
              approveBtn.innerHTML = `${ICONS.check} Approve Selected`;
            }
          };
        });
      }

      // Render Approved Translations for this language
      const approvedContainer = groupCard.querySelector(`#approved-container-${lang}`);
      if (approvedContainer) {
        if (approvedKeys.length === 0) {
          approvedContainer.innerHTML = `
            <div style="padding: 16px; background: rgba(255,255,255,0.015); border: 1px dashed var(--border-glass); border-radius: var(--radius-md); font-size: 13px; color: var(--text-tertiary); text-align: center;">
              No approved translations staged for ${escapeHtml(langMeta.name || lang)} yet.
            </div>
          `;
        } else {
          const approvedTable = document.createElement('div');
          approvedTable.className = 'admin-approved-table';

          approvedKeys.forEach(k => {
            const aObj = langApproved[k];
            const catItem = catMap.get(k);
            const src = catItem ? catItem.source : '';
            const val = aObj.value || aObj.translation || '';
            const hasPendingSuggestion = langPending.some(p => p.key === k);
            let contrib = '';
            if (Array.isArray(aObj.contributors) && aObj.contributors.length > 0) {
              contrib = aObj.contributors.map(c => String(c).replace(/^@+/, '').trim()).filter(Boolean).join(', ');
            } else {
              contrib = String(aObj.contributor || 'Unknown').replace(/^@+/, '');
            }

            let syncStatusHtml = '';
            if (aObj.prNumber) {
              syncStatusHtml = `
                <a href="${aObj.prUrl || '#'}" target="_blank" rel="noopener" class="admin-pr-link">
                  ${ICONS.gitPullRequest} PR #${aObj.prNumber} (${aObj.prState || 'Open'}) ↗
                </a>
              `;
            } else {
              syncStatusHtml = `
                <span class="admin-ready-badge">
                  ${ICONS.check} Ready to Sync
                </span>
              `;
            }

            const row = document.createElement('div');
            row.className = 'admin-approved-row';
            row.id = `approved-row-${lang}-${k}`;

            row.innerHTML = `
              <div class="approved-key-col">
                <span class="card-key-pill" title="${escapeHtml(k)}">${escapeHtml(k)}</span>
                ${catItem && catItem.comment ? `<div class="approved-context-text" title="${escapeHtml(catItem.comment)}">${escapeHtml(catItem.comment)}</div>` : ''}
              </div>

              <div class="approved-source-col">
                <div class="approved-col-label">English Source</div>
                <div class="approved-source-text">${escapeHtml(src || '-')}</div>
              </div>

              <div class="approved-trans-col">
                <div class="approved-col-label">Approved Translation</div>
                <div class="approved-val-text">${escapeHtml(val)}</div>
                ${hasPendingSuggestion ? `<div class="admin-pending-overwrite-badge"><svg class="svg-icon" viewBox="0 0 16 16" width="11" height="11" fill="none" stroke="currentColor" stroke-width="2"><path d="M8 2l6 11H2zM8 6v3m0 2h.01"/></svg> New Suggestion Pending Review Above</div>` : ''}
                <div class="approved-author-text">By ${escapeHtml(contrib)} ${aObj.approvedAt ? `• ${formatTimeAgo(aObj.approvedAt)}` : ''}</div>
              </div>

              <div class="approved-status-col">
                <div class="approved-col-label">Git Sync Status</div>
                <div>${syncStatusHtml}</div>
              </div>

              <div class="approved-action-col">
                <button class="btn btn-sm btn-glass admin-unapprove-btn" data-lang="${lang}" data-key="${escapeHtml(k)}" title="Remove this translation from approved staging">
                  ${ICONS.trash} Remove
                </button>
              </div>
            `;

            approvedTable.appendChild(row);

            // Wire unapprove / remove button
            const removeBtn = row.querySelector('.admin-unapprove-btn');
            if (removeBtn) {
              removeBtn.onclick = async () => {
                if (!confirm(`Remove "${k}" from approved staging? It will no longer be synced to Git.`)) return;
                removeBtn.disabled = true;
                removeBtn.textContent = 'Removing...';
                try {
                  const uRes = await fetch(`${WORKER_URL}/api/unapprove`, {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${sessionAdminPassword}` },
                    body: JSON.stringify({ language: lang, key: k })
                  });
                  if (uRes.ok) {
                    showToast(`Removed "${k}" from staging`);
                    loadAdminQueue();
                  } else {
                    alert("Failed to remove translation.");
                    removeBtn.disabled = false;
                    removeBtn.innerHTML = `${ICONS.trash} Remove`;
                  }
                } catch (e) {
                  alert("Error: " + e.message);
                  removeBtn.disabled = false;
                  removeBtn.innerHTML = `${ICONS.trash} Remove`;
                }
              };
            }
          });

          approvedContainer.appendChild(approvedTable);
        }
      }
    });
  }

  // Automated Pull Request Creation Action (Single Language or All)
  async function triggerSyncPr(targetLanguage = null, triggerBtn = null) {
    if (ENABLE_DEV_SIMULATION && (targetLanguage === SIM_LANG_CODE || !targetLanguage || targetLanguage === "all")) {
      showToast("Simulation: Pull Request creation simulated successfully!");
      return;
    }
    if (!sessionAdminPassword) {
      try {
        await requestAdminAuth();
      } catch {
        return;
      }
    }

    const prBtn = triggerBtn || document.getElementById('admin-sync-pr-btn');

    const originalBtnHtml = prBtn ? prBtn.innerHTML : '';
    if (prBtn) {
      prBtn.disabled = true;
      prBtn.innerHTML = `<span class="loading-spinner"></span> Creating Sync PR...`;
    }

    try {
      const res = await fetch(`${WORKER_URL}/api/sync-pr`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${sessionAdminPassword}`
        },
        body: JSON.stringify({
          language: (targetLanguage && targetLanguage !== "all") ? targetLanguage : "all"
        })
      });

      const data = await res.json();
      if (res.ok) {
        showToast(`Pull Request #${data.pr_number} created on GitHub!`);
        loadAdminQueue();
      } else {
        alert(data.error || "Failed to create PR.");
      }
    } catch (err) {
      alert("Error triggering sync PR: " + err.message);
    } finally {
      if (prBtn) {
        prBtn.disabled = false;
        prBtn.innerHTML = originalBtnHtml;
      }
    }
  }

  // -------------------------------------------------------------------------
  // 10. LOCAL STORAGE & EXPORT HELPERS
  // -------------------------------------------------------------------------

  function getLocalTranslation(lang, key) {
    try {
      const stored = safeStorageGet(`tunetastic_l10n_${lang}`, true);
      if (!stored) return '';
      const map = JSON.parse(stored);
      return map[key] || '';
    } catch {
      return '';
    }
  }

  function setLocalTranslation(lang, key, value, updateTimestamp = true) {
    try {
      const stored = safeStorageGet(`tunetastic_l10n_${lang}`, true);
      const map = stored ? JSON.parse(stored) : {};
      if (value && value.trim()) {
        map[key] = value;
        if (updateTimestamp) {
          safeStorageSet(`tunetastic_draft_updated_${lang}`, String(Date.now()), true);
        }
      } else {
        delete map[key];
        if (Object.keys(map).length === 0) {
          safeStorageRemove(`tunetastic_draft_updated_${lang}`);
        } else if (updateTimestamp) {
          safeStorageSet(`tunetastic_draft_updated_${lang}`, String(Date.now()), true);
        }
      }
      safeStorageSet(`tunetastic_l10n_${lang}`, JSON.stringify(map), true);
      updateDraftTimerDisplay();
    } catch (e) {}
  }

  function getAllLocalTranslations(lang) {
    try {
      const stored = safeStorageGet(`tunetastic_l10n_${lang}`, true);
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
      const val = input ? input.value.trim() : '';
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
        const val = e.target.value.trim();
        if (val) {
          safeStorageSet('tunetastic_author', val);
        } else {
          safeStorageRemove('tunetastic_author');
        }
      });
    }

    // View Submissions modal triggers
    const viewSubCloseBtn = document.getElementById('view-submissions-close-btn');
    if (viewSubCloseBtn) {
      viewSubCloseBtn.onclick = () => {
        if (viewSubmissionsModal) viewSubmissionsModal.classList.remove('active');
      };
    }

    // Modal backdrop clicks to close
    [viewSubmissionsModal, submitModal].forEach(modalEl => {
      if (modalEl) {
        modalEl.addEventListener('click', (e) => {
          if (e.target === modalEl) {
            modalEl.classList.remove('active');
          }
        });
      }
    });

    // Global Escape Key to close open modals
    window.addEventListener('keydown', (e) => {
      if (e.key === 'Escape') {
        if (viewSubmissionsModal && viewSubmissionsModal.classList.contains('active')) {
          viewSubmissionsModal.classList.remove('active');
        } else if (submitModal && submitModal.classList.contains('active')) {
          submitModal.classList.remove('active');
        }
      }
    });

    // Admin review triggers
    document.getElementById('admin-refresh-btn').onclick = loadAdminQueue;
    document.getElementById('admin-sync-pr-btn').onclick = triggerSyncPr;
    const clearAllBtn = document.getElementById('admin-clear-all-btn');
    if (clearAllBtn) {
      clearAllBtn.onclick = async () => {
        if (!sessionAdminPassword) {
          try {
            await requestAdminAuth();
          } catch {
            return;
          }
        }
        if (!confirm("Are you sure you want to discard and clear ALL community submissions across all keys? This cannot be undone.")) {
          return;
        }
        clearAllBtn.disabled = true;
        clearAllBtn.innerHTML = '<span class="loading-spinner"></span> Clearing...';
        try {
          const res = await fetch(`${WORKER_URL}/api/clear-all`, {
            method: 'POST',
            headers: {
              'Content-Type': 'application/json',
              'Authorization': `Bearer ${sessionAdminPassword}`
            },
            body: JSON.stringify({})
          });
          if (res.ok) {
            showToast("All submissions have been cleared!");
            loadAdminQueue();
          } else {
            alert("Failed to clear submissions.");
          }
        } catch (e) {
          alert("Error: " + e.message);
        } finally {
          clearAllBtn.disabled = false;
          clearAllBtn.innerHTML = `${ICONS.trash} Clear All`;
        }
      };
    }

    // Filter tabs
    document.querySelectorAll('.filter-tab').forEach(pill => {
      pill.addEventListener('click', (e) => {
        document.querySelectorAll('.filter-tab').forEach(p => p.classList.remove('active'));
        const target = e.currentTarget;
        target.classList.add('active');
        currentFilter = target.getAttribute('data-filter');
        currentPage = 1;
        renderCards();
        syncFilterPillActiveStates();
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

    // Immediate active editing lock release on tab close or page exit
    window.addEventListener('pagehide', () => {
      if (isIncognitoMode) return;
      const lang = currentLang || safeStorageGet('tunetastic_last_lang');
      if (!lang || !SESSION_ID) return;
      const url = `${WORKER_URL}/api/drafts/release-lock?lang=${encodeURIComponent(lang)}&sid=${encodeURIComponent(SESSION_ID)}`;
      try {
        if (navigator.sendBeacon) {
          navigator.sendBeacon(url);
        } else {
          fetch(url, { keepalive: true, method: 'POST' }).catch(() => {});
        }
      } catch {}
    });
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
