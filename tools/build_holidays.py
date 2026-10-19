"""Build Tradewatch/holidays.json: holiday closures (with names) and early closes.

Sources
- exchange_calendars: trading calendars for ~60 exchanges (closures + early closes)
- holidays: country public holidays, used for English holiday names everywhere and
  as the closure list for exchanges exchange_calendars doesn't cover (approximate)

Usage (from the trade-watch folder):
    python -m venv .venv
    .venv/Scripts/python -m pip install exchange_calendars holidays
    .venv/Scripts/python tools/build_holidays.py Tradewatch/holidays.json

Output format:
    { "<exchange name>": { "holidays": {"yyyy-mm-dd": "name"},
                           "earlyCloses": {"yyyy-mm-dd": "HH:mm"} } }
"""
import json
import sys
import warnings
from datetime import date, timedelta

import holidays
import pandas as pd
import exchange_calendars as xcals

warnings.filterwarnings("ignore")

START, END = "2026-01-01", "2027-12-31"
YEARS = (2026, 2027)
FRI_SAT = {4, 5}  # Python weekday(): Mon=0 .. Sun=6
SAT_SUN = {5, 6}

# exchange name -> (exchange_calendars code or None, country code, weekend days)
# Weekend days must match MainViewModel.
EXCHANGES = {
    "New York Stock Exchange (NYSE)": ("XNYS", "US", SAT_SUN),
    "NASDAQ": ("XNYS", "US", SAT_SUN),
    "Toronto Stock Exchange (TSX)": ("XTSE", "CA", SAT_SUN),
    "Bolsa Mexicana de Valores (BMV)": ("XMEX", "MX", SAT_SUN),
    "Bermuda Stock Exchange (BSX)": ("XBDA", "BM", SAT_SUN),
    "São Paulo Stock Exchange (B3)": ("BVMF", "BR", SAT_SUN),
    "Buenos Aires Stock Exchange (BCBA)": ("XBUE", "AR", SAT_SUN),
    "Santiago Stock Exchange (BCS)": ("XSGO", "CL", SAT_SUN),
    "Bolsa de Valores de Colombia (BVC)": ("XBOG", "CO", SAT_SUN),
    "Bolsa de Valores de Lima (BVL)": ("XLIM", "PE", SAT_SUN),
    "Jamaica Stock Exchange (JSE)": (None, "JM", SAT_SUN),
    "Trinidad and Tobago Stock Exchange (TTSE)": (None, "TT", SAT_SUN),
    "Barbados Stock Exchange (BDE)": (None, "BB", SAT_SUN),
    "Panama Stock Exchange (BVPSI)": (None, "PA", SAT_SUN),
    "London Stock Exchange (LSE)": ("XLON", "GB", SAT_SUN),
    "Euronext Paris": ("XPAR", "FR", SAT_SUN),
    "Deutsche Börse (Frankfurt)": ("XETR", "DE", SAT_SUN),
    "SIX Swiss Exchange (Zurich)": ("XSWX", "CH", SAT_SUN),
    "Madrid Stock Exchange (Bolsa de Madrid)": ("XMAD", "ES", SAT_SUN),
    "Borsa Italiana (Milan)": ("XMIL", "IT", SAT_SUN),
    "Euronext Amsterdam": ("XAMS", "NL", SAT_SUN),
    "Euronext Brussels": ("XBRU", "BE", SAT_SUN),
    "Euronext Dublin (ISEQ)": ("XDUB", "IE", SAT_SUN),
    "Euronext Lisbon (PSI)": ("XLIS", "PT", SAT_SUN),
    "Vienna Stock Exchange (WBAG)": ("XWBO", "AT", SAT_SUN),
    "Oslo Stock Exchange (OSE)": ("XOSL", "NO", SAT_SUN),
    "Stockholm Stock Exchange (OMX)": ("XSTO", "SE", SAT_SUN),
    "Luxembourg Stock Exchange (LuxSE)": ("XLUX", "LU", SAT_SUN),
    "Malta Stock Exchange": (None, "MT", SAT_SUN),
    "Nasdaq Iceland": ("XICE", "IS", SAT_SUN),
    "Helsinki Stock Exchange (NASDAQ OMX)": ("XHEL", "FI", SAT_SUN),
    "Nasdaq Tallinn": ("XTAL", "EE", SAT_SUN),
    "Nasdaq Riga": ("XRIS", "LV", SAT_SUN),
    "Nasdaq Vilnius": ("XLIT", "LT", SAT_SUN),
    "Athens Stock Exchange (ATHEX)": ("ASEX", "GR", SAT_SUN),
    "Warsaw Stock Exchange (GPW)": ("XWAR", "PL", SAT_SUN),
    "Prague Stock Exchange (PSE)": ("XPRA", "CZ", SAT_SUN),
    "Budapest Stock Exchange (BSE)": ("XBUD", "HU", SAT_SUN),
    "Bucharest Stock Exchange (BVB)": ("XBSE", "RO", SAT_SUN),
    "Zagreb Stock Exchange (ZSE)": ("XZAG", "HR", SAT_SUN),
    "Sofia Stock Exchange": (None, "BG", SAT_SUN),
    "Cyprus Stock Exchange (CSE)": ("XCYS", "CY", SAT_SUN),
    "Borsa Istanbul (BIST)": ("XIST", "TR", SAT_SUN),
    "Moscow Exchange (MOEX)": ("XMOS", "RU", SAT_SUN),
    "Nasdaq Copenhagen (XCSE)": ("XCSE", "DK", SAT_SUN),
    "Belgrade Stock Exchange (BELEX)": ("XBEL", "RS", SAT_SUN),
    "Ljubljana Stock Exchange (LJSE)": ("XLJU", "SI", SAT_SUN),
    "Saudi Stock Exchange (Tadawul)": ("XSAU", "SA", FRI_SAT),
    "Qatar Stock Exchange (QSE)": (None, "QA", FRI_SAT),
    "Kuwait Boursa": (None, "KW", FRI_SAT),
    "Bahrain Bourse (BHB)": (None, "BH", FRI_SAT),
    "Muscat Securities Market (MSM)": (None, "OM", FRI_SAT),
    "Egyptian Exchange (EGX)": (None, "EG", FRI_SAT),
    "Amman Stock Exchange (ASE)": (None, "JO", FRI_SAT),
    "Palestine Exchange (PEX)": (None, "PS", FRI_SAT),
    "Iraq Stock Exchange (ISX)": (None, "IQ", FRI_SAT),
    "Tel Aviv Stock Exchange (TASE)": ("XTAE", "IL", SAT_SUN),
    "Dubai Financial Market (DFM)": (None, "AE", SAT_SUN),
    "Abu Dhabi Securities Exchange (ADX)": (None, "AE", SAT_SUN),
    "Beirut Stock Exchange (BSE)": (None, "LB", SAT_SUN),
    "Casablanca Stock Exchange": (None, "MA", SAT_SUN),
    "Tunis Stock Exchange (BVMT)": (None, "TN", SAT_SUN),
    "Johannesburg Stock Exchange (JSE)": ("XJSE", "ZA", SAT_SUN),
    "Nigeria Exchange Group (NGX)": (None, "NG", SAT_SUN),
    "Nairobi Securities Exchange (NSE)": (None, "KE", SAT_SUN),
    "Ghana Stock Exchange (GSE)": (None, "GH", SAT_SUN),
    "BRVM (West Africa)": (None, "CI", SAT_SUN),  # BRVM is in Abidjan
    "Stock Exchange of Mauritius (SEM)": (None, "MU", SAT_SUN),
    "Botswana Stock Exchange (BSE)": (None, "BW", SAT_SUN),
    "Zimbabwe Stock Exchange (ZSE)": (None, "ZW", SAT_SUN),
    "Dar es Salaam Stock Exchange (DSE)": (None, "TZ", SAT_SUN),
    "Lusaka Securities Exchange (LuSE)": (None, "ZM", SAT_SUN),
    "Uganda Securities Exchange (USE)": (None, "UG", SAT_SUN),
    "Rwanda Stock Exchange (RSE)": (None, "RW", SAT_SUN),
    "Namibia Stock Exchange (NSX)": (None, "NA", SAT_SUN),
    "Bombay Stock Exchange (BSE)": ("XBOM", "IN", SAT_SUN),
    "National Stock Exchange of India (NSE)": ("XBOM", "IN", SAT_SUN),
    "Pakistan Stock Exchange (PSX)": ("XKAR", "PK", SAT_SUN),
    "Colombo Stock Exchange (CSE)": (None, "LK", SAT_SUN),
    "Dhaka Stock Exchange (DSE)": (None, "BD", FRI_SAT),
    "Nepal Stock Exchange (NEPSE)": (None, "NP", FRI_SAT),
    "Shanghai Stock Exchange (SSE)": ("XSHG", "CN", SAT_SUN),
    "Shenzhen Stock Exchange (SZSE)": ("XSHG", "CN", SAT_SUN),
    "Hong Kong Stock Exchange (HKEX)": ("XHKG", "HK", SAT_SUN),
    "Tokyo Stock Exchange (TSE)": ("XTKS", "JP", SAT_SUN),
    "Korea Exchange (KRX)": ("XKRX", "KR", SAT_SUN),
    "Taiwan Stock Exchange (TWSE)": ("XTAI", "TW", SAT_SUN),
    "Mongolia Stock Exchange (MSE)": (None, "MN", SAT_SUN),
    "Kazakhstan Stock Exchange (KASE)": (None, "KZ", SAT_SUN),
    "Beijing Stock Exchange (BSE)": ("XSHG", "CN", SAT_SUN),
    "Baku Stock Exchange (BSEX)": (None, "AZ", SAT_SUN),
    "Tashkent Stock Exchange (UZSE)": (None, "UZ", SAT_SUN),
    "Singapore Exchange (SGX)": ("XSES", "SG", SAT_SUN),
    "Bursa Malaysia (KLSE)": ("XKLS", "MY", SAT_SUN),
    "Philippine Stock Exchange (PSE)": ("XPHS", "PH", SAT_SUN),
    "Stock Exchange of Thailand (SET)": ("XBKK", "TH", SAT_SUN),
    "Ho Chi Minh Stock Exchange (HOSE)": (None, "VN", SAT_SUN),
    "Hanoi Stock Exchange (HNX)": (None, "VN", SAT_SUN),
    "Indonesia Stock Exchange (IDX)": ("XIDX", "ID", SAT_SUN),
    "Australian Securities Exchange (ASX)": ("XASX", "AU", SAT_SUN),
    "Australian Securities Exchange (ASX 24 Futures)": ("XASX", "AU", SAT_SUN),
    "New Zealand Exchange (NZX)": ("XNZE", "NZ", SAT_SUN),
    "PNGX Markets (Papua New Guinea)": (None, "PG", SAT_SUN),
}

# These calendars' data stops at end-2026, so 2027 is supplied by hand.
MANUAL_2027 = {
    # SGX: Singapore MOM gazetted 2027 public holidays (official), incl. 8 Feb in lieu
    "XSES": ["2027-01-01", "2027-02-08", "2027-03-10", "2027-03-26", "2027-05-17",
             "2027-05-20", "2027-08-09", "2027-10-28"],
    # NSE/BSE: ESTIMATE until NSE's circular (~Dec 2026)
    "XBOM": ["2027-01-26", "2027-03-10", "2027-03-22", "2027-03-26", "2027-04-14",
             "2027-04-15", "2027-04-19", "2027-05-17", "2027-06-15", "2027-10-29"],
    # SSE/SZSE/BSE: ESTIMATE until the State Council schedule (~Nov-Dec 2026)
    "XSHG": ["2027-01-01", "2027-02-05", "2027-02-08", "2027-02-09", "2027-02-10",
             "2027-02-11", "2027-04-05", "2027-05-03", "2027-05-04", "2027-06-09",
             "2027-09-15", "2027-10-01", "2027-10-04", "2027-10-05", "2027-10-06",
             "2027-10-07"],
}
MANUAL_EARLY_2027 = {
    # SGX half days: eve of Lunar New Year, Christmas Eve, New Year's Eve
    "XSES": {"2027-02-05": "12:00", "2027-12-24": "12:00", "2027-12-31": "12:00"},
}
# Library bugs patched by hand: (calendar, date) closures it's missing.
EXTRA_CLOSURES = {
    "XTAL": ["2026-12-25"], "XRIS": ["2026-12-25"], "XLIT": ["2026-12-25"],
}
MANUAL_NAMES = {"2027-02-05": "Chinese New Year's Eve"}


def english_holidays(cc):
    cal = holidays.country_holidays(cc, years=YEARS)
    lang = next((l for l in (cal.supported_languages or ()) if l.startswith("en")), None)
    if lang:
        cal = holidays.country_holidays(cc, years=YEARS, language=lang)
    return {d.isoformat(): name for d, name in cal.items()}


def from_calendar(mic):
    """(closure dates, regular-holiday names, early closes) from exchange_calendars."""
    try:
        cal = xcals.get_calendar(mic, start="2025-06-01", end="2028-01-31")
        end = END
    except Exception:  # data stops at end-2026
        cal = xcals.get_calendar(mic, start="2025-06-01", end="2026-12-31")
        end = "2026-12-31"
    sessions = set(cal.sessions_in_range(START, end).strftime("%Y-%m-%d"))
    closed = [d.strftime("%Y-%m-%d") for d in pd.date_range(START, end)
              if cal.weekmask[d.dayofweek] == "1" and d.strftime("%Y-%m-%d") not in sessions]
    names = {}
    if cal.regular_holidays is not None:
        for d, n in cal.regular_holidays.holidays(START, end, return_name=True).items():
            names[d.strftime("%Y-%m-%d")] = n
    early = {s.strftime("%Y-%m-%d"): cal.session_close(s).tz_convert(cal.tz).strftime("%H:%M")
             for s in cal.early_closes if START <= s.strftime("%Y-%m-%d") <= end}
    closed += MANUAL_2027.get(mic, []) + EXTRA_CLOSURES.get(mic, [])
    early.update(MANUAL_EARLY_2027.get(mic, {}))
    if mic == "XTAE":  # regular short Fridays are modelled by FridayClose in the app
        early = {d: t for d, t in early.items() if pd.Timestamp(d).dayofweek != 4}
    return sorted(set(closed)), names, early


def nearby_name(day, public, max_days=3):
    """Exchanges often close around a holiday (Eid, Golden Week): borrow the closest name."""
    d0 = date.fromisoformat(day)
    for offset in range(1, max_days + 1):
        for d in (d0 - timedelta(days=offset), d0 + timedelta(days=offset)):
            if d.isoformat() in public:
                base = public[d.isoformat()].split(" (")[0].split(";")[0]
                return base if base.lower().endswith("holiday") else base + " holiday"
    return None


def main(path):
    out, cal_cache, country_cache = {}, {}, {}
    for name, (mic, cc, weekend) in EXCHANGES.items():
        if cc not in country_cache:
            country_cache[cc] = english_holidays(cc)
        public = country_cache[cc]
        if mic:
            if mic not in cal_cache:
                cal_cache[mic] = from_calendar(mic)
            closed, cal_names, early = cal_cache[mic]
        else:
            closed = sorted(d for d in public if date.fromisoformat(d).weekday() not in weekend)
            cal_names, early = {}, {}
        hol = {d: public.get(d) or cal_names.get(d) or MANUAL_NAMES.get(d)
                  or nearby_name(d, public) or "Market holiday"
               for d in closed}
        out[name] = {"holidays": hol, "earlyCloses": dict(sorted(early.items()))}
        print(f"{name}: {len(hol)} closures, {len(early)} early closes", file=sys.stderr)

    with open(path, "w", encoding="utf-8", newline="\n") as f:
        json.dump(out, f, ensure_ascii=False, indent=2)
        f.write("\n")


if __name__ == "__main__":
    main(sys.argv[1])
