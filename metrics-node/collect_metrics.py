import os
import json
import math
from collections import defaultdict, OrderedDict

import matplotlib
from matplotlib.ticker import FuncFormatter

matplotlib.use("Agg")  
import matplotlib.pyplot as plt
import numpy as np

directories = [
    "blazor-desktop-anniversaries",
    "blazor-desktop-members",
    "blazor-desktop-myothergroups",
    "blazor-desktop-settings",
    "blazor-mobile-anniversaries",
    "blazor-mobile-members",
    "blazor-mobile-myothergroups",
    "blazor-mobile-settings",
    "react-desktop-anniversaries",
    "react-desktop-members",
    "react-desktop-myothergroups",
    "react-desktop-settings",
    "react-mobile-anniversaries",
    "react-mobile-members",
    "react-mobile-myothergroups",
    "react-mobile-settings",
]


def read_webvitals_metrics(dirs, reports_root="reports"):
    results = {}
    for d in dirs:
        path = os.path.join(reports_root, d, "metrics.json")
        if not os.path.isfile(path):
            print(f"[MISS] {path} not found")
            continue
        try:
            with open(path, "r", encoding="utf-8") as f:
                data = json.load(f)
            wv = data.get("webVitals", {})
            results[d] = {
                "FCP": wv.get("FCP"),
                "TTFB": wv.get("TTFB"),
                "LCP": wv.get("LCP"),
                "FID": wv.get("FID"),
            }
        except Exception as e:
            print(f"[ERR ] failed reading {path}: {e}")
    return results


def print_webvitals_summary(wv_results):
    if not wv_results:
        print("No metrics collected.")
        return
    header = f"{'Directory':35} {'FCP(ms)':>10} {'TTFB(ms)':>10} {'LCP(ms)':>10} {'FID(ms)':>10}"
    print("\nWeb Vitals Summary")
    print(header)
    print("-" * len(header))
    for d in sorted(wv_results.keys()):
        m = wv_results[d]
        def fmt(v):
            return f"{v:.2f}" if isinstance(v, (int, float)) else "-"
        print(f"{d:35} {fmt(m['FCP']):>10} {fmt(m['TTFB']):>10} {fmt(m['LCP']):>10} {fmt(m['FID']):>10}")


def generate_webvitals_chart(wv_results, output_path="webvitals_chart.png"):
    if not wv_results:
        return

    metrics = ["FCP", "TTFB", "LCP", "FID"]
    dirs = sorted(wv_results.keys())
    n_dirs = len(dirs)
    n_metrics = len(metrics)

    data = np.full((n_dirs, n_metrics), np.nan)
    for i, d in enumerate(dirs):
        for j, metric in enumerate(metrics):
            val = wv_results[d].get(metric)
            if isinstance(val, (int, float)) and not math.isnan(val):
                data[i, j] = val

    x = np.arange(n_metrics)
    total_group_width = 0.945  
    bar_width = total_group_width / max(n_dirs, 1)
    fig_width = max(11, n_metrics * 1.6) * 1.25  
    fig_height = 6 + (n_dirs > 8) * 1  
    fig, ax = plt.subplots(figsize=(fig_width, fig_height))

    cmap = plt.get_cmap('tab20')
    bars = []
    
    original_values = []  # list of (rect, value)
    for i, d in enumerate(dirs):
        offset = (i - (n_dirs - 1) / 2) * bar_width
        bar_positions = x + offset
        series = ax.bar(bar_positions, data[i], width=bar_width * 0.55, label=d, color=cmap(i % 20))
        bars.append(series)
        for rect, val in zip(series, data[i]):
            original_values.append((rect, val))

    
    fig.canvas.draw()
    axis_height_px = ax.bbox.height
    data_range = ax.get_ylim()[1] - ax.get_ylim()[0]
    data_per_pixel = data_range / axis_height_px if axis_height_px else 0
    data_offset = 10 * data_per_pixel  

    
    for rect, val in original_values:
        if not np.isnan(val):
            rect.set_height((val if val>0 else 0) + data_offset)

    
    current_top = ax.get_ylim()[1]
    ax.set_ylim(0, current_top + data_offset * 0.3)

    
    def value_formatter(y, _pos):
        
        if y < data_offset * 0.5:  
            return ""
        adj = y - data_offset
        if adj < 0:
            adj = 0
        return f"{adj:.0f}" if adj % 1 == 0 else f"{adj:.1f}"
    ax.yaxis.set_major_formatter(FuncFormatter(value_formatter))

    ticks = list(ax.get_yticks())
    if data_offset not in ticks:
        ticks.append(data_offset)
        ticks = sorted(ticks)
        ax.set_yticks(ticks)

    ax.axhline(data_offset, color='black', linewidth=0.8)

    extra_label_offset = 15 * data_per_pixel
    shown_count = 0
    for rect, val in original_values:
        if np.isnan(val):
            continue
        display_height = rect.get_height()
        base_offset = ( (val+data_offset) * 0.02 if val else data_offset * 0.4)
        stagger = extra_label_offset if (shown_count % 2 == 1) else 0
        ax.text(
            rect.get_x() + rect.get_width()/2,
            display_height + base_offset + stagger,
            f"{val:.0f}",
            ha='center', va='bottom', rotation=45, fontsize=8
        )
        shown_count += 1

    ax.set_xticks(x)
    ax.set_xticklabels(metrics, rotation=25)
    ax.set_ylabel('Milliseconds (ms)')
    ax.set_title('WebVitals page navigation metrics for Blazor and React')
    ax.legend(
        ncol=2 if n_dirs > 10 else 1,
        fontsize=8,
        title='Measurement Folder',
        loc='upper left',
        frameon=True,
        framealpha=0.85,
        borderpad=0.5,
        labelspacing=0.4,
        handlelength=1.2,
    )
    ax.margins(y=0.15)
    ax.grid(axis='y', linestyle='--', alpha=0.3)

    if n_metrics:
        left_limit = x[0] - (total_group_width / 2) - 0.05
        right_limit = x[-1] + (total_group_width / 2) + 0.05
        ax.set_xlim(left_limit, right_limit)
    fig.tight_layout()
    fig.savefig(output_path, dpi=150)
    plt.close(fig)
    print(f"Saved chart -> {output_path}")


def _normalize_content_type(ct: str) -> str:
    if not ct:
        return "unknown"
    return ct.split(";", 1)[0].strip().lower() or "unknown"


def read_network_aggregates(dirs, reports_root="reports"):
    """
    Read reports/<dir>/metrics.json for each dir and aggregate networkRequests by content-type.
    Returns:
      agg_by_folder: dict[folder] -> OrderedDict[contentType] -> {count, total_duration_ms, total_size_bytes}
    """
    agg_by_folder = {}
    for d in dirs:
        path = os.path.join(reports_root, d, "metrics.json")
        if not os.path.isfile(path):
            continue
        try:
            with open(path, "r", encoding="utf-8") as f:
                data = json.load(f)
        except Exception as e:
            print(f"[ERR ] failed reading {path}: {e}")
            continue

        by_ct = defaultdict(lambda: {"count": 0, "total_duration_ms": 0.0, "total_size_bytes": 0})
        for req in data.get("networkRequests", []):
            ct = _normalize_content_type(req.get("contentType", ""))
            start = req.get("startTime")
            end = req.get("endTime")
            duration = req.get("duration")
            if duration is None and start is not None and end is not None:
                try:
                    duration = float(end) - float(start)
                except Exception:
                    duration = None
            if duration is None:
                continue
            size = req.get("size", 0) or 0

            bucket = by_ct[ct]
            bucket["count"] += 1
            bucket["total_duration_ms"] += float(duration)
            try:
                bucket["total_size_bytes"] += int(size)
            except Exception:
                try:
                    bucket["total_size_bytes"] += int(float(size))
                except Exception:
                    pass

        ordered = OrderedDict(sorted(by_ct.items(), key=lambda kv: (-kv[1]["total_duration_ms"], kv[0])))
        agg_by_folder[d] = ordered

    return agg_by_folder


def write_aggregates_table(agg_by_folder, output_path="network-aggregates-all.txt"):
    lines = []
    lines.append("# Network aggregates across all folders (by content-type)")
    lines.append("Folder\tContent-Type\tCount\tTotal Duration (ms)\tTotal Size (KB)\tAvg Duration (ms)\tAvg Size (KB)")
    for folder in directories:
        folder_aggr = agg_by_folder.get(folder, {})
        if not folder_aggr:
            continue
        for ct, v in folder_aggr.items():
            count = v["count"]
            total_dur = round(v["total_duration_ms"], 2)
            total_kb = round(v["total_size_bytes"] / 1024.0, 2)
            avg_dur = round(total_dur / count, 2) if count else 0.0
            avg_kb = round(total_kb / count, 2) if count else 0.0
            lines.append(f"{folder}\t{ct}\t{count}\t{total_dur}\t{total_kb}\t{avg_dur}\t{avg_kb}")

    with open(output_path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")
    print(f"Saved aggregates table -> {output_path}")


def plot_aggregates_heatmap(agg_by_folder, output_path="network-aggregates-all.png"):
    ct_set = set()
    for v in agg_by_folder.values():
        ct_set.update(v.keys())
    content_types = sorted(ct_set)
    if not content_types:
        print("No network request aggregates to plot.")
        return

    rows = [d for d in directories if d in agg_by_folder]
    if not rows:
        print("No folders with aggregates to plot.")
        return

    dur_mat = np.zeros((len(rows), len(content_types)), dtype=float)
    size_mat = np.zeros((len(rows), len(content_types)), dtype=float)

    for i, folder in enumerate(rows):
        by_ct = agg_by_folder.get(folder, {})
        for j, ct in enumerate(content_types):
            v = by_ct.get(ct)
            if not v:
                continue
            dur_mat[i, j] = v["total_duration_ms"]
            size_mat[i, j] = v["total_size_bytes"] / 1024.0  # KB

    n_rows = len(rows)
    n_cols = len(content_types)
    fig_w = max(12, 1.2 * n_cols)
    fig_h = max(8, 0.5 * n_rows)
    fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(fig_w, fig_h), constrained_layout=True)

    cmap = plt.cm.YlOrRd

    im1 = ax1.imshow(dur_mat, aspect='auto', cmap=cmap)
    ax1.set_title('Total Duration (ms) by Content-Type and Folder')
    ax1.set_xlabel('Content-Type')
    ax1.set_ylabel('Folder')
    ax1.set_yticks(np.arange(n_rows))
    ax1.set_yticklabels(rows)
    ax1.set_xticks(np.arange(n_cols))
    ax1.set_xticklabels(content_types, rotation=45, ha='right')
    cbar1 = fig.colorbar(im1, ax=ax1, fraction=0.046, pad=0.04)
    cbar1.ax.set_ylabel('ms', rotation=90, va='bottom')

    im2 = ax2.imshow(size_mat, aspect='auto', cmap=cmap)
    ax2.set_title('Total Size (KB) by Content-Type and Folder')
    ax2.set_xlabel('Content-Type')
    ax2.set_ylabel('Folder')
    ax2.set_yticks(np.arange(n_rows))
    ax2.set_yticklabels(rows)
    ax2.set_xticks(np.arange(n_cols))
    ax2.set_xticklabels(content_types, rotation=45, ha='right')
    cbar2 = fig.colorbar(im2, ax=ax2, fraction=0.046, pad=0.04)
    cbar2.ax.set_ylabel('KB', rotation=90, va='bottom')

    try:
        def annotate_top(ax, mat, top_k=20, fmt="{:.0f}"):
            flat = mat.flatten()
            if flat.size == 0:
                return
            idx = np.argpartition(flat, -min(top_k, flat.size))[-min(top_k, flat.size):]
            for k in idx:
                i = k // mat.shape[1]
                j = k % mat.shape[1]
                val = mat[i, j]
                if val <= 0:
                    continue
                ax.text(j, i, fmt.format(val), ha='center', va='center', fontsize=7, color='black')

        annotate_top(ax1, dur_mat, top_k=25, fmt="{:.0f}")
        annotate_top(ax2, size_mat, top_k=25, fmt="{:.0f}")
    except Exception:
        pass

    fig.suptitle('Network Aggregates (rows = folders, columns = content types)')
    fig.savefig(output_path, dpi=150)
    plt.close(fig)
    print(f"Saved network aggregates heatmap -> {output_path}")


def generate_table_image(agg_by_folder, output_path="network-aggregates-all-table.png"):
    headers = [
        "Folder",
        "Content-Type",
        "Count",
        "Total Duration (ms)",
        "Total Size (KB)",
        "Avg Duration (ms)",
        "Avg Size (KB)",
    ]

    rows = []
    for folder in directories:
        folder_aggr = agg_by_folder.get(folder, {})
        for ct, v in folder_aggr.items():
            count = v["count"]
            total_dur = round(v["total_duration_ms"], 2)
            total_kb = round(v["total_size_bytes"] / 1024.0, 2)
            avg_dur = round(total_dur / count, 2) if count else 0.0
            avg_kb = round(total_kb / count, 2) if count else 0.0
            rows.append([
                folder,
                ct,
                str(count),
                f"{total_dur}",
                f"{total_kb}",
                f"{avg_dur}",
                f"{avg_kb}",
            ])

    if not rows:
        print("No rows to render for table image.")
        return

    n_rows = len(rows) + 1  
    n_cols = len(headers)
    col_w = 2.2
    row_h = 0.3
    fig_w = max(10, n_cols * col_w)
    fig_h = max(4, n_rows * row_h)

    fig, ax = plt.subplots(figsize=(fig_w, fig_h))
    ax.axis('off')

    the_table = ax.table(
        cellText=rows,
        colLabels=headers,
        loc='center',
        cellLoc='left',
        colLoc='left',
    )
    the_table.auto_set_font_size(False)
    the_table.set_fontsize(8)
    the_table.scale(1, 1.2)

    for (row, col), cell in the_table.get_celld().items():
        if row == 0:
            cell.set_text_props(weight='bold')
            cell.set_facecolor('#f0f0f0')

    fig.tight_layout()
    fig.savefig(output_path, dpi=150)
    plt.close(fig)
    print(f"Saved aggregates table image -> {output_path}")


def main():
    wv = read_webvitals_metrics(directories)
    print_webvitals_summary(wv)
    generate_webvitals_chart(wv)

    agg = read_network_aggregates(directories)
    write_aggregates_table(agg, output_path="network-aggregates-all.txt")
    plot_aggregates_heatmap(agg, output_path="network-aggregates-all.png")
    generate_table_image(agg, output_path="network-aggregates-all-table.png")


if __name__ == "__main__":
    main()