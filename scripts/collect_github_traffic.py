#!/usr/bin/env python3
"""Fetch the last 14 days of GitHub traffic and merge it into permanent daily history.

Usage:
  REPO=owner/name GH_TRAFFIC_TOKEN=... python scripts/collect_github_traffic.py
"""
import json
import os
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
PATH = ROOT / "data" / "github_traffic.json"
REPO = os.environ.get("REPO") or sys.exit("REPO is not set")
TOKEN = os.environ.get("GH_TRAFFIC_TOKEN") or sys.exit("GH_TRAFFIC_TOKEN is not set")
API = f"https://api.github.com/repos/{REPO}/traffic"


def get(endpoint):
    url = f"{API}/{endpoint}"
    headers = {
        "Authorization": f"Bearer {TOKEN}",
        "Accept": "application/vnd.github+json",
        "X-GitHub-Api-Version": "2022-11-28",
    }
    for attempt in range(4):
        req = urllib.request.Request(url, headers=headers)
        try:
            with urllib.request.urlopen(req, timeout=60) as r:
                return json.load(r)
        except urllib.error.HTTPError as e:
            if e.code in (429, 500, 502, 503, 504) and attempt < 3:
                time.sleep(30 * (attempt + 1))
                continue
            raise SystemExit(f"GitHub {endpoint} failed: HTTP {e.code}")
        except urllib.error.URLError as e:
            if attempt < 3:
                time.sleep(15)
                continue
            raise SystemExit(f"Network error: {e.reason}")


def blank():
    return {"views": 0, "unique_visitors": 0, "clones": 0, "unique_cloners": 0}


def main():
    history = json.loads(PATH.read_text()) if PATH.exists() else {}

    for v in get("views")["views"]:
        day = v["timestamp"][:10]
        history.setdefault(day, blank())
        history[day]["views"] = v["count"]
        history[day]["unique_visitors"] = v["uniques"]

    for c in get("clones")["clones"]:
        day = c["timestamp"][:10]
        history.setdefault(day, blank())
        history[day]["clones"] = c["count"]
        history[day]["unique_cloners"] = c["uniques"]

    PATH.parent.mkdir(parents=True, exist_ok=True)
    PATH.write_text(json.dumps(dict(sorted(history.items())), indent=1) + "\n")
    print(f"{len(history)} days stored in {PATH.relative_to(ROOT)}")


if __name__ == "__main__":
    main()