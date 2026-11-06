"""Bounded metadata/UI CDP operations on the existing Chrome MJ tab.

Never downloads images, restarts a browser, or launches generation jobs.
"""
import argparse
import json
import os
import sys
import time
import urllib.request
from pathlib import Path

import websocket


def main():
    sys.stdout.reconfigure(encoding="utf-8")
    os.environ["NO_PROXY"] = "localhost,127.0.0.1"
    parser = argparse.ArgumentParser()
    parser.add_argument("--eval")
    parser.add_argument("--file")
    parser.add_argument("--stdin", action="store_true")
    parser.add_argument("--out")
    parser.add_argument("--method")
    parser.add_argument("--params", default="{}")
    parser.add_argument("--timeout", type=int, default=25)
    args = parser.parse_args()
    opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))
    with opener.open("http://127.0.0.1:9223/json/list", timeout=5) as response:
        tabs = json.load(response)
    tab = next(t for t in tabs if t.get("type") == "page" and "https://www.midjourney.com/" in t.get("url", ""))
    ws = websocket.create_connection(tab["webSocketDebuggerUrl"], timeout=args.timeout, suppress_origin=True)
    try:
        if args.method:
            method, params = args.method, json.loads(sys.stdin.read().lstrip("\ufeff") if args.stdin else args.params)
        else:
            expression = sys.stdin.read() if args.stdin else Path(args.file).read_text(encoding="utf-8") if args.file else args.eval
            method = "Runtime.evaluate"
            params = {"expression": expression, "awaitPromise": True, "returnByValue": True,
                      "userGesture": True, "timeout": (args.timeout - 2) * 1000}
        ws.send(json.dumps({"id": 1, "method": method, "params": params}))
        deadline = time.monotonic() + args.timeout
        while time.monotonic() < deadline:
            ws.settimeout(max(0.1, deadline - time.monotonic()))
            message = json.loads(ws.recv())
            if message.get("id") != 1:
                continue
            if "error" in message:
                raise RuntimeError(message["error"])
            result = message.get("result", {})
            if "exceptionDetails" in result:
                raise RuntimeError(result["exceptionDetails"])
            value = result.get("result", {}).get("value") if method == "Runtime.evaluate" else result
            if args.out:
                Path(args.out).write_text(json.dumps(value, ensure_ascii=False, indent=2), encoding="utf-8")
                print(json.dumps({"saved": args.out, "items": len(value) if isinstance(value, (list, dict)) else None}))
            else:
                print(json.dumps(value, ensure_ascii=False))
            return
        raise TimeoutError("CDP command deadline expired")
    finally:
        ws.close()


if __name__ == "__main__":
    main()
