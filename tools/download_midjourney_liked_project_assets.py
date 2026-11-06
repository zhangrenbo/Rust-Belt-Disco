#!/usr/bin/env python3
"""Download liked Midjourney images whose positive prompts match this project.

The script connects to the already-running Edge instance on CDP port 9223. It
uses Midjourney's own Organize-page session to enumerate the account's jobs and
to download original PNG files. Re-running it is safe: valid existing PNGs are
kept and the manifests are rebuilt from the current liked-image state.
"""

from __future__ import annotations

import csv
import base64
import hashlib
import json
import os
import re
import sys
import time
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from urllib.parse import quote

from playwright.sync_api import (
    Error as PlaywrightError,
    Page,
    TimeoutError as PlaywrightTimeoutError,
    sync_playwright,
)


CDP_URL = "http://127.0.0.1:9223"
USER_ID = "f8e3246f-1480-40e8-80a1-ca1d565f673b"
PROJECT_ROOT = Path(__file__).resolve().parents[1]
OUTPUT_ROOT = PROJECT_ROOT / "Assets" / "Art" / "MidjourneyLikedProjectAssets"
IMAGE_ROOT = OUTPUT_ROOT / "Images"
MANIFEST_JSON = OUTPUT_ROOT / "manifest.json"
MANIFEST_CSV = OUTPUT_ROOT / "manifest.csv"

PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"


PERIOD_TERMS = (
    "1980s",
    "1980's",
    "1980",
    "1981",
    "1982",
    "1983",
    "1984",
    "1985",
    "1986",
    "1987",
    "1988",
    "1989",
    "80s",
    "80's",
    "eighties",
    "80年代",
    "八十年代",
)
CHINA_TERMS = (
    "china",
    "chinese",
    "mainland china",
    "northeast china",
    "northeastern china",
    "manchuria",
    "manchurian",
    "中国",
    "中国东北",
    "东北",
)
WINTER_TERMS = (
    "winter",
    "wintry",
    "snow",
    "snowy",
    "snowfall",
    "snowing",
    "snow-covered",
    "snow covered",
    "snowbound",
    "blizzard",
    "冬天",
    "冬季",
    "寒冬",
    "冰雪",
    "雪地",
    "积雪",
)
DISCO_TERMS = ("disco elysium", "disco-elysium", "极乐迪斯科")
REALISM_TERMS = (
    "photoreal",
    "photorealism",
    "photorealistic",
    "photo-real",
    "photo-realism",
    "photo-realistic",
    "pbr",
    "realistic",
    "architectural visualization",
    "architectural visualisation",
    "architectural photography",
)
ARCHITECTURE_TERMS = (
    "architecture",
    "architectural",
    "building",
    "buildings",
    "house",
    "houses",
    "apartment",
    "apartments",
    "residential",
    "factory",
    "factories",
    "street",
    "streets",
    "station",
    "stations",
    "hotel",
    "hotels",
    "shop",
    "shops",
    "storefront",
    "storefronts",
    "convenience store",
    "facade",
    "facades",
    "warehouse",
    "warehouses",
    "hospital",
    "hospitals",
    "school",
    "schools",
    "interior of",
    "restaurant",
    "restaurants",
    "town",
    "towns",
    "village",
    "villages",
    "urban",
    "alley",
    "alleys",
    "courtyard",
    "courtyards",
    "balcony",
    "balconies",
    "garage",
    "garages",
    "market",
    "markets",
    "cinema",
    "cinemas",
    "theater",
    "theaters",
    "theatre",
    "theatres",
    "depot",
    "depots",
    "power plant",
    "steel mill",
    "flour mill",
)


def positive_prompt(command: str) -> str:
    """Ignore Midjourney parameters and their negative-prompt contents."""
    return re.split(r"\s--[a-z]+\b", command, maxsplit=1, flags=re.IGNORECASE)[0].strip()


def contains_term(text: str, term: str) -> bool:
    if any(ord(char) > 127 for char in term):
        return term in text
    return re.search(
        rf"(?<![a-z0-9]){re.escape(term)}(?![a-z0-9])",
        text,
        flags=re.IGNORECASE,
    ) is not None


def contains_any(text: str, terms: tuple[str, ...]) -> bool:
    return any(contains_term(text, term) for term in terms)


def matched_rules(command: str) -> tuple[str, list[str]]:
    prompt = positive_prompt(command)
    rules: list[str] = []
    if contains_any(prompt, PERIOD_TERMS):
        rules.append("80s")
    if contains_any(prompt, CHINA_TERMS):
        rules.append("china")
    if contains_any(prompt, WINTER_TERMS):
        rules.append("winter")
    if contains_any(prompt, DISCO_TERMS):
        rules.append("disco_elysium")
    if contains_any(prompt, REALISM_TERMS) and contains_any(prompt, ARCHITECTURE_TERMS):
        rules.append("realistic_architecture")
    return prompt, rules


def find_midjourney_page(browser) -> Page:
    pages = [page for context in browser.contexts for page in context.pages]
    organize_pages = [page for page in pages if "midjourney.com/organize" in page.url]
    if organize_pages:
        return organize_pages[0]
    midjourney_pages = [page for page in pages if "midjourney.com" in page.url]
    if not midjourney_pages:
        raise RuntimeError("No open Midjourney tab was found in Edge.")
    page = midjourney_pages[0]
    page.goto("https://www.midjourney.com/organize", wait_until="domcontentloaded", timeout=60_000)
    return page


def get_csrf_token(page: Page) -> str:
    captured: dict[str, str | None] = {"value": None}

    def inspect_request(request) -> None:
        if "/api/imagine?" in request.url and not captured["value"]:
            captured["value"] = request.headers.get("x-csrf-protection")

    page.on("request", inspect_request)
    page.reload(wait_until="domcontentloaded", timeout=60_000)
    deadline = time.monotonic() + 15
    while not captured["value"] and time.monotonic() < deadline:
        page.wait_for_timeout(250)
    page.remove_listener("request", inspect_request)
    if not captured["value"]:
        raise RuntimeError("Could not capture Midjourney's session request token.")
    return captured["value"]


def fetch_jobs(page: Page, csrf_token: str) -> list[dict]:
    jobs: list[dict] = []
    cursor: str | None = None
    page_number = 0
    while True:
        url = f"/api/imagine?user_id={USER_ID}&page_size=1000"
        if cursor:
            url += "&cursor=" + quote(cursor, safe="")
        result = page.evaluate(
            """async ({url, token}) => {
                const response = await fetch(url, {
                    headers: {
                        'x-csrf-protection': token,
                        'content-type': 'application/json'
                    }
                });
                return {status: response.status, body: await response.json()};
            }""",
            {"url": url, "token": csrf_token},
        )
        if result["status"] != 200:
            raise RuntimeError(f"Midjourney API returned HTTP {result['status']}: {result['body']}")
        body = result["body"]
        batch = body.get("data", [])
        jobs.extend(batch)
        page_number += 1
        print(f"Indexed page {page_number}: {len(batch)} jobs ({len(jobs)} total)", flush=True)
        cursor = body.get("cursor")
        if not cursor or not batch:
            return jobs


def build_asset_records(jobs: list[dict]) -> list[dict]:
    records: dict[tuple[str, int], dict] = {}
    for job in jobs:
        prompt, rules = matched_rules(job.get("full_command", ""))
        if not rules:
            continue
        ratings = job.get("rating") or {}
        liked_indices = sorted(int(index) for index, value in ratings.items() if value == 5)
        for index in liked_indices:
            job_id = job["id"]
            enqueue_time = job.get("enqueue_time", "")
            date = enqueue_time[:10].replace("-", "") or "unknown_date"
            filename = f"MJ_{date}_{job_id}_i{index}.png"
            records[(job_id, index)] = {
                "job_id": job_id,
                "image_index": index,
                "enqueue_time": enqueue_time,
                "width": job.get("width"),
                "height": job.get("height"),
                "matched_rules": rules,
                "positive_prompt": prompt,
                "full_command": job.get("full_command", ""),
                "source_url": f"https://cdn.midjourney.com/{job_id}/0_{index}.png",
                "job_url": f"https://www.midjourney.com/jobs/{job_id}?index={index}",
                "file": f"Images/{filename}",
            }
    return sorted(records.values(), key=lambda item: (item["enqueue_time"], item["job_id"], item["image_index"]))


def is_valid_png(path: Path) -> bool:
    if not path.is_file() or path.stat().st_size < 1_000:
        return False
    with path.open("rb") as handle:
        return handle.read(8) == PNG_SIGNATURE


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def download_original(page: Page, record: dict, destination: Path) -> None:
    last_error: Exception | None = None
    for attempt in range(1, 6):
        object_url: str | None = None
        try:
            # Fetch first, outside expect_download. If the CDN returns 403/429,
            # this fails immediately instead of waiting for a download event
            # that can never arrive.
            blob_info = page.evaluate(
                """async (url) => {
                    const response = await fetch(url);
                    if (!response.ok) throw new Error(`HTTP ${response.status}`);
                    const blob = await response.blob();
                    if (blob.type !== 'image/png') throw new Error(`Unexpected type ${blob.type}`);
                    return {
                        size: blob.size,
                        type: blob.type,
                        objectUrl: URL.createObjectURL(blob)
                    };
                }""",
                record["source_url"],
            )
            object_url = blob_info["objectUrl"]
            with page.expect_download(timeout=120_000) as download_info:
                page.evaluate(
                    """({objectUrl, filename}) => {
                        const anchor = document.createElement('a');
                        anchor.href = objectUrl;
                        anchor.download = filename;
                        document.body.appendChild(anchor);
                        anchor.click();
                        anchor.remove();
                    }""",
                    {"objectUrl": object_url, "filename": destination.name},
                )
            download = download_info.value
            download.save_as(str(destination))
            if not is_valid_png(destination):
                raise RuntimeError("Downloaded file is not a valid PNG")
            if destination.stat().st_size != blob_info["size"]:
                raise RuntimeError("Downloaded file size does not match the browser response")
            return
        except (PlaywrightError, PlaywrightTimeoutError, OSError, RuntimeError) as error:
            last_error = error
            if destination.exists():
                destination.unlink()
            rate_limited = "HTTP 403" in str(error) or "HTTP 429" in str(error)
            delay_seconds = 15 * attempt if rate_limited else attempt
            print(
                f"  {destination.name}: retry {attempt}/5 in {delay_seconds}s after: {error}",
                flush=True,
            )
            if attempt < 5:
                page.wait_for_timeout(delay_seconds * 1_000)
        finally:
            if object_url:
                try:
                    page.evaluate("url => URL.revokeObjectURL(url)", object_url)
                except PlaywrightError:
                    pass

    # A small number of old CDN objects have occasionally produced an invalid
    # temporary browser-download file even though the fetched response is a
    # valid PNG. Transfer those bytes over CDP as a reliable compatibility path.
    print(f"  Falling back to direct CDP transfer after: {last_error}", flush=True)
    payload = page.evaluate(
        """async (url) => {
            const response = await fetch(url);
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            const bytes = new Uint8Array(await response.arrayBuffer());
            let binary = '';
            const chunkSize = 0x8000;
            for (let offset = 0; offset < bytes.length; offset += chunkSize) {
                binary += String.fromCharCode(...bytes.subarray(offset, offset + chunkSize));
            }
            return {
                size: bytes.length,
                type: response.headers.get('content-type'),
                base64: btoa(binary)
            };
        }""",
        record["source_url"],
    )
    destination.write_bytes(base64.b64decode(payload["base64"], validate=True))
    if payload["type"] != "image/png" or destination.stat().st_size != payload["size"] or not is_valid_png(destination):
        if destination.exists():
            destination.unlink()
        raise RuntimeError(f"CDP fallback also failed for {record['source_url']}")


def write_manifests(records: list[dict], total_jobs: int) -> None:
    rule_counts = Counter(rule for record in records for rule in record["matched_rules"])
    total_bytes = sum(record["bytes"] for record in records)
    document = {
        "generated_at": datetime.now(timezone.utc).isoformat(),
        "source": "Midjourney Organize / Liked",
        "selection": {
            "liked_value": 5,
            "logic": "liked image AND any matched positive-prompt rule",
            "rules": {
                "80s": list(PERIOD_TERMS),
                "china": list(CHINA_TERMS),
                "winter": list(WINTER_TERMS),
                "disco_elysium": list(DISCO_TERMS),
                "realistic_architecture": {
                    "realism_terms": list(REALISM_TERMS),
                    "architecture_terms": list(ARCHITECTURE_TERMS),
                },
            },
            "negative_prompt_and_parameters_ignored": True,
        },
        "summary": {
            "account_jobs_indexed": total_jobs,
            "assets": len(records),
            "bytes": total_bytes,
            "rule_hits": dict(sorted(rule_counts.items())),
        },
        "assets": records,
    }
    MANIFEST_JSON.write_text(json.dumps(document, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    columns = (
        "file",
        "job_id",
        "image_index",
        "enqueue_time",
        "width",
        "height",
        "bytes",
        "sha256",
        "matched_rules",
        "positive_prompt",
        "source_url",
        "job_url",
    )
    with MANIFEST_CSV.open("w", encoding="utf-8-sig", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=columns, extrasaction="ignore")
        writer.writeheader()
        for record in records:
            row = dict(record)
            row["matched_rules"] = "|".join(record["matched_rules"])
            writer.writerow(row)


def main() -> int:
    os.environ.setdefault("NO_PROXY", "localhost,127.0.0.1")
    os.environ.setdefault("no_proxy", "localhost,127.0.0.1")
    IMAGE_ROOT.mkdir(parents=True, exist_ok=True)

    with sync_playwright() as playwright:
        browser = playwright.chromium.connect_over_cdp(CDP_URL)
        page = find_midjourney_page(browser)
        print(f"Connected to {page.url}", flush=True)
        csrf_token = get_csrf_token(page)
        jobs = fetch_jobs(page, csrf_token)
        records = build_asset_records(jobs)
        rule_counts = Counter(rule for record in records for rule in record["matched_rules"])
        print(
            f"Selected {len(records)} liked project images; rule hits: {dict(sorted(rule_counts.items()))}",
            flush=True,
        )

        downloaded = 0
        reused = 0
        for position, record in enumerate(records, start=1):
            destination = OUTPUT_ROOT / record["file"]
            if is_valid_png(destination):
                reused += 1
                action = "kept"
            else:
                download_original(page, record, destination)
                downloaded += 1
                action = "downloaded"
                page.wait_for_timeout(350)
            record["bytes"] = destination.stat().st_size
            record["sha256"] = sha256_file(destination)
            if position == 1 or position % 10 == 0 or position == len(records):
                print(
                    f"[{position}/{len(records)}] {action}: {destination.name} "
                    f"({record['bytes'] / 1024 / 1024:.2f} MiB)",
                    flush=True,
                )

        write_manifests(records, len(jobs))
        total_bytes = sum(record["bytes"] for record in records)
        print(
            f"Complete: {downloaded} downloaded, {reused} kept, "
            f"{total_bytes / 1024 / 1024:.2f} MiB total",
            flush=True,
        )
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except KeyboardInterrupt:
        print("Interrupted; re-run the script to resume.", file=sys.stderr)
        raise SystemExit(130)
