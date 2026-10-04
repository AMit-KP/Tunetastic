#!/usr/bin/env python3
"""Fetch Microsoft Store stats, merge into permanent history, and redraw the README graphics.

Usage:
  python scripts/update_stats.py            # fetch + merge + render (needs PC_* secrets)
  python scripts/update_stats.py --render   # only redraw from data/ (no network)
"""
import datetime as dt
import json
import os
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
import render  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
DATA, OUT = ROOT / "data", ROOT / "charts"
HOST = "https://manage.devcenter.microsoft.com"
API = HOST + "/v1.0/my/analytics"


# ---------- helpers ----------
def load(path, default):
    return json.loads(path.read_text()) if path.exists() else default


def save(path, obj):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(obj, indent=1, sort_keys=True) + "\n")


def monday(d):
    return d - dt.timedelta(days=d.weekday())


def parse_day(s):
    return dt.date.fromisoformat(str(s)[:10])


def us_date(d):
    return f"{d.month}/{d.day}/{d.year}"


def http(method, url, token=None, form=None):
    headers, data = {}, None
    if form is not None:
        data = urllib.parse.urlencode(form).encode()
    if token:
        headers["Authorization"] = "Bearer " + token
    for attempt in range(4):
        req = urllib.request.Request(url, data=data, headers=headers, method=method)
        try:
            with urllib.request.urlopen(req, timeout=120) as r:
                return json.load(r)
        except urllib.error.HTTPError as e:
            if e.code in (429, 500, 502, 503, 504) and attempt < 3:
                time.sleep(30 * (attempt + 1))
                continue
            raise SystemExit(f"Request failed: HTTP {e.code} for {url.split('?')[0]}")
        except urllib.error.URLError as e:
            if attempt < 3:
                time.sleep(15)
                continue
            raise SystemExit(f"Network error: {e.reason}")


def get_token():
    tenant = os.environ["PC_TENANT_ID"]
    res = http("POST", f"https://login.microsoftonline.com/{tenant}/oauth2/token", form={
        "grant_type": "client_credentials",
        "client_id": os.environ["PC_CLIENT_ID"],
        "client_secret": os.environ["PC_CLIENT_SECRET"],
        "resource": HOST,
    })
    return res["access_token"]


def fetch_rows(token, endpoint, params):
    q = urllib.parse.urlencode(params, safe="/,", quote_via=urllib.parse.quote)
    url, rows = f"{API}/{endpoint}?{q}", []
    while url:
        res = http("GET", url, token)
        rows += res.get("Value", res.get("value", [])) or []
        nxt = res.get("@nextLink")
        if nxt and not nxt.startswith("http"):
            nxt = HOST + (nxt if nxt.startswith("/") else "/v1.0/my/analytics/" + nxt)
        url = nxt
    return rows


# ---------- fetch + merge ----------
def update_history(today, installs, ratings, cfg):
    last_complete = monday(today) - dt.timedelta(days=7)   # only whole weeks are plotted
    first_run = not installs
    start = today - dt.timedelta(days=360) if first_run else monday(today) - dt.timedelta(weeks=8)
    params = {"applicationId": cfg["store_id"], "startDate": us_date(start), "endDate": us_date(today),
              "aggregationLevel": "week", "top": 10000}
    token = get_token()

    # installs per week per country
    got = {}
    for r in fetch_rows(token, "installs", {**params, "groupby": "date,market"}):
        wk = monday(parse_day(r["date"]))
        mk = r.get("market") or "Unknown"
        got.setdefault(wk, {})
        got[wk][mk] = got[wk].get(mk, 0) + int(r.get("successfulInstallCount") or 0)

    # ratings per week (counts of 1..5 stars)
    rgot = {}
    for r in fetch_rows(token, "ratings", params):
        wk = monday(parse_day(r["date"]))
        cur = rgot.setdefault(wk, [0, 0, 0, 0, 0])
        for i, key in enumerate(["oneStar", "twoStars", "threeStars", "fourStars", "fiveStars"]):
            cur[i] += int(r.get(key) or 0)

    wk = monday(start)
    while wk <= last_complete:
        key = wk.isoformat()
        if wk in got and sum(got[wk].values()) > 0:
            installs[key] = got[wk]
        else:
            installs.pop(key, None)
        if wk in rgot and sum(rgot[wk]) > 0:
            ratings[key] = rgot[wk]
        else:
            ratings.pop(key, None)
        wk += dt.timedelta(weeks=1)
    print(f"Fetched {sum(len(v) for v in got.values())} install rows, {len(rgot)} rating weeks")


# ---------- render ----------
def render_all(today, installs, ratings, cfg):
    if not installs:
        raise SystemExit("No install data yet - nothing to draw.")
    world = json.loads((ROOT / "scripts" / "world.json").read_text())
    last_complete = monday(today) - dt.timedelta(days=7)
    keys = sorted(parse_day(k) for k in list(installs) + list(ratings))
    first = keys[0]
    weeks, wk = [], first
    while wk <= last_complete:
        weeks.append(wk)
        wk += dt.timedelta(weeks=1)
    vals = [sum(installs.get(w.isoformat(), {}).values()) for w in weeks]
    total, last3 = sum(vals), sum(vals[-13:])

    by_country = {}
    for wkv in installs.values():
        for mk, v in wkv.items():
            by_country[mk] = by_country.get(mk, 0) + v
    rows = []
    for mk, v in sorted(by_country.items(), key=lambda kv: (-kv[1], kv[0])):
        if mk == "Unknown":
            continue
        rows.append((mk if mk in world["c"] else None, world["c"][mk][0] if mk in world["c"] else mk, v))
    if by_country.get("Unknown"):
        rows.append((None, "Location not reported", by_country["Unknown"]))

    events = [(parse_day(k), v) for k, v in sorted(ratings.items())]
    updates = []
    for u in cfg.get("updates", []):
        updates.append((parse_day(u["date"]), u.get("label", "")))
    stamp = f"Updated {today.day} {today:%b %Y}"

    OUT.mkdir(exist_ok=True)
    for theme, c in render.THEMES.items():
        (OUT / f"installs-{theme}.svg").write_text(render.installs_card(c, weeks, vals, total, last3, updates, stamp))
        (OUT / f"map-{theme}.svg").write_text(render.map_card(c, world, rows, total, stamp))
        (OUT / f"ratings-{theme}.svg").write_text(render.ratings_card(c, weeks, events, stamp))
    print(f"Drew graphics: {total} installs, {len(rows)} locations, {sum(sum(v) for v in ratings.values())} ratings")


def main():
    today = dt.datetime.now(dt.timezone.utc).date()
    cfg = load(DATA / "config.json", {})
    if os.environ.get("STORE_ID"):
        cfg["store_id"] = os.environ["STORE_ID"]
    installs = load(DATA / "installs.json", {})
    ratings = load(DATA / "ratings.json", {})
    if "--render" not in sys.argv:
        if not cfg.get("store_id"):
            raise SystemExit("Missing store_id in data/config.json")
        update_history(today, installs, ratings, cfg)
        save(DATA / "installs.json", installs)
        save(DATA / "ratings.json", ratings)
    render_all(today, installs, ratings, cfg)


if __name__ == "__main__":
    main()
