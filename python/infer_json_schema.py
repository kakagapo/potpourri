#!/usr/bin/env python
"""Infer the structural schema of a large JSON file using streaming parsing."""

from __future__ import annotations

import argparse
import json
import sys
from collections import Counter
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any

try:
    import ijson
except ImportError:
    print(
        "This script requires ijson. Install it with: python -m pip install ijson",
        file=sys.stderr,
    )
    raise SystemExit(2)


@dataclass
class SchemaNode:
    type_counts: Counter[str] = field(default_factory=Counter)
    occurrences: int = 0
    object_count: int = 0
    incomplete_object_count: int = 0
    properties: dict[str, "SchemaNode"] = field(default_factory=dict)
    property_presence: Counter[str] = field(default_factory=Counter)
    item: "SchemaNode | None" = None
    array_count: int = 0
    incomplete_array_count: int = 0
    array_min_length: int | None = None
    array_max_length: int = 0

    def observe(self, value_type: str) -> None:
        self.occurrences += 1
        self.type_counts[value_type] += 1

    def observe_array_length(self, length: int) -> None:
        self.array_count += 1
        self.array_min_length = (
            length
            if self.array_min_length is None
            else min(self.array_min_length, length)
        )
        self.array_max_length = max(self.array_max_length, length)

    def to_dict(self) -> dict[str, Any]:
        result: dict[str, Any] = {
            "types": dict(sorted(self.type_counts.items())),
            "occurrences": self.occurrences,
        }
        if self.object_count:
            result["object_count"] = self.object_count
            result["incomplete_object_count"] = self.incomplete_object_count
            result["properties"] = {
                name: {
                    "presence": self.property_presence[name],
                    "required_in_all_observed_objects": (
                        self.incomplete_object_count == 0
                        and self.property_presence[name] == self.object_count
                    ),
                    **node.to_dict(),
                }
                for name, node in sorted(self.properties.items())
            }
        if self.array_count:
            result["array_count"] = self.array_count
            result["incomplete_array_count"] = self.incomplete_array_count
            result["array_length"] = {
                "minimum": self.array_min_length,
                "maximum": self.array_max_length,
            }
            result["items"] = self.item.to_dict() if self.item else None
        return result


@dataclass
class ContainerFrame:
    node: SchemaNode
    kind: str
    pending_key: str | None = None
    seen_keys: set[str] = field(default_factory=set)
    length: int = 0


def value_type(event: str) -> str:
    return {
        "start_map": "object",
        "start_array": "array",
        "string": "string",
        "number": "number",
        "boolean": "boolean",
        "null": "null",
    }[event]


def target_node(root: SchemaNode, stack: list[ContainerFrame]) -> SchemaNode:
    if not stack:
        return root

    parent = stack[-1]
    if parent.kind == "object":
        if parent.pending_key is None:
            raise ValueError("JSON parser emitted a value without an object key")
        key = parent.pending_key
        parent.pending_key = None
        parent.seen_keys.add(key)
        return parent.node.properties.setdefault(key, SchemaNode())

    parent.length += 1
    if parent.node.item is None:
        parent.node.item = SchemaNode()
    return parent.node.item


def infer_schema(path: Path) -> tuple[SchemaNode, str | None]:
    root = SchemaNode()
    stack: list[ContainerFrame] = []
    root_values = 0

    parse_error = None
    with path.open("rb") as stream:
        try:
            for _prefix, event, value in ijson.parse(stream):
                if event == "map_key":
                    stack[-1].pending_key = value
                    continue

                if event == "end_map":
                    frame = stack.pop()
                    frame.node.object_count += 1
                    frame.node.property_presence.update(frame.seen_keys)
                    continue

                if event == "end_array":
                    frame = stack.pop()
                    frame.node.observe_array_length(frame.length)
                    continue

                if event not in {
                    "start_map",
                    "start_array",
                    "string",
                    "number",
                    "boolean",
                    "null",
                }:
                    continue

                node = target_node(root, stack)
                if not stack:
                    root_values += 1
                    if root_values > 1:
                        raise ValueError(
                            "The input contains more than one top-level JSON value"
                        )

                node.observe(value_type(event))
                if event == "start_map":
                    stack.append(ContainerFrame(node=node, kind="object"))
                elif event == "start_array":
                    stack.append(ContainerFrame(node=node, kind="array"))
        except ijson.JSONError as error:
            parse_error = str(error)

    if stack:
        parse_error = parse_error or "The input ended before all containers were closed"
        while stack:
            frame = stack.pop()
            if frame.kind == "object":
                frame.node.object_count += 1
                frame.node.incomplete_object_count += 1
                frame.node.property_presence.update(frame.seen_keys)
            else:
                frame.node.observe_array_length(frame.length)
                frame.node.incomplete_array_count += 1
    if root_values == 0:
        raise ValueError("The input does not contain a JSON value")
    return root, parse_error


def render_tree(node: SchemaNode, name: str = "$", indent: str = "") -> list[str]:
    types = " | ".join(sorted(node.type_counts))
    details = [f"occurrences={node.occurrences}"]
    if node.object_count:
        details.append(
            f"objects={node.object_count}, incomplete={node.incomplete_object_count}"
        )
    if node.array_count:
        details.append(
            f"arrays={node.array_count}, incomplete={node.incomplete_array_count}, "
            f"length={node.array_min_length}..{node.array_max_length}"
        )
    lines = [f"{indent}{name}: {types} ({'; '.join(details)})"]

    if node.object_count:
        child_indent = indent + "  "
        for property_name, child in sorted(node.properties.items()):
            presence = node.property_presence[property_name]
            required = (
                node.incomplete_object_count == 0 and presence == node.object_count
            )
            marker = "required" if required else f"optional, present {presence}/{node.object_count}"
            child_lines = render_tree(child, property_name, child_indent)
            child_lines[0] += f" [{marker}]"
            lines.extend(child_lines)

    if node.item is not None:
        lines.extend(render_tree(node.item, "[]", indent + "  "))
    return lines


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Infer the schema of a large JSON file with bounded memory usage."
    )
    parser.add_argument("input", type=Path, help="Path to the JSON file")
    parser.add_argument(
        "-o",
        "--output",
        type=Path,
        help="Optional path for a detailed machine-readable schema report",
    )
    args = parser.parse_args()

    if not args.input.is_file():
        parser.error(f"Input file does not exist: {args.input}")

    try:
        schema, parse_error = infer_schema(args.input)
    except (OSError, ValueError) as error:
        print(f"Unable to infer schema: {error}", file=sys.stderr)
        return 1

    if parse_error:
        print(
            "WARNING: Input is incomplete or malformed; reporting the schema "
            f"observed before the parse stopped.\n{parse_error}\n",
            file=sys.stderr,
        )
    print("\n".join(render_tree(schema)))
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        with args.output.open("w", encoding="utf-8") as output:
            json.dump(
                {
                    "input_complete": parse_error is None,
                    "parse_error": parse_error,
                    "schema": schema.to_dict(),
                },
                output,
                indent=2,
                ensure_ascii=True,
            )
            output.write("\n")
        print(f"\nDetailed schema written to: {args.output}", file=sys.stderr)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
