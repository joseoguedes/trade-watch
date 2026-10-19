# Holiday data tools

`Tradewatch/holidays.json` is generated from the open-source
[exchange_calendars](https://github.com/gerrymanoim/exchange_calendars) library.

    python -m venv .venv && .venv/Scripts/python -m pip install exchange_calendars
    .venv/Scripts/python tools/gen_holidays.py > tools/generated.json
    .venv/Scripts/python tools/merge.py tools .    # writes tools/holidays.new.json + prints changes and hours mismatches

Then copy `holidays.new.json` over `Tradewatch/holidays.json` (keep the 5-per-line layout).

Hand-maintained bits (in `merge.py`, `MANUAL_2027`):
- SGX 2027 — Singapore MOM gazetted holidays (official).
- NSE/BSE India 2027 — ESTIMATE; replace when NSE publishes its circular (~Dec 2026).
- SSE/SZSE/BSE China 2027 — ESTIMATE; replace when the State Council schedule is out (~Nov-Dec 2026).
- Baltic (Tallinn/Riga/Vilnius) 25 Dec 2026 added by hand — library bug, it shows them open.

Exchanges the library doesn't cover (about 42, mostly Africa / Middle East / smaller markets) have no holiday data.

## Exchanges not in exchange_calendars (41)

    .venv/Scripts/python -m pip install holidays
    .venv/Scripts/python tools/gen_country_holidays.py Tradewatch/holidays.json

Uses each country's public holidays from the `holidays` package (BRVM -> Côte d'Ivoire).
This is an approximation: exchanges can close on extra days (Gulf Eid breaks are often
longer than the public holiday) and Islamic holiday dates are moon-sighting estimates.
Sri Lanka 2027 is empty until its holidays are gazetted — re-run then.
Run this AFTER the exchange_calendars step, since that one rewrites the whole file.
