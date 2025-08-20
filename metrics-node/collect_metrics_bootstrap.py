from collections import defaultdict
import json
import os
import matplotlib

matplotlib.use("Agg")

import matplotlib.pyplot as plt
import numpy as np

directories = [
    "react-desktop-bootstrap",
    "react-mobile-bootstrap",
    "blazor-desktop-bootstrap",
    "blazor-mobile-bootstrap"
]

audit_metrics = [
    "first-contentful-paint",      # ms
    "largest-contentful-paint",    # ms
    "total-blocking-time",         # ms
    "cumulative-layout-shift",
    "speed-index",                 # ms
    "interactive"                  # ms
]

audit_metrics_ms = [
    "first-contentful-paint",
    "largest-contentful-paint",
    "total-blocking-time",
    "speed-index",
    "interactive"
]


category_scores = [
    "performance_score",
    "accessibility_score",
    "best_practices_score",
    "seo_score"
]

def extract_metrics(report_path):
    with open(report_path, "r", encoding="utf-8") as f:
        data = json.load(f)
    results = {}
    audits = data.get("audits", {})

    for metric in audit_metrics:
        v = audits.get(metric, {}).get("numericValue")
        if metric in audit_metrics_ms:
            v = v / 1000.0 if v is not None else None  # ms -> s
        results[metric] = v

    categories = data.get("categories", {})
    def pct(cat_id):
        raw = categories.get(cat_id, {}).get("score")
        return raw * 100 if raw is not None else None

    results["performance_score"]    = pct("performance")
    results["accessibility_score"]  = pct("accessibility")
    results["best_practices_score"] = pct("best-practices")
    results["seo_score"]            = pct("seo")

    net_items = (
        audits.get("network-requests", {})
              .get("details", {})
              .get("items", [])
    )
    results["network_requests"] = aggregate_requests(net_items)
    return results

def aggregate_requests(items):
    agg = {}
    for it in items:
        mime = it.get("mimeType") or "unknown"
        transfer = it.get("transferSize") or 0
        start = it.get("networkRequestTime")
        end = it.get("networkEndTime")
        if isinstance(start, (int, float)) and isinstance(end, (int, float)) and end >= start:
            duration = end - start
        else:
            duration = 0.0
        if mime not in agg:
            agg[mime] = {
                "count": 0,
                "total_transfer_size": 0,
                "total_duration": 0.0
            }
        a = agg[mime]
        a["count"] += 1
        a["total_transfer_size"] += transfer
        a["total_duration"] += duration
    return agg

def generate_grouped_bar_chart(all_metrics_by_dir, output_path="metrics_chart.png"):
    metric_order = audit_metrics + category_scores

    data_matrix = []
    for metric in metric_order:
        row = []
        for d in directories:
            val = all_metrics_by_dir.get(d, {}).get(metric)
            row.append(val if isinstance(val, (int, float)) else np.nan)
        data_matrix.append(row)

    num_metrics = len(metric_order)
    num_dirs = len(directories)
    x = np.arange(num_metrics)
    total_group_width = 0.8
    bar_width = total_group_width / num_dirs

    fig, ax_time = plt.subplots(figsize=(max(16, num_metrics * 0.95), 8))
    ax_score = ax_time.twinx()

    legend_handles = {}
    timing_metric_names = {
        "first-contentful-paint",
        "largest-contentful-paint",
        "total-blocking-time",
        "speed-index",
        "interactive",
        "cumulative-layout-shift"
    }

    label_entries = []  # (axis, bar, metric_name, value, is_score)

    for dir_index, d in enumerate(directories):
        offsets = x - (total_group_width / 2) + (dir_index + 0.5) * bar_width
        heights_time = []
        heights_score = []
        for m_index, metric in enumerate(metric_order):
            v = data_matrix[m_index][dir_index]
            if metric in category_scores:
                heights_time.append(np.nan)
                heights_score.append(v)
            else:
                heights_time.append(v)
                heights_score.append(np.nan)

        timing_indices = [i for i, h in enumerate(heights_time) if not np.isnan(h)]
        timing_offsets = [offsets[i] for i in timing_indices]
        timing_values = [heights_time[i] for i in timing_indices]
        bars_time = ax_time.bar(
            timing_offsets,
            timing_values,
            width=bar_width,
            label=d,
            color=plt.get_cmap('tab10')(dir_index)
        )

        score_indices = [i for i, h in enumerate(heights_score) if not np.isnan(h)]
        score_offsets = [offsets[i] for i in score_indices]
        score_values = [heights_score[i] for i in score_indices]
        bars_score = ax_score.bar(
            score_offsets,
            score_values,
            width=bar_width,
            label=d,
            color=plt.get_cmap('tab10')(dir_index)
        )

        handle = bars_time.patches[0] if bars_time.patches else bars_score.patches[0]
        legend_handles[d] = handle

        for bar, idx_metric in zip(bars_time, timing_indices):
            metric_name = metric_order[idx_metric]
            val = timing_values[timing_indices.index(idx_metric)]
            label_entries.append((ax_time, bar, metric_name, val, False))

        for bar, idx_metric in zip(bars_score, score_indices):
            metric_name = metric_order[idx_metric]
            val = score_values[score_indices.index(idx_metric)]
            label_entries.append((ax_score, bar, metric_name, val, True))

    pretty_labels = [
        m.replace("-", " ").replace("_score", " score").title()
        for m in metric_order
    ]
    ax_time.set_xticks(x)
    ax_time.set_xticklabels(pretty_labels, rotation=35, ha="right")

    ax_time.set_ylabel("Timings (seconds) & CLS (unitless)")
    ax_score.set_ylabel("Category Scores (%)")
    ax_time.set_title("Bootstrap Lighthouse Metrics (Timings / CLS and Category Scores)")


    max_time_val = 0.0
    for metric in timing_metric_names:
        for d in directories:
            v = all_metrics_by_dir.get(d, {}).get(metric)
            if isinstance(v, (int, float)):
                max_time_val = max(max_time_val, v)
    if max_time_val <= 0:
        max_time_val = 1.0
    ax_time.set_ylim(0, max_time_val * 1.15)  
    ax_score.set_ylim(0, 100 * 1.08)          


    for axis, bar, metric_name, value, is_score in label_entries:
        if value is None or np.isnan(value):
            continue
        if is_score:
            text = f"{value:.0f}%"
            voffset = max_time_val * 0.01  
        else:
            if metric_name == "cumulative-layout-shift":
                text = f"{value:.4f}"
            else:
                text = f"{value:.2f}s"
            voffset = max_time_val * 0.01
        axis.text(
            bar.get_x() + bar.get_width() / 2,
            bar.get_height() + voffset,
            text,
            ha="center",
            va="bottom",
            rotation=45,
            fontsize=9,
            clip_on=False
        )

    ax_time.legend(legend_handles.values(), legend_handles.keys(), loc="upper left")
    fig.subplots_adjust(top=0.90)  
    fig.tight_layout()
    fig.savefig(output_path, dpi=150)
    plt.close(fig)

def generate_network_requests_table(all_metrics_by_dir, output_path="network_requests.png"):
    rows = []
    for directory in sorted(all_metrics_by_dir.keys()):
        net = all_metrics_by_dir[directory].get("network_requests", {}) or {}
        for mime in sorted(net.keys()):
            stats = net[mime]
            rows.append([
                directory,
                mime,
                stats["count"],
                stats["total_transfer_size"],
                f"{stats['total_duration']:.3f}"
            ])

    if not rows:
        print("No network request data to tabulate.")
        return

    col_labels = ["Directory", "Content Type", "Count", "Transfer (bytes)", "Duration (s)"]


    n_rows = len(rows)
    fig_height = max(3, min(0.2 * n_rows + 1, 40))  
    fig, ax = plt.subplots(figsize=(11, fig_height))
    ax.axis('off')

    table = ax.table(
        cellText=rows,
        colLabels=col_labels,
        loc='upper left',
        cellLoc='left'
    )

    table.auto_set_font_size(False)
    table.set_fontsize(9)


    col_widths = {
        0: 0.18,
        1: 0.32,
        2: 0.08,
        3: 0.18,
        4: 0.14
    }
    for (row, col), cell in table.get_celld().items():
        if row == 0:
            cell.set_text_props(weight='bold')
            cell.set_facecolor("#f0f0f0")
        if col in col_widths:
            cell.set_width(col_widths[col])

    plt.title("Aggregated Network Requests by Directory and Content Type", pad=12)
    plt.tight_layout()
    plt.savefig(output_path, dpi=150)
    plt.close(fig)

def main():
    all_metrics = {}
    for directory in directories:
        report_path = os.path.join("reports", directory, "report.json")
        if not os.path.isfile(report_path):
            print(f"[{directory}] report.json not found.")
            continue
        metrics = extract_metrics(report_path)
        all_metrics[directory] = metrics

        print(f"\nResults for {directory}:")
        for k, v in metrics.items():
            if k == "network_requests":
                continue
            print(f"  {k}: {v}")

    if all_metrics:
        generate_grouped_bar_chart(all_metrics)
        generate_network_requests_table(all_metrics)  

if __name__ == "__main__":
    main()