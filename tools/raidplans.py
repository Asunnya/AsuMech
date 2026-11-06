#!/usr/bin/env python3
"""Turn the guide dumps in AsuMech/Scenarios/Raidsplan/*.txt into data the Learn chain uses.

    python3 tools/raidplans.py build            # one catalog per fight -> AsuMech/Learn/Sources/guides/<fight>.json
    python3 tools/raidplans.py check            # scenarios exist, waymarks vs zone presets, diff vs plans.json and lessons
    python3 tools/raidplans.py apply [--write]  # add missing plans.json entries and lesson sources (dry run without --write)

Tags in the .txt (one per line, start of line):
    @fight: DSR / @family: Dsr / @credits: NAUR    file header
    @layout: <zone preset name>                    layout for the plans.json entries that follow
    @scenarios: A, B   (or "-" for none)           scenario types the following links/rules belong to
    @strat: <name>                                 plans.json "strat" for the following links
    @plans: -   (or "+")                           following links go to lesson sources only, not plans.json
@scenarios / @strat / @layout reset at every section header (a "----" line, a title, a "----" line).

apply only ever adds: it never removes or rewrites an existing plans.json entry or lesson source.
"""
import argparse
import glob
import json
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
RAIDSPLAN = os.path.join(ROOT, "AsuMech", "Scenarios", "Raidsplan")
SCENARIOS = os.path.join(ROOT, "AsuMech", "Scenarios")
PLANS = os.path.join(ROOT, "AsuMech", "Learn", "Toolbox", "plans.json")
LESSONS = os.path.join(ROOT, "AsuMech", "Learn", "Lessons")
OUT = os.path.join(ROOT, "AsuMech", "Learn", "Sources", "guides")

URL = re.compile(r"https?://[^\s)\]\"]+")
STGY = re.compile(r"\[stgy:[A-Za-z0-9+\-_]+\]")
PLAN_KINDS = {"toolbox", "raidplan.io", "stgy", "xivplan"}


def kind_of(url):
    u = url.lower()
    if u.startswith("[stgy:") or "ffxivstrats.io" in u: return "stgy"
    if "toolboxgaming.space" in u: return "toolbox"
    if "raidplan.io/plan/" in u: return "raidplan.io"
    if "xivplan" in u: return "xivplan"
    if "docs.google.com/presentation" in u or "slides.com" in u: return "slides"
    if "docs.google.com/spreadsheets" in u: return "sheet"
    if "docs.google.com/document" in u: return "doc"
    if "youtube.com" in u or "youtu.be" in u: return "video"
    if "tinyurl.com" in u or "bit.ly" in u: return "shortlink"
    return "other"


def norm(url):
    """Same plan, different spelling: drop toolbox's preview flag and any fragment."""
    if url.startswith("[stgy:"): return url
    u = url.split("#")[0].replace("&preview=1", "").replace("?preview=1", "")
    return u.rstrip("/")


def sheet_id(url):
    m = re.search(r"/spreadsheets/d/([A-Za-z0-9_-]+)", url)
    return m.group(1) if m else None


# ---------------------------------------------------------------- parse

def parse(path):
    lines = open(path, encoding="utf-8").read().split("\n")
    head = {"file": os.path.basename(path)}
    for l in lines[:12]:
        m = re.match(r"^(Source|Guide|Captured):\s+(.*)$", l)
        if m: head[m.group(1).lower()] = m.group(2).strip()
    title = next((l for l in lines[1:4] if l and not l.startswith("=")), "")
    head["title"] = title

    # the FRU dump carries NAUR's whole written guide after this marker: keep it apart, by phase
    guide_at = next((i for i, l in enumerate(lines) if l.strip() == "4. NAUR IN-DEPTH GUIDE"), None)
    body = lines if guide_at is None else lines[:guide_at]

    sections = []
    scope = {"scenarios": [], "strat": None, "layout": None, "plans": True}
    file_layout = None
    label = None
    waymarks = []
    i = 0

    def new_section(name):
        sec = {"title": name, "links": [], "rules": []}
        sections.append(sec)
        return sec

    cur = new_section("(header)")
    while i < len(body):
        l = body[i].rstrip()
        s = l.strip()
        # section header: ---- / title / ----
        banner = re.fullmatch(r"-{20,}|#{20,}", s)
        if banner and i + 2 < len(body) and body[i + 2].strip() == s:
            cur = new_section(body[i + 1].strip())
            scope = {"scenarios": [], "strat": None, "layout": file_layout, "plans": True}
            label = None
            i += 3
            continue
        if re.fullmatch(r"[=#]{20,}", s) or not s:
            i += 1
            continue
        m = re.match(r"^@(\w+):\s*(.*)$", s)
        if m:
            key, val = m.group(1), m.group(2).strip()
            if key == "scenarios":
                scope["scenarios"] = [] if val == "-" else [v.strip() for v in val.split(",") if v.strip()]
            elif key == "strat":
                scope["strat"] = None if val == "-" else val
            elif key == "plans":
                scope["plans"] = val != "-"
            elif key == "layout":
                scope["layout"] = None if val == "-" else val
                if cur["title"] == "(header)": file_layout = scope["layout"]
            else:
                head[key] = val
            i += 1
            continue
        if s == "{":  # waymark preset JSON
            j = i
            while j < len(body) and body[j].strip() != "}": j += 1
            try:
                waymarks.append(json.loads("\n".join(body[i:j + 1])))
            except json.JSONDecodeError as e:
                print(f"{head['file']}:{i + 1}: bad waymark JSON: {e}", file=sys.stderr)
            i = j + 1
            continue
        if s.startswith("- "):  # rule bullet, continuation lines are indented deeper
            text = [s[2:]]
            indent = len(l) - len(l.lstrip())
            while i + 1 < len(body) and body[i + 1].strip() and not body[i + 1].strip().startswith(("-", "@")) \
                    and len(body[i + 1]) - len(body[i + 1].lstrip()) > indent and not URL.search(body[i + 1]):
                i += 1
                text.append(body[i].strip())
            rule = {"text": " ".join(text), "scenarios": list(scope["scenarios"])}
            cur["rules"].append(rule)
            i += 1
            continue
        if s.startswith("PF names:"):
            names = [n.strip() for n in s[len("PF names:"):].split(",")]
            while i + 1 < len(body) and body[i + 1].startswith(" " * 12) and not URL.search(body[i + 1]):
                i += 1
                names += [n.strip() for n in body[i].split(",")]
            names = [n for n in names if n]
            if cur["links"]:
                for ln in cur["links"]:
                    if ln["label"] == label: ln["pfNames"] = names
            i += 1
            continue
        found = [(m.group(0).rstrip(".,"), m.start()) for m in URL.finditer(s)] + [(m.group(0), m.start()) for m in STGY.finditer(s)]
        if found:
            before = s[:min(p for _, p in found)].strip().rstrip(":").strip()
            # "Toolbox: url" / "NAUR Room Mits   url": the text before the link is its own label
            if before and before.lower() not in ("toolbox", "slides", "raidplan", "tool"):
                link_label = before
            elif before:
                link_label = f"{label} ({before})" if label else before
            else:
                link_label = label or cur["title"]
            for url, _ in found:
                entry = {"label": link_label, "url": url, "kind": kind_of(url), "section": cur["title"],
                         "scenarios": list(scope["scenarios"])}
                if scope["strat"]: entry["strat"] = scope["strat"]
                if scope["layout"]: entry["layout"] = scope["layout"]
                if not scope["plans"]: entry["sourceOnly"] = True
                if entry["kind"] == "sheet": entry["sheetId"] = sheet_id(url)
                cur["links"].append(entry)
            i += 1
            continue
        # anything else is a label for the links below it (joined when it wraps onto a second line)
        if label is not None and i > 0 and body[i - 1].strip() == label.split("\n")[-1] and not URL.search(body[i - 1]) \
                and not body[i - 1].strip().startswith(("@", "-")) and (label.endswith(("-", "and", "|")) or s[0].islower()):
            label = f"{label} {s}"
        else:
            label = s
        i += 1

    guide = []
    if guide_at is not None:
        cur_g = None
        g = lines[guide_at + 1:]
        k = 0
        while k < len(g):
            if re.fullmatch(r"={20,}", g[k].strip()) and k + 2 < len(g) and re.fullmatch(r"={20,}", g[k + 2].strip()):
                cur_g = {"title": g[k + 1].strip(), "text": []}
                guide.append(cur_g)
                k += 3
                continue
            if cur_g is None:
                cur_g = {"title": "Introduction", "text": []}
                guide.append(cur_g)
            cur_g["text"].append(g[k])
            k += 1
        for p in guide: p["text"] = "\n".join(p["text"]).strip()

    sections = [s for s in sections if s["links"] or s["rules"]]
    return head, sections, waymarks, guide


# ---------------------------------------------------------------- repo facts

def scenario_types():
    out = {}
    for f in glob.glob(os.path.join(SCENARIOS, "**", "*.cs"), recursive=True):
        for m in re.finditer(r"\bclass (\w+Scenario)\b", open(f, encoding="utf-8").read()):
            out[m.group(1)] = os.path.relpath(f, ROOT)
    return out


def zone_presets(family):
    """WaymarkLayouts of a family's zone as scenario-local offsets, read straight from the C#."""
    folder = os.path.join(SCENARIOS, family)
    src = {f: open(f, encoding="utf-8").read() for f in glob.glob(os.path.join(folder, "*.cs"))}
    zone = next((t for f, t in src.items() if f.endswith("Zone.cs")), "")
    every = "\n".join(src.values())

    def block(text, start):
        k = text.index("[", start)
        depth = 0
        for j in range(k, len(text)):
            depth += {"[": 1, "]": -1}.get(text[j], 0)
            if depth == 0: return text[k:j + 1]
        return ""

    def markers(text):
        out = {}
        for m in re.finditer(r"WaymarkSlot\.(\w+),\s*new Vector3\(([^)]*)\)", text):
            out[m.group(1)] = [float(v.strip().rstrip("f") or 0) for v in m.group(2).split(",")]
        return out

    presets = {}
    for m in re.finditer(r'new WaymarkLayout\("([^"]+)",\s*', zone):
        name, rest = m.group(1), zone[m.end():]
        ring = re.match(r"(?:Core\.Game\.)?WaymarkPresets\.Ring\(([\d.]+)f?\)", rest)
        if ring:
            r = float(ring.group(1))
            import math
            slots = ["A", "One", "B", "Two", "C", "Three", "D", "Four"]
            presets[name] = {s: [r * math.sin(math.pi - i * math.pi / 4), 0.0, r * math.cos(math.pi - i * math.pi / 4)] for i, s in enumerate(slots)}
        elif rest.lstrip().startswith("["):
            presets[name] = markers(block(zone, m.end()))
        else:
            named = re.match(r"([\w.]+)", rest)
            if not named: continue
            ref = named.group(1).split(".")[-1]
            d = re.search(rf"\b{ref}\s*(=>|=)", every)
            presets[name] = markers(block(every, d.end())) if d else {}
    origin = re.search(r"Origin\s*=>\s*new\(([^)]*)\)", zone)  # Vector3 literal
    o = [float(v.strip().rstrip("f")) for v in origin.group(1).split(",")] if origin else [0, 0, 0]
    return presets, o


def lesson_files():
    out = {}
    for f in glob.glob(os.path.join(LESSONS, "*", "*.json")):
        try:
            d = json.load(open(f, encoding="utf-8"))
        except (json.JSONDecodeError, UnicodeDecodeError):
            continue
        if isinstance(d, dict) and "scenario" in d and "steps" in d:
            out.setdefault(d["scenario"], []).append(f)
    return out


def dump(obj):
    return json.dumps(obj, indent=2, ensure_ascii=False) + "\n"


# ---------------------------------------------------------------- catalog

def catalog(path):
    head, sections, waymarks, guide = parse(path)
    family = head.get("family")
    presets, origin = zone_presets(family) if family else ({}, [0, 0, 0])
    credits = head.get("credits", "")

    marks = []
    for w in waymarks:
        local = {s: [round(w[s]["X"] - origin[0], 3), round(w[s]["Y"] - origin[1], 3), round(w[s]["Z"] - origin[2], 3)]
                 for s in ("A", "B", "C", "D", "One", "Two", "Three", "Four") if s in w and w[s].get("Active", True)}
        best, worst = None, None
        for name, p in presets.items():
            if set(p) != set(local): continue
            dev = max(((local[s][0] - p[s][0]) ** 2 + (local[s][2] - p[s][2]) ** 2) ** 0.5 for s in local)
            if worst is None or dev < worst: best, worst = name, dev
        marks.append({"name": w.get("Name"), "mapId": w.get("MapID"), "local": local,
                      "closestPreset": best, "maxDeviation": None if worst is None else round(worst, 3)})

    per = {}
    for sec in sections:
        for ln in sec["links"]:
            for sc in ln["scenarios"]:
                e = per.setdefault(sc, {"plans": [], "sources": [], "rules": []})
                if ln["kind"] in PLAN_KINDS and not ln.get("sourceOnly"):
                    plan = {"url": ln["url"]}
                    if ln.get("layout"): plan["layout"] = ln["layout"]
                    plan["credits"] = f"{credits} ({ln['label']})" if credits else ln["label"]
                    if ln.get("strat"): plan["strat"] = ln["strat"]
                    e["plans"].append(plan)
                if ln["kind"] != "stgy":
                    src = {"label": f"{credits} - {ln['label']}" if credits else ln["label"], "url": ln["url"]}
                    if ln.get("strat"): src["strat"] = ln["strat"]
                    twin = next((x for x in e["sources"] if x["label"] == src["label"]), None)
                    if twin is None: e["sources"].append(src)
                    elif kind_of(twin["url"]) == "shortlink" and ln["kind"] != "shortlink": twin["url"] = ln["url"]
        for r in sec["rules"]:
            for sc in r["scenarios"]:
                per.setdefault(sc, {"plans": [], "sources": [], "rules": []})["rules"].append(r["text"])

    mit = [ln for sec in sections for ln in sec["links"] if ln["kind"] == "sheet" or re.search(r"\bmit", ln["label"], re.I)]
    return {
        "fight": head.get("fight"), "family": family, "title": head.get("title"),
        "source": head.get("source"), "guide": head.get("guide"), "captured": head.get("captured"),
        "credits": credits,
        "sections": sections,
        "mitigation": [{"label": m["label"], "url": m["url"], **({"sheetId": m["sheetId"]} if m.get("sheetId") else {})} for m in mit],
        "waymarks": marks,
        "scenarios": per,
        "guideText": guide,
    }


def all_catalogs():
    return [catalog(f) for f in sorted(glob.glob(os.path.join(RAIDSPLAN, "*.txt")))]


# ---------------------------------------------------------------- commands

def cmd_build(args):
    os.makedirs(OUT, exist_ok=True)
    for c in all_catalogs():
        if not c["fight"]:
            print("skipped (no @fight tag)")
            continue
        path = os.path.join(OUT, c["fight"].lower() + ".json")
        open(path, "w", encoding="utf-8").write(dump(c))
        links = sum(len(s["links"]) for s in c["sections"])
        print(f"{os.path.relpath(path, ROOT)}: {len(c['sections'])} sections, {links} links, "
              f"{len(c['scenarios'])} scenarios, {len(c['waymarks'])} waymark presets, {len(c['guideText'])} guide parts")
    return 0


def diff(catalogs):
    plans = json.load(open(PLANS, encoding="utf-8"))
    lessons = lesson_files()
    add_plans, add_sources = [], []
    for c in catalogs:
        for sc, e in c["scenarios"].items():
            have = {norm(p["url"]) for p in plans.get(sc, [])}
            for p in e["plans"]:
                if norm(p["url"]) not in have:
                    add_plans.append((sc, p))
                    have.add(norm(p["url"]))
            for f in lessons.get(sc, []):
                d = json.load(open(f, encoding="utf-8"))
                got = {norm(s["url"]) for s in d.get("sources") or []}
                for s in e["sources"]:
                    if s.get("strat") and s["strat"] != d.get("strat"): continue
                    if norm(s["url"]) not in got:
                        add_sources.append((f, {"label": s["label"], "url": s["url"]}))
                        got.add(norm(s["url"]))
    return plans, add_plans, add_sources


def cmd_check(args):
    catalogs = all_catalogs()
    types = scenario_types()
    problems = 0
    for c in catalogs:
        print(f"== {c['fight']} ({c['family']})")
        for sc in c["scenarios"]:
            if sc not in types:
                print(f"  ! unknown scenario type {sc}")
                problems += 1
        family_presets = zone_presets(c["family"])[0] if c["family"] else {}
        for sec in c["sections"]:
            for ln in sec["links"]:
                if ln.get("layout") and ln["layout"] not in family_presets:
                    print(f"  ! layout '{ln['layout']}' is not a {c['family']} zone preset ({', '.join(family_presets)})")
                    problems += 1
                    break
        for w in c["waymarks"]:
            if w["closestPreset"] is None:
                print(f"  waymarks '{w['name']}': no zone preset with the same slots")
            elif w["maxDeviation"] <= 0.05:
                print(f"  waymarks '{w['name']}' = zone preset '{w['closestPreset']}'")
            else:
                print(f"  ~ waymarks '{w['name']}': closest preset '{w['closestPreset']}' is off by up to {w['maxDeviation']}y")
        unmapped = [ln for s in c["sections"] for ln in s["links"] if ln["kind"] in PLAN_KINDS and not ln["scenarios"]]
        for ln in unmapped:
            print(f"  - plan link without @scenarios: [{ln['section']}] {ln['label']}  {ln['url'][:70]}")
        plans = json.load(open(PLANS, encoding="utf-8"))
        guide_urls = {sc: {norm(p["url"]) for p in e["plans"]} for sc, e in c["scenarios"].items()}
        fam_scenarios = [sc for sc, f in types.items() if c["family"] and f"/{c['family']}/" in f.replace(os.sep, "/")]
        for sc in fam_scenarios:
            for p in plans.get(sc, []):
                if norm(p["url"]) not in guide_urls.get(sc, set()):
                    print(f"  . plans.json {sc} has a plan this guide doesn't map there: {p.get('credits')}  {p['url'][:60]}")
            if sc not in c["scenarios"]:
                print(f"  . {sc}: nothing in this guide maps to it")
    _, add_plans, add_sources = diff(catalogs)
    print(f"\nplans.json: {len(add_plans)} entr{'y' if len(add_plans) == 1 else 'ies'} to add")
    for sc, p in add_plans: print(f"  + {sc}: {p['credits']}  {p['url'][:70]}")
    print(f"lesson sources: {len(add_sources)} to add")
    for f, s in add_sources: print(f"  + {os.path.relpath(f, LESSONS)}: {s['label']}")
    return 1 if problems else 0


def cmd_apply(args):
    catalogs = all_catalogs()
    plans, add_plans, add_sources = diff(catalogs)
    for sc, p in add_plans:
        plans.setdefault(sc, []).append(p)
        print(f"plans.json + {sc}: {p['credits']}")
    by_file = {}
    for f, s in add_sources: by_file.setdefault(f, []).append(s)
    skipped = []
    for f, items in by_file.items():
        text = open(f, encoding="utf-8").read()
        d = json.loads(text)
        if dump(d) != text:
            # written by the C# tools (other escaping): only an empty "sources": [] is patched in place
            m = re.search(r'^( *)"sources": \[\]', text, re.M)
            if not m or text.count('"sources": []') != 1:
                skipped.append(f)
                continue
            pad = m.group(1)
            body = ",\n".join(f'{pad}  {{\n{pad}    "label": {json.dumps(s["label"], ensure_ascii=False)},\n'
                              f'{pad}    "url": {json.dumps(s["url"], ensure_ascii=False)}\n{pad}  }}' for s in items)
            text = text[:m.start()] + f'{pad}"sources": [\n{body}\n{pad}]' + text[m.end():]
            json.loads(text)
            print(f"{os.path.relpath(f, LESSONS)} + {len(items)} source(s)")
            if args.write: open(f, "w", encoding="utf-8").write(text)
            continue
        d["sources"] = (d.get("sources") or []) + items
        print(f"{os.path.relpath(f, LESSONS)} + {len(items)} source(s)")
        if args.write: open(f, "w", encoding="utf-8").write(dump(d))
    if args.write and add_plans: open(PLANS, "w", encoding="utf-8").write(dump(plans))
    for f in skipped: print(f"skipped {os.path.relpath(f, LESSONS)} (its JSON layout isn't Python's; add the sources by hand)")
    if not args.write: print("\ndry run: pass --write to save")
    return 0


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    sub.add_parser("build")
    sub.add_parser("check")
    a = sub.add_parser("apply")
    a.add_argument("--write", action="store_true")
    args = ap.parse_args()
    return {"build": cmd_build, "check": cmd_check, "apply": cmd_apply}[args.cmd](args)


if __name__ == "__main__":
    sys.exit(main())
