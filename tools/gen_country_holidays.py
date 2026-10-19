"""Fill holidays.json for exchanges exchange_calendars doesn't cover, using each
country's public holidays from the `holidays` package (an approximation: the
exchange may close on extra days, e.g. longer Eid breaks in the Gulf).

Usage: python tools/gen_country_holidays.py Tradewatch/holidays.json
Only touches the exchanges listed below; everything else is left as is.
"""
import json
import sys
from datetime import date, timedelta

import holidays

YEARS = (2026, 2027)
FRI_SAT = {4, 5}  # Python weekday(): Mon=0 .. Sun=6
SAT_SUN = {5, 6}

# exchange name -> (country code, weekend days) ; weekend must match MainViewModel
COUNTRY = {
    "Jamaica Stock Exchange (JSE)": ("JM", SAT_SUN),
    "Trinidad and Tobago Stock Exchange (TTSE)": ("TT", SAT_SUN),
    "Barbados Stock Exchange (BDE)": ("BB", SAT_SUN),
    "Panama Stock Exchange (BVPSI)": ("PA", SAT_SUN),
    "Malta Stock Exchange": ("MT", SAT_SUN),
    "Sofia Stock Exchange": ("BG", SAT_SUN),
    "Qatar Stock Exchange (QSE)": ("QA", FRI_SAT),
    "Kuwait Boursa": ("KW", FRI_SAT),
    "Bahrain Bourse (BHB)": ("BH", FRI_SAT),
    "Muscat Securities Market (MSM)": ("OM", FRI_SAT),
    "Egyptian Exchange (EGX)": ("EG", FRI_SAT),
    "Amman Stock Exchange (ASE)": ("JO", FRI_SAT),
    "Palestine Exchange (PEX)": ("PS", FRI_SAT),
    "Iraq Stock Exchange (ISX)": ("IQ", FRI_SAT),
    "Dubai Financial Market (DFM)": ("AE", SAT_SUN),
    "Abu Dhabi Securities Exchange (ADX)": ("AE", SAT_SUN),
    "Beirut Stock Exchange (BSE)": ("LB", SAT_SUN),
    "Casablanca Stock Exchange": ("MA", SAT_SUN),
    "Tunis Stock Exchange (BVMT)": ("TN", SAT_SUN),
    "Nigeria Exchange Group (NGX)": ("NG", SAT_SUN),
    "Nairobi Securities Exchange (NSE)": ("KE", SAT_SUN),
    "Ghana Stock Exchange (GSE)": ("GH", SAT_SUN),
    "BRVM (West Africa)": ("CI", SAT_SUN),  # BRVM is in Abidjan
    "Stock Exchange of Mauritius (SEM)": ("MU", SAT_SUN),
    "Botswana Stock Exchange (BSE)": ("BW", SAT_SUN),
    "Zimbabwe Stock Exchange (ZSE)": ("ZW", SAT_SUN),
    "Dar es Salaam Stock Exchange (DSE)": ("TZ", SAT_SUN),
    "Lusaka Securities Exchange (LuSE)": ("ZM", SAT_SUN),
    "Uganda Securities Exchange (USE)": ("UG", SAT_SUN),
    "Rwanda Stock Exchange (RSE)": ("RW", SAT_SUN),
    "Namibia Stock Exchange (NSX)": ("NA", SAT_SUN),
    "Colombo Stock Exchange (CSE)": ("LK", SAT_SUN),
    "Dhaka Stock Exchange (DSE)": ("BD", FRI_SAT),
    "Nepal Stock Exchange (NEPSE)": ("NP", FRI_SAT),
    "Mongolia Stock Exchange (MSE)": ("MN", SAT_SUN),
    "Kazakhstan Stock Exchange (KASE)": ("KZ", SAT_SUN),
    "Baku Stock Exchange (BSEX)": ("AZ", SAT_SUN),
    "Tashkent Stock Exchange (UZSE)": ("UZ", SAT_SUN),
    "Ho Chi Minh Stock Exchange (HOSE)": ("VN", SAT_SUN),
    "Hanoi Stock Exchange (HNX)": ("VN", SAT_SUN),
    "PNGX Markets (Papua New Guinea)": ("PG", SAT_SUN),
}


def closures(country, weekend):
    cal = holidays.country_holidays(country, years=YEARS)
    return sorted(d.isoformat() for d in cal if d.weekday() not in weekend)


if __name__ == "__main__":
    path = sys.argv[1]
    data = json.load(open(path, encoding="utf-8-sig"))
    for name, (cc, weekend) in COUNTRY.items():
        data[name] = closures(cc, weekend)
        print(f"{name}: {len(data[name])} closures", file=sys.stderr)

    # keep the file's 5-dates-per-line layout
    lines = ["{"]
    items = list(data.items())
    for i, (k, v) in enumerate(items):
        v = sorted(v)
        rows = [v[j:j + 5] for j in range(0, len(v), 5)]
        body = ",\n".join("    " + ",".join(json.dumps(d) for d in r) for r in rows)
        lines.append(f"  {json.dumps(k, ensure_ascii=False)}: [\n{body}\n  ]" + ("," if i < len(items) - 1 else ""))
    lines.append("}")
    open(path, "w", encoding="utf-8", newline="\n").write("\n".join(lines) + "\n")
