import re
import matplotlib.pyplot as plt

def parse_summary(filepath):
    with open(filepath, 'r') as f:
        content = f.read()
    
    match = re.search(r'Total nloc\s+Avg\.NLOC\s+AvgCCN\s+Avg\.token\s+Fun Cnt\s+Warning cnt\s+Fun Rt\s+nloc Rt\n[-]+\n([^\n]+)', content, re.MULTILINE)
    if not match:
        raise ValueError(f"Summary table not found in {filepath}")
    row = match.group(1)
    
    numbers = re.findall(r'[\d.]+', row)
    
    keys = ['Total nloc', 'Avg.NLOC', 'AvgCCN', 'Avg.token', 'Fun Cnt', 'Warning cnt', 'Fun Rt', 'nloc Rt']
    return dict(zip(keys, map(float, numbers)))


blazor_metrics = parse_summary('blazor.txt')
react_metrics = parse_summary('react.txt')


metrics_to_plot = ['Total nloc', 'Avg.NLOC', 'AvgCCN', 'Avg.token', 'Fun Cnt']

blazor_values = [blazor_metrics[m] for m in metrics_to_plot]
react_values = [react_metrics[m] for m in metrics_to_plot]

x = range(len(metrics_to_plot))
width = 0.35

fig, ax = plt.subplots(figsize=(10, 6))
bars1 = ax.bar([i - width/2 for i in x], blazor_values, width, label='Blazor', color='#0078D7')
bars2 = ax.bar([i + width/2 for i in x], react_values, width, label='React', color='#00C853')


for bars in [bars1, bars2]:
    for bar in bars:
        height = bar.get_height()
        ax.annotate(f'{height:.1f}',
                    xy=(bar.get_x() + bar.get_width() / 2, height),
                    xytext=(0, 3),  
                    textcoords="offset points",
                    ha='center', va='bottom', fontsize=9)

ax.set_ylabel('Metric Value')
ax.set_title('Code Metrics Comparison: Blazor vs React')
ax.set_xticks(x)
ax.set_xticklabels(metrics_to_plot)


description = (
    "Total nloc - Total lines of code without comments\n"
    "Avg.NLOC - Average lines of code per function\n"
    "AvgCCN - Average code complexity number\n"
    "Avg.token - Average number of tokens per function\n"
    "Fun Cnt - Total number of functions"
)

legend = ax.legend(loc='upper right')

ax.text(0.98, 0.82, description, transform=ax.transAxes, fontsize=9, va='top', ha='right',
    bbox=dict(facecolor='white', edgecolor='none', alpha=0.8))

plt.tight_layout(rect=[0, 0.05, 1, 1])
plt.savefig('metrics.png')