/**
 * Tunetastic Community Localization Staging Worker v2
 * Handles:
 * 1. Zero-Login 48-Hour Sliding Draft Sessions (/api/draft, /api/drafts/resume)
 * 2. Multi-Version Community Proposals (Max 10) with Exact-Match Guard (/api/submit, /api/suggest)
 * 3. Collapsible Issue #143 Tracker Notifications
 * 4. Admin Review Queue & Approval (/api/pending, /api/approve)
 * 5. Automated Pull Request Creation via GitHub REST API (/api/sync-pr)
 * 6. Safe Post-Merge KV Cleanup (/api/cleanup-synced)
 */

function getCorsHeaders(request) {
  const origin = request.headers.get("Origin");
  return {
    "Access-Control-Allow-Origin": origin && origin !== "null" ? origin : "*",
    "Access-Control-Allow-Methods": "GET, POST, OPTIONS",
    "Access-Control-Allow-Headers": "Content-Type, Authorization, X-Session-ID"
  };
}

function jsonResponse(data, status = 200, corsHeaders) {
  return new Response(JSON.stringify(data), {
    status,
    headers: { ...corsHeaders, "Content-Type": "application/json" }
  });
}

function verifyAdminAuth(request, env) {
  if (!env.ADMIN_PASSWORD_HASH || typeof env.ADMIN_PASSWORD_HASH !== "string" || env.ADMIN_PASSWORD_HASH.trim().length === 0) {
    return false;
  }
  const auth = request.headers.get("Authorization");
  return auth === `Bearer ${env.ADMIN_PASSWORD_HASH}`;
}


function formatToIST(date = new Date()) {
  try {
    const formatted = new Intl.DateTimeFormat("en-GB", {
      timeZone: "Asia/Kolkata",
      weekday: "short",
      day: "2-digit",
      month: "short",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
      second: "2-digit",
      hour12: true
    }).format(date);
    return formatted + " IST";
  } catch {
    const istOffset = 5.5 * 60 * 60 * 1000;
    return new Date(date.getTime() + istOffset).toUTCString().replace("GMT", "IST");
  }
}

function getSessionId(request) {
  const sid = request.headers.get("X-Session-ID") || "";
  return sid.replace(/[^a-zA-Z0-9_\-]/g, "").slice(0, 64) || "anon";
}

function setLockSession(currentLocks, k, sId, time) {
  if (!currentLocks[k] || typeof currentLocks[k] !== 'object') {
    currentLocks[k] = { sessions: {} };
  }
  if (!currentLocks[k].sessions || typeof currentLocks[k].sessions !== 'object') {
    const existingSId = currentLocks[k].sessionId;
    const existingTime = currentLocks[k].updatedAt || time;
    currentLocks[k].sessions = {};
    if (existingSId) {
      currentLocks[k].sessions[existingSId] = existingTime;
    }
  }
  currentLocks[k].sessions[sId] = time;
  currentLocks[k].sessionId = sId;
  currentLocks[k].updatedAt = time;
}

function removeLockSession(currentLocks, k, sId) {
  if (!currentLocks[k]) return;
  if (currentLocks[k].sessions && typeof currentLocks[k].sessions === 'object') {
    delete currentLocks[k].sessions[sId];
    const remaining = Object.entries(currentLocks[k].sessions);
    if (remaining.length === 0) {
      delete currentLocks[k];
    } else {
      currentLocks[k].sessionId = remaining[remaining.length - 1][0];
      currentLocks[k].updatedAt = remaining[remaining.length - 1][1];
    }
  } else if (currentLocks[k].sessionId === sId) {
    delete currentLocks[k];
  }
}

// XML Resw helper to merge translations into existing resw content
function mergeReswXml(existingXml, newTranslations) {
  let xml = existingXml;
  for (const [key, val] of Object.entries(newTranslations)) {
    const escapedVal = String(val).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;");
    const regex = new RegExp(`(<data\\s+name="${key}"[\\s\\S]*?<value>)[\\s\\S]*?(<\\/value>)`, 'g');
    if (regex.test(xml)) {
      xml = xml.replace(regex, `$1${escapedVal}$2`);
    } else {
      // Append before </root>
      const entry = `  <data name="${key}" xml:space="preserve">\n    <value>${escapedVal}</value>\n  </data>\n</root>`;
      xml = xml.replace(/<\/root>\s*$/, entry);
    }
  }
  return xml;
}


async function syncPrStatuses(env, owner, repo) {
  if (!env.GITHUB_TOKEN) return;
  try {
    const list = await env.SUBMISSIONS.list({ prefix: "approved:" });
    const prCache = new Map();

    for (const item of list.keys) {
      const lang = item.name.replace("approved:", "");
      const raw = await env.SUBMISSIONS.get(item.name);
      if (!raw) continue;
      let approvedMap = JSON.parse(raw);
      let changed = false;
      const mergedKeys = {};

      for (const [key, obj] of Object.entries(approvedMap)) {
        if (!obj.prNumber) continue;

        let prInfo = prCache.get(obj.prNumber);
        if (!prInfo) {
          try {
            const res = await fetch(`https://api.github.com/repos/${owner}/${repo}/pulls/${obj.prNumber}`, {
              headers: {
                "Authorization": `Bearer ${env.GITHUB_TOKEN}`,
                "User-Agent": "Tunetastic-L10n-Bot"
              }
            });
            if (res.ok) {
              const data = await res.json();
              prInfo = {
                state: data.state, // "open" or "closed"
                merged_at: data.merged_at, // string ISO or null
                html_url: data.html_url
              };
              prCache.set(obj.prNumber, prInfo);
            }
          } catch (e) {
            console.warn(`Could not check PR #${obj.prNumber}:`, e);
          }
        }

        if (prInfo) {
          if (prInfo.state === "closed" && prInfo.merged_at) {
            // PR IS MERGED (DONE!)
            mergedKeys[key] = {
              value: obj.value,
              translation: obj.translation || obj.value,
              contributor: obj.contributor,
              mergedAt: prInfo.merged_at,
              prNumber: obj.prNumber
            };
            delete approvedMap[key];
            changed = true;
          } else if (prInfo.state === "closed" && !prInfo.merged_at) {
            // PR IS CLOSED WITHOUT MERGE!
            delete obj.prNumber;
            delete obj.prUrl;
            delete obj.prBranch;
            delete obj.prState;
            delete obj.syncedAt;
            changed = true;
          } else if (prInfo.state === "open") {
            if (obj.prState !== "open") {
              obj.prState = "open";
              changed = true;
            }
          }
        }
      }

      if (Object.keys(mergedKeys).length > 0) {
        const mergedKey = `merged:${lang}`;
        const existingMergedRaw = await env.SUBMISSIONS.get(mergedKey);
        const existingMerged = existingMergedRaw ? JSON.parse(existingMergedRaw) : {};
        Object.assign(existingMerged, mergedKeys);
        await env.SUBMISSIONS.put(mergedKey, JSON.stringify(existingMerged));
      }

      if (changed) {
        if (Object.keys(approvedMap).length === 0) {
          await env.SUBMISSIONS.delete(item.name);
        } else {
          await env.SUBMISSIONS.put(item.name, JSON.stringify(approvedMap));
        }
      }
    }
  } catch (err) {
    console.warn("syncPrStatuses error:", err);
  }
}

export default {
  async fetch(request, env) {
    const corsHeaders = getCorsHeaders(request);

    if (request.method === "OPTIONS") {
      return new Response(null, { headers: corsHeaders });
    }

    const url = new URL(request.url);
    const path = url.pathname;
    const sessionId = getSessionId(request);

    try {
      // -----------------------------------------------------------------------
      // 1. DRAFT AUTOSAVE & 48-HOUR SLIDING WINDOW (Supports single & batch drafts)
      // -----------------------------------------------------------------------
      if (path === "/api/draft" && request.method === "POST") {
        const body = await request.json().catch(() => ({}));
        const { language, key, text, drafts, updatedAt } = body;
        if (!language || (!key && !drafts)) {
          return jsonResponse({ error: "Language and either key or drafts required" }, 400, corsHeaders);
        }

        const draftKey = `draft:${language}:${sessionId}`;
        const existingDraftRaw = await env.SUBMISSIONS.get(draftKey);
        const draftData = existingDraftRaw ? JSON.parse(existingDraftRaw) : { keys: {}, updatedAt: null };

        // Update active locks map
        const lockMapKey = `active_locks:${language}`;
        const currentLocksRaw = await env.SUBMISSIONS.get(lockMapKey);
        const currentLocks = currentLocksRaw ? JSON.parse(currentLocksRaw) : {};

        if (drafts && typeof drafts === "object") {
          const now = Date.now();
          for (const [dKey, dText] of Object.entries(drafts)) {
            if (dText && typeof dText === "string" && dText.trim().length > 0) {
              draftData.keys[dKey] = dText;
              setLockSession(currentLocks, dKey, sessionId, now);
            } else {
              delete draftData.keys[dKey];
              removeLockSession(currentLocks, dKey, sessionId);
            }
          }
          draftData.updatedAt = updatedAt || new Date().toISOString();
        } else if (key) {
          if (text && typeof text === "string" && text.trim().length > 0) {
            draftData.keys[key] = text;
            draftData.updatedAt = new Date().toISOString();
            setLockSession(currentLocks, key, sessionId, Date.now());
          } else {
            delete draftData.keys[key];
            removeLockSession(currentLocks, key, sessionId);
          }
        }

        // 48-hour sliding TTL (172800 seconds)
        await env.SUBMISSIONS.put(draftKey, JSON.stringify(draftData), { expirationTtl: 172800 });
        await env.SUBMISSIONS.put(lockMapKey, JSON.stringify(currentLocks), { expirationTtl: 172800 });

        return jsonResponse({ success: true, ttl: 172800 }, 200, corsHeaders);
      }

      // Resume drafts for current session
      if (path === "/api/drafts/resume" && request.method === "GET") {
        const lang = url.searchParams.get("lang");
        if (!lang) return jsonResponse({ drafts: {} }, 200, corsHeaders);

        const draftKey = `draft:${lang}:${sessionId}`;
        const raw = await env.SUBMISSIONS.get(draftKey);
        if (!raw) return jsonResponse({ drafts: {} }, 200, corsHeaders);

        const data = JSON.parse(raw);
        return jsonResponse({ drafts: data.keys || {}, updatedAt: data.updatedAt || null }, 200, corsHeaders);
      }

      // Discard drafts for current session
      if (path === "/api/drafts/discard" && request.method === "POST") {
        const { language } = await request.json().catch(() => ({}));
        if (language) {
          const draftKey = `draft:${language}:${sessionId}`;
          await env.SUBMISSIONS.delete(draftKey);

          // Clear active locks for this session
          const lockMapKey = `active_locks:${language}`;
          const currentLocksRaw = await env.SUBMISSIONS.get(lockMapKey);
          if (currentLocksRaw) {
            const currentLocks = JSON.parse(currentLocksRaw);
            let modified = false;
            for (const k of Object.keys(currentLocks)) {
              removeLockSession(currentLocks, k, sessionId);
              modified = true;
            }
            if (modified) {
              await env.SUBMISSIONS.put(lockMapKey, JSON.stringify(currentLocks), { expirationTtl: 172800 });
            }
          }
        }
        return jsonResponse({ success: true }, 200, corsHeaders);
      }

      // Release active locks for session on tab close / unload (supports keepalive fetch, sendBeacon, and image ping)
      if (path === "/api/drafts/release-lock" && (request.method === "POST" || request.method === "GET")) {
        let body = {};
        if (request.method === "POST") {
          try {
            const rawText = await request.text();
            if (rawText) body = JSON.parse(rawText);
          } catch {}
        }
        const language = body.language || url.searchParams.get("lang");
        const sid = body.sessionId || url.searchParams.get("sessionId") || url.searchParams.get("sid") || request.headers.get("X-Session-ID") || sessionId;

        if (language && sid) {
          const lockMapKey = `active_locks:${language}`;
          const currentLocksRaw = await env.SUBMISSIONS.get(lockMapKey);
          if (currentLocksRaw) {
            const currentLocks = JSON.parse(currentLocksRaw);
            let modified = false;
            for (const k of Object.keys(currentLocks)) {
              removeLockSession(currentLocks, k, sid);
              modified = true;
            }
            if (modified) {
              await env.SUBMISSIONS.put(lockMapKey, JSON.stringify(currentLocks), { expirationTtl: 172800 });
            }
          }
        }
        return jsonResponse({ success: true }, 200, corsHeaders);
      }

      // Reset/clear active locks for a language (admin reset or clearing orphaned locks)
      if (path === "/api/locks/clear" && (request.method === "POST" || request.method === "GET")) {
        const language = url.searchParams.get("lang");
        if (language) {
          await env.SUBMISSIONS.delete(`active_locks:${language}`);
        }
        return jsonResponse({ success: true, message: `Active locks cleared for ${language}` }, 200, corsHeaders);
      }

      // Key-specific Proposals Stack (up to 10 versions)
      if (path === "/api/proposals" && request.method === "GET") {
        const lang = url.searchParams.get("lang");
        const key = url.searchParams.get("key");
        if (!lang || !key) return jsonResponse({ proposals: [] }, 200, corsHeaders);
        const proposalKey = `proposals:${lang}:${key}`;
        const raw = await env.SUBMISSIONS.get(proposalKey);
        const proposals = raw ? JSON.parse(raw) : [];
        return jsonResponse({ proposals }, 200, corsHeaders);
      }

      // Live status of locks, approved strings, and proposals for a language
      if (path === "/api/status" && request.method === "GET") {
        const lang = url.searchParams.get("lang");
        if (!lang) return jsonResponse({ locks: {}, approved: {}, proposals: {} }, 200, corsHeaders);

        // Active Locks
        const locksRaw = await env.SUBMISSIONS.get(`active_locks:${lang}`);
        const allLocks = locksRaw ? JSON.parse(locksRaw) : {};
        // Filter out expired locks older than 48 hours
        const activeLocks = {};
        const now = Date.now();
        for (const [k, lock] of Object.entries(allLocks)) {
          let hasMine = false;
          let hasOther = false;

          if (lock && lock.sessions && typeof lock.sessions === "object") {
            for (const [sId, sTime] of Object.entries(lock.sessions)) {
              if (now - sTime < 172800 * 1000) {
                if (sId === sessionId) hasMine = true;
                else hasOther = true;
              }
            }
          } else if (lock && lock.updatedAt && (now - lock.updatedAt < 172800 * 1000)) {
            if (lock.sessionId === sessionId) hasMine = true;
            else hasOther = true;
          }

          if (hasMine && hasOther) {
            activeLocks[k] = "both";
          } else if (hasMine) {
            activeLocks[k] = "mine";
          } else if (hasOther) {
            activeLocks[k] = "other";
          }
        }

        // Approved
        const approvedRaw = await env.SUBMISSIONS.get(`approved:${lang}`);
        const approved = approvedRaw ? JSON.parse(approvedRaw) : {};

        // Proposals summary (counts and preview) - guarded against KV list() daily quota exhaustion
        let proposalsSummary = {};
        try {
          const summaryRaw = await env.SUBMISSIONS.get(`proposals_summary:${lang}`);
          if (summaryRaw) {
            proposalsSummary = JSON.parse(summaryRaw);
          } else {
            try {
              const proposalsList = await env.SUBMISSIONS.list({ prefix: `proposals:${lang}:` });
              for (const item of proposalsList.keys) {
                const k = item.name.replace(`proposals:${lang}:`, "");
                const rawP = await env.SUBMISSIONS.get(item.name);
                if (rawP) {
                  const arr = JSON.parse(rawP);
                  proposalsSummary[k] = {
                    count: arr.length,
                    isFull: arr.length >= 10
                  };
                }
              }
            } catch (listErr) {
              // KV list() daily limit exceeded - gracefully fallback so /api/status NEVER fails
            }
          }
        } catch {}

        // Merged (PR merged into repository)
        const mergedRaw = await env.SUBMISSIONS.get(`merged:${lang}`);
        const merged = mergedRaw ? JSON.parse(mergedRaw) : {};

        return jsonResponse({ locks: activeLocks, approved, merged, proposals: proposalsSummary }, 200, corsHeaders);
      }

      // -----------------------------------------------------------------------
      // 2. PROPOSALS SUBMISSION & EXACT-MATCH GUARD (MAX 10 SLOTS)
      // -----------------------------------------------------------------------
      if (path === "/api/submit" && request.method === "POST") {
        const body = await request.json();
        const { language, contributor = "Unknown", translations = {}, details = {} } = body;

        const sanitizedContributor = String(contributor).slice(0, 50).trim() || "Unknown";
        const keys = Object.keys(translations);
        if (!language || keys.length === 0) {
          return jsonResponse({ error: "Language and non-empty translations required" }, 400, corsHeaders);
        }

        const accepted = [];
        const duplicates = [];
        const fullSlots = [];

        for (const [k, text] of Object.entries(translations)) {
          const proposalKey = `proposals:${language}:${k}`;
          const raw = await env.SUBMISSIONS.get(proposalKey);
          let proposals = raw ? JSON.parse(raw) : [];

          // 1. Max 10 slots check
          if (proposals.length >= 10) {
            fullSlots.push(k);
            continue;
          }

          // 2. Strict Exact Match Duplicate Guard (Preserving genuine whitespace)
          const isExactMatch = proposals.some(p => p.text === text);
          if (isExactMatch) {
            duplicates.push(k);
            continue;
          }

          // Add to proposals
          const newProposal = {
            id: `p_${Date.now()}_${Math.random().toString(36).substr(2, 6)}`,
            text,
            contributor: sanitizedContributor,
            sessionId,
            timestamp: new Date().toISOString()
          };
          proposals.push(newProposal);
          await env.SUBMISSIONS.put(proposalKey, JSON.stringify(proposals));
          const kSource = (details && details[k] && details[k].source) ? details[k].source : "";
          const kContext = (details && details[k] && details[k].context) ? details[k].context : "";
          accepted.push({ key: k, text, source: kSource, context: kContext });
        }

        // Update proposals summary index
        if (accepted.length > 0) {
          try {
            const summaryKey = `proposals_summary:${language}`;
            const existingRaw = await env.SUBMISSIONS.get(summaryKey);
            const summary = existingRaw ? JSON.parse(existingRaw) : {};
            for (const a of accepted) {
              const pKey = `proposals:${language}:${a.key}`;
              const pRaw = await env.SUBMISSIONS.get(pKey);
              if (pRaw) {
                const arr = JSON.parse(pRaw);
                summary[a.key] = {
                  count: arr.length,
                  isFull: arr.length >= 10
                };
              }
            }
            await env.SUBMISSIONS.put(summaryKey, JSON.stringify(summary));
          } catch {}
        }

        // Release locks for submitted keys for this session
        const lockMapKey = `active_locks:${language}`;
        const currentLocksRaw = await env.SUBMISSIONS.get(lockMapKey);
        if (currentLocksRaw) {
          const currentLocks = JSON.parse(currentLocksRaw);
          for (const k of keys) {
            removeLockSession(currentLocks, k, sessionId);
          }
          await env.SUBMISSIONS.put(lockMapKey, JSON.stringify(currentLocks), { expirationTtl: 172800 });
        }

        // Send Collapsed GitHub Tracker Comment to Issue #143
        if (env.GITHUB_TOKEN && env.TRACKER_ISSUE_NUMBER && accepted.length > 0) {
          try {
            const escapeCell = (val) => String(val || "").replace(/\|/g, "\\|").replace(/\r?\n/g, " ").trim() || "-";
            const tableRows = accepted.slice(0, 100).map(a =>
              `| \`${a.key}\` | ${escapeCell(a.context)} | ${escapeCell(a.source)} | \`${escapeCell(a.text)}\` |`
            ).join("\n");

            const commentBody =
              `🌐 **New Translation Submission for \`${language}\`**\n\n` +
              `- **Contributor:** ${sanitizedContributor.replace(/^@+/, "")}\n` +
              `- **Strings Submitted:** ${accepted.length} string(s)\n` +
              `- **Timestamp:** ${formatToIST(new Date())}\n\n` +
              `<details>\n<summary><b>Click to view translated strings (${accepted.length})</b></summary>\n\n` +
              `| Key | App Context | English Source | Submitted Translation |\n` +
              `| :--- | :--- | :--- | :--- |\n` +
              `${tableRows}\n\n` +
              `</details>\n\n` +
              `👉 [Review & Approve in Portal](https://${env.GITHUB_OWNER || "AMit-KP"}.github.io/${env.GITHUB_REPO || "Tunetastic"}/#admin)`;

            const owner = env.GITHUB_OWNER || "AMit-KP";
            const repo = env.GITHUB_REPO || "Tunetastic";
            const issueNum = String(env.TRACKER_ISSUE_NUMBER || "143");
            const workflowRef = env.GITHUB_WORKFLOW_REF || "gh-pages";

            // Attempt to dispatch GitHub Actions workflow so comment is posted by github-actions[bot]
            let dispatched = false;
            try {
              const dispatchRes = await fetch(`https://api.github.com/repos/${owner}/${repo}/actions/workflows/notify-submission.yml/dispatches`, {
                method: "POST",
                headers: {
                  "Authorization": `Bearer ${env.GITHUB_TOKEN}`,
                  "Accept": "application/vnd.github+json",
                  "User-Agent": "Tunetastic-L10n-Bot",
                  "Content-Type": "application/json"
                },
                body: JSON.stringify({
                  ref: workflowRef,
                  inputs: {
                    commentBody: commentBody,
                    issueNumber: issueNum
                  }
                })
              });
              if (dispatchRes.status === 204 || dispatchRes.ok) {
                dispatched = true;
              } else {
                const errText = await dispatchRes.text();
                console.warn(`Workflow dispatch returned ${dispatchRes.status}: ${errText}. Falling back to direct comment.`);
              }
            } catch (dispErr) {
              console.warn("Workflow dispatch error, falling back to direct comment:", dispErr);
            }

            // Fallback: direct issue comment if workflow dispatch was not triggered
            if (!dispatched) {
              await fetch(`https://api.github.com/repos/${owner}/${repo}/issues/${issueNum}/comments`, {
                method: "POST",
                headers: {
                  "Authorization": `Bearer ${env.GITHUB_TOKEN}`,
                  "User-Agent": "Tunetastic-L10n-Bot",
                  "Content-Type": "application/json"
                },
                body: JSON.stringify({ body: commentBody })
              });
            }
          } catch (botErr) {
            console.error("Failed to post GitHub comment:", botErr);
          }
        }

        return jsonResponse({
          success: true,
          acceptedCount: accepted.length,
          duplicateKeys: duplicates,
          fullSlotKeys: fullSlots
        }, 200, corsHeaders);
      }

      // Single Key Alternative Suggestion
      if (path === "/api/suggest" && request.method === "POST") {
        const { language, key, text, contributor = "Unknown" } = await request.json();
        if (!language || !key || text === undefined) {
          return jsonResponse({ error: "Language, key, and text required" }, 400, corsHeaders);
        }

        const proposalKey = `proposals:${language}:${key}`;
        const raw = await env.SUBMISSIONS.get(proposalKey);
        let proposals = raw ? JSON.parse(raw) : [];

        if (proposals.length >= 10) {
          return jsonResponse({ error: "Max suggestions reached (10/10) for this string." }, 429, corsHeaders);
        }

        // Strict exact match check
        if (proposals.some(p => p.text === text)) {
          return jsonResponse({ error: "This exact translation has already been suggested." }, 409, corsHeaders);
        }

        const sanitizedContributor = String(contributor).slice(0, 50).trim() || "Unknown";
        proposals.push({
          id: `p_${Date.now()}_${Math.random().toString(36).substr(2, 6)}`,
          text,
          contributor: sanitizedContributor,
          sessionId,
          timestamp: new Date().toISOString()
        });

        await env.SUBMISSIONS.put(proposalKey, JSON.stringify(proposals));

        // Update proposals summary index
        try {
          const summaryKey = `proposals_summary:${language}`;
          const existingRaw = await env.SUBMISSIONS.get(summaryKey);
          const summary = existingRaw ? JSON.parse(existingRaw) : {};
          summary[key] = { count: proposals.length, isFull: proposals.length >= 10 };
          await env.SUBMISSIONS.put(summaryKey, JSON.stringify(summary));
        } catch {}

        return jsonResponse({ success: true, count: proposals.length }, 200, corsHeaders);
      }

      // -----------------------------------------------------------------------
      // 3. ADMIN REVIEW QUEUE (/api/pending, /api/approve)
      // -----------------------------------------------------------------------
      if (path === "/api/pending" && request.method === "GET") {
        if (!verifyAdminAuth(request, env)) {
          return jsonResponse({ error: "Unauthorized" }, 401, corsHeaders);
        }

        const owner = env.GITHUB_OWNER || "AMit-KP";
        const repo = env.GITHUB_REPO || "Tunetastic";

        // Automatically sync PR statuses with GitHub (merged vs closed vs open)
        await syncPrStatuses(env, owner, repo);

        // Fetch all pending proposals
        const list = await env.SUBMISSIONS.list({ prefix: "proposals:" });
        const pendingItems = [];
        for (const item of list.keys) {
          // Format: proposals:<lang>:<key>
          const parts = item.name.split(":");
          const lang = parts[1];
          const key = parts.slice(2).join(":");
          const raw = await env.SUBMISSIONS.get(item.name);
          if (raw) {
            pendingItems.push({ language: lang, key, proposals: JSON.parse(raw) });
          }
        }

        // Fetch all approved translations
        const approvedList = await env.SUBMISSIONS.list({ prefix: "approved:" });
        const approvedPerLang = {};
        for (const item of approvedList.keys) {
          const lang = item.name.replace("approved:", "");
          const raw = await env.SUBMISSIONS.get(item.name);
          if (raw) {
            const data = JSON.parse(raw);
            if (Object.keys(data).length > 0) {
              approvedPerLang[lang] = data;
            }
          }
        }

        return jsonResponse({ pending: pendingItems, approved: approvedPerLang }, 200, corsHeaders);
      }

      if (path === "/api/approve" && request.method === "POST") {
        if (!verifyAdminAuth(request, env)) {
          return jsonResponse({ error: "Unauthorized" }, 401, corsHeaders);
        }

        const { language, key, selectedText, contributor } = await request.json();
        if (!language || !key || selectedText === undefined) {
          return jsonResponse({ error: "Missing required approval fields" }, 400, corsHeaders);
        }

        // Save into approved:<lang>
        const approvedKey = `approved:${language}`;
        const existingRaw = await env.SUBMISSIONS.get(approvedKey);
        const approvedMap = existingRaw ? JSON.parse(existingRaw) : {};
        approvedMap[key] = {
          value: selectedText,
          translation: selectedText,
          contributor: contributor || "Unknown",
          approvedAt: new Date().toISOString()
        };
        await env.SUBMISSIONS.put(approvedKey, JSON.stringify(approvedMap));

        // Delete proposals for this key since it is now approved
        await env.SUBMISSIONS.delete(`proposals:${language}:${key}`);

        // Clean up proposals summary index
        try {
          const summaryKey = `proposals_summary:${language}`;
          const existingRaw = await env.SUBMISSIONS.get(summaryKey);
          if (existingRaw) {
            const summary = JSON.parse(existingRaw);
            delete summary[key];
            await env.SUBMISSIONS.put(summaryKey, JSON.stringify(summary));
          }
        } catch {}

        return jsonResponse({ success: true, key, approvedValue: selectedText }, 200, corsHeaders);
      }

      // Clear / Reject all proposals for a single key
      if ((path === "/api/reject" || path === "/api/clear-proposals") && request.method === "POST") {
        if (!verifyAdminAuth(request, env)) {
          return jsonResponse({ error: "Unauthorized" }, 401, corsHeaders);
        }

        const { language, key } = await request.json();
        if (!language || !key) {
          return jsonResponse({ error: "Missing language or key" }, 400, corsHeaders);
        }

        // Delete proposals for this key
        await env.SUBMISSIONS.delete(`proposals:${language}:${key}`);

        // Clean up proposals summary index
        try {
          const summaryKey = `proposals_summary:${language}`;
          const existingRaw = await env.SUBMISSIONS.get(summaryKey);
          if (existingRaw) {
            const summary = JSON.parse(existingRaw);
            delete summary[key];
            await env.SUBMISSIONS.put(summaryKey, JSON.stringify(summary));
          }
        } catch {}

        return jsonResponse({ success: true, key }, 200, corsHeaders);
      }

      // Batch Clear all proposals
      if (path === "/api/clear-all" && request.method === "POST") {
        if (!verifyAdminAuth(request, env)) {
          return jsonResponse({ error: "Unauthorized" }, 401, corsHeaders);
        }
        const { language } = await request.json();
        const prefix = language ? `proposals:${language}:` : "proposals:";
        const list = await env.SUBMISSIONS.list({ prefix });
        for (const item of list.keys) {
          await env.SUBMISSIONS.delete(item.name);
        }
        if (language) {
          await env.SUBMISSIONS.delete(`proposals_summary:${language}`);
        }
        return jsonResponse({ success: true, count: list.keys.length }, 200, corsHeaders);
      }

      // -----------------------------------------------------------------------
      // 4. AUTOMATED PULL REQUEST CREATION (/api/sync-pr)
      // -----------------------------------------------------------------------
      if (path === "/api/sync-pr" && request.method === "POST") {
        if (!verifyAdminAuth(request, env)) {
          return jsonResponse({ error: "Unauthorized" }, 401, corsHeaders);
        }

        const owner = env.GITHUB_OWNER || "AMit-KP";
        const repo = env.GITHUB_REPO || "Tunetastic";
        const baseBranch = "feature/localization";

        let bodyJson = {};
        try {
          bodyJson = await request.json();
        } catch {}

        const targetLang = (bodyJson.language && bodyJson.language !== "all") ? bodyJson.language : null;

        const approvedPerLang = {};
        const contributorsSet = new Set();
        const langSummaries = [];

        if (targetLang) {
          const raw = await env.SUBMISSIONS.get(`approved:${targetLang}`);
          if (raw) {
            const data = JSON.parse(raw);
            const count = Object.keys(data).length;
            if (count > 0) {
              approvedPerLang[targetLang] = data;
              for (const v of Object.values(data)) {
                if (v.contributor && v.contributor !== "Unknown") contributorsSet.add(v.contributor);
              }
              langSummaries.push(`- **\`${targetLang}\`**: ${count} string(s)`);
            }
          }
        } else {
          const list = await env.SUBMISSIONS.list({ prefix: "approved:" });
          for (const item of list.keys) {
            const lang = item.name.replace("approved:", "");
            const raw = await env.SUBMISSIONS.get(item.name);
            if (raw) {
              const data = JSON.parse(raw);
              const count = Object.keys(data).length;
              if (count > 0) {
                approvedPerLang[lang] = data;
                for (const v of Object.values(data)) {
                  if (v.contributor && v.contributor !== "Unknown") contributorsSet.add(v.contributor);
                }
                langSummaries.push(`- **\`${lang}\`**: ${count} string(s)`);
              }
            }
          }
        }

        if (Object.keys(approvedPerLang).length === 0) {
          return jsonResponse({ error: targetLang ? `No approved translations found for ${targetLang}.` : "No translations found in approved queue." }, 400, corsHeaders);
        }

        // 1. Get HEAD commit of base branch
        const refRes = await fetch(`https://api.github.com/repos/${owner}/${repo}/git/ref/heads/${baseBranch}`, {
          headers: {
            "Authorization": `Bearer ${env.GITHUB_TOKEN}`,
            "User-Agent": "Tunetastic-L10n-Bot"
          }
        });
        if (!refRes.ok) {
          return jsonResponse({ error: `Could not fetch branch ${baseBranch}: ${await refRes.text()}` }, 500, corsHeaders);
        }
        const refData = await refRes.json();
        const baseSha = refData.object.sha;

        // 2. Create new branch: l10n/sync-<lang>-<timestamp> or l10n/community-sync-<timestamp>
        const syncBranch = targetLang ? `l10n/sync-${targetLang}-${Date.now()}` : `l10n/community-sync-${Date.now()}`;
        const createBranchRes = await fetch(`https://api.github.com/repos/${owner}/${repo}/git/refs`, {
          method: "POST",
          headers: {
            "Authorization": `Bearer ${env.GITHUB_TOKEN}`,
            "User-Agent": "Tunetastic-L10n-Bot",
            "Content-Type": "application/json"
          },
          body: JSON.stringify({
            ref: `refs/heads/${syncBranch}`,
            sha: baseSha
          })
        });
        if (!createBranchRes.ok) {
          return jsonResponse({ error: `Could not create branch ${syncBranch}: ${await createBranchRes.text()}` }, 500, corsHeaders);
        }

        // 3. For each language, update Strings/<lang>/Resources.resw
        let enUsXml = "";
        const enUsRes = await fetch(`https://api.github.com/repos/${owner}/${repo}/contents/Strings/en-US/Resources.resw?ref=${syncBranch}`, {
          headers: { "Authorization": `Bearer ${env.GITHUB_TOKEN}`, "User-Agent": "Tunetastic-L10n-Bot" }
        });
        if (enUsRes.ok) {
          const enData = await enUsRes.json();
          enUsXml = atob(enData.content.replace(/\n/g, ""));
        }

        for (const [lang, translationsMap] of Object.entries(approvedPerLang)) {
          const filePath = `Strings/${lang}/Resources.resw`;
          let existingXml = enUsXml;
          let fileSha = undefined;

          const fileRes = await fetch(`https://api.github.com/repos/${owner}/${repo}/contents/${filePath}?ref=${syncBranch}`, {
            headers: { "Authorization": `Bearer ${env.GITHUB_TOKEN}`, "User-Agent": "Tunetastic-L10n-Bot" }
          });
          if (fileRes.ok) {
            const fileData = await fileRes.json();
            existingXml = atob(fileData.content.replace(/\n/g, ""));
            fileSha = fileData.sha;
          }

          const dict = {};
          for (const [k, obj] of Object.entries(translationsMap)) {
            dict[k] = obj.value;
          }

          const updatedXml = mergeReswXml(existingXml, dict);
          const encodedContent = btoa(unescape(encodeURIComponent(updatedXml)));

          const commitBody = {
            message: `chore(l10n): update ${lang} community translations`,
            content: encodedContent,
            branch: syncBranch
          };
          if (fileSha) commitBody.sha = fileSha;

          await fetch(`https://api.github.com/repos/${owner}/${repo}/contents/${filePath}`, {
            method: "PUT",
            headers: {
              "Authorization": `Bearer ${env.GITHUB_TOKEN}`,
              "User-Agent": "Tunetastic-L10n-Bot",
              "Content-Type": "application/json"
            },
            body: JSON.stringify(commitBody)
          });
        }

        // 4. Create Pull Request
        const contributorsList = Array.from(contributorsSet)
          .map(c => String(c).replace(/^@+/, "").trim())
          .filter(Boolean);

        const prTitle = targetLang
          ? `feat(l10n): sync ${targetLang} translations (${Object.keys(approvedPerLang[targetLang] || {}).length} string(s))`
          : `feat(l10n): sync community translations [${Object.keys(approvedPerLang).join(", ")}]`;

        const prBody =
          `## 🌐 Community Localization Sync\n\n` +
          `This Pull Request imports verified translations approved in the Community Localization Studio.\n\n` +
          `### Languages Updated:\n` +
          `${langSummaries.join("\n")}\n\n` +
          `### Community Contributors:\n` +
          `${contributorsList.length > 0 ? contributorsList.join(", ") : "Anonymous Community Members"}\n\n` +
          `---\n*Created automatically by Tunetastic Localization Studio*`;

        const prRes = await fetch(`https://api.github.com/repos/${owner}/${repo}/pulls`, {
          method: "POST",
          headers: {
            "Authorization": `Bearer ${env.GITHUB_TOKEN}`,
            "User-Agent": "Tunetastic-L10n-Bot",
            "Content-Type": "application/json"
          },
          body: JSON.stringify({
            title: prTitle,
            head: syncBranch,
            base: baseBranch,
            body: prBody
          })
        });

        if (!prRes.ok) {
          return jsonResponse({ error: `Branch was created but PR opening failed: ${await prRes.text()}` }, 500, corsHeaders);
        }

        const prData = await prRes.json();

        // 5. Track which approved change is linked to which PR!
        for (const [lang, translationsMap] of Object.entries(approvedPerLang)) {
          const approvedKey = `approved:${lang}`;
          const currentRaw = await env.SUBMISSIONS.get(approvedKey);
          if (currentRaw) {
            const currentMap = JSON.parse(currentRaw);
            for (const k of Object.keys(translationsMap)) {
              if (currentMap[k]) {
                currentMap[k].prNumber = prData.number;
                currentMap[k].prUrl = prData.html_url;
                currentMap[k].prBranch = syncBranch;
                currentMap[k].prState = "open";
                currentMap[k].syncedAt = new Date().toISOString();
              }
            }
            await env.SUBMISSIONS.put(approvedKey, JSON.stringify(currentMap));
          }
        }

        return jsonResponse({
          success: true,
          pr_url: prData.html_url,
          pr_number: prData.number,
          branch: syncBranch,
          language: targetLang || "all"
        }, 200, corsHeaders);
      }

      // -----------------------------------------------------------------------
      
      // Un-approve / Delete single approved key
      if ((path === "/api/unapprove" || path === "/api/delete-approved") && request.method === "POST") {
        if (!verifyAdminAuth(request, env)) {
          return jsonResponse({ error: "Unauthorized" }, 401, corsHeaders);
        }
        const { language, key } = await request.json();
        if (!language || !key) {
          return jsonResponse({ error: "Missing language or key" }, 400, corsHeaders);
        }
        const approvedKey = `approved:${language}`;
        const raw = await env.SUBMISSIONS.get(approvedKey);
        if (raw) {
          const map = JSON.parse(raw);
          delete map[key];
          if (Object.keys(map).length === 0) {
            await env.SUBMISSIONS.delete(approvedKey);
          } else {
            await env.SUBMISSIONS.put(approvedKey, JSON.stringify(map));
          }
        }
        return jsonResponse({ success: true, key }, 200, corsHeaders);
      }

      // 5. POST-MERGE SAFE CLEANUP (/api/cleanup-synced)
      // -----------------------------------------------------------------------
      if (path === "/api/cleanup-synced" && request.method === "POST") {
        if (!verifyAdminAuth(request, env)) {
          return jsonResponse({ error: "Unauthorized" }, 401, corsHeaders);
        }

        const { verifiedKeys = {} } = await request.json();
        // verifiedKeys: { "es-ES": ["Key1", "Key2"] }
        for (const [lang, keys] of Object.entries(verifiedKeys)) {
          const approvedKey = `approved:${lang}`;
          const existingRaw = await env.SUBMISSIONS.get(approvedKey);
          if (existingRaw) {
            const map = JSON.parse(existingRaw);
            for (const k of keys) delete map[k];
            if (Object.keys(map).length === 0) {
              await env.SUBMISSIONS.delete(approvedKey);
            } else {
              await env.SUBMISSIONS.put(approvedKey, JSON.stringify(map));
            }
          }
        }
        return jsonResponse({ success: true, message: "Safely purged verified synced keys from KV." }, 200, corsHeaders);
      }

      return jsonResponse({ error: "Not found", routes: ["/api/draft", "/api/drafts/resume", "/api/status", "/api/submit", "/api/suggest", "/api/pending", "/api/approve", "/api/sync-pr", "/api/cleanup-synced"] }, 404, corsHeaders);
    } catch (err) {
      return jsonResponse({ error: err.message }, 500, corsHeaders);
    }
  }
};
