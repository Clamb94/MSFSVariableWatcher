"""Generates KEvents.json from the local MSFS SDK documentation.

K: events cannot be enumerated from a running sim (SimConnect only enumerates
aircraft Input Events), so the catalogue is baked from the SDK docs at dev time.

Usage:
    python gen_kevents.py <sdk-docs-root> <output-json>

<sdk-docs-root> is the MSFS_SDK_Docs checkout containing msfs2020/ and msfs2024/.
"""

import json
import re
import sys
from pathlib import Path

DOC_SETS = [
    ("2020", "msfs2020/Programming_Tools/Event_IDs"),
    ("2024", "msfs2024/flighting/programming-apis/key-events"),
]

NAME_RE = re.compile(r"^[A-Z][A-Z0-9_]{2,}$")
BACKTICK_RE = re.compile(r"`([^`]+)`")
LINK_RE = re.compile(r"\[([^\]]*)\]\([^)]*\)")


def clean(cell):
    """Strips markdown links, backticks, escapes and collapses whitespace."""
    text = LINK_RE.sub(r"\1", cell)
    text = text.replace("`", "").replace(r"\*", "*").replace(r"\_", "_")
    text = re.sub(r"\s+", " ", text).strip()
    # The 2024 tables run parameters together ("[0]: x[1]: y"); give them air.
    text = re.sub(r"(?<!^)(?<!\s)(\[\d+\]\s*:)", r" \g<1>", text)
    return "" if text.upper() in ("N/A", "-") else text


def split_row(line):
    line = line.strip()
    if line.startswith("|"):
        line = line[1:]
    if line.endswith("|"):
        line = line[:-1]
    return [c.strip() for c in line.split("|")]


def is_separator(cells):
    return all(re.fullmatch(r":?-{2,}:?", c) for c in cells if c)


def parse_file(path, category_default):
    """Yields (category, name, parameters, description) for every table row."""
    category = category_default
    params_col = desc_col = None
    in_table = False

    for raw in path.read_text(encoding="utf-8", errors="replace").splitlines():
        line = raw.strip()

        if line.startswith("#"):
            heading = line.lstrip("#").strip()
            # The all-caps page-level heading repeats the file title; skip it.
            if heading and heading != heading.upper():
                category = heading
            in_table = False
            continue

        if not line.startswith("|"):
            in_table = False
            continue

        cells = split_row(line)
        if is_separator(cells):
            continue

        header = [c.lower() for c in cells]
        if any("param" in c for c in header) or any("descri" in c for c in header):
            # Table header: remember where the parameter / description columns are.
            params_col = next((i for i, c in enumerate(header) if "param" in c), None)
            desc_col = next((i for i, c in enumerate(header) if "descri" in c), None)
            in_table = True
            continue

        if not in_table:
            continue

        names = [n.strip() for n in BACKTICK_RE.findall(cells[0])]
        if not names:
            names = [cells[0].strip()]
        names = [n for n in names if NAME_RE.fullmatch(n)]
        if not names:
            continue

        def col(index, fallback):
            if index is not None and index < len(cells):
                return clean(cells[index])
            # Short rows (a few tables omit the parameter column entirely) fall
            # back to "last cell is the description".
            return clean(cells[fallback]) if fallback < len(cells) else ""

        params = col(params_col, len(cells)) if params_col is not None else ""
        desc = col(desc_col, len(cells) - 1)
        if params_col is not None and params_col >= len(cells) and len(cells) == 2:
            params = ""
            desc = clean(cells[1])

        for name in names:
            yield category, name, params, desc


# The 2020 and 2024 docs name a few pages differently; keep one group per topic.
GROUP_ALIASES = {
    "Aircraft Autopilot Flight Assist Events": "Aircraft Autopilot And Flight Assistance Events",
    "View Camera Events": "View And Camera Events",
    "Aircraft Misc Events": "Aircraft Miscellaneous Events",
}


def title_from_filename(path):
    stem = path.stem.replace("_", " ").replace("-", " ")
    title = re.sub(r"\s+", " ", stem).strip().title()
    return GROUP_ALIASES.get(title, title)


def main():
    if len(sys.argv) != 3:
        print(__doc__)
        return 1

    root = Path(sys.argv[1])
    out = Path(sys.argv[2])
    events = {}

    for version, rel in DOC_SETS:
        folder = root / rel
        if not folder.is_dir():
            print(f"WARNING: {folder} not found, skipping")
            continue
        for path in sorted(folder.glob("*.md")):
            if "index" in path.stem.lower():
                continue
            group = title_from_filename(path)
            for category, name, params, desc in parse_file(path, group):
                entry = events.setdefault(name, {
                    "name": name,
                    "group": group,
                    "category": category,
                    "parameters": "",
                    "description": "",
                    "sims": [],
                })
                # Later doc sets (2024) win where they actually say something.
                if params:
                    entry["parameters"] = params
                if desc:
                    entry["description"] = desc
                if version not in entry["sims"]:
                    entry["sims"].append(version)
                entry["group"] = group
                entry["category"] = category

    ordered = sorted(events.values(), key=lambda e: e["name"])
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(ordered, indent=1), encoding="utf-8")
    print(f"{len(ordered)} K: events written to {out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
