"""Builds the README graphics (light + dark SVGs) from stored data."""
import datetime as dt
import math
from xml.sax.saxutils import escape

FONT = "'Segoe UI', system-ui, -apple-system, Helvetica, Arial, sans-serif"
THEMES = {
    "dark": dict(card="#111826", bd="#263042", tx="#e6edf3", mu="#8b949e", a="#38bdf8", b="#a78bfa",
                 dot="#3b5268", grid="#46566e", empty="#263042"),
    "light": dict(card="#f6f8fa", bd="#d0d7de", tx="#1f2328", mu="#59636e", a="#0284c7", b="#7c3aed",
                  dot="#6f8199", grid="#95a3b5", empty="#d0d7de"),
}
W, PAD = 880, 24
IW = W - 2 * PAD
STAR = "M10 1l2.6 5.6 6.1.7-4.5 4.2 1.2 6L10 14.4 4.6 17.5l1.2-6L1.3 7.3l6.1-.7z"

# Shared layout for line graphs (all-time + one panel per year)
PL, PR = 58, W - PAD
PANEL_H = 244      # vertical distance between year panels
SWEEP = 8          # seconds the glow wave takes to cross one graph (same speed as before)


def fmt_date(d):
    return f"{d.day} {d:%b %Y}"


def smooth(pts):
    d = "M%.1f,%.1f" % pts[0]
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        mx = (x0 + x1) / 2
        d += "C%.1f,%.1f %.1f,%.1f %.1f,%.1f" % (mx, y0, mx, y1, x1, y1)
    return d


def axis(maxv):
    maxv = max(maxv, 1)
    s = 1
    for s in (1, 2, 5, 10, 20, 25, 50, 100, 200, 250, 500, 1000, 2000, 2500, 5000, 10000):
        if math.ceil(maxv / s) <= 4:
            break
    top = math.ceil(maxv / s) * s
    if top - maxv < 0.08 * top:
        top += s
    return s, top


def card(c, h, title, hint, body, stamp, extra_defs="", extra_css=""):
    css = (
        "@keyframes draw{from{stroke-dasharray:1;stroke-dashoffset:1}to{stroke-dasharray:1;stroke-dashoffset:0}}"
        "@keyframes fade{from{opacity:0}}"
        ".ln,.gw{animation:draw 2.2s ease-out .2s backwards}"
        ".fd{animation:fade 1.4s ease 1.2s backwards}"
        + extra_css +
        "@media (prefers-reduced-motion:reduce){*{animation:none!important}}"
    )
    defs = (
        f'<linearGradient id="lg" x1="0" x2="1"><stop offset="0" stop-color="{c["a"]}"/><stop offset="1" stop-color="{c["b"]}"/></linearGradient>'
        f'<linearGradient id="ag" x1="0" x2="0" y1="0" y2="1"><stop offset="0" stop-color="{c["a"]}" stop-opacity=".4"/><stop offset="1" stop-color="{c["b"]}" stop-opacity="0"/></linearGradient>'
        '<filter id="gw" x="-5%" y="-30%" width="110%" height="160%"><feGaussianBlur stdDeviation="4"/></filter>'
        + extra_defs
    )
    return (
        f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {W} {h}" width="{W}" height="{h}" role="img" '
        f'aria-label="{escape(title)}" font-family="{FONT}">\n<title>{escape(title)}</title>\n'
        f"<style>{css}</style>\n<defs>{defs}</defs>\n"
        f'<rect x=".5" y=".5" width="{W-1}" height="{h-1}" rx="14" fill="{c["card"]}" stroke="{c["bd"]}"/>\n'
        f'<text x="{PAD}" y="38" font-size="18" font-weight="700" fill="{c["tx"]}">{escape(title)}</text>\n'
        f'<text x="{PAD}" y="59" font-size="13" fill="{c["mu"]}">{escape(hint)}</text>\n'
        f'<text x="{W-PAD}" y="38" font-size="12" fill="{c["mu"]}" text-anchor="end">{escape(stamp)}</text>\n'
        f"{body}\n</svg>\n"
    )


def grid_and_months(c, weeks, X, PL, PR, PT, PB, step, top, label_y, years=None):
    """years: True/False forces the year suffix on month labels; None = decide from span."""
    out = []
    Y = lambda v: PB - (PB - PT) * v / top
    for v in range(0, top + 1, step):
        out.append(f'<line x1="{PL}" x2="{PR}" y1="{Y(v):.1f}" y2="{Y(v):.1f}" stroke="{c["grid"]}" stroke-width="1.2" stroke-dasharray="3 4"/>')
        out.append(f'<text x="{PL-8}" y="{Y(v)+4:.1f}" font-size="11" fill="{c["mu"]}" text-anchor="end">{v}</text>')
    prev, lastx = None, -99
    long_span = (weeks[-1] - weeks[0]).days > 300 if years is None else years
    for i, w in enumerate(weeks):
        if w.month != prev:
            prev = w.month
            x = X(i)
            if x - lastx >= 34:
                lab = f"{w:%b}" + (f" {w:%y}" if long_span and (w.month == 1 or i == 0) else "")
                out.append(f'<text x="{x:.1f}" y="{label_y}" font-size="11" fill="{c["mu"]}" text-anchor="middle">{lab}</text>')
                lastx = x
    return "".join(out), Y


# ---------------------------------------------------------------- shared line-graph helpers

def wave_def(c, k, total):
    """Glow wave for graph number k of `total`. Graphs take turns, top to bottom, then loop."""
    S, E = "-20 0", "825 0"  # wave starts just left of the line and ends just past its right edge
    kt, v = [0.0], [S]
    if k > 0:
        kt.append(k / total); v.append(S)
    kt.append((k + 1) / total); v.append(E)
    if k < total - 1:
        kt.append(1.0); v.append(E)
    return (f'<linearGradient id="wv{k}" gradientUnits="userSpaceOnUse" x1="0" y1="0" x2="{W}" y2="0" gradientTransform="translate(-20,0)">'
            '<stop offset="0" stop-color="#fff" stop-opacity="0"/><stop offset=".04" stop-color="#fff" stop-opacity="0"/>'
            '<stop offset=".08" stop-color="#fff" stop-opacity="1"/><stop offset=".086" stop-color="#fff" stop-opacity="0"/>'
            '<stop offset="1" stop-color="#fff" stop-opacity="0"/>'
            f'<animateTransform attributeName="gradientTransform" type="translate" values="{";".join(v)}" '
            f'keyTimes="{";".join(f"{t:.5f}" for t in kt)}" dur="{total * SWEEP}s" begin="2.4s" repeatCount="indefinite"/>'
            '</linearGradient>')


def flags_svg(c, weeks, updates, X, PT, PB, idx_max):
    """Release flags. A flag that would run past the right edge is flipped to face left."""
    n = len(weeks)
    merged = []
    for d, label in updates:  # versions released in the same week share one flag box
        if merged and merged[-1][0] == d:
            merged[-1][1] += " · " + label
        else:
            merged.append([d, label])
    out, edge = "", []
    for d, label in merged:
        idx = (d - weeks[0]).days / 7
        if idx < 0 or idx > idx_max:
            continue
        x = X(min(idx, n - 1))
        label = label or f"{d.day} {d:%b}"
        wd = len(label) * 6.3 + 12

        flip = x - 1 + wd > W - 12
        if flip:
            left, right = x - wd + 1, x + 1
            tx, anchor = x - 5, "end"
        else:
            left, right = x - 1, x - 1 + wd
            tx, anchor = x + 5, "start"

        row = 0
        while row < len(edge) and left <= edge[row] + 6:
            row += 1
        if row == len(edge):
            edge.append(right)
        else:
            edge[row] = right
        row = min(row, 3)
        y = PT - 36 + row * 17
        out += (f'<line x1="{x:.1f}" x2="{x:.1f}" y1="{y+14}" y2="{PB}" stroke="{c["b"]}" stroke-opacity=".5"/>'
                f'<rect x="{left:.1f}" y="{y}" width="{wd:.0f}" height="15" rx="4" fill="{c["b"]}" fill-opacity=".2"/>'
                f'<text x="{tx:.1f}" y="{y+11}" font-size="10" font-weight="600" fill="{c["b"]}" text-anchor="{anchor}">{escape(label)}</text>')
    return out


def line_panel(c, k, total, dates, vals, PT, PB, label_y, step, top, flags=None, years=None, delay=0.0):
    """One line graph (grid, months, flags, line, glow, wave). Returns (svg, defs)."""
    n = len(dates)
    X = lambda i: PL + (i * (PR - PL) / (n - 1) if n > 1 else (PR - PL) / 2)
    g, Y = grid_and_months(c, dates, X, PL, PR, PT, PB, step, top, label_y, years)
    pts = [(X(i), Y(v)) for i, v in enumerate(vals)]
    if n == 1:
        pts = [(PL, Y(vals[0])), (PR, Y(vals[0]))]
    line = smooth(pts)
    area = line + f"L{pts[-1][0]:.1f},{Y(0):.1f}L{pts[0][0]:.1f},{Y(0):.1f}Z"
    fl = flags(X) if flags else ""
    dl = lambda t: f' style="animation-delay:{delay + t:.2f}s"'
    body = (g + fl +
            f'<path class="fd" d="{area}" fill="url(#ag)"{dl(1.2)}/>'
            f'<path class="gw" d="{line}" pathLength="1" fill="none" stroke="url(#lg)" stroke-width="7" stroke-opacity=".45" filter="url(#gw)"{dl(.2)}/>'
            f'<path class="ln" d="{line}" pathLength="1" fill="none" stroke="url(#lg)" stroke-width="2.5" stroke-linejoin="round"{dl(.2)}/>'
            f'<path class="lwv" d="{line}" fill="none" stroke="url(#wv{k})" stroke-width="5.5" stroke-linecap="round" filter="url(#gw)"/>'
            f'<path class="lwv" d="{line}" fill="none" stroke="url(#wv{k})" stroke-width="2" stroke-linecap="round"/>'
            f'<circle class="fd" cx="{pts[-1][0]:.1f}" cy="{pts[-1][1]:.1f}" r="5" fill="{c["card"]}" stroke="{c["a"]}" stroke-width="2"{dl(1.2)}/>')
    return body, wave_def(c, k, total)


def year_slices(dates, vals):
    """[(year, dates, vals)] newest first. A year with fewer than 2 points is skipped."""
    by = {}
    for d, v in zip(dates, vals):
        by.setdefault(d.year, ([], []))
        by[d.year][0].append(d)
        by[d.year][1].append(v)
    return [(y, by[y][0], by[y][1]) for y in sorted(by, reverse=True) if len(by[y][0]) >= 2]


def year_panels(c, slices, first_y0, step, top, updates=None):
    """One graph per year, stacked newest first. The glow wave relays down them in order.
    Returns (svg, defs, height)."""
    total = len(slices)
    body, defs = "", ""
    h = first_y0
    for j, (yr, yd, yv) in enumerate(slices):
        y0 = first_y0 + j * PANEL_H
        PT = y0 + 58
        PB = PT + 150
        label_y = y0 + 224
        n = len(yd)
        fl = (lambda X, yd=yd, PT=PT, PB=PB, n=n: flags_svg(c, yd, updates, X, PT, PB, n - 1e-9)) if updates else None
        bd, df = line_panel(c, j, total, yd, yv, PT, PB, label_y, step, top,
                            flags=fl, years=False, delay=0.6 * j)
        if j > 0:
            body += f'<line x1="{PAD}" x2="{W-PAD}" y1="{y0-16}" y2="{y0-16}" stroke="{c["bd"]}"/>'
        body += f'<text x="{PAD}" y="{y0+13}" font-size="14" font-weight="700" fill="{c["tx"]}">{yr}</text>' + bd
        defs += df
        h = y0 + 238
    return body, defs, h


# ---------------------------------------------------------------- cards

FIRST_Y0 = 160  # top of the first year graph, just below the summary boxes


def installs_card(c, weeks, vals, total, last3, updates, stamp):
    step, top = axis(max(vals))  # one scale shared by every year graph
    # fallback: if no year has 2+ points yet, draw everything as a single graph
    slices = year_slices(weeks, vals) or [(weeks[0].year, weeks, vals)]

    boxes = ""
    for i, (num, lab) in enumerate([(total, "Total installs"), (last3, "Last 3 months")]):
        x = PAD + i * 216
        boxes += (f'<rect x="{x}" y="78" width="204" height="70" rx="10" fill="none" stroke="{c["bd"]}"/>'
                  f'<text x="{x+16}" y="115" font-size="30" font-weight="700" fill="url(#lg)">{num:,}</text>'
                  f'<text x="{x+16}" y="136" font-size="12" fill="{c["mu"]}">{lab}</text>')

    body, defs, h = year_panels(c, slices, FIRST_Y0, step, top, updates)
    icss = '@media (prefers-reduced-motion:reduce){.lwv{display:none}}'
    return card(c, h, "Installs over time", f"Weekly installs since launch ({fmt_date(weeks[0])}). Flags mark releases.",
                boxes + body, stamp, defs, icss)


def traffic_card(c, dates, values, title, hint, stamp, p_label, last3=None):
    """Daily line for one metric: one graph per year, newest first."""
    step, top = axis(max(values + [1]))
    slices = year_slices(dates, values) or [(dates[0].year, dates, values)]

    total = sum(values)
    if last3 is None:
        cutoff = (dates[-1] - dt.timedelta(days=90)) if dates else None
        last3 = sum(v for d, v in zip(dates, values) if d >= cutoff) if cutoff else 0
    boxes = ""
    for i, (num, lab) in enumerate([(f"{total:,}", f"{p_label} (all time)"),
                                    (f"{last3:,}", "Last 3 months")]):
        x = PAD + i * 216
        boxes += (f'<rect x="{x}" y="78" width="204" height="70" rx="10" fill="none" stroke="{c["bd"]}"/>'
                  f'<text x="{x+16}" y="115" font-size="30" font-weight="700" fill="url(#lg)">{num}</text>'
                  f'<text x="{x+16}" y="136" font-size="12" fill="{c["mu"]}">{escape(lab)}</text>')

    body, defs, h = year_panels(c, slices, FIRST_Y0, step, top)
    icss = '@media (prefers-reduced-motion:reduce){.lwv{display:none}}'
    return card(c, h, title, hint, boxes + body, stamp, defs, icss)


def map_card(c, world, rows, total, stamp):
    """rows: list of (code, name, count) sorted desc; code None = not plotted."""
    vb = world["vb"]
    k = IW / vb[2]
    mh = vb[3] * k
    my = 72
    vmax = max(r[2] for r in rows)
    land, brd = world["land"], world["borders"]
    bub, labels = "", ""
    plotted = [r for r in rows if r[0] in world["c"]]
    min_label_v = plotted[min(4, len(plotted) - 1)][2] if plotted else 0
    for i, (code, name, v) in enumerate(plotted):
        _, x, y = world["c"][code]
        r = 2.5 + 16 * math.sqrt(v / vmax)
        d = 0.25 + i * 0.018
        bub += (f'<circle class="bp" cx="{x}" cy="{y}" r="{r:.1f}" fill="url(#bg2)" stroke="{c["a"]}" stroke-width="1"'
                f' style="animation-delay:{d:.2f}s"/>')
        if v > 7:
            phase = (i * 0.37) % 2.8
            bub += (f'<circle class="ph" cx="{x}" cy="{y}" r="{r:.1f}" fill="none" stroke="{c["a"]}" stroke-width="1.5"'
                    f' vector-effect="non-scaling-stroke" style="animation-delay:{d + .6 + phase:.2f}s"/>')
        if v >= min_label_v and v > 0:
            labels += (f'<text x="{x}" y="{y - r - 5:.1f}" font-size="13" font-weight="700" fill="{c["tx"]}" text-anchor="middle"'
                       f' style="animation:fade .6s ease {d + .2:.2f}s backwards">{escape(name)}</text>')
    mapsvg = (f'<g transform="translate({PAD},{my}) scale({k:.4f}) translate({-vb[0]},{-vb[1]})" style="animation:fade .5s ease backwards">'
              f'<path d="{land}" fill="{c["dot"]}" fill-opacity=".16"/><path d="{land}" fill="url(#dots)"/>'
              f'<path d="{brd}" fill="none" stroke="{c["dot"]}" stroke-width=".5" stroke-opacity=".8"/>{bub}{labels}</g>')
    ncol = 3
    per = math.ceil(len(rows) / ncol)
    gap = 28
    cw = (IW - gap * (ncol - 1)) / ncol
    y0 = my + mh + 34
    lst = ""
    for i, (code, name, v) in enumerate(rows):
        col, row = divmod(i, per)
        x = PAD + col * (cw + gap)
        y = y0 + row * 28
        lst += (f'<text x="{x:.1f}" y="{y+11}" font-size="13" fill="{c["tx"]}">{escape(name)}</text>'
                f'<text x="{x+cw:.1f}" y="{y+11}" font-size="12" fill="{c["mu"]}" text-anchor="end">{v} ({v/total*100:.1f}%)</text>'
                f'<rect x="{x:.1f}" y="{y+16}" width="{cw:.1f}" height="4" rx="2" fill="{c["empty"]}"/>'
                f'<rect x="{x:.1f}" y="{y+16}" width="{max(cw*v/vmax, 3):.1f}" height="4" rx="2" fill="url(#lg)"/>')
    h = int(y0 + per * 28 + 12)
    extra = (f'<pattern id="dots" width="4" height="4" patternUnits="userSpaceOnUse"><circle cx="2" cy="2" r="1.05" fill="{c["dot"]}"/></pattern>'
             f'<radialGradient id="bg2"><stop offset="0" stop-color="{c["a"]}" stop-opacity=".15"/><stop offset="1" stop-color="{c["a"]}" stop-opacity=".5"/></radialGradient>')
    n = len(rows)
    mcss = ('@keyframes pop{from{opacity:0;transform:scale(0)}}'
            '@keyframes ping{0%{transform:scale(1);opacity:.7}70%,100%{transform:scale(2.2);opacity:0}}'
            '.bp{transform-box:fill-box;transform-origin:center;animation:pop .5s cubic-bezier(.34,1.56,.64,1) backwards}'
            '.ph{transform-box:fill-box;transform-origin:center;opacity:0;animation:ping 2.8s ease-out infinite}')
    return card(c, h, "Where people install from", f"Every country counts: {n} locations so far. Bubble size shows installs.", mapsvg + lst, stamp, extra, mcss)


def ratings_card(c, weeks, events, stamp):
    """events: list of (date, [c1..c5]) sorted by date."""
    h = 372
    PL, PR, PT, PB = 58, W - PAD, 150, 335
    n = len(weeks)
    X = lambda i: PL + (i * (PR - PL) / (n - 1) if n > 1 else (PR - PL) / 2)
    counts = [sum(e[1]) for e in events]
    total = sum(counts)
    avg = sum((s + 1) * ct for e in events for s, ct in enumerate(e[1])) / total if total else 0
    step, top = axis(max(total, 1))
    g, Y = grid_and_months(c, weeks, X, PL, PR, PT, PB, step, top, h - 14)
    path = f"M{X(0):.1f},{Y(0):.1f}"
    dots, cum, nd = "", 0, 0
    for (d, _), ct in zip(events, counts):
        if ct == 0:
            continue
        idx = min(max((d - weeks[0]).days / 7, 0), n - 1)
        x = X(idx)
        cum += ct
        dd = 2.5 + nd * 0.18
        nd += 1
        path += f"H{x:.1f}V{Y(cum):.1f}"
        dots += (f'<circle class="dp" cx="{x:.1f}" cy="{Y(cum):.1f}" r="4.5" fill="{c["card"]}" stroke="{c["a"]}" stroke-width="2"'
                 f' style="animation-delay:{dd:.2f}s"/>'
                 f'<text x="{x-9:.1f}" y="{Y(cum)-3:.1f}" font-size="11" font-weight="700" fill="{c["tx"]}" text-anchor="end"'
                 f' style="animation:fade .4s ease {dd + .12:.2f}s backwards">{cum}</text>')
    path += f"H{X(n-1):.1f}"
    area = path + f"V{Y(0):.1f}H{X(0):.1f}Z"
    stars_defs, stars = "", ""
    for i in range(5):
        fr = max(0, min(1, avg - i))
        if 0 < fr < 1:
            stars_defs += (f'<linearGradient id="sf{i}"><stop offset="{fr*100:.0f}%" stop-color="{c["a"]}"/>'
                           f'<stop offset="{fr*100:.0f}%" stop-color="{c["empty"]}"/></linearGradient>')
            fill = f"url(#sf{i})"
        else:
            fill = "url(#lg)" if fr >= 1 else c["empty"]
        stars += (f'<g transform="translate({i*25},0) scale(1.15)">'
                  f'<path class="sp" d="{STAR}" fill="{fill}" style="animation-delay:{0.35 + i * 0.25:.2f}s"/></g>')
    head = (f'<text x="{PAD}" y="106" font-size="46" font-weight="700" fill="url(#lg)"'
            f' style="animation:fade .5s ease .15s backwards">{avg:.1f}</text>'
            f'<g transform="translate(122,70)">{stars}</g>'
            f'<text x="122" y="112" font-size="13" fill="{c["mu"]}"'
            f' style="animation:fade .5s ease .3s backwards">average from {total} rating{"s" if total != 1 else ""}</text>') if total else \
           f'<text x="{PAD}" y="100" font-size="15" fill="{c["mu"]}">No ratings yet</text>'
    wdef = (f'<linearGradient id="wv" gradientUnits="userSpaceOnUse" x1="0" y1="0" x2="{W}" y2="0" gradientTransform="translate(-90,0)">'
            '<stop offset="0" stop-color="#fff" stop-opacity="0"/><stop offset=".04" stop-color="#fff" stop-opacity="0"/>'
            '<stop offset=".08" stop-color="#fff" stop-opacity="1"/><stop offset=".086" stop-color="#fff" stop-opacity="0"/>'
            '<stop offset="1" stop-color="#fff" stop-opacity="0"/>'
            '<animateTransform attributeName="gradientTransform" type="translate" from="-90 0" to="970 0" dur="10s" begin="2.4s" repeatCount="indefinite"/>'
            '</linearGradient>')
    body = (head + g +
            f'<path class="fd" d="{area}" fill="url(#ag)"/>'
            f'<path class="gw" d="{path}" pathLength="1" fill="none" stroke="url(#lg)" stroke-width="7" stroke-opacity=".45" filter="url(#gw)"/>'
            f'<path class="ln" d="{path}" pathLength="1" fill="none" stroke="url(#lg)" stroke-width="2.5" stroke-linejoin="round"/>'
            f'<path class="lwv" d="{path}" fill="none" stroke="url(#wv)" stroke-width="5.5" stroke-linecap="round" filter="url(#gw)"/>'
            f'<path class="lwv" d="{path}" fill="none" stroke="url(#wv)" stroke-width="2" stroke-linecap="round"/>' + dots)
    rcss = ('@keyframes pop{from{opacity:0;transform:scale(0)}}'
            '.sp{transform-box:fill-box;transform-origin:center;animation:pop .55s cubic-bezier(.34,1.56,.64,1) backwards}'
            '.dp{transform-box:fill-box;transform-origin:center;animation:pop .45s cubic-bezier(.34,1.56,.64,1) backwards}'
            '@media (prefers-reduced-motion:reduce){.lwv{display:none}}')
    return card(c, h, "Ratings", "Total ratings collected since launch.", body, stamp, stars_defs + wdef, rcss)
