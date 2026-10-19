"""Generate Tradewatch holidays (2026-2027) from exchange_calendars.

Output: JSON {exchange name: [yyyy-mm-dd, ...]} on stdout, plus a report on stderr
with each calendar's weekmask and last session so gaps are visible.
"""
import json
import sys
import warnings

import pandas as pd
import exchange_calendars as xcals

warnings.filterwarnings("ignore")

START, END = "2026-01-01", "2027-12-31"

MIC = {
    "New York Stock Exchange (NYSE)": "XNYS",
    "NASDAQ": "XNYS",
    "Toronto Stock Exchange (TSX)": "XTSE",
    "Bolsa Mexicana de Valores (BMV)": "XMEX",
    "Bermuda Stock Exchange (BSX)": "XBDA",
    "São Paulo Stock Exchange (B3)": "BVMF",
    "Buenos Aires Stock Exchange (BCBA)": "XBUE",
    "Santiago Stock Exchange (BCS)": "XSGO",
    "Bolsa de Valores de Colombia (BVC)": "XBOG",
    "Bolsa de Valores de Lima (BVL)": "XLIM",
    "London Stock Exchange (LSE)": "XLON",
    "Euronext Paris": "XPAR",
    "Deutsche Börse (Frankfurt)": "XETR",
    "SIX Swiss Exchange (Zurich)": "XSWX",
    "Madrid Stock Exchange (Bolsa de Madrid)": "XMAD",
    "Borsa Italiana (Milan)": "XMIL",
    "Euronext Amsterdam": "XAMS",
    "Euronext Brussels": "XBRU",
    "Euronext Dublin (ISEQ)": "XDUB",
    "Euronext Lisbon (PSI)": "XLIS",
    "Vienna Stock Exchange (WBAG)": "XWBO",
    "Oslo Stock Exchange (OSE)": "XOSL",
    "Stockholm Stock Exchange (OMX)": "XSTO",
    "Luxembourg Stock Exchange (LuxSE)": "XLUX",
    "Nasdaq Iceland": "XICE",
    "Helsinki Stock Exchange (NASDAQ OMX)": "XHEL",
    "Nasdaq Tallinn": "XTAL",
    "Nasdaq Riga": "XRIS",
    "Nasdaq Vilnius": "XLIT",
    "Athens Stock Exchange (ATHEX)": "ASEX",
    "Warsaw Stock Exchange (GPW)": "XWAR",
    "Prague Stock Exchange (PSE)": "XPRA",
    "Budapest Stock Exchange (BSE)": "XBUD",
    "Bucharest Stock Exchange (BVB)": "XBSE",
    "Zagreb Stock Exchange (ZSE)": "XZAG",
    "Cyprus Stock Exchange (CSE)": "XCYS",
    "Borsa Istanbul (BIST)": "XIST",
    "Moscow Exchange (MOEX)": "XMOS",
    "Nasdaq Copenhagen (XCSE)": "XCSE",
    "Belgrade Stock Exchange (BELEX)": "XBEL",
    "Ljubljana Stock Exchange (LJSE)": "XLJU",
    "Saudi Stock Exchange (Tadawul)": "XSAU",
    "Tel Aviv Stock Exchange (TASE)": "XTAE",
    "Johannesburg Stock Exchange (JSE)": "XJSE",
    "Bombay Stock Exchange (BSE)": "XBOM",
    "National Stock Exchange of India (NSE)": "XBOM",
    "Pakistan Stock Exchange (PSX)": "XKAR",
    "Shanghai Stock Exchange (SSE)": "XSHG",
    "Shenzhen Stock Exchange (SZSE)": "XSHG",
    "Beijing Stock Exchange (BSE)": "XSHG",
    "Hong Kong Stock Exchange (HKEX)": "XHKG",
    "Tokyo Stock Exchange (TSE)": "XTKS",
    "Korea Exchange (KRX)": "XKRX",
    "Taiwan Stock Exchange (TWSE)": "XTAI",
    "Singapore Exchange (SGX)": "XSES",
    "Bursa Malaysia (KLSE)": "XKLS",
    "Philippine Stock Exchange (PSE)": "XPHS",
    "Stock Exchange of Thailand (SET)": "XBKK",
    "Indonesia Stock Exchange (IDX)": "XIDX",
    "Australian Securities Exchange (ASX)": "XASX",
    "Australian Securities Exchange (ASX 24 Futures)": "XASX",
    "New Zealand Exchange (NZX)": "XNZE",
}

out, cache = {}, {}
for name, mic in MIC.items():
    if mic not in cache:
        try:
            cal = xcals.get_calendar(mic, start="2025-06-01", end="2028-01-31")
        except Exception as e:  # calendar can't reach END
            print(f"{mic}: FAILED {e}", file=sys.stderr)
            cache[mic] = None
            continue
        sessions = set(cal.sessions_in_range(START, END).strftime("%Y-%m-%d"))
        weekmask = cal.weekmask  # e.g. "1111100" Mon..Sun
        days = pd.date_range(START, END)
        hol = [d.strftime("%Y-%m-%d") for d in days
               if weekmask[d.dayofweek] == "1" and d.strftime("%Y-%m-%d") not in sessions]
        cache[mic] = hol
        print(f"{mic}: weekmask {weekmask}  last session {cal.last_session.date()}  "
              f"holidays 2026={sum(h < '2027' for h in hol)} 2027={sum(h >= '2027' for h in hol)}",
              file=sys.stderr)
    if cache[mic] is not None:
        out[name] = cache[mic]

print(json.dumps(out, ensure_ascii=False))
