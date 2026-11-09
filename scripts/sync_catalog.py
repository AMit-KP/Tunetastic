#!/usr/bin/env python3
"""
Tunetastic Localization Studio Catalog Synchronizer (gh-pages)
Scans the C# application repository's Strings/ folder, dynamically detects added/removed
languages and strings (skipping qps-ploc), updates data/catalog.json, and optionally
pings Cloudflare KV to safely clean up verified synced keys.
"""

import os
import re
import sys
import json
import argparse
import urllib.request
import xml.etree.ElementTree as ET
from pathlib import Path

def parse_resw(file_path: Path) -> dict:
    if not file_path.exists():
        return {}
    try:
        tree = ET.parse(file_path)
        root = tree.getroot()
        res = {}
        for data in root.findall("data"):
            name = data.get("name")
            val = data.find("value")
            comment = data.find("comment")
            res[name] = {
                "value": val.text if val is not None and val.text is not None else "",
                "comment": comment.text if comment is not None and comment.text is not None else ""
            }
        return res
    except Exception as e:
        print(f"Warning: could not parse {file_path}: {e}")
        return {}

def resolve_placeholder_heuristics(key: str, text: str, comment: str, token: str) -> dict:
    combined = f"{key} {text} {comment}".lower()
    if "singular" in combined:
        return {"label": "Count (Singular)", "sample": "1"}
    if any(w in combined for w in ["plural", "count", "showing", "out of", "files", "songs", "tracks"]):
        return {"label": "Count", "sample": "12"} if token == "{0}" else {"label": "Total Count", "sample": "100"}
    if "artist" in combined:
        return {"label": "Artist Name", "sample": "Queen"}
    if "album" in combined:
        return {"label": "Album Title", "sample": "Abbey Road"}
    if "playlist" in combined:
        return {"label": "Playlist Name", "sample": "Rock Classics"}
    if any(w in combined for w in ["song", "track", "title"]):
        return {"label": "Song Title", "sample": "Bohemian Rhapsody"}
    if any(w in combined for w in ["path", "file", "folder", "directory"]):
        return {"label": "File Path", "sample": "D:\\Music\\Rock\\Track.flac"}
    if any(w in combined for w in ["hour", "min", "sec", "duration", "time", "ago"]):
        return {"label": "Duration / Time", "sample": "5"}
    if any(w in combined for w in ["key", "shortcut", "press"]):
        return {"label": "Key Shortcut", "sample": "Ctrl+F"}
    if "year" in combined:
        return {"label": "Year", "sample": "1994"}
    if "genre" in combined:
        return {"label": "Genre", "sample": "Rock"}

    samples = {"{0}": ("Value 1", "10"), "{1}": ("Value 2", "100"), "{2}": ("Value 3", "Favorites")}
    label, sample = samples.get(token, ("Parameter", "Sample"))
    return {"label": label, "sample": sample}

def sync_catalog(csharp_repo_root: Path, output_catalog_path: Path, placeholders_path: Path):
    strings_dir = csharp_repo_root / "Strings"
    en_us_path = strings_dir / "en-US" / "Resources.resw"

    if not en_us_path.exists():
        print(f"Error: {en_us_path} not found.")
        sys.exit(1)

    overrides = {}
    if placeholders_path.exists():
        with open(placeholders_path, "r", encoding="utf-8") as f:
            overrides = json.load(f)

    master_strings = parse_resw(en_us_path)
    print(f"Loaded {len(master_strings)} master strings from en-US.")

    # Discovered Languages (Strictly skipping qps-ploc)
    available_languages = {}
    verified_translations = {}

    for lang_dir in strings_dir.iterdir():
        if lang_dir.is_dir() and (lang_dir / "Resources.resw").exists():
            code = lang_dir.name
            if code == "en-US" or code == "qps-ploc" or "ploc" in code.lower():
                continue

            lang_dict = parse_resw(lang_dir / "Resources.resw")
            translated_keys = [k for k, v in lang_dict.items() if v.get("value") and v.get("value").strip()]
            pct = round((len(translated_keys) / len(master_strings)) * 100) if master_strings else 0

            available_languages[code] = {
                "code": code,
                "translatedCount": len(translated_keys),
                "totalCount": len(master_strings),
                "progressPct": pct,
                "translations": {k: v["value"] for k, v in lang_dict.items() if v.get("value") and v.get("value").strip()}
            }
            verified_translations[code] = translated_keys

    # Ensure standard community languages are present in the catalog even before first merge
    default_locales = ["es-ES", "de-DE", "fr-FR", "hi-IN", "ja-JP", "ru-RU", "pt-BR", "it-IT", "zh-CN", "ar-SA"]
    for d_code in default_locales:
        if d_code not in available_languages:
            available_languages[d_code] = {
                "code": d_code,
                "translatedCount": 0,
                "totalCount": len(master_strings),
                "progressPct": 0,
                "translations": {}
            }

    # Build Items
    token_pattern = re.compile(r"\{(\d+)\}")
    items = []

    for key, data in master_strings.items():
        text = data["value"]
        comment = data["comment"]
        raw_tokens = sorted(list(set(token_pattern.findall(text))), key=lambda x: int(x))
        placeholders = []
        key_overrides = overrides.get(key, {})

        for t in raw_tokens:
            token_str = f"{{{t}}}"
            if token_str in key_overrides:
                placeholders.append({
                    "token": token_str,
                    "label": key_overrides[token_str].get("label", f"Token {t}"),
                    "sample": key_overrides[token_str].get("sample", "Sample")
                })
            else:
                h = resolve_placeholder_heuristics(key, text, comment, token_str)
                placeholders.append({
                    "token": token_str,
                    "label": h["label"],
                    "sample": h["sample"]
                })

        items.append({
            "key": key,
            "source": text,
            "comment": comment,
            "placeholders": placeholders
        })

    catalog = {
        "version": "2.0",
        "totalStrings": len(items),
        "languages": available_languages,
        "items": items
    }

    output_catalog_path.parent.mkdir(parents=True, exist_ok=True)
    with open(output_catalog_path, "w", encoding="utf-8") as f:
        json.dump(catalog, f, ensure_ascii=False, indent=2)

    print(f"Successfully generated catalog at {output_catalog_path} with {len(items)} items and {len(available_languages)} languages.")

    # Cleanup Cloudflare KV if environment secrets are available
    worker_url = os.environ.get("CLOUDFLARE_WORKER_URL")
    admin_auth = os.environ.get("CLOUDFLARE_ADMIN_AUTH")

    if worker_url and admin_auth and verified_translations:
        try:
            req_data = json.dumps({"verifiedKeys": verified_translations}).encode("utf-8")
            req = urllib.request.Request(
                f"{worker_url}/api/cleanup-synced",
                data=req_data,
                headers={
                    "Content-Type": "application/json",
                    "Authorization": f"Bearer {admin_auth}"
                },
                method="POST"
            )
            with urllib.request.urlopen(req) as resp:
                print(f"Purged verified translations from Cloudflare staging KV (HTTP {resp.status}).")
        except Exception as e:
            print(f"Notice: Could not trigger Cloudflare cleanup: {e}")

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Tunetastic Localization Catalog Synchronizer")
    parser.add_argument("--repo", "-r", required=True, help="Path to C# application repo root")
    parser.add_argument("--out", "-o", required=True, help="Path to output catalog.json")
    parser.add_argument("--placeholders", "-p", default=None, help="Path to placeholders.json")
    args = parser.parse_args()

    csharp_root = Path(args.repo).resolve()
    out_path = Path(args.out).resolve()
    ph_path = Path(args.placeholders).resolve() if args.placeholders else out_path.parent / "placeholders.json"

    sync_catalog(csharp_root, out_path, ph_path)
