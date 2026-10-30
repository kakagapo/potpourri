#!/usr/bin/env python
"""Repair a truncated HAR by retaining only complete entries."""

from __future__ import annotations

import argparse
import mmap
import re
import sys
from pathlib import Path


ENTRIES_PATTERN = re.compile(rb'"entries"\s*:\s*\[')


def find_complete_entries(data: mmap.mmap, array_start: int) -> tuple[int, int]:
    in_string = False
    escaped = False
    depth = 0
    entry_started = False
    complete_entries = 0
    last_complete_end = -1

    for position in range(array_start, len(data)):
        byte = data[position]

        if in_string:
            if escaped:
                escaped = False
            elif byte == 0x5C:  # Backslash
                escaped = True
            elif byte == 0x22:  # Double quote
                in_string = False
            continue

        if byte == 0x22:
            in_string = True
            continue

        if not entry_started:
            if byte in b" \t\r\n,":
                continue
            if byte == 0x5D:  # Closing bracket
                break
            if byte != 0x7B:  # Opening brace
                raise ValueError(
                    f"Expected a HAR entry object at byte {position}, "
                    f"but found byte 0x{byte:02x}"
                )
            entry_started = True
            depth = 1
            continue

        if byte in (0x7B, 0x5B):  # Opening brace or bracket
            depth += 1
        elif byte in (0x7D, 0x5D):  # Closing brace or bracket
            depth -= 1
            if depth == 0:
                complete_entries += 1
                last_complete_end = position + 1
                entry_started = False

    if last_complete_end < 0:
        raise ValueError("No complete entries were found in the HAR entries array")
    return complete_entries, last_complete_end


def repair_har(source: Path, destination: Path) -> int:
    with source.open("rb") as input_file:
        with mmap.mmap(input_file.fileno(), 0, access=mmap.ACCESS_READ) as data:
            match = ENTRIES_PATTERN.search(data)
            if match is None:
                raise ValueError('Could not find the HAR "entries" array')

            entries_array_start = match.end()
            entry_count, last_complete_end = find_complete_entries(
                data, entries_array_start
            )

            destination.parent.mkdir(parents=True, exist_ok=True)
            with destination.open("wb") as output_file:
                output_file.write(data[:last_complete_end])
                output_file.write(b"\n    ]\n  }\n}\n")

    return entry_count


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Repair a truncated HAR by dropping its incomplete final entry."
    )
    parser.add_argument("source", type=Path, help="Path to the truncated HAR/JSON file")
    parser.add_argument("destination", type=Path, help="Path for the repaired .har file")
    args = parser.parse_args()

    if not args.source.is_file():
        parser.error(f"Source file does not exist: {args.source}")
    if args.source.resolve() == args.destination.resolve():
        parser.error("Source and destination must be different files")

    try:
        entry_count = repair_har(args.source, args.destination)
    except (OSError, ValueError) as error:
        print(f"Unable to repair HAR: {error}", file=sys.stderr)
        return 1

    print(f"Repaired HAR written to: {args.destination}")
    print(f"Complete entries retained: {entry_count}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
