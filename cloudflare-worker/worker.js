/**
 * Tunetastic Community Localization Staging Worker
 * Handles silent staging of community translations in Cloudflare KV,
 * posts notification comments to the single GitHub tracker issue,
 * and serves the Admin Review Queue & GitHub Action sync endpoints.
 */

const ALLOWED_ORIGINS = [
  "https://amit-kp.github.io",
  "http://localhost",
  "http://127.0.0.1"
];

function getCorsHeaders(request) {
  const origin = request.headers.get("Origin") || "";
  const isAllowed = ALLOWED_ORIGINS.some(allowed => origin.toLowerCase().startsWith(allowed));
  return {
    "Access-Control-Allow-Origin": isAllowed ? origin : "https://amit-kp.github.io",
    "Access-Control-Allow-Methods": "GET, POST, OPTIONS",
    "Access-Control-Allow-Headers": "Content-Type, Authorization"
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
    return false; // Strictly refuse if secret is missing or empty
  }
  const auth = request.headers.get("Authorization");
  return auth === `Bearer ${env.ADMIN_PASSWORD_HASH}`;
}

export default {
  async fetch(request, env) {
    const corsHeaders = getCorsHeaders(request);

    if (request.method === "OPTIONS") {
      return new Response(null, { headers: corsHeaders });
    }

    const url = new URL(request.url);
    const path = url.pathname;

    try {
      // 1. Contributor Submits Translations (Silent Staging)
      if (path === "/api/submit" && request.method === "POST") {
        const body = await request.json();
        const { language, contributor = "Anonymous", translations = {} } = body;

        // Security Validation
        if (!language || typeof language !== "string" || !/^[a-zA-Z]{2,4}(-[a-zA-Z0-9]{2,8})*$/.test(language)) {
          return jsonResponse({ error: "Invalid language code format." }, 400, corsHeaders);
        }

        const keys = Object.keys(translations);
        if (keys.length === 0) {
          return jsonResponse({ error: "Translations cannot be empty." }, 400, corsHeaders);
        }
        if (keys.length > 1000) {
          return jsonResponse({ error: "Submission exceeds maximum payload limit (1000 keys)." }, 400, corsHeaders);
        }

        // Sanitize contributor string (max 50 chars, alphanumeric + dashes/underscores)
        const sanitizedContributor = String(contributor).slice(0, 50).replace(/[^a-zA-Z0-9_\- ]/g, "").trim() || "Anonymous";

        // Sanitize each translation entry (max 5,000 characters per string)
        const sanitizedTranslations = {};
        for (const [k, v] of Object.entries(translations)) {
          if (typeof k === "string" && k.length <= 200 && typeof v === "string") {
            sanitizedTranslations[k] = v.slice(0, 5000);
          }
        }

        const id = `pending:${language}:${sanitizedContributor.replace(/\s+/g, "_")}:${Date.now()}`;
        const record = {
          id,
          language,
          contributor: sanitizedContributor,
          translations: sanitizedTranslations,
          timestamp: new Date().toISOString()
        };

        if (env.SUBMISSIONS) {
          await env.SUBMISSIONS.put(id, JSON.stringify(record));
        }

        // Send Bot Comment to Single GitHub Tracker Issue
        if (env.GITHUB_TOKEN && env.TRACKER_ISSUE_NUMBER) {
          try {
            const commentBody = 
              `🌐 **New Community Translation Submitted!**\n\n` +
              `- **Language:** \`${language}\`\n` +
              `- **Contributor:** ${sanitizedContributor}\n` +
              `- **Strings Translated:** ${keys.length} string(s)\n` +
              `- **Timestamp:** ${new Date().toUTCString()}\n\n` +
              `👉 [Open Review Queue in Portal](https://${env.GITHUB_OWNER || "AMit-KP"}.github.io/${env.GITHUB_REPO || "Tunetastic"}/#admin)`;

            await fetch(`https://api.github.com/repos/${env.GITHUB_OWNER || "AMit-KP"}/${env.GITHUB_REPO || "Tunetastic"}/issues/${env.TRACKER_ISSUE_NUMBER}/comments`, {
              method: "POST",
              headers: {
                "Authorization": `Bearer ${env.GITHUB_TOKEN}`,
                "User-Agent": "Tunetastic-L10n-Bot",
                "Content-Type": "application/json"
              },
              body: JSON.stringify({ body: commentBody })
            });
          } catch (botErr) {
            console.error("Failed to post GitHub tracker comment:", botErr);
          }
        }

        return jsonResponse({ success: true, count: Object.keys(sanitizedTranslations).length, id }, 200, corsHeaders);
      }

      // 2. Admin Review Mode: Fetch Pending Submissions
      if (path === "/api/pending" && request.method === "GET") {
        if (!verifyAdminAuth(request, env)) {
          return jsonResponse({ error: "Unauthorized" }, 401, corsHeaders);
        }

        if (!env.SUBMISSIONS) {
          return jsonResponse({ submissions: [] }, 200, corsHeaders);
        }
        const list = await env.SUBMISSIONS.list({ prefix: "pending:" });
        const submissions = [];
        for (const key of list.keys) {
          const raw = await env.SUBMISSIONS.get(key.name);
          if (raw) submissions.push(JSON.parse(raw));
        }
        return jsonResponse({ submissions }, 200, corsHeaders);
      }

      // 3. Admin Review Mode: Approve / Edit Translations
      if (path === "/api/approve" && request.method === "POST") {
        if (!verifyAdminAuth(request, env)) {
          return jsonResponse({ error: "Unauthorized" }, 401, corsHeaders);
        }

        const body = await request.json();
        const { language, approvedTranslations = {}, removeSubmissionIds = [] } = body;

        if (env.SUBMISSIONS) {
          // Merge approved translations under approved key
          const approvedKey = `approved:${language}`;
          const existingRaw = await env.SUBMISSIONS.get(approvedKey);
          const existing = existingRaw ? JSON.parse(existingRaw) : {};
          const merged = { ...existing, ...approvedTranslations };
          await env.SUBMISSIONS.put(approvedKey, JSON.stringify(merged));

          // Clean up processed pending submissions
          for (const id of removeSubmissionIds) {
            await env.SUBMISSIONS.delete(id);
          }
        }

        return jsonResponse({ success: true, count: Object.keys(approvedTranslations).length }, 200, corsHeaders);
      }

      // 4. GitHub Action: Fetch Approved Translations for Sync
      if (path === "/api/approved" && request.method === "GET") {
        if (!verifyAdminAuth(request, env)) {
          return jsonResponse({ error: "Unauthorized" }, 401, corsHeaders);
        }

        if (!env.SUBMISSIONS) {
          return jsonResponse({ approved: {} }, 200, corsHeaders);
        }

        const list = await env.SUBMISSIONS.list({ prefix: "approved:" });
        const approved = {};
        for (const key of list.keys) {
          const lang = key.name.replace("approved:", "");
          const raw = await env.SUBMISSIONS.get(key.name);
          if (raw) {
            approved[lang] = JSON.parse(raw);
          }
        }
        return jsonResponse({ approved }, 200, corsHeaders);
      }

      // 5. GitHub Action: Clear Processed Approved Translations
      if (path === "/api/clear-approved" && request.method === "POST") {
        if (!verifyAdminAuth(request, env)) {
          return jsonResponse({ error: "Unauthorized" }, 401, corsHeaders);
        }

        const { languages = [] } = await request.json();
        if (env.SUBMISSIONS) {
          for (const lang of languages) {
            await env.SUBMISSIONS.delete(`approved:${lang}`);
          }
        }
        return jsonResponse({ success: true }, 200, corsHeaders);
      }

      return jsonResponse({ error: "Not found", routes: ["/api/submit", "/api/pending", "/api/approve", "/api/approved"] }, 404, corsHeaders);
    } catch (err) {
      return jsonResponse({ error: err.message }, 500, corsHeaders);
    }
  }
};


