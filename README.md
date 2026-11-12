# 🌐 Tunetastic Community Localization Studio

Welcome to the **Tunetastic Community Localization Studio** (`gh-pages` branch).

This repository powers the live web portal hosted at [**amit-kp.github.io/Tunetastic**](https://amit-kp.github.io/Tunetastic/), enabling music lovers and volunteers worldwide to translate [Tunetastic](https://github.com/AMit-KP/Tunetastic) into their native languages without needing Git, forks, or technical software localization knowledge.

---

* **No Sign-Up Needed**: Contributors translate directly in their web browser. No GitHub account or third-party login is required.
* **48-Hour Sliding Drafts**: Active drafts are automatically saved to Cloudflare KV and extended as you type. If you close your browser or step away, you can resume right where you left off.
* **Tab & Incognito Isolation**: Drafts are safely isolated across browser tabs using session storage, while Incognito mode operates in a zero-network private sandbox.
---

## 🔄 Translation Lifecycle & Statuses

Each string in the localization workspace carries a real-time status badge matching the filter tabs:

| Status | UI Badge | Description |
| :--- | :--- | :--- |
| **Need Translation** | `Need Translation` | Untranslated string waiting for initial community translation. |
| **Drafted By You** | `Drafted By You` | Unsubmitted draft saved in your browser session (backed by 48-hour cloud autosave). |
| **In Progress By Others** | `In Progress By Others` | Currently being drafted by another contributor in an active editing session. |
| **Submitted** | `Submitted` | Community proposal(s) submitted to the server, awaiting admin review in `#admin`. |
| **Approved** | `Approved` | Reviewed and approved by the admin; staged on the server ready for Git PR sync. |
| **Already Translated** | `Already Translated` | Official translation merged into Tunetastic's `.resw` resource dictionary. |


> [!TIP]
> **Refining Existing Strings**: Community members can click **Suggest Alternative** on strings with to propose improved phrasings. Admin will receive a visual diff showing what was before and what was suggested.

---

## 🤝 Contributing

We welcome translations in all languages!
1. Visit [**amit-kp.github.io/Tunetastic**](https://amit-kp.github.io/Tunetastic/).
2. Select your native language.
3. Translate strings that need translation or suggest alternative improvements.
4. Click **Submit Work** and enter your name or handle to receive official credit.

