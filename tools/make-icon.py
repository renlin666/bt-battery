#!/usr/bin/env python3
"""画一个蓝色圆角方块上的白色蓝牙符，写成多尺寸 ICO。"""

import struct
import zlib
from pathlib import Path


def chunk(tag: bytes, data: bytes) -> bytes:
    return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)


def png(size: int, pixels: bytes) -> bytes:
    raw = b"".join(b"\x00" + pixels[y * size * 4 : (y + 1) * size * 4] for y in range(size))
    ihdr = struct.pack(">IIBBBBB", size, size, 8, 6, 0, 0, 0)
    return b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", ihdr) + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b"")


def dist_point_segment(px, py, ax, ay, bx, by) -> float:
    dx, dy = bx - ax, by - ay
    length = dx * dx + dy * dy
    if length == 0:
        return ((px - ax) ** 2 + (py - ay) ** 2) ** 0.5
    t = max(0.0, min(1.0, ((px - ax) * dx + (py - ay) * dy) / length))
    qx, qy = ax + t * dx, ay + t * dy
    return ((px - qx) ** 2 + (py - qy) ** 2) ** 0.5


def render(size: int) -> bytes:
    buf = bytearray(size * size * 4)
    radius = size * 0.22
    stroke = max(1.4, size * 0.075)
    # 符文用 0-1 坐标，和运行时托盘图标是同一形状。
    lines = [
        ((0.50, 0.16), (0.74, 0.34)),
        ((0.74, 0.34), (0.50, 0.50)),
        ((0.50, 0.50), (0.74, 0.66)),
        ((0.74, 0.66), (0.50, 0.84)),
        ((0.50, 0.84), (0.50, 0.16)),
        ((0.28, 0.34), (0.74, 0.66)),
        ((0.28, 0.66), (0.74, 0.34)),
    ]
    for y in range(size):
        for x in range(size):
            nx = (x + 0.5) / size
            ny = (y + 0.5) / size
            # 圆角矩形的覆盖：用到边的距离近似。
            inset = 0.08
            cx = min(max(nx, inset + radius / size), 1 - inset - radius / size)
            cy = min(max(ny, inset + radius / size), 1 - inset - radius / size)
            # 更直接：圆角方块 SDF
            half = 0.5 - inset
            qx = abs(nx - 0.5) - half + radius / size
            qy = abs(ny - 0.5) - half + radius / size
            outside = (max(qx, 0) ** 2 + max(qy, 0) ** 2) ** 0.5 + min(max(qx, qy), 0) - radius / size
            if outside > 0.6 / size:
                continue
            edge = max(0.0, min(1.0, 0.5 - outside * size))
            r, g, b = 0, 103, 192
            alpha = int(255 * edge)
            nearest = min(dist_point_segment(nx, ny, a[0], a[1], b_[0], b_[1]) for a, b_ in lines)
            if nearest < stroke / size:
                cover = max(0.0, min(1.0, (stroke / size - nearest) * size))
                r = g = b = int(255 * cover + (1 - cover) * 0)
                # 白色符文盖在蓝色上
                base_r, base_g, base_b = 0, 103, 192
                r = int(base_r * (1 - cover) + 255 * cover)
                g = int(base_g * (1 - cover) + 255 * cover)
                b = int(base_b * (1 - cover) + 255 * cover)
            i = (y * size + x) * 4
            buf[i : i + 4] = bytes((r, g, b, alpha))
    return bytes(buf)


def ico(images: list[tuple[int, bytes]]) -> bytes:
    count = len(images)
    header = struct.pack("<HHH", 0, 1, count)
    offset = 6 + 16 * count
    entries = b""
    body = b""
    for size, data in images:
        width = 0 if size >= 256 else size
        entries += struct.pack("<BBBBHHII", width, width, 0, 0, 1, 32, len(data), offset)
        body += data
        offset += len(data)
    return header + entries + body


def main() -> None:
    images = []
    for size in (16, 32, 48, 256):
        images.append((size, png(size, render(size))))
    target = Path(__file__).resolve().parents[1] / "src" / "BtBattery.App" / "Assets" / "app.ico"
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_bytes(ico(images))
    print(target, target.stat().st_size)


if __name__ == "__main__":
    main()
