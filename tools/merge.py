"""Build the new holidays.json and print what changed + an hours cross-check."""
import json
import re
import sys
import warnings

import exchange_calendars as xcals

warnings.filterwarnings("ignore")
sys.stdout.reconfigure(encoding="utf-8")

SCRATCH = sys.argv[1]
REPO = sys.argv[2]

exec(open(f"{SCRATCH}/gen_holidays.py", encoding="utf-8").read().split("out, cache")[0])  # reuse MIC map

gen = json.load(open(f"{SCRATCH}/generated.json", encoding="utf-8"))
old = json.load(open(f"{REPO}/Tradewatch/holidays.json", encoding="utf-8"))

# Calendars whose library data stops at end-2026: take 2026 from the library,
# 2027 from the sources noted below.
def lib_2026(mic):
    cal = xcals.get_calendar(mic, start="2025-06-01", end="2026-12-31")
    s = set(cal.sessions_in_range("2026-01-01", "2026-12-31").strftime("%Y-%m-%d"))
    import pandas as pd
    return [d.strftime("%Y-%m-%d") for d in pd.date_range("2026-01-01", "2026-12-31")
            if d.dayofweek < 5 and d.strftime("%Y-%m-%d") not in s]

MANUAL_2027 = {
    # SGX: MOM gazetted 2027 public holidays (weekday closures incl. 8 Feb in lieu)
    "XSES": ["2027-01-01", "2027-02-08", "2027-03-10", "2027-03-26", "2027-05-17",
             "2027-05-20", "2027-08-09", "2027-10-28"],
    # NSE/BSE: ESTIMATE (third-party list; official circular due ~Dec 2026) + Diwali
    "XBOM": ["2027-01-26", "2027-03-10", "2027-03-22", "2027-03-26", "2027-04-14",
             "2027-04-15", "2027-04-19", "2027-05-17", "2027-06-15", "2027-10-29"],
    # SSE/SZSE/BSE: ESTIMATE (State Council schedule due ~Nov-Dec 2026)
    "XSHG": ["2027-01-01", "2027-02-05", "2027-02-08", "2027-02-09", "2027-02-10",
             "2027-02-11", "2027-04-05", "2027-05-03", "2027-05-04", "2027-06-09",
             "2027-09-15", "2027-10-01", "2027-10-04", "2027-10-05", "2027-10-06",
             "2027-10-07"],
}
for name, mic in MIC.items():
    if mic in MANUAL_2027:
        gen[name] = lib_2026(mic) + MANUAL_2027[mic]

# Report changes still ahead of us (today onward)
TODAY = "2026-10-19"
print("== Changes from today on (exchanges that had data before) ==")
for name in old:
    o = {d for d in old[name] if d >= TODAY}
    n = {d for d in gen.get(name, []) if d >= TODAY}
    if o != n:
        print(f"{name}\n   removed: {sorted(o - n)}\n   added:   {sorted(n - o)}")
print(f"\nexchanges with holiday data: before {len(old)}, after {len(gen)}")

# Hours cross-check against the app
src = open(f"{REPO}/Tradewatch/ViewModels/MainViewModel.cs", encoding="utf-8").read()
print("\n== Regular hours that differ from the library (app vs library, local time) ==")
seen = set()
for m in re.finditer(r'Name = "([^"]+)".*?Open = new TimeSpan\((\d+),(\d+),0\), Close = new TimeSpan\((\d+),(\d+),0\)', src):
    name = m.group(1)
    mic = MIC.get(name)
    if not mic or name in seen:
        continue
    seen.add(name)
    try:
        cal = xcals.get_calendar(mic, start="2026-10-01", end="2026-12-31")
    except Exception:
        cal = xcals.get_calendar(mic, start="2026-10-01", end="2026-12-31")
    o = cal.open_times[-1][1]
    c = cal.close_times[-1][1]
    app_o = f"{int(m.group(2)):02d}:{m.group(3)}"
    app_c = f"{int(m.group(4)):02d}:{m.group(5)}"
    lib_o, lib_c = o.strftime("%H:%M"), c.strftime("%H:%M")
    if (app_o, app_c) != (lib_o, lib_c):
        print(f"{name}: app {app_o}-{app_c}  library {lib_o}-{lib_c}")

json.dump(gen, open(f"{SCRATCH}/holidays.new.json", "w", encoding="utf-8"), ensure_ascii=False)
