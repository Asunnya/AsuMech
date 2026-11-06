#!/usr/bin/env python3
"""The AnoMech -> AsuMech rename as one deterministic transformation.

  apply [--root DIR]        rename paths and text in a git worktree (idempotent)
  reverse-patch             stdin patch in AsuMech names -> stdout patch in AnoMech names
  upstream [--from REF] [--branch NAME] [--push REMOTE]
                            add a commit to NAME (default upstream-asu) whose tree is REF
                            (default upstream/master) with `apply` run on it

`upstream` is how upstream changes come in after the rename: every commit on upstream-asu is a
renamed snapshot of upstream, parented on the previous snapshot, so merging it into an AsuMech
branch shows only what upstream changed since the last sync. The upstream sha of each snapshot
is kept in its message (Upstream-Commit: trailer).

A line holding `rename:keep` is never touched; so are ImGui `###` ids (imgui.ini keys window
positions by them), the upstream repo's own URLs, and the ANOMECH_* environment variables.
"""
import argparse
import os
import re
import subprocess
import sys
import tempfile

OLD, NEW = "AnoMech", "AsuMech"
KEEP_MARKER = "rename:keep"

# Applied before the general rename, in order.
REWRITES = [
    ("ghcr.io/anomek/anomech-relay", "ghcr.io/asunnya/asumech-relay"),
    ("https://github.com/anomek/AnoMech/blob/", "https://github.com/Asunnya/AsuMech/blob/"),
    ("<PackageProjectUrl>https://github.com/anomek/AnoMech<", "<PackageProjectUrl>https://github.com/Asunnya/AsuMech<"),
    ("<Authors>Anomek</Authors>", "<Authors>Asunnya, Anomek</Authors>"),
]
PROTECTED = re.compile(r"###AnoMech\w*|anomek[/:]AnoMech|Anomek's AnoMech|fork of AnoMech|from AnoMech")
RENAMES = [(re.compile(OLD), NEW), (re.compile(OLD.lower()), NEW.lower())]

# pluginmaster.json is the fork's own and names AnoMech on purpose (the changelog tells players to
# uninstall it).
SKIP_FILES = {"LICENSE", "LICENSE.md", "tools/asumech_rename.py", "pluginmaster.json"}
SKIP_PREFIXES = ("datalogs/",)


def git(root, *args, input=None):
    return subprocess.run(["git", "-C", root, *args], check=True, capture_output=True, text=True, input=input).stdout


def tracked(root):
    return [p for p in git(root, "ls-files", "-z").split("\0") if p]


def rename_text(text):
    out = []
    for line in text.splitlines(keepends=True):
        if KEEP_MARKER in line:
            out.append(line)
            continue
        for a, b in REWRITES:
            line = line.replace(a, b)
        kept = []

        def hide(m):
            kept.append(m.group(0))
            return f"\0{len(kept) - 1}\0"

        line = PROTECTED.sub(hide, line)
        for pattern, repl in RENAMES:
            line = pattern.sub(repl, line)
        line = re.sub(r"\0(\d+)\0", lambda m: kept[int(m.group(1))], line)
        out.append(line)
    return "".join(out)


def renamed_path(path):
    return "/".join(c.replace(OLD, NEW) for c in path.split("/"))


def move_paths(root):
    """git mv the shallowest path component naming OLD, until none is left."""
    moves = 0
    while True:
        best = None
        for path in tracked(root):
            parts = path.split("/")
            for i, part in enumerate(parts):
                if OLD in part:
                    if best is None or i < best[0]:
                        best = (i, "/".join(parts[: i + 1]))
                    break
        if best is None:
            return moves
        src = best[1]
        dst = renamed_path(src)
        if os.path.exists(os.path.join(root, dst)):
            # The target already exists (a half-applied run): move what's tracked file by file.
            for path in tracked(root):
                if path == src or path.startswith(src + "/"):
                    target = renamed_path(path)
                    os.makedirs(os.path.dirname(os.path.join(root, target)) or root, exist_ok=True)
                    git(root, "mv", path, target)
        else:
            git(root, "mv", src, dst)
        moves += 1


def rewrite_files(root):
    changed = 0
    for path in tracked(root):
        if path in SKIP_FILES or path.startswith(SKIP_PREFIXES):
            continue
        full = os.path.join(root, path)
        if os.path.islink(full) or not os.path.isfile(full):
            continue
        with open(full, "rb") as f:
            raw = f.read()
        if b"\0" in raw:
            continue
        try:
            text = raw.decode("utf-8")
        except UnicodeDecodeError:
            continue
        new = rename_text(text)
        if new != text:
            with open(full, "wb") as f:
                f.write(new.encode("utf-8"))
            changed += 1
    return changed


def apply(root):
    moves = move_paths(root)
    changed = rewrite_files(root)
    print(f"{moves} path(s) moved, {changed} file(s) rewritten", file=sys.stderr)


def reverse_patch():
    """Only for small patches meant for upstream: names that were already AsuMech before the
    rename (the /asumech command, the plugin's display name) come back as AnoMech too, so read
    the result before sending it."""
    for line in sys.stdin:
        if KEEP_MARKER not in line:
            line = line.replace(NEW, OLD).replace(NEW.lower(), OLD.lower())
            line = line.replace("ghcr.io/asunnya/asumech-relay", "ghcr.io/anomek/anomech-relay")
        sys.stdout.write(line)


SCENARIO_DIR = re.compile(rf"^{NEW}/Scenarios/([^/]+/[^/]+)/")


def triage_changes(repo, old_tree, new_tree):
    """Sorts a sync for review: new scenarios come in, changed ones and everything structural are
    read before taking them, since on conflict the fork's own version wins."""
    before = set(git(repo, "ls-tree", "-r", "--name-only", old_tree).splitlines())
    new_scenarios, changed_scenarios, structural = set(), set(), []
    known = {m.group(1) for p in before if (m := SCENARIO_DIR.match(p))}
    for line in git(repo, "diff-tree", "-r", "--name-status", old_tree, new_tree).splitlines():
        status, path = line.split("\t", 1)
        path = path.split("\t")[-1]
        if m := SCENARIO_DIR.match(path):
            (changed_scenarios if m.group(1) in known else new_scenarios).add(m.group(1))
        elif not path.startswith("tests/"):
            structural.append(f"{status[0]} {path}")
    parts = []
    if new_scenarios:
        parts.append("New scenarios (take):\n" + "\n".join(f"  {s}" for s in sorted(new_scenarios)))
    if changed_scenarios:
        parts.append("Changed scenarios (ours win on conflict):\n" + "\n".join(f"  {s}" for s in sorted(changed_scenarios)))
    if structural:
        parts.append("Structural (review; adopt what fixes a problem we have):\n" + "\n".join(f"  {s}" for s in structural))
    return "\n\n".join(parts) + ("\n" if parts else "")


def upstream(repo, ref, branch, push):
    sha = git(repo, "rev-parse", ref + "^{commit}").strip()
    parent = None
    try:
        parent = git(repo, "rev-parse", "--verify", "-q", branch).strip()
    except subprocess.CalledProcessError:
        pass
    if parent:
        last = re.search(r"^Upstream-Commit: (\w+)$", git(repo, "log", "-1", "--format=%B", parent), re.M)
        if last and last.group(1) == sha:
            print(f"{branch} already at upstream {sha[:10]}", file=sys.stderr)
            return
    with tempfile.TemporaryDirectory(prefix="upstream-asu-") as tmp:
        work = os.path.join(tmp, "tree")
        git(repo, "worktree", "add", "--detach", work, sha)
        try:
            apply(work)
            git(work, "add", "-A")
            tree = git(work, "write-tree").strip()
        finally:
            git(repo, "worktree", "remove", "--force", work)
    since = triage = ""
    if parent:
        last = re.search(r"^Upstream-Commit: (\w+)$", git(repo, "log", "-1", "--format=%B", parent), re.M)
        if last:
            since = git(repo, "log", "--oneline", "--no-merges", f"{last.group(1)}..{sha}")
        triage = triage_changes(repo, parent, tree)
    message = f"Upstream {ref} renamed to {NEW}\n\n{since}\n{triage}\nUpstream-Commit: {sha}\n"
    args = ["commit-tree", tree, "-F", "-"] + (["-p", parent] if parent else [])
    commit = git(repo, *args, input=message).strip()
    git(repo, "update-ref", f"refs/heads/{branch}", commit)
    print(f"{branch} -> {commit[:10]} (upstream {sha[:10]})", file=sys.stderr)
    if push:
        git(repo, "push", push, f"{branch}:{branch}")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="cmd", required=True)
    a = sub.add_parser("apply")
    a.add_argument("--root", default=".")
    sub.add_parser("reverse-patch")
    u = sub.add_parser("upstream")
    u.add_argument("--from", dest="ref", default="upstream/master")
    u.add_argument("--branch", default="upstream-asu")
    u.add_argument("--push")
    args = parser.parse_args()
    if args.cmd == "apply":
        apply(git(args.root, "rev-parse", "--show-toplevel").strip())
    elif args.cmd == "reverse-patch":
        reverse_patch()
    else:
        upstream(git(".", "rev-parse", "--show-toplevel").strip(), args.ref, args.branch, args.push)


if __name__ == "__main__":
    main()
