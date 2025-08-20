import os
import json
import math

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


def main():
    wv = read_webvitals_metrics(directories)
    print_webvitals_summary(wv)
    generate_webvitals_chart(wv)


if __name__ == "__main__":
    main()